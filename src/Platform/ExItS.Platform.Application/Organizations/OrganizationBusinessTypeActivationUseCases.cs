using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

public sealed record OrganizationBusinessTypeActivationDto(
    Guid OrganizationId,
    Guid BusinessTypeId,
    string? BusinessTypeCode,
    DateTimeOffset ActivatedAtUtc);

/// <summary>Merchant-facing option for a plan-granted Business Type (WP11).</summary>
public sealed record OrganizationBusinessTypeOptionDto(
    Guid Id,
    string Code,
    string Name,
    bool IsPrimary,
    bool IsGranted,
    bool IsActivated,
    bool IsEffective);

public sealed record OrganizationBusinessTypeEntitlementDto(
    Guid OrganizationId,
    Guid? PrimaryBusinessTypeId,
    Guid? SubscriptionId,
    Guid? PlanVersionId,
    IReadOnlyList<Guid> GrantedBusinessTypeIds,
    IReadOnlyList<Guid> ActivatedBusinessTypeIds,
    IReadOnlyList<Guid> EffectiveBusinessTypeIds,
    IReadOnlyDictionary<string, string> EffectiveBusinessTypeCodesById,
    int MaxActiveBusinessTypes = 1,
    int EffectiveCount = 0,
    int RemainingCapacity = 0,
    IReadOnlyList<OrganizationBusinessTypeOptionDto>? BusinessTypes = null);

public sealed class GetOrganizationBusinessTypeEntitlement(
    IOrganizationBusinessTypeEntitlementResolver resolver,
    IPlanRepository plans,
    IBusinessTypeRepository businessTypes)
{
    public async Task<ApplicationResult<OrganizationBusinessTypeEntitlementDto>> ExecuteAsync(
        Guid organizationId,
        string? productCode = null,
        CancellationToken cancellationToken = default)
    {
        var product = string.IsNullOrWhiteSpace(productCode)
            ? null
            : ProductCode.Create(productCode);
        var result = await resolver
            .ResolveAsync(PlatformOrganizationId.From(organizationId), product, cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ApplicationResult<OrganizationBusinessTypeEntitlementDto>.Failure(
                result.ErrorCode!,
                result.ErrorMessage!);
        }

        var value = result.Value!;
        var maxActive = 1;
        if (value.PlanVersionId is Guid planVersionId)
        {
            var planVersion = await plans
                .GetVersionByIdAsync(PlanVersionId.From(planVersionId), cancellationToken)
                .ConfigureAwait(false);
            if (planVersion is not null)
            {
                var plan = await plans.GetByIdAsync(planVersion.PlanId, cancellationToken).ConfigureAwait(false);
                if (plan is not null)
                {
                    maxActive = plan.MaxActiveBusinessTypes;
                }
            }
        }

        var effectiveCount = value.EffectiveBusinessTypeIds.Count;
        var remaining = Math.Max(0, maxActive - effectiveCount);

        var optionIds = value.GrantedBusinessTypeIds
            .Select(id => id.Value)
            .Concat(value.PrimaryBusinessTypeId is { } primary ? [primary.Value] : Array.Empty<Guid>())
            .Distinct()
            .ToList();
        var typeEntities = await businessTypes.GetByIdsAsync(optionIds, cancellationToken).ConfigureAwait(false);
        var byId = typeEntities.ToDictionary(t => t.Id.Value);
        var activated = value.ActivatedBusinessTypeIds.Select(id => id.Value).ToHashSet();
        var effective = value.EffectiveBusinessTypeIds.Select(id => id.Value).ToHashSet();
        var granted = value.GrantedBusinessTypeIds.Select(id => id.Value).ToHashSet();
        var primaryId = value.PrimaryBusinessTypeId?.Value;

        var options = optionIds
            .Select(id =>
            {
                byId.TryGetValue(id, out var entity);
                return new OrganizationBusinessTypeOptionDto(
                    id,
                    entity?.Code ?? value.EffectiveBusinessTypeCodes.GetValueOrDefault(id) ?? id.ToString("D"),
                    entity?.Name ?? entity?.Code ?? id.ToString("D"),
                    primaryId == id,
                    granted.Contains(id) || primaryId == id,
                    activated.Contains(id) || primaryId == id,
                    effective.Contains(id));
            })
            .OrderByDescending(o => o.IsPrimary)
            .ThenBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ApplicationResult<OrganizationBusinessTypeEntitlementDto>.Success(
            new OrganizationBusinessTypeEntitlementDto(
                value.OrganizationId.Value,
                primaryId,
                value.SubscriptionId,
                value.PlanVersionId,
                value.GrantedBusinessTypeIds.Select(id => id.Value).ToList(),
                value.ActivatedBusinessTypeIds.Select(id => id.Value).ToList(),
                value.EffectiveBusinessTypeIds.Select(id => id.Value).ToList(),
                value.EffectiveBusinessTypeCodes.ToDictionary(
                    kv => kv.Key.ToString("D"),
                    kv => kv.Value,
                    StringComparer.OrdinalIgnoreCase),
                maxActive,
                effectiveCount,
                remaining,
                options));
    }
}

