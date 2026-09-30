using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// True when the seller can accept Personal/customer online orders:
/// StoreCustomerOrdering entitlement AND at least one Active branch with Online ON
/// (enabled, not paused) and customer-ordering setup ready.
/// Does not require open-now; authenticated place/quote remain operational authority.
/// </summary>
public interface IOrganizationCustomerOrderingAvailability
{
    Task<bool> IsAvailableAsync(
        PlatformOrganization organization,
        CancellationToken cancellationToken = default);
}

public sealed class OrganizationCustomerOrderingAvailability(
    IOrganizationBranchRepository branches,
    IBranchOperatingHoursRepository hours,
    IBranchDeliveryPolicyRepository policies,
    EntitlementQueryService entitlements,
    IBranchFulfillmentReadinessEvaluator readinessEvaluator,
    IClock clock) : IOrganizationCustomerOrderingAvailability
{
    public async Task<bool> IsAvailableAsync(
        PlatformOrganization organization,
        CancellationToken cancellationToken = default)
    {
        var caps = await ResolveCapabilitiesAsync(organization.Id, cancellationToken).ConfigureAwait(false);
        if (!caps.CanUseCustomerOrdering)
        {
            return false;
        }

        var orgBranches = await branches
            .ListByOrganizationAsync(organization.Id, cancellationToken)
            .ConfigureAwait(false);

        var now = clock.UtcNow;
        foreach (var branch in orgBranches)
        {
            if (branch.Status is not OrganizationBranchStatus.Active)
            {
                continue;
            }

            if (!branch.CustomerOrderingEnabled || branch.OnlineOrdersPaused)
            {
                continue;
            }

            var branchHours = await hours.GetByBranchIdAsync(branch.Id, cancellationToken).ConfigureAwait(false);
            var policy = await policies.GetByBranchIdAsync(branch.Id, cancellationToken).ConfigureAwait(false);
            var result = readinessEvaluator.Evaluate(new BranchFulfillmentReadinessInput(
                branch,
                branchHours,
                policy,
                organization.Profile.TimeZoneId,
                organization.Profile.ContactPhone,
                caps,
                now,
                HasActiveDeliveryServiceArea: false));

            if (result.CustomerOrderingReady)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<BranchEntitlementCapabilities> ResolveCapabilitiesAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await entitlements
            .GetLatestAsync(organizationId.Value, ProductCode.PinoyBusinessPos, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
        {
            return new BranchEntitlementCapabilities(false, false);
        }

        var canOrder = snapshot.Grants.Any(g =>
            g.Enabled
            && string.Equals(g.FeatureCode, FeatureCode.StoreCustomerOrdering, StringComparison.Ordinal));
        var canDelivery = canOrder && snapshot.Grants.Any(g =>
            g.Enabled
            && string.Equals(g.FeatureCode, FeatureCode.StoreDeliveryOrders, StringComparison.Ordinal));
        return new BranchEntitlementCapabilities(canOrder, canDelivery);
    }
}
