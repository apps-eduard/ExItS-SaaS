using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryTransferRepository : IInventoryTransferRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public InventoryTransferRepository(PosDbContext db) => _db = db;

    public async Task<InventoryTransfer?> GetByIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryTransfers.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == transferId.Value && t.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadLinesAsync([record.Id], organizationId, cancellationToken).ConfigureAwait(false);
        return InventoryTransferEntityMapper.ToDomain(
            record,
            lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<(IReadOnlyList<InventoryTransfer> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        InventoryTransferFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.InventoryTransfers.AsNoTracking()
            .Where(t => t.OrganizationId == organizationId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && InventoryTransferStatuses.TryParse(filter.Status, out var status))
        {
            var code = InventoryTransferStatuses.ToCode(status);
            query = query.Where(t => t.Status == code);
        }

        if (!string.IsNullOrWhiteSpace(filter.TransferNumber))
        {
            var term = filter.TransferNumber.Trim().ToUpperInvariant();
            query = query.Where(t => t.TransferNumber != null && t.TransferNumber.Contains(term));
        }

        if (filter.SourceBranchId is Guid source && source != Guid.Empty)
        {
            query = query.Where(t => t.SourceBranchId == source);
        }

        if (filter.DestinationBranchId is Guid dest && dest != Guid.Empty)
        {
            query = query.Where(t => t.DestinationBranchId == dest);
        }

        var direction = filter.Direction?.Trim();
        var acting = filter.ActingBranchId is Guid actingId && actingId != Guid.Empty
            ? actingId
            : (Guid?)null;
        if (string.Equals(direction, "outgoing", StringComparison.OrdinalIgnoreCase))
        {
            var outgoingSource = filter.SourceBranchId ?? acting;
            if (outgoingSource is Guid outgoingId && outgoingId != Guid.Empty)
            {
                query = query.Where(t =>
                    t.SourceBranchId == outgoingId
                    && (t.Status == nameof(InventoryTransferStatus.Draft)
                        || t.Status == nameof(InventoryTransferStatus.InTransit)));
            }
        }
        else if (string.Equals(direction, "incoming", StringComparison.OrdinalIgnoreCase))
        {
            var incomingDest = filter.DestinationBranchId ?? acting;
            if (incomingDest is Guid incomingId && incomingId != Guid.Empty)
            {
                query = query.Where(t =>
                    t.DestinationBranchId == incomingId
                    && t.Status == nameof(InventoryTransferStatus.InTransit));
            }
        }
        else if (string.Equals(direction, "history", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t =>
                t.Status == nameof(InventoryTransferStatus.Received)
                || t.Status == nameof(InventoryTransferStatus.PartiallyReceived)
                || t.Status == nameof(InventoryTransferStatus.Cancelled));
            if (acting is Guid involved)
            {
                query = query.Where(t => t.SourceBranchId == involved || t.DestinationBranchId == involved);
            }
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(t => t.UpdatedAtUtc)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var lines = await LoadLinesAsync(records.Select(r => r.Id).ToList(), organizationId, cancellationToken)
            .ConfigureAwait(false);
        var items = records
            .Select(r => InventoryTransferEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
        return (items, total);
    }

    public Task AddAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default)
    {
        _db.InventoryTransfers.Add(InventoryTransferEntityMapper.ToRecord(transfer));
        foreach (var line in transfer.Lines)
        {
            _db.InventoryTransferLines.Add(InventoryTransferEntityMapper.ToRecord(line));
        }

        return Task.CompletedTask;
    }

    public async Task UpdateAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryTransfers
            .FirstOrDefaultAsync(
                t => t.Id == transfer.Id.Value && t.OrganizationId == transfer.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Inventory transfer was not found.");
        }

        InventoryTransferEntityMapper.ApplyToRecord(transfer, record);
        var existingLines = await _db.InventoryTransferLines
            .Where(l => l.TransferId == transfer.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.InventoryTransferLines.RemoveRange(existingLines);
        foreach (var line in transfer.Lines)
        {
            _db.InventoryTransferLines.Add(InventoryTransferEntityMapper.ToRecord(line));
        }
    }

    public async Task<string> AllocateNextNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default)
    {
        await _db.Database
            .ExecuteSqlRawAsync(LockSequenceSql, [SequenceLockKey(organizationId, businessDateUtc)], cancellationToken)
            .ConfigureAwait(false);

        var sequence = await _db.InventoryTransferNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);
        long value;
        if (sequence is null)
        {
            _db.InventoryTransferNumberSequences.Add(new InventoryTransferNumberSequenceRecord
            {
                OrganizationId = organizationId.Value,
                BusinessDate = businessDateUtc,
                LastValue = 1
            });
            value = 1;
        }
        else
        {
            sequence.LastValue += 1;
            value = sequence.LastValue;
        }

        return InventoryTransferNumbers.Format(businessDateUtc, value);
    }

    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc)
    {
        Span<byte> bytes = stackalloc byte[21];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..20], businessDateUtc.DayNumber);
        bytes[20] = 11;

        unchecked
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            return (long)hash;
        }
    }

    private async Task<Dictionary<Guid, List<InventoryTransferLineRecord>>> LoadLinesAsync(
        IReadOnlyCollection<Guid> transferIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.InventoryTransferLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && transferIds.Contains(l.TransferId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.TransferId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}

internal sealed class InventoryBranchBalanceRepository : IInventoryBranchBalanceRepository
{
    private readonly PosDbContext _db;

    public InventoryBranchBalanceRepository(PosDbContext db) => _db = db;

    public async Task<InventoryBranchBalance?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryBranchBalances
            .FirstOrDefaultAsync(
                b => b.OrganizationId == organizationId.Value
                    && b.BranchId == branchId.Value
                    && b.ProductId == productId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : InventoryTransferEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var ids = productIds.Select(p => p.Value).ToList();
        var records = await _db.InventoryBranchBalances
            .Where(b => b.OrganizationId == organizationId.Value && ids.Contains(b.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<InventoryBranchBalance>> ListByBranchAndProductIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var ids = productIds.Select(p => p.Value).ToList();
        var records = await _db.InventoryBranchBalances
            .Where(b => b.OrganizationId == organizationId.Value
                && b.BranchId == branchId.Value
                && ids.Contains(b.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public async Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
    {
        var record = _db.InventoryBranchBalances.Local.FirstOrDefault(b =>
                b.OrganizationId == balance.OrganizationId.Value
                && b.BranchId == balance.BranchId.Value
                && b.ProductId == balance.ProductId.Value)
            ?? await _db.InventoryBranchBalances
                .FirstOrDefaultAsync(
                    b => b.OrganizationId == balance.OrganizationId.Value
                        && b.BranchId == balance.BranchId.Value
                        && b.ProductId == balance.ProductId.Value,
                    cancellationToken)
                .ConfigureAwait(false);
        if (record is null)
        {
            _db.InventoryBranchBalances.Add(InventoryTransferEntityMapper.ToRecord(balance));
            return;
        }

        InventoryTransferEntityMapper.ApplyToRecord(balance, record);
    }
}