public sealed class ActivateOrganizationBusinessType(
    IPlatformOrganizationRepository organizations,
    IOrganizationBusinessTypeEntitlementResolver resolver,
    IOrganizationBusinessTypeActivationRepository activations,
    IBusinessTypeRepository businessTypes,
    IPlanRepository plans,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<OrganizationBusinessTypeActivationDto>> ExecuteAsync(
        Guid organizationId,
        Guid businessTypeId,
        string? productCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PlatformOrganizationId.From(organizationId);
            var btId = BusinessTypeId.From(businessTypeId);
            var organization = await organizations.GetByIdAsync(orgId, cancellationToken).ConfigureAwait(false);
            if (organization is null)
            {
                return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                    ApplicationErrorCodes.OrganizationNotFound,
                    "Organization was not found.");
            }

            var businessType = await businessTypes.GetByIdAsync(btId, cancellationToken).ConfigureAwait(false);
            if (businessType is null)
            {
                return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                    ApplicationErrorCodes.BusinessTypeNotFound,
                    "Business type was not found.");
            }

            if (businessType.Status != BusinessTypeStatus.Active)
            {
                return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                    ApplicationErrorCodes.BusinessTypeInactive,
                    "Inactive or archived business types cannot be newly activated.");
            }

            var product = string.IsNullOrWhiteSpace(productCode)
                ? null
                : ProductCode.Create(productCode);

            ApplicationResult<OrganizationBusinessTypeActivationDto>? lockedResult = null;
            await unitOfWork.ExecuteWithOrganizationLockAsync(
                organizationId,
                async ct =>
                {
                    lockedResult = await ActivateUnderLockAsync(
                            orgId,
                            btId,
                            businessType,
                            organization,
                            product,
                            ct)
                        .ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);

            return lockedResult
                   ?? ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                       ApplicationErrorCodes.DomainViolation,
                       "Business type activation did not complete.");
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException)
        {
            var orgId = PlatformOrganizationId.From(organizationId);
            var btId = BusinessTypeId.From(businessTypeId);
            var existing = await activations.GetAsync(orgId, btId, cancellationToken).ConfigureAwait(false);
            var businessType = await businessTypes.GetByIdAsync(btId, cancellationToken).ConfigureAwait(false);
            if (existing is not null && businessType is not null)
            {
                return ApplicationResult<OrganizationBusinessTypeActivationDto>.Success(
                    new OrganizationBusinessTypeActivationDto(
                        orgId.Value,
                        btId.Value,
                        businessType.Code,
                        existing.ActivatedAtUtc));
            }

            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "A concurrent business type activation conflict occurred.");
        }
    }

    private async Task<ApplicationResult<OrganizationBusinessTypeActivationDto>> ActivateUnderLockAsync(
        PlatformOrganizationId orgId,
        BusinessTypeId btId,
        BusinessType businessType,
        PlatformOrganization organization,
        ProductCode? product,
        CancellationToken cancellationToken)
    {
        var entitlement = await resolver.ResolveAsync(orgId, product, cancellationToken).ConfigureAwait(false);
        if (!entitlement.IsSuccess)
        {
            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                entitlement.ErrorCode!,
                entitlement.ErrorMessage!);
        }

        var granted = entitlement.Value!.GrantedBusinessTypeIds
            .Select(id => id.Value)
            .ToHashSet();
        if (!granted.Contains(btId.Value))
        {
            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                ApplicationErrorCodes.BusinessTypeNotEntitled,
                "Current subscription does not grant this business type.");
        }

        var existingActivation = await activations.GetAsync(orgId, btId, cancellationToken).ConfigureAwait(false);
        if (existingActivation is not null
            || entitlement.Value.EffectiveBusinessTypeIds.Any(id => id == btId))
        {
            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Success(
                new OrganizationBusinessTypeActivationDto(
                    orgId.Value,
                    btId.Value,
                    businessType.Code,
                    existingActivation?.ActivatedAtUtc ?? clock.UtcNow));
        }

        if (entitlement.Value.PlanVersionId is null)
        {
            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "No plan version is bound for business type capacity evaluation.");
        }

        var planVersion = await plans
            .GetVersionByIdAsync(PlanVersionId.From(entitlement.Value.PlanVersionId.Value), cancellationToken)
            .ConfigureAwait(false);
        if (planVersion is null)
        {
            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Bound plan version was not found.");
        }

        var plan = await plans.GetByIdAsync(planVersion.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Current plan was not found.");
        }

        // Recount under the org advisory lock so concurrent activations cannot exceed capacity.
        var effectiveCount = entitlement.Value.EffectiveBusinessTypeIds.Count;
        if (effectiveCount >= plan.MaxActiveBusinessTypes)
        {
            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(
                ApplicationErrorCodes.BusinessTypeActivationCapacityExceeded,
                $"Active business type capacity ({plan.MaxActiveBusinessTypes}) has been reached.");
        }

        var activation = OrganizationBusinessTypeActivation.Activate(
            orgId,
            btId,
            clock.UtcNow,
            organization.PrimaryBusinessTypeId);

        await activations.AddAsync(activation, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ApplicationResult<OrganizationBusinessTypeActivationDto>.Success(
            new OrganizationBusinessTypeActivationDto(
                orgId.Value,
                btId.Value,
                businessType.Code,
                activation.ActivatedAtUtc));
    }
}

public sealed class DeactivateOrganizationBusinessType(
    IPlatformOrganizationRepository organizations,
    IOrganizationBusinessTypeActivationRepository activations,
    IPlatformUnitOfWork unitOfWork)
{
    public async Task<ApplicationResult> ExecuteAsync(
        Guid organizationId,
        Guid businessTypeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PlatformOrganizationId.From(organizationId);
            var btId = BusinessTypeId.From(businessTypeId);
            var organization = await organizations.GetByIdAsync(orgId, cancellationToken).ConfigureAwait(false);
            if (organization is null)
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.OrganizationNotFound,
                    "Organization was not found.");
            }

            if (organization.PrimaryBusinessTypeId is { } primary && primary == btId)
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.BusinessTypePrimaryCannotDeactivate,
                    "The primary business type cannot be deactivated.");
            }

            var existing = await activations.GetAsync(orgId, btId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                // Idempotent: already inactive / never activated (non-primary).
                return ApplicationResult.Success();
            }

            await activations.RemoveAsync(orgId, btId, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult.Success();
        }
        catch (DomainException ex)
        {
            return ApplicationResult.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
