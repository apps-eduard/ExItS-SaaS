using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Authorization;

/// <summary>
/// Resolves Platform system role permissions for a Platform User. Platform grants product access;
/// this service governs only Platform Admin operational permissions
/// (docs/engineering/authorization-matrix.md) and never product-local (clinical/POS) permissions.
/// </summary>
public interface IPlatformAuthorizationService
{
    /// <summary>
    /// Union of permissions across all active role assignments applicable to <paramref name="organizationId"/>
    /// (platform-wide assignments always apply; organization-scoped assignments apply only to a matching organization).
    /// </summary>
    Task<IReadOnlySet<string>> ResolvePermissionsAsync(
        PlatformUserId userId,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves permissions for an actor context. PlatformUser actors use role assignments.
    /// DevelopmentOperator actors receive all permissions only when
    /// <see cref="DevelopmentAuthorizationOptions.GrantDevelopmentOperatorFullAccess"/> is enabled
    /// (Development/Testing only — not production authentication).
    /// </summary>
    Task<IReadOnlySet<string>> ResolvePermissionsForActorAsync(
        PlatformActorContext actor,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(
        PlatformUserId userId,
        string permission,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a Denied result (<see cref="DomainErrorCodes.AuthorizationDenied"/>) when the permission is missing.</summary>
    Task<ApplicationResult> EnsurePermissionAsync(
        PlatformUserId userId,
        string permission,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult> EnsurePermissionForActorAsync(
        PlatformActorContext actor,
        string permission,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default);
}

public sealed class PlatformAuthorizationService : IPlatformAuthorizationService
{
    private readonly IPlatformRoleAssignmentRepository _roleAssignments;
    private readonly IPlatformCustomRoleAssignmentRepository _customAssignments;
    private readonly IPlatformRoleDefinitionRepository _roleDefinitions;
    private readonly DevelopmentAuthorizationOptions _developmentOptions;

    public PlatformAuthorizationService(
        IPlatformRoleAssignmentRepository roleAssignments,
        IPlatformCustomRoleAssignmentRepository customAssignments,
        IPlatformRoleDefinitionRepository roleDefinitions,
        IOptions<DevelopmentAuthorizationOptions> developmentOptions)
    {
        _roleAssignments = roleAssignments;
        _customAssignments = customAssignments;
        _roleDefinitions = roleDefinitions;
        _developmentOptions = developmentOptions.Value;
    }

    public async Task<IReadOnlySet<string>> ResolvePermissionsAsync(
        PlatformUserId userId,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _roleAssignments.ListActiveByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var assignment in assignments)
        {
            var appliesToScope = assignment.OrganizationId is null
                || (organizationId is not null && assignment.OrganizationId == organizationId);
            if (!appliesToScope)
            {
                continue;
            }

            foreach (var permission in PlatformRolePermissionCatalog.GetPermissions(assignment.Role))
            {
                permissions.Add(permission);
            }
        }

        var customAssignments = await _customAssignments
            .ListActiveByUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        foreach (var assignment in customAssignments)
        {
            var definition = await _roleDefinitions
                .GetByIdAsync(assignment.RoleDefinitionId, cancellationToken)
                .ConfigureAwait(false);
            if (definition is null || definition.Status != PlatformRoleLifecycleStatus.Active)
            {
                continue;
            }

            foreach (var permission in definition.Permissions)
            {
                permissions.Add(permission);
            }
        }

        return permissions;
    }

    public Task<IReadOnlySet<string>> ResolvePermissionsForActorAsync(
        PlatformActorContext actor,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.ActorType == AuditActorType.DevelopmentOperator
            && _developmentOptions.GrantDevelopmentOperatorFullAccess)
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(PlatformPermission.All, StringComparer.Ordinal));
        }

        if (actor.ActorType != AuditActorType.PlatformUser || actor.PlatformUserId is null)
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
        }

        return ResolvePermissionsAsync(actor.PlatformUserId, organizationId, cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(
        PlatformUserId userId,
        string permission,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureKnownPermission(permission);
        var permissions = await ResolvePermissionsAsync(userId, organizationId, cancellationToken).ConfigureAwait(false);
        return permissions.Contains(permission);
    }

    public async Task<ApplicationResult> EnsurePermissionAsync(
        PlatformUserId userId,
        string permission,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var granted = await HasPermissionAsync(userId, permission, organizationId, cancellationToken).ConfigureAwait(false);
        return granted
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(
                DomainErrorCodes.AuthorizationDenied,
                organizationId is null
                    ? $"Platform User does not hold permission '{permission}'."
                    : $"Platform User does not hold permission '{permission}' for organization '{organizationId.Value}'.");
    }

    public async Task<ApplicationResult> EnsurePermissionForActorAsync(
        PlatformActorContext actor,
        string permission,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        EnsureKnownPermission(permission);
        var permissions = await ResolvePermissionsForActorAsync(actor, organizationId, cancellationToken)
            .ConfigureAwait(false);
        return permissions.Contains(permission)
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(
                DomainErrorCodes.AuthorizationDenied,
                organizationId is null
                    ? $"Actor '{actor.ActorIdentifier}' does not hold permission '{permission}'."
                    : $"Actor '{actor.ActorIdentifier}' does not hold permission '{permission}' for organization '{organizationId.Value}'.");
    }

    private static void EnsureKnownPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission) || !PlatformPermission.All.Contains(permission, StringComparer.Ordinal))
        {
            throw new DomainException(DomainErrorCodes.InvalidPermissionCode, "Permission code is not recognized.");
        }
    }
}
