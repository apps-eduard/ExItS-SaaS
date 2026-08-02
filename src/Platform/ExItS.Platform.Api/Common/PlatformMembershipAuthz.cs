using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Common;

/// <summary>
/// Membership-management authorization: Platform <see cref="PlatformPermission.ManageMemberships"/>
/// or an active Organization Owner/Administrator in trusted organization context.
/// </summary>
internal sealed class PlatformMembershipAuthz(
    PlatformAuthz authz,
    IOrganizationMembershipRepository memberships,
    IPlatformAuthorizationService authorizationService)
{
    public PlatformAuthz Inner => authz;

    public async Task<IResult?> EnsureCanManageMembershipsAsync(
        string actionCode,
        string targetType,
        string targetId,
        Guid organizationId,
        string? reason = null,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        var denied = await authz.EnsureAsync(
            PlatformPermission.ManageMemberships,
            actionCode,
            targetType,
            targetId,
            organizationId,
            reason: reason,
            summary: summary,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (denied is null)
        {
            return null;
        }

        var actor = authz.CurrentActor;
        if (actor.PlatformUserId is null)
        {
            return denied;
        }

        // Org-admin path requires trusted selected organization context matching the target org.
        if (actor.OrganizationId is null || actor.OrganizationId.Value != organizationId)
        {
            return denied;
        }

        var membership = await memberships
            .FindActiveByUserAndOrganizationAsync(
                actor.PlatformUserId,
                PlatformOrganizationId.From(organizationId),
                cancellationToken)
            .ConfigureAwait(false);
        if (membership is not null && OrganizationMembershipGuard.IsGoverningAdmin(membership.Role))
        {
            return null;
        }

        return denied;
    }

    /// <summary>
    /// Active Organization membership in trusted selected organization context (any role),
    /// or Platform ManageProductAccess / ManageMemberships.
    /// </summary>
    public async Task<IResult?> EnsureActiveOrganizationMemberAsync(
        string actionCode,
        string targetType,
        string targetId,
        Guid organizationId,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        var denied = await authz.EnsureAsync(
            PlatformPermission.ManageProductAccess,
            actionCode,
            targetType,
            targetId,
            organizationId,
            summary: summary,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (denied is null)
        {
            return null;
        }

        var manageDenied = await EnsureCanManageMembershipsAsync(
            actionCode,
            targetType,
            targetId,
            organizationId,
            summary: summary,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (manageDenied is null)
        {
            return null;
        }

        var actor = authz.CurrentActor;
        if (actor.PlatformUserId is null
            || actor.OrganizationId is null
            || actor.OrganizationId.Value != organizationId)
        {
            return denied;
        }

        var membership = await memberships
            .FindActiveByUserAndOrganizationAsync(
                actor.PlatformUserId,
                PlatformOrganizationId.From(organizationId),
                cancellationToken)
            .ConfigureAwait(false);
        return membership is null ? denied : null;
    }

    public async Task<(OrganizationRole? ActorMembershipRole, bool HasPlatformManageMemberships)> ResolveActorMembershipAuthorityAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var actor = authz.CurrentActor;
        var hasPlatform = false;
        if (actor.PlatformUserId is not null)
        {
            hasPlatform = await authorizationService
                .HasPermissionAsync(
                    actor.PlatformUserId,
                    PlatformPermission.ManageMemberships,
                    PlatformOrganizationId.From(organizationId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else if (actor.ActorType == Domain.Audit.AuditActorType.DevelopmentOperator)
        {
            var perms = await authorizationService
                .ResolvePermissionsForActorAsync(actor, PlatformOrganizationId.From(organizationId), cancellationToken)
                .ConfigureAwait(false);
            hasPlatform = perms.Contains(PlatformPermission.ManageMemberships);
        }

        OrganizationRole? role = null;
        if (actor.PlatformUserId is not null)
        {
            var membership = await memberships
                .FindActiveByUserAndOrganizationAsync(
                    actor.PlatformUserId,
                    PlatformOrganizationId.From(organizationId),
                    cancellationToken)
                .ConfigureAwait(false);
            role = membership?.Role;
        }

        return (role, hasPlatform);
    }
}
