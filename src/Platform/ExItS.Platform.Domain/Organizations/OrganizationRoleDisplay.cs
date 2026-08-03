namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Business display labels for organization membership roles.
/// Persisted enum values remain <see cref="OrganizationRole.OrganizationMember"/> etc.;
/// WP11 scope displays Owner and Staff only (Member → Staff).
/// </summary>
public static class OrganizationRoleDisplay
{
    public const string Owner = "Owner";
    public const string Staff = "Staff";
    public const string Administrator = "Administrator";

    public static string ToDisplayLabel(OrganizationRole role) => role switch
    {
        OrganizationRole.OrganizationOwner => Owner,
        OrganizationRole.OrganizationMember => Staff,
        OrganizationRole.OrganizationAdministrator => Administrator,
        _ => role.ToString()
    };

    public static string ToDisplayLabel(string? roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return string.Empty;
        }

        return Enum.TryParse<OrganizationRole>(roleCode, ignoreCase: true, out var role)
            ? ToDisplayLabel(role)
            : roleCode;
    }

    /// <summary>Roles offered for Organization staff invite/add/change in WP11 scope.</summary>
    /// <summary>
    /// MVP staff invitation/assignment roles. Organization Owner is unique and created at Start a Business;
    /// it is not an assignable staff role.
    /// </summary>
    public static bool IsAssignableOrganizationStaffRole(OrganizationRole role) =>
        role is OrganizationRole.OrganizationMember;
}
