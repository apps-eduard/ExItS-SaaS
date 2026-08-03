using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Static role-to-permission catalog matching the authorization matrix intent
/// (docs/engineering/authorization-matrix.md). Platform Administrator does not automatically
/// receive unrestricted clinical or POS operational access; break-glass is deferred.
/// </summary>
public static class PlatformRolePermissionCatalog
{
    private static readonly IReadOnlyDictionary<PlatformSystemRole, IReadOnlySet<string>> RolePermissions =
        new Dictionary<PlatformSystemRole, IReadOnlySet<string>>
        {
            [PlatformSystemRole.PlatformAdministrator] =
                new HashSet<string>(PlatformPermission.All, StringComparer.Ordinal),

            [PlatformSystemRole.BillingAdministrator] = new HashSet<string>(
                [
                    PlatformPermission.ViewPortfolio,
                    PlatformPermission.ManageOrganizations,
                    PlatformPermission.ManageSubscriptions,
                    PlatformPermission.ManageManualPayments,
                    PlatformPermission.ViewAuditRecords
                ],
                StringComparer.Ordinal),

            // Support can view/manage memberships & product access for support operations;
            // not payments activation, not organization or subscription management.
            [PlatformSystemRole.PlatformSupport] = new HashSet<string>(
                [
                    PlatformPermission.ViewPortfolio,
                    PlatformPermission.ManageMemberships,
                    PlatformPermission.ManageProductAccess,
                    PlatformPermission.ViewAuditRecords
                ],
                StringComparer.Ordinal),

            [PlatformSystemRole.PlatformAuditor] = new HashSet<string>(
                [
                    PlatformPermission.ViewPortfolio,
                    PlatformPermission.ViewAuditRecords
                ],
                StringComparer.Ordinal)
        };

    public static IReadOnlySet<string> GetPermissions(PlatformSystemRole role)
    {
        if (!RolePermissions.TryGetValue(role, out var permissions))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformSystemRole,
                "Platform system role is not defined.");
        }

        return permissions;
    }

    public static bool RoleHasPermission(PlatformSystemRole role, string permission) =>
        GetPermissions(role).Contains(permission);
}
