using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed class AddOrganizationMembership
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AddOrganizationMembership(
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations,
        IOrganizationMembershipRepository memberships,
        EnsureAccountProfilesForUser ensureProfiles,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _organizations = organizations;
        _memberships = memberships;
        _ensureProfiles = ensureProfiles;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationMembership>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        OrganizationRole role,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                DomainErrorCodes.OrganizationNotActive,
                "Membership can only be added to an active Platform Organization.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        if (user.Status != AccountStatus.Active)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                DomainErrorCodes.UserNotActive,
                "Membership can only be added for an active Platform User.");
        }

        var existing = await _memberships
            .FindCurrentByUserAndOrganizationAsync(userId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                ApplicationErrorCodes.MembershipConflict,
                "A current membership already exists for this user and organization.");
        }

        try
        {
            var membership = OrganizationMembership.Create(organizationId, userId, role, _clock.UtcNow);
            await _memberships.AddAsync(membership, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _ensureProfiles
                .ExecuteAsync(userId, AccountClass.Organization, exclusivePreferredClass: false, cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<OrganizationMembership>.Success(membership);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationMembership>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
