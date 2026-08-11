using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
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

public sealed record OrganizationBusinessTypeEntitlementDto(
    Guid OrganizationId,
    Guid? PrimaryBusinessTypeId,
    Guid? SubscriptionId,
    Guid? PlanVersionId,
    IReadOnlyList<Guid> GrantedBusinessTypeIds,
    IReadOnlyList<Guid> ActivatedBusinessTypeIds,
    IReadOnlyList<Guid> EffectiveBusinessTypeIds,
    IReadOnlyDictionary<string, string> EffectiveBusinessTypeCodesById);

public sealed class GetOrganizationBusinessTypeEntitlement(
    IOrganizationBusinessTypeEntitlementResolver resolver)
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

        return ApplicationResult<OrganizationBusinessTypeEntitlementDto>.Success(Map(result.Value!));
    }

    internal static OrganizationBusinessTypeEntitlementDto Map(OrganizationBusinessTypeEntitlement value) =>
        new(
            value.OrganizationId.Value,
            value.PrimaryBusinessTypeId?.Value,
            value.SubscriptionId,
            value.PlanVersionId,
            value.GrantedBusinessTypeIds.Select(id => id.Value).ToList(),
            value.ActivatedBusinessTypeIds.Select(id => id.Value).ToList(),
            value.EffectiveBusinessTypeIds.Select(id => id.Value).ToList(),
            value.EffectiveBusinessTypeCodes.ToDictionary(
                kv => kv.Key.ToString("D"),
                kv => kv.Value,
                StringComparer.OrdinalIgnoreCase));
}

public sealed class ActivateOrganizationBusinessType(
    IPlatformOrganizationRepository organizations,
    IOrganizationBusinessTypeEntitlementResolver resolver,
    IOrganizationBusinessTypeActivationRepository activations,
    IBusinessTypeRepository businessTypes,
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
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationBusinessTypeActivationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeactivateOrganizationBusinessType(
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
            var existing = await activations.GetAsync(orgId, btId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.BusinessTypeActivationNotFound,
                    "Business type activation was not found.");
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
