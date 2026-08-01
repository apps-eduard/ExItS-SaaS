namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Organization-scoped permission codes for Organization Admin RBAC.
/// Never grants product-local POS/clinical permissions.
/// </summary>
public static class OrganizationPermission
{
    public const string ViewOrganization = "organization.permission.view_organization";
    public const string ManageMembers = "organization.permission.manage_members";
    public const string ManageInvitations = "organization.permission.manage_invitations";
    public const string ManageRoles = "organization.permission.manage_roles";
    public const string ViewCommercial = "organization.permission.view_commercial";
    public const string ManageBranding = "organization.permission.manage_branding";

    public static readonly IReadOnlyList<string> All =
    [
        ViewOrganization,
        ManageMembers,
        ManageInvitations,
        ManageRoles,
        ViewCommercial,
        ManageBranding
    ];

    public static readonly IReadOnlyDictionary<string, string> Descriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ViewOrganization] = "View organization profile and membership directory for the current organization.",
            [ManageMembers] = "Add, suspend, reactivate, and change organization membership roles.",
            [ManageInvitations] = "Create, resend, and revoke organization invitations.",
            [ManageRoles] = "Manage custom organization roles and their assignments.",
            [ViewCommercial] = "View organization subscription, entitlement, and commercial summaries.",
            [ManageBranding] = "Update organization branding settings."
        };
}
