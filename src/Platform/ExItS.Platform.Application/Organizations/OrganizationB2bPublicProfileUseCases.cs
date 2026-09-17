using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Public/business relationship fields for a Connected seller viewing a buyer organization.
/// Never includes private membership directories or personal user contact data.
/// </summary>
public sealed record OrganizationB2bPublicProfileDto(
    Guid OrganizationId,
    string DisplayName,
    string? PublicOrganizationId,
    string? LogoUrl,
    string? BusinessPhone,
    string? BusinessEmail,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode);

public sealed class GetOrganizationB2bPublicProfile
{
    private readonly IPlatformOrganizationRepository _organizations;

    public GetOrganizationB2bPublicProfile(IPlatformOrganizationRepository organizations)
    {
        _organizations = organizations;
    }

    public async Task<ApplicationResult<OrganizationB2bPublicProfileDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations
            .GetByIdAsync(PlatformOrganizationId.From(organizationId), cancellationToken)
            .ConfigureAwait(false);
        if (organization is null || organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<OrganizationB2bPublicProfileDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found.");
        }

        var profile = organization.Profile;
        return ApplicationResult<OrganizationB2bPublicProfileDto>.Success(
            new OrganizationB2bPublicProfileDto(
                organization.Id.Value,
                organization.DisplayName,
                organization.PublicOrganizationId,
                organization.Branding.LogoUrl,
                profile.ContactPhone,
                profile.ContactEmail,
                profile.AddressLine1,
                profile.AddressLine2,
                profile.City,
                profile.Region,
                profile.PostalCode,
                profile.CountryCode));
    }
}
