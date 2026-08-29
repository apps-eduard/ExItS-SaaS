using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

public sealed class StockUseRepository : IStockUseRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public StockUseRepository(PosDbContext db) => _db = db;

    public async Task<StockUse?> GetByIdAsync(
        PosOrganizationId organizationId,
        StockUseId stockUseId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.StockUses.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == stockUseId.Value && r.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await _db.StockUseLines.AsNoTracking()
            .Where(l => l.StockUseId == record.Id && l.OrganizationId == organizationId.Value)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return StockUseEntityMapper.ToDomain(record, lines);
    }

    public async Task<StockUse?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey.Trim();
        var record = await _db.StockUses.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.IdempotencyKey == key,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await _db.StockUseLines.AsNoTracking()
            .Where(l => l.StockUseId == record.Id && l.OrganizationId == organizationId.Value)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return StockUseEntityMapper.ToDomain(record, lines);
    }

    public async Task<(IReadOnlyList<StockUse> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        StockUseFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.StockUses.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value);

        if (filter.FromOccurredAtUtc is DateTimeOffset from)
        {
            query = query.Where(r => r.OccurredAtUtc >= from);
        }

        if (filter.ToOccurredAtUtc is DateTimeOffset to)
        {
            query = query.Where(r => r.OccurredAtUtc <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.Reason)
            && StockUseReasons.TryParse(filter.Reason, out var reason))
        {
            var code = StockUseReasons.ToCode(reason);
            query = query.Where(r => r.Reason == code);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && StockUseStatuses.TryParse(filter.Status, out var status))
        {
            var code = StockUseStatuses.ToCode(status);
            query = query.Where(r => r.Status == code);
        }

        if (filter.BranchId is Guid branchId)
        {
            query = query.Where(r => r.BranchId == branchId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ReferenceNumber))
        {
            var reference = filter.ReferenceNumber.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.ReferenceNumber != null && r.ReferenceNumber.ToLower().Contains(reference));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(r => r.OccurredAtUtc)
            .ThenByDescending(r => r.StockUseNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var stockUseIds = records.Select(r => r.Id).ToList();
        var linesByStockUse = await LoadLinesAsync(stockUseIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var items = records
            .Select(r => StockUseEntityMapper.ToDomain(
                r,
                linesByStockUse.TryGetValue(r.Id, out var lines) ? lines : []))
            .ToList();
        return (items, total);
    }

    public async Task AddAsync(StockUse stockUse, CancellationToken cancellationToken = default)
    {
        _db.StockUses.Add(StockUseEntityMapper.ToRecord(stockUse));
        foreach (var line in stockUse.Lines)
        {
            _db.StockUseLines.Add(StockUseEntityMapper.ToRecord(line));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task UpdateAsync(StockUse stockUse, CancellationToken cancellationToken = default)
    {
        var record = await _db.StockUses
            .FirstOrDefaultAsync(
                r => r.Id == stockUse.Id.Value && r.OrganizationId == stockUse.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Stock use was not found for update.");

        StockUseEntityMapper.Apply(stockUse, record);

        var existingLines = await _db.StockUseLines
            .Where(l => l.StockUseId == stockUse.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.StockUseLines.RemoveRange(existingLines);
        foreach (var line in stockUse.Lines)
        {
            _db.StockUseLines.Add(StockUseEntityMapper.ToRecord(line));
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

        var sequence = await _db.StockUseNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);
        long value;
        if (sequence is null)
        {
            _db.StockUseNumberSequences.Add(new StockUseNumberSequenceRecord
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

        return StockUseNumbers.Format(businessDateUtc, value);
    }

    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc)
    {
        Span<byte> bytes = stackalloc byte[21];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..20], businessDateUtc.DayNumber);
        bytes[20] = 22;

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

    private async Task<Dictionary<Guid, List<StockUseLineRecord>>> LoadLinesAsync(
        IReadOnlyCollection<Guid> stockUseIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.StockUseLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && stockUseIds.Contains(l.StockUseId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.StockUseId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
