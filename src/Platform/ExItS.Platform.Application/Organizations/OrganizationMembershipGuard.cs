using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Safety rules for organization membership lifecycle: last active Owner protection
/// and OrganizationOwner assignment restrictions for non-Owner actors.
/// </summary>
public static class OrganizationMembershipGuard
{
    /// <summary>Organization Owner may manage staff memberships in their own Organization.</summary>
    public static bool CanManageOrganizationStaff(OrganizationRole role) =>
        role == OrganizationRole.OrganizationOwner;

    /// <summary>Last-seat protection applies to Organization Owner only (not legacy Administrator).</summary>
    public static bool IsProtectedGoverningSeat(OrganizationRole role) =>
        role == OrganizationRole.OrganizationOwner;

    [Obsolete("Use CanManageOrganizationStaff or IsProtectedGoverningSeat.")]
    public static bool IsGoverningAdmin(OrganizationRole role) =>
        CanManageOrganizationStaff(role);

    public static async Task<ApplicationResult?> EnsureCanRemoveGoverningSeatAsync(
        IOrganizationMembershipRepository memberships,
        OrganizationMembership membership,
        CancellationToken cancellationToken = default)
    {
        if (membership.Status != MembershipStatus.Active || !IsProtectedGoverningSeat(membership.Role))
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
                "Cannot remove or demote the final Organization Owner for this organization.");
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
        if (!OrganizationRoleDisplay.IsAssignableOrganizationStaffRole(newRole)
            && !actorHasPlatformManageMemberships)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.InvalidOrganizationRole,
                "Organization staff roles are Owner and Staff only.");
        }

        if (!actorHasPlatformManageMemberships
            && actorMembershipRole != OrganizationRole.OrganizationOwner
            && newRole == OrganizationRole.OrganizationOwner)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.OrganizationOwnerAssignmentDenied,
                "Only Organization Owners can assign the Owner role.");
        }

        if (membership.Role == newRole)
        {
            return null;
        }

        if (membership.Status == MembershipStatus.Active
            && IsProtectedGoverningSeat(membership.Role)
            && !IsProtectedGoverningSeat(newRole))
        {
            return await EnsureCanRemoveGoverningSeatAsync(memberships, membership, cancellationToken)
                .ConfigureAwait(false);
        }

        return null;
    }
}
