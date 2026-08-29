using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

public sealed class WasteLossRepository : IWasteLossRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public WasteLossRepository(PosDbContext db) => _db = db;

    public async Task<WasteLoss?> GetByIdAsync(
        PosOrganizationId organizationId,
        WasteLossId wasteLossId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.WasteLosses.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == wasteLossId.Value && r.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await _db.WasteLossLines.AsNoTracking()
            .Where(l => l.WasteLossId == record.Id && l.OrganizationId == organizationId.Value)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return WasteLossEntityMapper.ToDomain(record, lines);
    }

    public async Task<WasteLoss?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey.Trim();
        var record = await _db.WasteLosses.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.IdempotencyKey == key,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await _db.WasteLossLines.AsNoTracking()
            .Where(l => l.WasteLossId == record.Id && l.OrganizationId == organizationId.Value)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return WasteLossEntityMapper.ToDomain(record, lines);
    }

    public async Task<(IReadOnlyList<WasteLoss> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        WasteLossFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WasteLosses.AsNoTracking()
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
            && WasteLossReasons.TryParse(filter.Reason, out var reason))
        {
            var code = WasteLossReasons.ToCode(reason);
            query = query.Where(r => r.Reason == code);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && WasteLossStatuses.TryParse(filter.Status, out var status))
        {
            var code = WasteLossStatuses.ToCode(status);
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
            .ThenByDescending(r => r.WasteLossNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var wasteLossIds = records.Select(r => r.Id).ToList();
        var linesByWasteLoss = await LoadLinesAsync(wasteLossIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var items = records
            .Select(r => WasteLossEntityMapper.ToDomain(
                r,
                linesByWasteLoss.TryGetValue(r.Id, out var lines) ? lines : []))
            .ToList();
        return (items, total);
    }

    public async Task AddAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default)
    {
        _db.WasteLosses.Add(WasteLossEntityMapper.ToRecord(wasteLoss));
        foreach (var line in wasteLoss.Lines)
        {
            _db.WasteLossLines.Add(WasteLossEntityMapper.ToRecord(line));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task UpdateAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default)
    {
        var record = await _db.WasteLosses
            .FirstOrDefaultAsync(
                r => r.Id == wasteLoss.Id.Value && r.OrganizationId == wasteLoss.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Waste/loss was not found for update.");

        WasteLossEntityMapper.Apply(wasteLoss, record);

        var existingLines = await _db.WasteLossLines
            .Where(l => l.WasteLossId == wasteLoss.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.WasteLossLines.RemoveRange(existingLines);
        foreach (var line in wasteLoss.Lines)
        {
            _db.WasteLossLines.Add(WasteLossEntityMapper.ToRecord(line));
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

        var sequence = await _db.WasteLossNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);
        long value;
        if (sequence is null)
        {
            _db.WasteLossNumberSequences.Add(new WasteLossNumberSequenceRecord
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

        return WasteLossNumbers.Format(businessDateUtc, value);
    }

    public async Task<InventoryDocumentCostPeriodAggregate> AggregatePostedCostForPeriodAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        const string posted = nameof(WasteLossStatus.Posted);
        const string complete = nameof(ProductionCostStatus.Complete);
        const string partial = nameof(ProductionCostStatus.Partial);
        const string unavailable = nameof(ProductionCostStatus.Unavailable);

        var from = new DateTimeOffset(fromDateUtc.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var exclusiveTo = new DateTimeOffset(
            toDateUtc.AddDays(1).ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var query = _db.WasteLosses.AsNoTracking()
            .Where(w => w.OrganizationId == organizationId.Value
                        && w.Status == posted
                        && w.OccurredAtUtc >= from
                        && w.OccurredAtUtc < exclusiveTo);

        if (branchId is not null)
        {
            query = query.Where(w => w.BranchId == branchId.Value);
        }

        var row = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                PostedCount = g.Count(),
                CompleteCostCount = g.Count(w => w.CostStatus == complete),
                PartialCostCount = g.Count(w => w.CostStatus == partial),
                UnavailableCostCount = g.Count(w => w.CostStatus == unavailable),
                KnownCost = g.Where(w => w.CostStatus != unavailable)
                    .Sum(w => (decimal?)w.TotalCostSnapshot) ?? 0m
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            return new InventoryDocumentCostPeriodAggregate(0m, 0, 0, 0, 0);
        }

        return new InventoryDocumentCostPeriodAggregate(
            SaleMoney.RoundMoney(row.KnownCost),
            row.PostedCount,
            row.CompleteCostCount,
            row.PartialCostCount,
            row.UnavailableCostCount);
    }

    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc)
    {
        Span<byte> bytes = stackalloc byte[21];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..20], businessDateUtc.DayNumber);
        bytes[20] = 24; // distinct from stock_use (22) and production (23)

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

    private async Task<Dictionary<Guid, List<WasteLossLineRecord>>> LoadLinesAsync(
        IReadOnlyCollection<Guid> wasteLossIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.WasteLossLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && wasteLossIds.Contains(l.WasteLossId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.WasteLossId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
