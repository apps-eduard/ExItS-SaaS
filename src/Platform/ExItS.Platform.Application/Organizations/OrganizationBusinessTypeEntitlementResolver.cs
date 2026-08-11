using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Single authority for organization effective Business Types (Phase 23 WP03).
/// Uses current active-like subscription + bound PlanVersion.BusinessTypeGrants.
/// Stale activations outside grants remain stored but are excluded from effective set.
/// </summary>
public sealed class OrganizationBusinessTypeEntitlementResolver : IOrganizationBusinessTypeEntitlementResolver
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly IOrganizationBusinessTypeActivationRepository _activations;
    private readonly IBusinessTypeRepository _businessTypes;

    public OrganizationBusinessTypeEntitlementResolver(
        IPlatformOrganizationRepository organizations,
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        IOrganizationBusinessTypeActivationRepository activations,
        IBusinessTypeRepository businessTypes)
    {
        _organizations = organizations;
        _subscriptions = subscriptions;
        _plans = plans;
        _activations = activations;
        _businessTypes = businessTypes;
    }

    public async Task<ApplicationResult<OrganizationBusinessTypeEntitlement>> ResolveAsync(
        PlatformOrganizationId organizationId,
        ProductCode? productCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        var product = productCode ?? ProductCode.Create(ProductCode.PinoyBusinessPos);

        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<OrganizationBusinessTypeEntitlement>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found.");
        }

        var subscription = await _subscriptions
            .GetCurrentForOrganizationProductAsync(organizationId, product, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null || !Subscription.IsActiveLike(subscription.Status))
        {
            return ApplicationResult<OrganizationBusinessTypeEntitlement>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "An active-like subscription is required to resolve business type entitlements.");
        }

        var planVersion = await _plans
            .GetVersionByIdAsync(subscription.PlanVersionId, cancellationToken)
            .ConfigureAwait(false);
        if (planVersion is null)
        {
            return ApplicationResult<OrganizationBusinessTypeEntitlement>.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Subscription plan version was not found.");
        }

        var granted = planVersion.BusinessTypeGrants
            .Select(id => id.Value)
            .ToHashSet();

        var activations = await _activations
            .ListByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var activatedIds = activations.Select(a => a.BusinessTypeId).ToList();

        // Downgrade-safe: activation rows may remain, but only granted ∩ activated count.
        var entitledActivationIds = activatedIds
            .Where(id => granted.Contains(id.Value))
            .Select(id => id.Value)
            .ToHashSet();

        var candidateIds = new HashSet<Guid>();
        if (organization.PrimaryBusinessTypeId is { } primary)
        {
            // Primary remains effective for legacy orgs even when plan BT packs are empty/unseeded.
            candidateIds.Add(primary.Value);
        }

        foreach (var id in entitledActivationIds)
        {
            candidateIds.Add(id);
        }

        var types = await _businessTypes.GetByIdsAsync(candidateIds, cancellationToken).ConfigureAwait(false);
        var byId = types.ToDictionary(t => t.Id.Value);

        var effective = new List<BusinessTypeId>();
        var codes = new Dictionary<Guid, string>();

        if (organization.PrimaryBusinessTypeId is { } primaryId
            && byId.TryGetValue(primaryId.Value, out var primaryType))
        {
            // Primary always included when present (legacy continuity), even if Inactive.
            effective.Add(primaryId);
            codes[primaryId.Value] = primaryType.Code;
        }

        foreach (var activationId in entitledActivationIds.OrderBy(x => x))
        {
            if (organization.PrimaryBusinessTypeId is { } p && p.Value == activationId)
            {
                continue;
            }

            if (!byId.TryGetValue(activationId, out var type))
            {
                continue;
            }

            // Additional types must be Active to remain effective for discovery/activation.
            if (type.Status != BusinessTypeStatus.Active)
            {
                continue;
            }

            var btId = BusinessTypeId.From(activationId);
            effective.Add(btId);
            codes[activationId] = type.Code;
        }

        return ApplicationResult<OrganizationBusinessTypeEntitlement>.Success(
            new OrganizationBusinessTypeEntitlement
            {
                OrganizationId = organizationId,
                PrimaryBusinessTypeId = organization.PrimaryBusinessTypeId,
                SubscriptionId = subscription.Id.Value,
                PlanVersionId = planVersion.Id.Value,
                GrantedBusinessTypeIds = planVersion.BusinessTypeGrants.ToList(),
                ActivatedBusinessTypeIds = activatedIds,
                EffectiveBusinessTypeIds = effective,
                EffectiveBusinessTypeCodes = codes
            });
    }

    public async Task<ApplicationResult> EnsureEntitledAsync(
        PlatformOrganizationId organizationId,
        BusinessTypeId businessTypeId,
        ProductCode? productCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(businessTypeId);
        var resolved = await ResolveAsync(organizationId, productCode, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult.Failure(resolved.ErrorCode!, resolved.ErrorMessage!);
        }

        if (!resolved.Value!.IsEntitled(businessTypeId))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.BusinessTypeNotEntitled,
                "Organization is not entitled to this business type.");
        }

        return ApplicationResult.Success();
    }
}
