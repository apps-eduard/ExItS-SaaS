using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

public sealed class DirectPurchaseReceiptRepository : IDirectPurchaseReceiptRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public DirectPurchaseReceiptRepository(PosDbContext db) => _db = db;

    public async Task<DirectPurchaseReceipt?> GetByIdAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptId receiptId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.DirectPurchaseReceipts.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == receiptId.Value && r.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await _db.DirectPurchaseReceiptLines.AsNoTracking()
            .Where(l => l.ReceiptId == record.Id && l.OrganizationId == organizationId.Value)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return DirectPurchaseReceiptEntityMapper.ToDomain(record, lines);
    }

    public async Task<DirectPurchaseReceipt?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey.Trim();
        var record = await _db.DirectPurchaseReceipts.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.IdempotencyKey == key,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await _db.DirectPurchaseReceiptLines.AsNoTracking()
            .Where(l => l.ReceiptId == record.Id && l.OrganizationId == organizationId.Value)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return DirectPurchaseReceiptEntityMapper.ToDomain(record, lines);
    }

    public async Task<(IReadOnlyList<DirectPurchaseReceipt> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.DirectPurchaseReceipts.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value);

        if (filter.FromPurchaseDate is DateOnly from)
        {
            query = query.Where(r => r.PurchaseDate >= from);
        }

        if (filter.ToPurchaseDate is DateOnly to)
        {
            query = query.Where(r => r.PurchaseDate <= to);
        }

        if (filter.SupplierId is Guid supplierId)
        {
            query = query.Where(r => r.SupplierId == supplierId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SourceSearch))
        {
            var term = filter.SourceSearch.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.SourceNameSnapshot != null && r.SourceNameSnapshot.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.ReferenceNumber))
        {
            var reference = filter.ReferenceNumber.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.ReferenceNumber != null && r.ReferenceNumber.ToLower().Contains(reference));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.ReceiptNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var receiptIds = records.Select(r => r.Id).ToList();
        var linesByReceipt = await LoadLinesAsync(receiptIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var items = records
            .Select(r => DirectPurchaseReceiptEntityMapper.ToDomain(
                r,
                linesByReceipt.TryGetValue(r.Id, out var lines) ? lines : []))
            .ToList();
        return (items, total);
    }

    public async Task AddAsync(DirectPurchaseReceipt receipt, CancellationToken cancellationToken = default)
    {
        _db.DirectPurchaseReceipts.Add(DirectPurchaseReceiptEntityMapper.ToRecord(receipt));
        foreach (var line in receipt.Lines)
        {
            _db.DirectPurchaseReceiptLines.Add(DirectPurchaseReceiptEntityMapper.ToRecord(line));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task UpdateAsync(DirectPurchaseReceipt receipt, CancellationToken cancellationToken = default)
    {
        var record = await _db.DirectPurchaseReceipts
            .FirstOrDefaultAsync(
                r => r.Id == receipt.Id.Value && r.OrganizationId == receipt.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Direct purchase receipt was not found for update.");

        DirectPurchaseReceiptEntityMapper.Apply(receipt, record);

        var existingLines = await _db.DirectPurchaseReceiptLines
            .Where(l => l.ReceiptId == receipt.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.DirectPurchaseReceiptLines.RemoveRange(existingLines);
        foreach (var line in receipt.Lines)
        {
            _db.DirectPurchaseReceiptLines.Add(DirectPurchaseReceiptEntityMapper.ToRecord(line));
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

        var sequence = await _db.DirectPurchaseReceiptNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);
        long value;
        if (sequence is null)
        {
            _db.DirectPurchaseReceiptNumberSequences.Add(new DirectPurchaseReceiptNumberSequenceRecord
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

        return DirectPurchaseReceiptNumbers.Format(businessDateUtc, value);
    }

    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc)
    {
        Span<byte> bytes = stackalloc byte[21];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..20], businessDateUtc.DayNumber);
        bytes[20] = 21;

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

    private async Task<Dictionary<Guid, List<DirectPurchaseReceiptLineRecord>>> LoadLinesAsync(
        IReadOnlyCollection<Guid> receiptIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.DirectPurchaseReceiptLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && receiptIds.Contains(l.ReceiptId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.ReceiptId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
