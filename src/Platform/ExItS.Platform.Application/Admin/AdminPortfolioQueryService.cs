using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Admin;

public sealed class AdminPortfolioQueryService
{
    private readonly IAdminPortfolioReadStore _store;
    private readonly CatalogQueryService _catalog;
    private readonly OrganizationQueryService _organizations;
    private readonly SubscriptionQueryService _subscriptions;
    private readonly SaaSPaymentQueryService _payments;

    public AdminPortfolioQueryService(
        IAdminPortfolioReadStore store,
        CatalogQueryService catalog,
        OrganizationQueryService organizations,
        SubscriptionQueryService subscriptions,
        SaaSPaymentQueryService payments)
    {
        _store = store;
        _catalog = catalog;
        _organizations = organizations;
        _subscriptions = subscriptions;
        _payments = payments;
    }

    public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(CancellationToken cancellationToken = default)
    {
        var failures = new List<string>();
        var activeProducts = await TryCountAsync(
            "activeProducts",
            () => _store.CountProductsAsync(ProductStatus.Active, cancellationToken),
            failures).ConfigureAwait(false);
        var publishedPlans = await TryCountAsync(
            "publishedPlanVersions",
            () => _store.CountPublishedPlanVersionsAsync(cancellationToken),
            failures).ConfigureAwait(false);
        var organizations = await TryCountAsync(
            "organizations",
            () => _store.CountOrganizationsAsync(cancellationToken),
            failures).ConfigureAwait(false);
        var trialing = await TryCountAsync(
            "trialingSubscriptions",
            () => _store.CountSubscriptionsByStatusAsync(SubscriptionStatus.Trialing, cancellationToken),
            failures).ConfigureAwait(false);
        var active = await TryCountAsync(
            "activeSubscriptions",
            () => _store.CountSubscriptionsByStatusAsync(SubscriptionStatus.Active, cancellationToken),
            failures).ConfigureAwait(false);
        var grace = await TryCountAsync(
            "gracePeriodSubscriptions",
            () => _store.CountSubscriptionsByStatusAsync(SubscriptionStatus.GracePeriod, cancellationToken),
            failures).ConfigureAwait(false);
        var pastDue = await TryCountAsync(
            "pastDueSubscriptions",
            () => _store.CountSubscriptionsByStatusAsync(SubscriptionStatus.PastDue, cancellationToken),
            failures).ConfigureAwait(false);
        var suspended = await TryCountAsync(
            "suspendedSubscriptions",
            () => _store.CountSubscriptionsByStatusAsync(SubscriptionStatus.Suspended, cancellationToken),
            failures).ConfigureAwait(false);
        var pendingPayments = await TryCountAsync(
            "pendingManualPayments",
            () => _store.CountPaymentsByStatusAsync(SaaSPaymentStatus.PendingConfirmation, cancellationToken),
            failures).ConfigureAwait(false);
        var latestEntitlements = await TryCountAsync(
            "latestEntitlementSnapshots",
            () => _store.CountLatestEntitlementPairsAsync(cancellationToken),
            failures).ConfigureAwait(false);

        return new PortfolioSummaryDto(
            activeProducts ?? 0,
            publishedPlans ?? 0,
            organizations ?? 0,
            trialing ?? 0,
            active ?? 0,
            grace ?? 0,
            pastDue ?? 0,
            suspended ?? 0,
            pendingPayments ?? 0,
            latestEntitlements ?? 0,
            failures);
    }

    public async Task<ProductOverviewDto?> GetProductOverviewAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        ProductCode code;
        try
        {
            code = ProductCode.Create(productCode);
        }
        catch (DomainException)
        {
            return null;
        }

        var product = await _catalog.GetProductByCodeAsync(code.Value, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return null;
        }

        var features = await _catalog.ListFeaturesByProductAsync(code.Value, cancellationToken).ConfigureAwait(false);
        var plans = await _catalog.ListPlansByProductAsync(code.Value, cancellationToken).ConfigureAwait(false);
        var trials = await _catalog.ListTrialsByProductAsync(code.Value, cancellationToken).ConfigureAwait(false);

        var publishedVersions = new List<PlanVersionDto>();
        foreach (var plan in plans)
        {
            var versions = await _catalog.ListPlanVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
            publishedVersions.AddRange(versions.Where(v =>
                string.Equals(v.Status, nameof(PlanVersionStatus.Published), StringComparison.Ordinal)));
        }

        return new ProductOverviewDto(product, features, plans, publishedVersions, trials);
    }

    public async Task<OrganizationCommercialSummaryDto?> GetOrganizationCommercialSummaryAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return null;
        }

        var subscriptions = await _subscriptions
            .ListByOrganizationAsync(organizationId, status: null, page: 1, pageSize: CatalogPagination.MaxPageSize, cancellationToken)
            .ConfigureAwait(false);
        var payments = await _payments
            .ListByOrganizationAsync(organizationId, status: null, page: 1, pageSize: CatalogPagination.MaxPageSize, cancellationToken)
            .ConfigureAwait(false);
        var latest = await _store
            .ListLatestEntitlementsForOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        return new OrganizationCommercialSummaryDto(
            organization,
            subscriptions.Items,
            payments.Items,
            latest);
    }

    public async Task<PagedResult<EntitlementLatestSummaryDto>> ListLatestEntitlementsAsync(
        int? page,
        int? pageSize,
        EntitlementListSortBy? sortBy = null,
        bool? sortDescending = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var pageNumber = Math.Max(page ?? 1, 1);
        var (items, totalCount) = await _store
            .ListLatestEntitlementSummariesAsync(
                skip,
                take,
                sortBy ?? EntitlementListSortBy.GeneratedAtUtc,
                sortDescending ?? true,
                cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<EntitlementLatestSummaryDto>(items, totalCount, pageNumber, take);
    }

    private static async Task<int?> TryCountAsync(
        string section,
        Func<Task<int>> countAsync,
        ICollection<string> failures)
    {
        try
        {
            return await countAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            failures.Add($"{section}: {ex.Message}");
            return null;
        }
    }
}
