using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CreditEntryRepository : ICreditEntryRepository
{
    private readonly PosDbContext _db;

    public CreditEntryRepository(PosDbContext db) => _db = db;

    public async Task<CreditEntry?> GetByIdAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CreditEntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CreditEntries.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == entryId.Value
                     && e.CustomerId == customerId.Value
                     && e.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CreditEntryEntityMapper.ToDomain(record);
    }

    public async Task<CreditEntry?> GetByIdForOrganizationAsync(
        PosOrganizationId organizationId,
        CreditEntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CreditEntries.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == entryId.Value && e.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CreditEntryEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        IReadOnlySet<Guid>? historyBranchIds = null)
    {
        var query = ApplyHistoryBranchFilter(
            _db.CreditEntries.AsNoTracking()
                .Where(e => e.OrganizationId == organizationId.Value && e.CustomerId == customerId.Value),
            organizationId,
            historyBranchIds);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .ThenByDescending(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CreditEntryEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
        PosOrganizationId organizationId,
        DateTimeOffset? sinceUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CreditEntries.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value);

        if (sinceUtc is not null)
        {
            var since = sinceUtc.Value.ToUniversalTime();
            query = query.Where(e =>
                e.CreatedAtUtc > since
                || (e.ReversedAtUtc != null && e.ReversedAtUtc > since));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(e => e.CreatedAtUtc)
            .ThenBy(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CreditEntryEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var active = CreditEntryStatus.Active.ToString();
        var records = await _db.CreditEntries.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value && e.Status == active)
            .OrderBy(e => e.CreatedAtUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(CreditEntryEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        CancellationToken cancellationToken = default)
    {
        var from = new DateTimeOffset(fromDateUtc.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var exclusiveTo = new DateTimeOffset(
            toDateUtc.AddDays(1).ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var records = await _db.CreditEntries.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value
                        && e.CreatedAtUtc >= from
                        && e.CreatedAtUtc < exclusiveTo)
            .OrderBy(e => e.CreatedAtUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(CreditEntryEntityMapper.ToDomain).ToList();
    }

    public async Task<decimal> SumActiveAmountAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default,
        IReadOnlySet<Guid>? historyBranchIds = null)
    {
        var active = CreditEntryStatus.Active.ToString();
        var query = ApplyHistoryBranchFilter(
            _db.CreditEntries.AsNoTracking()
                .Where(e => e.OrganizationId == organizationId.Value
                            && e.CustomerId == customerId.Value
                            && e.Status == active),
            organizationId,
            historyBranchIds);
        return await query.SumAsync(e => e.Amount, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountActiveAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default,
        IReadOnlySet<Guid>? historyBranchIds = null)
    {
        var active = CreditEntryStatus.Active.ToString();
        var query = ApplyHistoryBranchFilter(
            _db.CreditEntries.AsNoTracking()
                .Where(e => e.OrganizationId == organizationId.Value
                            && e.CustomerId == customerId.Value
                            && e.Status == active),
            organizationId,
            historyBranchIds);
        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<CreditEntryRecord> ApplyHistoryBranchFilter(
        IQueryable<CreditEntryRecord> query,
        PosOrganizationId organizationId,
        IReadOnlySet<Guid>? historyBranchIds)
    {
        if (historyBranchIds is null)
        {
            return query;
        }

        if (historyBranchIds.Count == 0)
        {
            return query.Where(_ => false);
        }

        var branchIds = historyBranchIds.ToList();
        return query.Where(e =>
            e.SourceSaleId != null
            && _db.Sales.Any(s =>
                s.Id == e.SourceSaleId
                && s.OrganizationId == organizationId.Value
                && s.BranchId != null
                && branchIds.Contains(s.BranchId.Value)));
    }

    public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default)
    {
        _db.CreditEntries.Add(CreditEntryEntityMapper.ToRecord(entry));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default)
    {
        var record = await _db.CreditEntries
            .FirstOrDefaultAsync(
                e => e.Id == entry.Id.Value
                     && e.CustomerId == entry.CustomerId.Value
                     && e.OrganizationId == entry.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CreditEntryNotFound,
                "Credit entry was not found.");
        }

        CreditEntryEntityMapper.ApplyToRecord(entry, record);
    }
}
