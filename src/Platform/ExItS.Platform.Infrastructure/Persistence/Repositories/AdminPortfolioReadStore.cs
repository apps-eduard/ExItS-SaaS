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

        var joined = from snapshot in _db.EntitlementSnapshots.AsNoTracking()
                     join latest in latestVersions
                         on new { snapshot.OrganizationId, snapshot.ProductCode, snapshot.SnapshotVersion }
                         equals new { latest.OrganizationId, latest.ProductCode, latest.SnapshotVersion }
                     select snapshot;

        var totalCount = await joined.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await joined
            .OrderByDescending(s => s.GeneratedAtUtc)
            .ThenByDescending(s => s.SnapshotVersion)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(Map).ToList(), totalCount);
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

        var records = await (
                from snapshot in _db.EntitlementSnapshots.AsNoTracking()
                where snapshot.OrganizationId == organizationId
                join latest in latestVersions
                    on new { snapshot.ProductCode, snapshot.SnapshotVersion }
                    equals new { latest.ProductCode, latest.SnapshotVersion }
                orderby snapshot.ProductCode
                select snapshot)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(Map).ToList();
    }

    private static EntitlementLatestSummaryDto Map(EntitlementSnapshotRecord snapshot) =>
        new(
            snapshot.Id,
            snapshot.OrganizationId,
            snapshot.ProductCode,
            snapshot.SubscriptionId,
            snapshot.SubscriptionStatus,
            snapshot.SnapshotVersion,
            snapshot.SchemaVersion,
            snapshot.GeneratedAtUtc,
            snapshot.EffectiveAtUtc,
            snapshot.RefreshByUtc,
            snapshot.ExpiresAtUtc,
            snapshot.InGracePeriod);
}
