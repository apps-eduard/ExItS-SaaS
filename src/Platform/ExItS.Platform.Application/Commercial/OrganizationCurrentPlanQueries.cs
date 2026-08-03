using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Commercial;

public sealed record PendingPlanChangeDto(
    Guid PlanId,
    string? PlanKey,
    string? DisplayName,
    DateTimeOffset? EffectiveAtUtc);

public sealed record OrganizationCurrentPlanDto(
    Guid OrganizationId,
    string ProductCode,
    SubscriptionDto? CurrentSubscription,
    PlanDto? CurrentPlan,
    PendingPlanChangeDto? PendingPlanChange,
    IReadOnlyList<PlanDto> AvailablePlans,
    EntitlementLatestSummaryDto? Entitlement,
    bool? ProductInstancePresent);

public sealed class OrganizationCurrentPlanQueryService
{
    private readonly CommercialCatalogQueryService _commercialCatalog;
    private readonly SubscriptionQueryService _subscriptions;
    private readonly CatalogQueryService _catalog;
    private readonly IAdminPortfolioReadStore _adminReadStore;

    public OrganizationCurrentPlanQueryService(
        CommercialCatalogQueryService commercialCatalog,
        SubscriptionQueryService subscriptions,
        CatalogQueryService catalog,
        IAdminPortfolioReadStore adminReadStore)
    {
        _commercialCatalog = commercialCatalog;
        _subscriptions = subscriptions;
        _catalog = catalog;
        _adminReadStore = adminReadStore;
    }

    public async Task<OrganizationCurrentPlanDto?> GetCurrentPlanAsync(
        Guid organizationId,
        string? productCode,
        CancellationToken cancellationToken = default)
    {
        var resolvedProductCode = string.IsNullOrWhiteSpace(productCode)
            ? ProductCode.PinoyBusinessPos
            : productCode;

        SubscriptionDto? currentSubscription = null;
        try
        {
            currentSubscription = await _subscriptions
                .GetCurrentAsync(organizationId, resolvedProductCode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // No subscription or mapping failure — treat as empty current plan.
        }

        PlanDto? currentPlan = null;
        PendingPlanChangeDto? pendingPlanChange = null;
        if (currentSubscription is not null)
        {
            try
            {
                currentPlan = await _catalog
                    .GetPlanByIdAsync(currentSubscription.PlanId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Plan may have been retired; keep subscription without plan enrichment.
            }

            if (currentSubscription.PendingPlanId is Guid pendingPlanId)
            {
                pendingPlanChange = new PendingPlanChangeDto(
                    pendingPlanId,
                    currentSubscription.PendingPlanKey,
                    currentSubscription.PendingPlanDisplayName,
                    currentSubscription.PendingPlanEffectiveAtUtc);
            }
        }

        IReadOnlyList<PlanDto> availablePlans;
        try
        {
            availablePlans = await _commercialCatalog
                .ListActivePlansAsync(resolvedProductCode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            availablePlans = [];
        }

        EntitlementLatestSummaryDto? entitlement = null;
        try
        {
            var latest = await _adminReadStore
                .ListLatestEntitlementsForOrganizationAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);
            entitlement = latest.FirstOrDefault(e =>
                string.Equals(e.ProductCode, resolvedProductCode, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // Entitlement lookup is optional for current-plan display.
        }

        return new OrganizationCurrentPlanDto(
            organizationId,
            resolvedProductCode,
            currentSubscription,
            currentPlan,
            pendingPlanChange,
            availablePlans,
            entitlement,
            ProductInstancePresent: null);
    }
}
