using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public static class SalesDocumentCapabilityStatuses
{
    public const string NotEnabled = "NotEnabled";
}

public sealed record OrganizationSalesDocumentCapabilityDto(
    Guid OrganizationId,
    bool TransactionSummaryAvailable,
    string TaxDocumentIssuanceStatus,
    bool TaxDocumentIssuanceEnabled);

public interface IOrganizationSalesDocumentCapabilityRepository
{
    Task<OrganizationSalesDocumentCapability?> GetByOrganizationIdAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationSalesDocumentCapability capability,
        CancellationToken cancellationToken = default);
}

public sealed class GetOrganizationSalesDocumentCapability(
    IOrganizationSalesDocumentCapabilityRepository capabilities)
{
    public async Task<ApplicationResult<OrganizationSalesDocumentCapabilityDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<OrganizationSalesDocumentCapabilityDto>.Success(ToDto(organizationId, capability));
    }

    internal static OrganizationSalesDocumentCapabilityDto ToDto(
        PlatformOrganizationId organizationId,
        OrganizationSalesDocumentCapability? capability)
    {
        var enabled = capability?.TaxDocumentIssuanceEnabled == true;
        return new(
            organizationId.Value,
            TransactionSummaryAvailable: true,
            enabled ? "Enabled" : SalesDocumentCapabilityStatuses.NotEnabled,
            enabled);
    }
}

public sealed class EnsureOrganizationSalesDocumentCapability(
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<OrganizationSalesDocumentCapability> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (capability is not null)
        {
            return capability;
        }

        capability = OrganizationSalesDocumentCapability.CreateDefault(organizationId, clock.UtcNow);
        await capabilities.AddAsync(capability, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return capability;
    }
}

public sealed class EnsureTaxDocumentIssuanceAllowed(
    IOrganizationSalesDocumentCapabilityRepository capabilities)
{
    public async Task<ApplicationResult> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        return capability?.TaxDocumentIssuanceEnabled == true
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(
                ApplicationErrorCodes.TaxDocumentIssuanceNotEnabled,
                "Tax-document issuance is not available for this organization.");
    }
}

public sealed class RequestTaxDocumentIssuance(EnsureTaxDocumentIssuanceAllowed ensureAllowed)
{
    public Task<ApplicationResult> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        ensureAllowed.ExecuteAsync(organizationId, cancellationToken);
}
