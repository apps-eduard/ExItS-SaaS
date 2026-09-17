using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Customer-safe seller branding for documents (Personal linked + org members).
/// Same public fields as B2B public profile — never staff or private admin contacts.
/// </summary>
public sealed class GetOrganizationDocumentPublicIdentity
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly ILinkedCustomerAppUserRepository _links;

    public GetOrganizationDocumentPublicIdentity(
        IPlatformOrganizationRepository organizations,
        IOrganizationMembershipRepository memberships,
        ILinkedCustomerAppUserRepository links)
    {
        _organizations = organizations;
        _memberships = memberships;
        _links = links;
    }

    public async Task<ApplicationResult<OrganizationB2bPublicProfileDto>> ExecuteAsync(
        PlatformUserId actorUserId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PlatformOrganizationId.From(organizationId);

        var membership = await _memberships
            .FindActiveByUserAndOrganizationAsync(actorUserId, orgId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            var link = await _links
                .FindActiveByUserAndOrganizationAsync(actorUserId, orgId, cancellationToken)
                .ConfigureAwait(false);
            if (link is null)
            {
                return ApplicationResult<OrganizationB2bPublicProfileDto>.Failure(
                    ApplicationErrorCodes.OrganizationNotFound,
                    "Organization was not found.");
            }
        }

        var organization = await _organizations.GetByIdAsync(orgId, cancellationToken).ConfigureAwait(false);
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
