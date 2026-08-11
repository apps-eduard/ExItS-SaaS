using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Resolves merchant discovery filter constraints from organization entitlements.
/// Platform Admin (ViewGlobalCatalog) remains unrestricted.
/// </summary>
public sealed class MerchantCatalogEntitlementGate(
    IOrganizationBusinessTypeEntitlementResolver resolver,
    IPlatformAuthorizationService authorization,
    IPlatformActorAccessor actors,
    IBusinessTypeRepository businessTypes)
{
    public sealed record DiscoveryScope(
        bool Unrestricted,
        PlatformOrganizationId? OrganizationId,
        IReadOnlyList<Guid> AllowedBusinessTypeIds,
        OrganizationBusinessTypeEntitlement? Entitlement);

    /// <summary>
    /// Resolves discovery scope for the current actor.
    /// When <paramref name="organizationIdOverride"/> is null, uses the session organization claim via actor.
    /// </summary>
    public async Task<ApplicationResult<DiscoveryScope>> ResolveDiscoveryScopeAsync(
        Guid? organizationIdOverride = null,
        ProductCode? productCode = null,
        CancellationToken cancellationToken = default)
    {
        var actor = actors.GetCurrent();
        var adminCheck = await authorization
            .EnsurePermissionForActorAsync(
                actor,
                PlatformPermission.ViewGlobalCatalog,
                organizationId: null,
                cancellationToken)
            .ConfigureAwait(false);
        if (adminCheck.IsSuccess)
        {
            return ApplicationResult<DiscoveryScope>.Success(
                new DiscoveryScope(true, null, Array.Empty<Guid>(), null));
        }

        var orgIdValue = organizationIdOverride ?? actor.OrganizationId?.Value;
        if (orgIdValue is null || orgIdValue == Guid.Empty)
        {
            return ApplicationResult<DiscoveryScope>.Failure(
                ApplicationErrorCodes.OrganizationContextNotEligible,
                "An organization context is required for merchant catalog discovery.");
        }

        var orgId = PlatformOrganizationId.From(orgIdValue.Value);
        var entitlement = await resolver.ResolveAsync(orgId, productCode, cancellationToken).ConfigureAwait(false);
        if (!entitlement.IsSuccess)
        {
            return ApplicationResult<DiscoveryScope>.Failure(entitlement.ErrorCode!, entitlement.ErrorMessage!);
        }

        var allowed = entitlement.Value!.EffectiveBusinessTypeIds.Select(id => id.Value).ToList();
        return ApplicationResult<DiscoveryScope>.Success(
            new DiscoveryScope(false, orgId, allowed, entitlement.Value));
    }

    /// <summary>
    /// Intersects optional client BT filter with allowed set.
    /// Missing client filter ⇒ all allowed types (not unrestricted Platform catalog).
    /// Forged/unentitled client filter ⇒ BusinessTypeNotEntitled.
    /// </summary>
    public async Task<ApplicationResult<(Guid? SingleBusinessTypeId, IReadOnlyCollection<Guid>? AllowedBusinessTypeIds)>>
        ResolveListFilterAsync(
            DiscoveryScope scope,
            Guid? businessTypeId,
            string? businessTypeCode,
            CancellationToken cancellationToken = default)
    {
        if (scope.Unrestricted)
        {
            return ApplicationResult<(Guid?, IReadOnlyCollection<Guid>?)>.Success((businessTypeId, null));
        }

        if (scope.AllowedBusinessTypeIds.Count == 0)
        {
            return ApplicationResult<(Guid?, IReadOnlyCollection<Guid>?)>.Success((null, Array.Empty<Guid>()));
        }

        Guid? requested = businessTypeId;
        if (requested is null && !string.IsNullOrWhiteSpace(businessTypeCode))
        {
            var byCode = await businessTypes.GetByCodeAsync(businessTypeCode.Trim(), cancellationToken)
                .ConfigureAwait(false);
            if (byCode is null)
            {
                return ApplicationResult<(Guid?, IReadOnlyCollection<Guid>?)>.Failure(
                    ApplicationErrorCodes.BusinessTypeNotFound,
                    "Business type was not found.");
            }

            requested = byCode.Id.Value;
        }

        if (requested is Guid requestedId)
        {
            if (!scope.AllowedBusinessTypeIds.Contains(requestedId))
            {
                return ApplicationResult<(Guid?, IReadOnlyCollection<Guid>?)>.Failure(
                    ApplicationErrorCodes.BusinessTypeNotEntitled,
                    "Organization is not entitled to this business type.");
            }

            return ApplicationResult<(Guid?, IReadOnlyCollection<Guid>?)>.Success((requestedId, null));
        }

        // No client filter: constrain to the full effective set.
        return ApplicationResult<(Guid?, IReadOnlyCollection<Guid>?)>.Success(
            (null, scope.AllowedBusinessTypeIds));
    }

    public ApplicationResult EnsureResourceEntitled(
        DiscoveryScope scope,
        IEnumerable<BusinessTypeId> resourceBusinessTypeIds)
    {
        if (scope.Unrestricted)
        {
            return ApplicationResult.Success();
        }

        if (scope.AllowedBusinessTypeIds.Count == 0)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.BusinessTypeNotEntitled,
                "Organization has no effective business type entitlements.");
        }

        var resourceIds = resourceBusinessTypeIds.Select(id => id.Value).ToHashSet();
        if (!resourceIds.Overlaps(scope.AllowedBusinessTypeIds))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.BusinessTypeNotEntitled,
                "Resource is outside the organization's effective business types.");
        }

        return ApplicationResult.Success();
    }

    public ApplicationResult EnsureResourceEntitled(DiscoveryScope scope, BusinessTypeId primaryBusinessTypeId) =>
        EnsureResourceEntitled(scope, [primaryBusinessTypeId]);
}
