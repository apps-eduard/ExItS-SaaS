using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationComplianceProfileRepository
{
    Task<OrganizationComplianceProfile?> GetByOrganizationIdAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationComplianceProfile profile,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Combined Organization compliance view. Does not invent TIN or BIR registration fields.
/// Future confirmed requirements extend this DTO only after roadmap confirmation.
/// </summary>
public sealed record OrganizationComplianceProfileDto(
    Guid OrganizationId,
    bool ProfileInitialized,
    DateTimeOffset? ProfileCreatedAtUtc,
    DateTimeOffset? ProfileUpdatedAtUtc,
    string? LegalName,
    string? RegisteredAddressLine1,
    string? RegisteredCity,
    string? RegisteredRegion,
    string? RegisteredPostalCode,
    string? RegisteredCountryCode,
    string ComplianceEligibilityStatus,
    bool TaxDocumentIssuanceEnabled,
    bool TaxDocumentImplementationAvailable,
    string DocumentMode,
    string SnapshotGuidance);

public sealed class EnsureOrganizationComplianceProfile(
    IOrganizationComplianceProfileRepository profiles,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<OrganizationComplianceProfile> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string? actorReference = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await profiles
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var created = OrganizationComplianceProfile.Create(organizationId, clock.UtcNow, actorReference);
        await profiles.AddAsync(created, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }
}

public sealed class GetOrganizationComplianceProfile(
    IOrganizationComplianceProfileRepository profiles,
    IPlatformOrganizationRepository organizations,
    IOrganizationSalesDocumentCapabilityRepository capabilities)
{
    public async Task<ApplicationResult<OrganizationComplianceProfileDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var organization = await organizations
            .GetByIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<OrganizationComplianceProfileDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found.");
        }

        var profile = await profiles
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var orgProfile = organization.Profile;

        return ApplicationResult<OrganizationComplianceProfileDto>.Success(new(
            organizationId.Value,
            ProfileInitialized: profile is not null,
            profile?.CreatedAtUtc,
            profile?.UpdatedAtUtc,
            orgProfile?.LegalName,
            orgProfile?.AddressLine1,
            orgProfile?.City,
            orgProfile?.Region,
            orgProfile?.PostalCode,
            orgProfile?.CountryCode,
            capability?.ComplianceEligibilityStatus
                ?? OrganizationComplianceEligibilityStatuses.NotRequested,
            capability?.TaxDocumentIssuanceEnabled == true,
            TaxDocumentIssuanceRuntime.ImplementationAvailable,
            DocumentMode: "TransactionSummary",
            SnapshotGuidance:
                "Future TaxDocument issuance must snapshot seller compliance facts at issuance time; organization profile changes must not rewrite historical documents."));
    }
}
