using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Admin;

/// <summary>
/// Focused read store for Platform Admin portfolio aggregates (P4-WP01).
/// Not a generic repository — named counts and latest-entitlement indexes only.
/// </summary>
public interface IAdminPortfolioReadStore
{
    Task<int> CountProductsAsync(ProductStatus? status, CancellationToken cancellationToken = default);

    Task<int> CountPublishedPlanVersionsAsync(CancellationToken cancellationToken = default);

    Task<int> CountOrganizationsAsync(CancellationToken cancellationToken = default);

    Task<int> CountSubscriptionsByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default);

    Task<int> CountPaymentsByStatusAsync(SaaSPaymentStatus status, CancellationToken cancellationToken = default);

    Task<int> CountLatestEntitlementPairsAsync(CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<EntitlementLatestSummaryDto> Items, int TotalCount)> ListLatestEntitlementSummariesAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EntitlementLatestSummaryDto>> ListLatestEntitlementsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
