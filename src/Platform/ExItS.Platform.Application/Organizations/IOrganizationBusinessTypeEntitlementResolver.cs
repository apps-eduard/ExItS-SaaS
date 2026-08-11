using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Authoritative organization Business Type entitlement snapshot (WP03).
/// Effective = Primary ∪ (activations ∩ plan-version grants), with Active-only add-ons.
/// </summary>
public sealed class OrganizationBusinessTypeEntitlement
{
    public required PlatformOrganizationId OrganizationId { get; init; }
    public BusinessTypeId? PrimaryBusinessTypeId { get; init; }
    public Guid? SubscriptionId { get; init; }
    public Guid? PlanVersionId { get; init; }
    public required IReadOnlyList<BusinessTypeId> GrantedBusinessTypeIds { get; init; }
    public required IReadOnlyList<BusinessTypeId> ActivatedBusinessTypeIds { get; init; }
    public required IReadOnlyList<BusinessTypeId> EffectiveBusinessTypeIds { get; init; }
    public required IReadOnlyDictionary<Guid, string> EffectiveBusinessTypeCodes { get; init; }

    public bool IsEntitled(BusinessTypeId businessTypeId) =>
        EffectiveBusinessTypeIds.Any(id => id == businessTypeId);

    public bool IsEntitled(Guid businessTypeId) =>
        EffectiveBusinessTypeIds.Any(id => id.Value == businessTypeId);
}

public interface IOrganizationBusinessTypeEntitlementResolver
{
    /// <summary>Resolves effective BTs for an organization. Failures are fail-closed for enforcement callers.</summary>
    Task<ApplicationResult<OrganizationBusinessTypeEntitlement>> ResolveAsync(
        PlatformOrganizationId organizationId,
        ProductCode? productCode = null,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult> EnsureEntitledAsync(
        PlatformOrganizationId organizationId,
        BusinessTypeId businessTypeId,
        ProductCode? productCode = null,
        CancellationToken cancellationToken = default);
}
