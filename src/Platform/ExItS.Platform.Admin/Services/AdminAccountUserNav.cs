using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Account / People / Contacts side-menu definitions by server-validated account class.
/// UI convenience only — never replaces API authorization.
/// </summary>
public static class AdminAccountUserNav
{
    public sealed record Item(
        string Key,
        string LabelKey,
        string? Route,
        bool Implemented,
        bool Authorized);

    /// <summary>Platform Accounts submenu. Unauthorized items omitted (hidden).</summary>
    public static IReadOnlyList<Item> PlatformAccounts(bool canManagePlatformUsers)
    {
        if (!canManagePlatformUsers)
        {
            return [];
        }

        return
        [
            new("all-accounts", "Nav_AllAccounts", "/admin/users", Implemented: true, Authorized: true),
            new("platform-accounts", "Nav_PlatformAccounts", "/admin/users/platform-staff", Implemented: true, Authorized: true),
            new("organization-accounts", "Nav_OrganizationAccounts", "/admin/users/organization", Implemented: true, Authorized: true),
            // Personal directory filter is not implemented without API/directory changes.
            new("personal-accounts", "Nav_PersonalAccounts", Route: null, Implemented: false, Authorized: true),
            new("needs-review", "Nav_NeedsReview", "/admin/users/unassigned", Implemented: true, Authorized: true)
        ];
    }

    /// <summary>Organization People submenu. Unauthorized Owner-only items omitted for members.</summary>
    public static IReadOnlyList<Item> OrganizationPeople(bool isOrganizationOwnerOrAdmin, Guid? selectedOrganizationId)
    {
        var orgId = selectedOrganizationId;
        var staffRoute = orgId is Guid id ? $"/admin/organizations/{id}/members" : null;
        var invitationsRoute = orgId is Guid inviteOrg
            ? $"/admin/organizations/{inviteOrg}/members?tab=invitations"
            : null;

        var items = new List<Item>();

        if (isOrganizationOwnerOrAdmin)
        {
            items.Add(new(
                "org-staff",
                "Nav_OrganizationStaff",
                staffRoute,
                Implemented: true,
                Authorized: true));

            items.Add(new(
                "org-invitations",
                "Nav_Invitations",
                invitationsRoute,
                Implemented: true,
                Authorized: true));
        }

        // Customers / linking: planned; Owners and Members may see Customers (Coming soon).
        // Customer Linking is Owner/Admin-only when shown.
        items.Add(new("org-customers", "Nav_Customers", Route: null, Implemented: false, Authorized: true));

        if (isOrganizationOwnerOrAdmin)
        {
            items.Add(new("org-customer-linking", "Nav_CustomerLinking", Route: null, Implemented: false, Authorized: true));
        }

        return items;
    }

    /// <summary>Personal account/user menu: Contacts only.</summary>
    public static IReadOnlyList<Item> PersonalContacts() =>
    [
        new("personal-contacts", "Nav_Contacts", "/admin/personal/utang/people", Implemented: true, Authorized: true)
    ];

    public static bool CanManagePlatformAccounts(PlatformPermissionState permissions) =>
        permissions.HasPermission(PlatformPermissionCodes.ManagePlatformUsers);

    public static string ScopeLabel(AdminShellMode mode) => mode switch
    {
        AdminShellMode.Platform => "Platform",
        AdminShellMode.Organization => "Organization",
        AdminShellMode.Personal => "Personal",
        _ => "Limited"
    };
}
