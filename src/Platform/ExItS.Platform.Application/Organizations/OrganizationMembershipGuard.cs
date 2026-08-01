using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Safety rules for organization membership lifecycle: last governing admin protection
/// and OrganizationOwner assignment restrictions for OrganizationAdministrators.
/// </summary>
public static class OrganizationMembershipGuard
{
    public static bool IsGoverningAdmin(OrganizationRole role) =>
        role is OrganizationRole.OrganizationOwner or OrganizationRole.OrganizationAdministrator;

    public static async Task<ApplicationResult?> EnsureCanRemoveGoverningSeatAsync(
        IOrganizationMembershipRepository memberships,
        OrganizationMembership membership,
        CancellationToken cancellationToken = default)
    {
        if (membership.Status != MembershipStatus.Active || !IsGoverningAdmin(membership.Role))
        {
            return null;
        }

        var count = await memberships
            .CountActiveGoverningAdminsAsync(membership.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (count <= 1)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.LastGoverningAdminProtected,
                "Cannot remove or demote the final Organization Owner/Administrator for this organization.");
        }

        return null;
    }

    public static async Task<ApplicationResult?> EnsureCanChangeRoleAsync(
        IOrganizationMembershipRepository memberships,
        OrganizationMembership membership,
        OrganizationRole newRole,
        OrganizationRole? actorMembershipRole,
        bool actorHasPlatformManageMemberships,
        CancellationToken cancellationToken = default)
    {
        if (!actorHasPlatformManageMemberships
            && actorMembershipRole == OrganizationRole.OrganizationAdministrator
            && newRole == OrganizationRole.OrganizationOwner)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.OrganizationOwnerAssignmentDenied,
                "Organization Administrators cannot assign the OrganizationOwner role.");
        }

        if (membership.Role == newRole)
        {
            return null;
        }

        if (membership.Status == MembershipStatus.Active
            && IsGoverningAdmin(membership.Role)
            && !IsGoverningAdmin(newRole))
        {
            return await EnsureCanRemoveGoverningSeatAsync(memberships, membership, cancellationToken)
                .ConfigureAwait(false);
        }

        return null;
    }
}
