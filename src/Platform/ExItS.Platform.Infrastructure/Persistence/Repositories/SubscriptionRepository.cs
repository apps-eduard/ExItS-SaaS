using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class SubscriptionRepository : ISubscriptionRepository
{
    private static readonly string[] ActiveLikeStatuses = Enum.GetValues<SubscriptionStatus>()
        .Where(Subscription.IsActiveLike)
        .Select(s => s.ToString())
        .ToArray();

    private readonly PlatformDbContext _db;

    public SubscriptionRepository(PlatformDbContext db) => _db = db;

    public async Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : SubscriptionEntityMapper.ToDomain(record);
    }

    public async Task<Subscription?> GetCurrentForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value
                        && s.ProductCode == productCode.Value
                        && ActiveLikeStatuses.Contains(s.Status))
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        record ??= await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value && s.ProductCode == productCode.Value)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : SubscriptionEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        SubscriptionStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking().Where(s => s.OrganizationId == organizationId.Value);
        if (status is not null)
        {
            query = query.Where(s => s.Status == status.Value.ToString());
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SubscriptionEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByProductAsync(
        ProductCode productCode,
        SubscriptionStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking().Where(s => s.ProductCode == productCode.Value);
        if (status is not null)
        {
            query = query.Where(s => s.Status == status.Value.ToString());
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SubscriptionEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListExpiringTrialsAsync(
        DateTimeOffset asOfUtc,
        DateTimeOffset throughUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking().Where(s =>
            s.Status == SubscriptionStatus.Trialing.ToString()
            && s.TrialEndUtc != null
            && s.TrialEndUtc >= asOfUtc
            && s.TrialEndUtc <= throughUtc);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(s => s.TrialEndUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SubscriptionEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByStatusAsync(
        SubscriptionStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking().Where(s => s.Status == status.ToString());

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(s => s.UpdatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SubscriptionEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListAsync(
        PlatformOrganizationId? organizationId,
        ProductCode? productCode,
        SubscriptionStatus? status,
        string? search,
        bool? isTrial,
        Guid? planId,
        SubscriptionListSortBy sortBy,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking();
        if (organizationId is not null)
        {
            query = query.Where(s => s.OrganizationId == organizationId.Value);
        }

        if (productCode is not null)
        {
            query = query.Where(s => s.ProductCode == productCode.Value);
        }

        if (status is not null)
        {
            query = query.Where(s => s.Status == status.Value.ToString());
        }

        if (planId is not null)
        {
            query = query.Where(s => s.PlanId == planId.Value);
        }

        if (isTrial is true)
        {
            query = query.Where(s => s.TrialDefinitionId != null || s.Status == SubscriptionStatus.Trialing.ToString());
        }
        else if (isTrial is false)
        {
            query = query.Where(s => s.TrialDefinitionId == null && s.Status != SubscriptionStatus.Trialing.ToString());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            var matchingOrgIds = _db.Organizations.AsNoTracking()
                .Where(o => o.DisplayName.ToLower().Contains(term))
                .Select(o => o.Id);
            var matchingPlanIds = _db.Plans.AsNoTracking()
                .Where(p => p.DisplayName.ToLower().Contains(term) || p.Code.ToLower().Contains(term))
                .Select(p => p.Id);
            var matchingProductCodes = _db.Products.AsNoTracking()
                .Where(p => p.DisplayName.ToLower().Contains(term))
                .Select(p => p.Code);

            query = query.Where(s =>
                s.ProductCode.ToLower().Contains(term)
                || s.Status.ToLower().Contains(term)
                || matchingOrgIds.Contains(s.OrganizationId)
                || matchingPlanIds.Contains(s.PlanId)
                || matchingProductCodes.Contains(s.ProductCode)
                || s.OrganizationId.ToString().ToLower().Contains(term)
                || s.PlanId.ToString().ToLower().Contains(term)
                || s.Id.ToString().ToLower().Contains(term));
        }

        query = (sortBy, sortDescending) switch
        {
            (SubscriptionListSortBy.Status, false) => query.OrderBy(s => s.Status).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.Status, true) => query.OrderByDescending(s => s.Status).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.ProductCode, false) => query.OrderBy(s => s.ProductCode).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.ProductCode, true) => query.OrderByDescending(s => s.ProductCode).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.ProductDisplayName, false) =>
                query.OrderBy(s => _db.Products.Where(p => p.Code == s.ProductCode).Select(p => p.DisplayName).FirstOrDefault())
                    .ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.ProductDisplayName, true) =>
                query.OrderByDescending(s => _db.Products.Where(p => p.Code == s.ProductCode).Select(p => p.DisplayName).FirstOrDefault())
                    .ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.OrganizationName, false) =>
                query.OrderBy(s => _db.Organizations.Where(o => o.Id == s.OrganizationId).Select(o => o.DisplayName).FirstOrDefault())
                    .ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.OrganizationName, true) =>
                query.OrderByDescending(s => _db.Organizations.Where(o => o.Id == s.OrganizationId).Select(o => o.DisplayName).FirstOrDefault())
                    .ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.PlanDisplayName, false) =>
                query.OrderBy(s => _db.Plans.Where(p => p.Id == s.PlanId).Select(p => p.DisplayName).FirstOrDefault())
                    .ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.PlanDisplayName, true) =>
                query.OrderByDescending(s => _db.Plans.Where(p => p.Id == s.PlanId).Select(p => p.DisplayName).FirstOrDefault())
                    .ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.TrialEndUtc, false) => query.OrderBy(s => s.TrialEndUtc).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.TrialEndUtc, true) => query.OrderByDescending(s => s.TrialEndUtc).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.PaidPeriodEndUtc, false) => query.OrderBy(s => s.PaidPeriodEndUtc).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.PaidPeriodEndUtc, true) => query.OrderByDescending(s => s.PaidPeriodEndUtc).ThenByDescending(s => s.UpdatedAtUtc),
            (SubscriptionListSortBy.CreatedAtUtc, false) => query.OrderBy(s => s.CreatedAtUtc),
            (SubscriptionListSortBy.CreatedAtUtc, true) => query.OrderByDescending(s => s.CreatedAtUtc),
            (SubscriptionListSortBy.UpdatedAtUtc, false) => query.OrderBy(s => s.UpdatedAtUtc),
            _ => query.OrderByDescending(s => s.UpdatedAtUtc)
        };

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SubscriptionEntityMapper.ToDomain).ToList(), totalCount);
    }

    public Task<bool> ExistsActiveLikeAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default) =>
        _db.Subscriptions
            .AsNoTracking()
            .AnyAsync(
                s => s.OrganizationId == organizationId.Value
                     && s.ProductCode == productCode.Value
                     && ActiveLikeStatuses.Contains(s.Status),
                cancellationToken);

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _db.Subscriptions.Add(SubscriptionEntityMapper.ToRecord(subscription));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        var record = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscription.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        SubscriptionEntityMapper.ApplyToRecord(subscription, record);
    }
}
