using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Common;

/// <summary>
/// Organization lifecycle authorization: Platform manage/view permissions, or trusted
/// organization Owner for permitted self-service profile/branding.
/// </summary>
internal sealed class PlatformOrganizationAuthz(
    PlatformAuthz authz,
    IOrganizationMembershipRepository memberships,
    IPlatformAuthorizationService authorizationService)
{
    public PlatformAuthz Inner => authz;

    public async Task<IResult?> EnsureCanListOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        if (await HasPlatformPermissionAsync(PlatformPermission.ViewPortfolio, organizationId: null, cancellationToken)
                .ConfigureAwait(false)
            || await HasPlatformPermissionAsync(PlatformPermission.ManageOrganizations, organizationId: null, cancellationToken)
                .ConfigureAwait(false))
        {
            return null;
        }

        return await authz.EnsureAsync(
            PlatformPermission.ViewPortfolio,
            PlatformAuditActions.PlatformAccessChecked,
            nameof(PlatformOrganization),
            "list",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IResult?> EnsureCanViewOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (await HasPlatformPermissionAsync(PlatformPermission.ViewPortfolio, organizationId, cancellationToken)
                .ConfigureAwait(false)
            || await HasPlatformPermissionAsync(PlatformPermission.ManageOrganizations, organizationId, cancellationToken)
                .ConfigureAwait(false)
            || await HasTrustedActiveMembershipAsync(organizationId, governingAdminOnly: false, cancellationToken)
                .ConfigureAwait(false))
        {
            return null;
        }

        return await authz.EnsureAsync(
            PlatformPermission.ViewPortfolio,
            PlatformAuditActions.PlatformAccessChecked,
            nameof(PlatformOrganization),
            organizationId.ToString("D"),
            organizationId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IResult? Denied, bool IsPlatformManager)> EnsureCanEditOrganizationProfileAsync(
        Guid organizationId,
        string actionCode,
        CancellationToken cancellationToken = default)
    {
        var denied = await authz.EnsureAsync(
            PlatformPermission.ManageOrganizations,
            actionCode,
            nameof(PlatformOrganization),
            organizationId.ToString("D"),
            organizationId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (denied is null)
        {
            return (null, true);
        }

        if (await HasTrustedActiveMembershipAsync(organizationId, governingAdminOnly: true, cancellationToken)
                .ConfigureAwait(false))
        {
            return (null, false);
        }

        return (denied, false);
    }

    public Task<IResult?> EnsureCanManageOrganizationLifecycleAsync(
        Guid organizationId,
        string actionCode,
        CancellationToken cancellationToken = default) =>
        authz.EnsureAsync(
            PlatformPermission.ManageOrganizations,
            actionCode,
            nameof(PlatformOrganization),
            organizationId.ToString("D"),
            organizationId,
            cancellationToken: cancellationToken);

    private async Task<bool> HasTrustedActiveMembershipAsync(
        Guid organizationId,
        bool governingAdminOnly,
        CancellationToken cancellationToken)
    {
        var actor = authz.CurrentActor;
        if (actor.PlatformUserId is null
            || actor.OrganizationId is null
            || actor.OrganizationId.Value != organizationId)
        {
            return false;
        }

        var membership = await memberships
            .FindActiveByUserAndOrganizationAsync(
                actor.PlatformUserId,
                PlatformOrganizationId.From(organizationId),
                cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return false;
        }

        return !governingAdminOnly || OrganizationMembershipGuard.CanManageOrganizationStaff(membership.Role);
    }

    public Task<bool> HasPlatformManageOrganizationsAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default) =>
        HasPlatformPermissionAsync(PlatformPermission.ManageOrganizations, organizationId, cancellationToken);

    private async Task<bool> HasPlatformPermissionAsync(
        string permission,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var actor = authz.CurrentActor;
        PlatformOrganizationId? orgId = organizationId.HasValue
            ? PlatformOrganizationId.From(organizationId.Value)
            : null;

        if (actor.PlatformUserId is not null)
        {
            return await authorizationService
                .HasPermissionAsync(actor.PlatformUserId, permission, orgId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (actor.ActorType == Domain.Audit.AuditActorType.DevelopmentOperator)
        {
            var perms = await authorizationService
                .ResolvePermissionsForActorAsync(actor, orgId, cancellationToken)
                .ConfigureAwait(false);
            return perms.Contains(permission);
        }

        return false;
    }
}
