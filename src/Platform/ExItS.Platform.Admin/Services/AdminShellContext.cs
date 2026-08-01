using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// UI shell mode derived from Platform permissions and organization memberships.
/// Convenience only — never replaces server-side authorization.
/// </summary>
public enum AdminShellMode
{
    /// <summary>No platform permissions and no org-admin membership.</summary>
    Limited,

    /// <summary>Holds one or more Platform permission codes → Platform Admin menu.</summary>
    Platform,

    /// <summary>Org Owner/Administrator membership without Platform permissions → Organization Admin menu.</summary>
    Organization
}

/// <summary>
/// Circuit-scoped identity + shell mode for the Ant Design Admin chrome.
/// </summary>
public sealed class AdminShellContext(
    IPlatformApiClient api,
    PlatformPermissionState permissions)
{
    private Task? _loadTask;

    public bool Loaded { get; private set; }
    public AdminShellMode Mode { get; private set; } = AdminShellMode.Limited;
    public string? DisplayName { get; private set; }
    public string? Username { get; private set; }
    public string RoleLabel { get; private set; } = "—";
    public Guid? SelectedOrganizationId { get; private set; }
    public string? SelectedOrganizationName { get; private set; }
    public string? MembershipRole { get; private set; }
    public int OrganizationCount { get; private set; }
    public bool HasMultipleOrganizations => OrganizationCount > 1;

    public bool IsPlatformShell => Mode == AdminShellMode.Platform;
    public bool IsOrganizationShell => Mode == AdminShellMode.Organization;

    public Task EnsureLoadedAsync()
    {
        _loadTask ??= LoadAsync();
        return _loadTask;
    }

    public async Task RefreshAsync()
    {
        _loadTask = LoadAsync();
        await _loadTask;
    }

    private async Task LoadAsync()
    {
        // Blazor circuit-scoped: keep awaits on the sync context when mutating shell state.
        await permissions.EnsureLoadedAsync();

        string? displayName = null;
        string? username = null;
        Guid? selectedOrgId = null;
        string? selectedOrgName = null;
        var orgCount = 0;
        string? membershipRole = null;

        var me = await api.GetAuthMeAsync();
        if (me.IsSuccess && me.Data is not null)
        {
            displayName = me.Data.DisplayName;
            username = me.Data.Username;
            selectedOrgId = me.Data.SelectedOrganizationId;
            selectedOrgName = me.Data.SelectedOrganizationDisplayName;
            orgCount = me.Data.ActiveOrganizationCount;
        }

        var orgs = await api.GetEligibleOrganizationsAsync();
        if (orgs.IsSuccess && orgs.Data is not null)
        {
            var list = orgs.Data;
            orgCount = Math.Max(orgCount, list.Count);
            if (selectedOrgId is Guid sid)
            {
                var match = list.FirstOrDefault(o => o.OrganizationId == sid);
                if (match is not null)
                {
                    membershipRole = match.MembershipRole;
                    selectedOrgName ??= match.DisplayName;
                }
            }
            else if (list.Count == 1)
            {
                membershipRole = list[0].MembershipRole;
                selectedOrgId = list[0].OrganizationId;
                selectedOrgName = list[0].DisplayName;
            }
        }

        DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName;
        Username = username;
        SelectedOrganizationId = selectedOrgId;
        SelectedOrganizationName = selectedOrgName;
        MembershipRole = membershipRole;
        OrganizationCount = orgCount;

        var isPlatform = permissions.HasAnyPermission(
            PlatformPermissionCodes.ViewPortfolio,
            PlatformPermissionCodes.ManageOrganizations,
            PlatformPermissionCodes.ManageCatalog,
            PlatformPermissionCodes.ManagePlatformUsers,
            PlatformPermissionCodes.ManageMemberships,
            PlatformPermissionCodes.ManageProductAccess,
            PlatformPermissionCodes.ManageSubscriptions,
            PlatformPermissionCodes.ManageManualPayments,
            PlatformPermissionCodes.ManageEntitlementOverrides,
            PlatformPermissionCodes.ViewAuditRecords);

        var isOrgAdminMembership = IsOrgAdminRole(membershipRole)
            || (orgs.IsSuccess && orgs.Data is not null
                && orgs.Data.Any(o => IsOrgAdminRole(o.MembershipRole)));

        if (isPlatform)
        {
            Mode = AdminShellMode.Platform;
            RoleLabel = ResolvePlatformRoleLabel();
        }
        else if (isOrgAdminMembership)
        {
            Mode = AdminShellMode.Organization;
            RoleLabel = FormatMembershipRole(membershipRole) ?? "Organization Administrator";
        }
        else
        {
            Mode = AdminShellMode.Limited;
            RoleLabel = FormatMembershipRole(membershipRole) ?? "Signed in";
        }

        Loaded = true;
    }

    private string ResolvePlatformRoleLabel()
    {
        if (permissions.HasPermission(PlatformPermissionCodes.ManagePlatformUsers)
            && permissions.HasPermission(PlatformPermissionCodes.ManageEntitlementOverrides))
        {
            return "Platform Administrator";
        }

        if (permissions.HasPermission(PlatformPermissionCodes.ManageSubscriptions)
            && permissions.HasPermission(PlatformPermissionCodes.ManageManualPayments)
            && !permissions.HasPermission(PlatformPermissionCodes.ManagePlatformUsers))
        {
            return "Billing Administrator";
        }

        if (permissions.HasPermission(PlatformPermissionCodes.ManageMemberships)
            && permissions.HasPermission(PlatformPermissionCodes.ManageProductAccess)
            && !permissions.HasPermission(PlatformPermissionCodes.ManageSubscriptions))
        {
            return "Platform Support";
        }

        return "Platform operator";
    }

    private static bool IsOrgAdminRole(string? role) =>
        string.Equals(role, "OrganizationOwner", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "OrganizationAdministrator", StringComparison.OrdinalIgnoreCase);

    private static string? FormatMembershipRole(string? role) => role switch
    {
        "OrganizationOwner" => "Organization Owner",
        "OrganizationAdministrator" => "Organization Administrator",
        "OrganizationMember" => "Organization Member",
        _ => null
    };
}
