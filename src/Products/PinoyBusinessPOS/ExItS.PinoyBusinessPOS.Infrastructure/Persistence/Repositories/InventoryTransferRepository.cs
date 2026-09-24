using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
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
        var (receipts, receiptLines) = await LoadReceiptsAsync([record.Id], organizationId, cancellationToken)
            .ConfigureAwait(false);
        return InventoryTransferEntityMapper.ToDomain(
            record,
            lines.TryGetValue(record.Id, out var found) ? found : [],
            receipts.TryGetValue(record.Id, out var receiptRecords) ? receiptRecords : [],
            receiptLines);
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
                    && (t.Status == nameof(InventoryTransferStatus.InTransit)
                        || t.Status == nameof(InventoryTransferStatus.PartiallyReceived)));
            }
        }
        else if (string.Equals(direction, "history", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t =>
                t.Status == nameof(InventoryTransferStatus.Received)
                || t.Status == nameof(InventoryTransferStatus.PartiallyReceived)
                || t.Status == nameof(InventoryTransferStatus.ClosedWithDiscrepancy)
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

        var transferIds = records.Select(r => r.Id).ToList();
        var lines = await LoadLinesAsync(transferIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var (receipts, receiptLines) = await LoadReceiptsAsync(transferIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var items = records
            .Select(r => InventoryTransferEntityMapper.ToDomain(
                r,
                lines.TryGetValue(r.Id, out var found) ? found : [],
                receipts.TryGetValue(r.Id, out var receiptRecords) ? receiptRecords : [],
                receiptLines))
            .ToList();
        return (items, total);
    }

    public async Task<IReadOnlyList<InventoryTransfer>> ListByStockRequestIdAsync(
        PosOrganizationId organizationId,
        StockRequestId stockRequestId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.InventoryTransfers.AsNoTracking()
            .Where(t => t.OrganizationId == organizationId.Value && t.StockRequestId == stockRequestId.Value)
            .OrderBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (records.Count == 0)
        {
            return [];
        }

        var transferIds = records.Select(r => r.Id).ToList();
        var lines = await LoadLinesAsync(transferIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var (receipts, receiptLines) = await LoadReceiptsAsync(transferIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        return records
            .Select(r => InventoryTransferEntityMapper.ToDomain(
                r,
                lines.TryGetValue(r.Id, out var found) ? found : [],
                receipts.TryGetValue(r.Id, out var receiptRecords) ? receiptRecords : [],
                receiptLines))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryTransfer>> ListByRootTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId rootTransferId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.InventoryTransfers.AsNoTracking()
            .Where(t =>
                t.OrganizationId == organizationId.Value
                && (t.Id == rootTransferId.Value || t.RootTransferId == rootTransferId.Value))
            .OrderBy(t => t.ReplacementSequence ?? 0)
            .ThenBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (records.Count == 0)
        {
            return [];
        }

        var transferIds = records.Select(r => r.Id).ToList();
        var lines = await LoadLinesAsync(transferIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var (receipts, receiptLines) = await LoadReceiptsAsync(transferIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        return records
            .Select(r => InventoryTransferEntityMapper.ToDomain(
                r,
                lines.TryGetValue(r.Id, out var found) ? found : [],
                receipts.TryGetValue(r.Id, out var receiptRecords) ? receiptRecords : [],
                receiptLines))
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryTransferOpenCommitment>> ListOpenCommitmentsForBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId>? productIds = null,
        CancellationToken cancellationToken = default)
    {
        var openStatuses = new[]
        {
            InventoryTransferStatuses.ToCode(InventoryTransferStatus.InTransit),
            InventoryTransferStatuses.ToCode(InventoryTransferStatus.PartiallyReceived),
        };

        var productFilter = productIds is { Count: > 0 }
            ? productIds.Select(p => p.Value).ToHashSet()
            : null;

        var query =
            from transfer in _db.InventoryTransfers.AsNoTracking()
            join line in _db.InventoryTransferLines.AsNoTracking() on transfer.Id equals line.TransferId
            where transfer.OrganizationId == organizationId.Value
                && openStatuses.Contains(transfer.Status)
                && (transfer.SourceBranchId == branchId.Value || transfer.DestinationBranchId == branchId.Value)
            select new { transfer, line };

        if (productFilter is not null)
        {
            query = query.Where(x => productFilter.Contains(x.line.ProductId));
        }

        var rows = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<InventoryTransferOpenCommitment>(rows.Count);
        foreach (var row in rows)
        {
            var outstanding = row.line.SentQty - row.line.ReceivedQty - row.line.ClosedQty;
            if (outstanding <= 0m)
            {
                continue;
            }

            var outbound = row.transfer.SourceBranchId == branchId.Value;
            result.Add(new InventoryTransferOpenCommitment(
                row.transfer.Id,
                row.transfer.TransferNumber,
                row.line.ProductId,
                outstanding,
                outbound ? "Outbound" : "Inbound",
                outbound ? row.transfer.DestinationBranchId : row.transfer.SourceBranchId,
                row.transfer.CreatedAtUtc));
        }

        return result;
    }

    public Task AddAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default)
    {
        _db.InventoryTransfers.Add(InventoryTransferEntityMapper.ToRecord(transfer));
        foreach (var line in transfer.Lines)
        {
            _db.InventoryTransferLines.Add(InventoryTransferEntityMapper.ToRecord(line));
        }

        foreach (var receipt in transfer.Receipts)
        {
            _db.InventoryTransferReceipts.Add(InventoryTransferEntityMapper.ToRecord(receipt));
            foreach (var receiptLine in receipt.Lines)
            {
                _db.InventoryTransferReceiptLines.Add(InventoryTransferEntityMapper.ToRecord(receiptLine));
            }
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
        var lineById = existingLines.ToDictionary(l => l.Id);
        foreach (var line in transfer.Lines)
        {
            if (lineById.TryGetValue(line.Id.Value, out var existingLine))
            {
                var updated = InventoryTransferEntityMapper.ToRecord(line);
                existingLine.SentQty = updated.SentQty;
                existingLine.ReceivedQty = updated.ReceivedQty;
                existingLine.ClosedQty = updated.ClosedQty;
                existingLine.WaivedQty = updated.WaivedQty;
                existingLine.DiscrepancyReason = updated.DiscrepancyReason;
                existingLine.DiscrepancyNote = updated.DiscrepancyNote;
                existingLine.UnitCostSnapshot = updated.UnitCostSnapshot;
            }
            else
            {
                _db.InventoryTransferLines.Add(InventoryTransferEntityMapper.ToRecord(line));
            }
        }

        var existingReceiptIds = await _db.InventoryTransferReceipts
            .Where(r => r.TransferId == transfer.Id.Value)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var knownReceiptIds = existingReceiptIds.ToHashSet();
        foreach (var receipt in transfer.Receipts)
        {
            if (knownReceiptIds.Contains(receipt.Id.Value))
            {
                continue;
            }

            _db.InventoryTransferReceipts.Add(InventoryTransferEntityMapper.ToRecord(receipt));
            foreach (var receiptLine in receipt.Lines)
            {
                _db.InventoryTransferReceiptLines.Add(InventoryTransferEntityMapper.ToRecord(receiptLine));
            }
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

    public async Task<IReadOnlyDictionary<Guid, InventoryTransferTransactionRef>> ResolveStockMovementTransactionRefsAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<StockMovement> movements,
        CancellationToken cancellationToken = default)
    {
        if (movements.Count == 0)
        {
            return new Dictionary<Guid, InventoryTransferTransactionRef>();
        }

        var transferSourceIds = new HashSet<Guid>();
        var receiptSourceIds = new HashSet<Guid>();
        var receiptLineSourceIds = new HashSet<Guid>();
        var custodySourceIds = new HashSet<Guid>();

        foreach (var movement in movements)
        {
            if (movement.SourceId is not Guid sourceId
                || sourceId == Guid.Empty
                || movement.SourceType != StockMovementSourceType.InventoryTransfer)
            {
                continue;
            }

            switch (movement.MovementType)
            {
                case StockMovementType.TransferOut:
                case StockMovementType.TransferCancelRestore:
                    transferSourceIds.Add(sourceId);
                    break;
                case StockMovementType.TransferIn:
                    receiptSourceIds.Add(sourceId);
                    break;
                case StockMovementType.TransferDamageHold:
                    receiptLineSourceIds.Add(sourceId);
                    break;
                case StockMovementType.TransferDamageRecovery:
                case StockMovementType.TransferDamageReturnOut:
                case StockMovementType.TransferDamageReturnIn:
                case StockMovementType.TransferDamageWriteOff:
                    custodySourceIds.Add(sourceId);
                    break;
            }
        }

        var transferIdByReceiptId = new Dictionary<Guid, Guid>();
        if (receiptSourceIds.Count > 0)
        {
            var receipts = await _db.InventoryTransferReceipts.AsNoTracking()
                .Where(r =>
                    r.OrganizationId == organizationId.Value
                    && receiptSourceIds.Contains(r.Id))
                .Select(r => new { r.Id, r.TransferId })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var receipt in receipts)
            {
                transferIdByReceiptId[receipt.Id] = receipt.TransferId;
                transferSourceIds.Add(receipt.TransferId);
            }
        }

        var transferIdByReceiptLineId = new Dictionary<Guid, Guid>();
        if (receiptLineSourceIds.Count > 0)
        {
            var lineRows = await (
                    from line in _db.InventoryTransferReceiptLines.AsNoTracking()
                    join receipt in _db.InventoryTransferReceipts.AsNoTracking()
                        on line.ReceiptId equals receipt.Id
                    where receipt.OrganizationId == organizationId.Value
                        && receiptLineSourceIds.Contains(line.Id)
                    select new { LineId = line.Id, receipt.TransferId })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var row in lineRows)
            {
                transferIdByReceiptLineId[row.LineId] = row.TransferId;
                transferSourceIds.Add(row.TransferId);
            }
        }

        var transferIdByCustodyId = new Dictionary<Guid, Guid>();
        if (custodySourceIds.Count > 0)
        {
            var custodies = await _db.InventoryTransferDamageCustodies.AsNoTracking()
                .Where(c =>
                    c.OrganizationId == organizationId.Value
                    && custodySourceIds.Contains(c.Id))
                .Select(c => new { c.Id, c.TransferId })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var custody in custodies)
            {
                transferIdByCustodyId[custody.Id] = custody.TransferId;
                transferSourceIds.Add(custody.TransferId);
            }
        }

        var transferNumberById = new Dictionary<Guid, string?>();
        if (transferSourceIds.Count > 0)
        {
            var transfers = await _db.InventoryTransfers.AsNoTracking()
                .Where(t =>
                    t.OrganizationId == organizationId.Value
                    && transferSourceIds.Contains(t.Id))
                .Select(t => new { t.Id, t.TransferNumber })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var transfer in transfers)
            {
                transferNumberById[transfer.Id] = transfer.TransferNumber;
            }
        }

        var result = new Dictionary<Guid, InventoryTransferTransactionRef>();
        foreach (var movement in movements)
        {
            if (movement.SourceId is not Guid sourceId || sourceId == Guid.Empty)
            {
                continue;
            }

            Guid? transferId = movement.MovementType switch
            {
                StockMovementType.TransferOut or StockMovementType.TransferCancelRestore
                    when transferNumberById.ContainsKey(sourceId) => sourceId,
                StockMovementType.TransferIn
                    when transferIdByReceiptId.TryGetValue(sourceId, out var fromReceipt) => fromReceipt,
                StockMovementType.TransferDamageHold
                    when transferIdByReceiptLineId.TryGetValue(sourceId, out var fromLine) => fromLine,
                StockMovementType.TransferDamageRecovery
                    or StockMovementType.TransferDamageReturnOut
                    or StockMovementType.TransferDamageReturnIn
                    or StockMovementType.TransferDamageWriteOff
                    when transferIdByCustodyId.TryGetValue(sourceId, out var fromCustody) => fromCustody,
                _ => null
            };

            if (transferId is not Guid resolvedId
                || !transferNumberById.TryGetValue(resolvedId, out var number))
            {
                continue;
            }

            result[movement.Id.Value] = new InventoryTransferTransactionRef(resolvedId, number);
        }

        return result;
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

    private async Task<(
        Dictionary<Guid, List<InventoryTransferReceiptRecord>> Receipts,
        IReadOnlyList<InventoryTransferReceiptLineRecord> ReceiptLines)> LoadReceiptsAsync(
        IReadOnlyCollection<Guid> transferIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.InventoryTransferReceipts.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value && transferIds.Contains(r.TransferId))
            .OrderBy(r => r.Sequence)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (records.Count == 0)
        {
            return ([], []);
        }

        var receiptIds = records.Select(r => r.Id).ToList();
        var lineRecords = await _db.InventoryTransferReceiptLines.AsNoTracking()
            .Where(l => receiptIds.Contains(l.ReceiptId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byTransfer = records
            .GroupBy(r => r.TransferId)
            .ToDictionary(g => g.Key, g => g.ToList());
        return (byTransfer, lineRecords);
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

internal sealed class InventoryTransferDamageCustodyRepository : IInventoryTransferDamageCustodyRepository
{
    private readonly PosDbContext _db;

    public InventoryTransferDamageCustodyRepository(PosDbContext db) => _db = db;

    public async Task<InventoryTransferDamageCustody?> GetByIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferDamageCustodyId custodyId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryTransferDamageCustodies
            .FirstOrDefaultAsync(
                c => c.Id == custodyId.Value && c.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : InventoryTransferEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<InventoryTransferDamageCustody>> ListByTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.InventoryTransferDamageCustodies.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && c.TransferId == transferId.Value)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<InventoryTransferDamageCustody>> ListByRootTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId rootTransferId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.InventoryTransferDamageCustodies.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && c.RootTransferId == rootTransferId.Value)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<InventoryTransferDamageCustody>> ListByStockRequestIdAsync(
        PosOrganizationId organizationId,
        StockRequestId stockRequestId,
        CancellationToken cancellationToken = default)
    {
        var transferIds = await _db.InventoryTransfers.AsNoTracking()
            .Where(t => t.OrganizationId == organizationId.Value && t.StockRequestId == stockRequestId.Value)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (transferIds.Count == 0)
        {
            return [];
        }

        var records = await _db.InventoryTransferDamageCustodies.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && transferIds.Contains(c.TransferId))
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(InventoryTransferDamageCustody custody, CancellationToken cancellationToken = default)
    {
        _db.InventoryTransferDamageCustodies.Add(InventoryTransferEntityMapper.ToRecord(custody));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(InventoryTransferDamageCustody custody, CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryTransferDamageCustodies
            .FirstOrDefaultAsync(
                c => c.Id == custody.Id.Value && c.OrganizationId == custody.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyId,
                "Damage custody was not found.");
        }

        InventoryTransferEntityMapper.ApplyToRecord(custody, record);
    }
}

internal sealed class InventoryTransferExceptionCustodyRepository : IInventoryTransferExceptionCustodyRepository
{
    private readonly PosDbContext _db;

    public InventoryTransferExceptionCustodyRepository(PosDbContext db) => _db = db;

    public async Task<InventoryTransferExceptionCustody?> GetByIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferExceptionCustodyId custodyId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryTransferExceptionCustodies
            .FirstOrDefaultAsync(
                c => c.Id == custodyId.Value && c.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : InventoryTransferEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<InventoryTransferExceptionCustody>> ListByTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.InventoryTransferExceptionCustodies.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && c.TransferId == transferId.Value)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<InventoryTransferExceptionCustody>> ListByRootTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId rootTransferId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.InventoryTransferExceptionCustodies.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && c.RootTransferId == rootTransferId.Value)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<InventoryTransferExceptionCustody>> ListByStockRequestIdAsync(
        PosOrganizationId organizationId,
        StockRequestId stockRequestId,
        CancellationToken cancellationToken = default)
    {
        var transferIds = await _db.InventoryTransfers.AsNoTracking()
            .Where(t => t.OrganizationId == organizationId.Value && t.StockRequestId == stockRequestId.Value)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (transferIds.Count == 0)
        {
            return [];
        }

        var records = await _db.InventoryTransferExceptionCustodies.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && transferIds.Contains(c.TransferId))
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(InventoryTransferExceptionCustody custody, CancellationToken cancellationToken = default)
    {
        _db.InventoryTransferExceptionCustodies.Add(InventoryTransferEntityMapper.ToRecord(custody));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(InventoryTransferExceptionCustody custody, CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryTransferExceptionCustodies
            .FirstOrDefaultAsync(
                c => c.Id == custody.Id.Value && c.OrganizationId == custody.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyId,
                "Exception custody was not found.");
        }

        InventoryTransferEntityMapper.ApplyToRecord(custody, record);
    }
}
