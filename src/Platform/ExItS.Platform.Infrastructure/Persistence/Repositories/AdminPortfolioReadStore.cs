using ExItS.Platform.Application.Admin;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Entitlements;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class AdminPortfolioReadStore : IAdminPortfolioReadStore
{
    private readonly PlatformDbContext _db;

    public AdminPortfolioReadStore(PlatformDbContext db) => _db = db;

    public Task<int> CountProductsAsync(ProductStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _db.Products.AsNoTracking();
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(p => p.Status == statusName);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<int> CountPublishedPlanVersionsAsync(CancellationToken cancellationToken = default)
    {
        var published = nameof(PlanVersionStatus.Published);
        return _db.PlanVersions.AsNoTracking().CountAsync(v => v.Status == published, cancellationToken);
    }

    public Task<int> CountOrganizationsAsync(CancellationToken cancellationToken = default) =>
        _db.Organizations.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountSubscriptionsByStatusAsync(
        SubscriptionStatus status,
        CancellationToken cancellationToken = default)
    {
        var statusName = status.ToString();
        return _db.Subscriptions.AsNoTracking().CountAsync(s => s.Status == statusName, cancellationToken);
    }

    public Task<int> CountPaymentsByStatusAsync(
        SaaSPaymentStatus status,
        CancellationToken cancellationToken = default)
    {
        var statusName = status.ToString();
        return _db.SaaSPayments.AsNoTracking().CountAsync(p => p.Status == statusName, cancellationToken);
    }

    public async Task<int> CountLatestEntitlementPairsAsync(CancellationToken cancellationToken = default)
    {
        var count = await _db.EntitlementSnapshots
            .AsNoTracking()
            .GroupBy(s => new { s.OrganizationId, s.ProductCode })
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        return count;
    }

    public async Task<(IReadOnlyList<EntitlementLatestSummaryDto> Items, int TotalCount)> ListLatestEntitlementSummariesAsync(
        int skip,
        int take,
        EntitlementListSortBy sortBy = EntitlementListSortBy.GeneratedAtUtc,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var latestVersions = _db.EntitlementSnapshots
            .AsNoTracking()
            .GroupBy(s => new { s.OrganizationId, s.ProductCode })
            .Select(g => new
            {
                g.Key.OrganizationId,
                g.Key.ProductCode,
                SnapshotVersion = g.Max(x => x.SnapshotVersion)
            });

        var enriched =
            from snapshot in _db.EntitlementSnapshots.AsNoTracking()
            join latest in latestVersions
                on new { snapshot.OrganizationId, snapshot.ProductCode, snapshot.SnapshotVersion }
                equals new { latest.OrganizationId, latest.ProductCode, latest.SnapshotVersion }
            join org in _db.Organizations.AsNoTracking()
                on snapshot.OrganizationId equals org.Id into orgJoin
            from org in orgJoin.DefaultIfEmpty()
            join product in _db.Products.AsNoTracking()
                on snapshot.ProductCode equals product.Code into productJoin
            from product in productJoin.DefaultIfEmpty()
            select new EntitlementLatestQueryRow
            {
                Snapshot = snapshot,
                OrganizationDisplayName = org != null ? org.DisplayName : null,
                ProductDisplayName = product != null ? product.DisplayName : null
            };

        var totalCount = await enriched.CountAsync(cancellationToken).ConfigureAwait(false);
        var ordered = ApplySort(enriched, sortBy, sortDescending);
        var page = await ordered
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (page.Select(Map).ToList(), totalCount);
    }

    public async Task<IReadOnlyList<EntitlementLatestSummaryDto>> ListLatestEntitlementsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var latestVersions = _db.EntitlementSnapshots
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId)
            .GroupBy(s => s.ProductCode)
            .Select(g => new
            {
                ProductCode = g.Key,
                SnapshotVersion = g.Max(x => x.SnapshotVersion)
            });

        var rows = await (
                from snapshot in _db.EntitlementSnapshots.AsNoTracking()
                where snapshot.OrganizationId == organizationId
                join latest in latestVersions
                    on new { snapshot.ProductCode, snapshot.SnapshotVersion }
                    equals new { latest.ProductCode, latest.SnapshotVersion }
                join org in _db.Organizations.AsNoTracking()
                    on snapshot.OrganizationId equals org.Id into orgJoin
                from org in orgJoin.DefaultIfEmpty()
                join product in _db.Products.AsNoTracking()
                    on snapshot.ProductCode equals product.Code into productJoin
                from product in productJoin.DefaultIfEmpty()
                orderby product != null ? product.DisplayName : snapshot.ProductCode
                select new EntitlementLatestQueryRow
                {
                    Snapshot = snapshot,
                    OrganizationDisplayName = org != null ? org.DisplayName : null,
                    ProductDisplayName = product != null ? product.DisplayName : null
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(Map).ToList();
    }

    private static IOrderedQueryable<EntitlementLatestQueryRow> ApplySort(
        IQueryable<EntitlementLatestQueryRow> query,
        EntitlementListSortBy sortBy,
        bool sortDescending)
    {
        // Default / Generated: Generated DESC then Organization ASC.
        // Other keys: primary sort, then Organization ASC for stability (except Organization itself).
        return sortBy switch
        {
            EntitlementListSortBy.OrganizationDisplayName when sortDescending =>
                query.OrderByDescending(r => r.OrganizationDisplayName)
                    .ThenByDescending(r => r.Snapshot.GeneratedAtUtc),
            EntitlementListSortBy.OrganizationDisplayName =>
                query.OrderBy(r => r.OrganizationDisplayName)
                    .ThenByDescending(r => r.Snapshot.GeneratedAtUtc),

            EntitlementListSortBy.ProductDisplayName when sortDescending =>
                query.OrderByDescending(r => r.ProductDisplayName)
                    .ThenBy(r => r.OrganizationDisplayName),
            EntitlementListSortBy.ProductDisplayName =>
                query.OrderBy(r => r.ProductDisplayName)
                    .ThenBy(r => r.OrganizationDisplayName),

            EntitlementListSortBy.Status when sortDescending =>
                query.OrderByDescending(r => r.Snapshot.SubscriptionStatus)
                    .ThenBy(r => r.OrganizationDisplayName),
            EntitlementListSortBy.Status =>
                query.OrderBy(r => r.Snapshot.SubscriptionStatus)
                    .ThenBy(r => r.OrganizationDisplayName),

            EntitlementListSortBy.Revision when sortDescending =>
                query.OrderByDescending(r => r.Snapshot.SnapshotVersion)
                    .ThenBy(r => r.OrganizationDisplayName),
            EntitlementListSortBy.Revision =>
                query.OrderBy(r => r.Snapshot.SnapshotVersion)
                    .ThenBy(r => r.OrganizationDisplayName),

            EntitlementListSortBy.GeneratedAtUtc when !sortDescending =>
                query.OrderBy(r => r.Snapshot.GeneratedAtUtc)
                    .ThenBy(r => r.OrganizationDisplayName),

            _ =>
                query.OrderByDescending(r => r.Snapshot.GeneratedAtUtc)
                    .ThenBy(r => r.OrganizationDisplayName)
        };
    }

    private static EntitlementLatestSummaryDto Map(EntitlementLatestQueryRow row) =>
        new(
            row.Snapshot.Id,
            row.Snapshot.OrganizationId,
            row.Snapshot.ProductCode,
            row.Snapshot.SubscriptionId,
            row.Snapshot.SubscriptionStatus,
            row.Snapshot.SnapshotVersion,
            row.Snapshot.SchemaVersion,
            row.Snapshot.GeneratedAtUtc,
            row.Snapshot.EffectiveAtUtc,
            row.Snapshot.RefreshByUtc,
            row.Snapshot.ExpiresAtUtc,
            row.Snapshot.InGracePeriod,
            row.OrganizationDisplayName,
            row.ProductDisplayName);

    private sealed class EntitlementLatestQueryRow
    {
        public required EntitlementSnapshotRecord Snapshot { get; init; }
        public string? OrganizationDisplayName { get; init; }
        public string? ProductDisplayName { get; init; }
    }
}
