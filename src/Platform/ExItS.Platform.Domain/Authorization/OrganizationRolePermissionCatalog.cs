using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Static built-in organization membership role → organization permission map.
/// Custom organization roles store their own permission sets on <see cref="OrganizationRoleDefinition"/>.
/// </summary>
public static class OrganizationRolePermissionCatalog
{
    private static readonly IReadOnlyDictionary<OrganizationRole, IReadOnlySet<string>> RolePermissions =
        new Dictionary<OrganizationRole, IReadOnlySet<string>>
        {
            [OrganizationRole.OrganizationOwner] =
                new HashSet<string>(OrganizationPermission.All, StringComparer.Ordinal),
            [OrganizationRole.OrganizationAdministrator] =
                new HashSet<string>(OrganizationPermission.All, StringComparer.Ordinal),
            [OrganizationRole.OrganizationMember] = new HashSet<string>(
                [
                    OrganizationPermission.ViewOrganization,
                    OrganizationPermission.ViewCommercial
                ],
                StringComparer.Ordinal)
        };

    public static IReadOnlySet<string> GetPermissions(OrganizationRole role)
    {
        if (!RolePermissions.TryGetValue(role, out var permissions))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationRole,
                "Organization role is not defined.");
        }

        return permissions;
    }
}
