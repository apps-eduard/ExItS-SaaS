using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// UI shell mode derived from the server-validated account class (and org membership for labels).
/// Convenience only — never replaces server-side authorization.
/// </summary>
public enum AdminShellMode
{
    /// <summary>No usable account class / unsigned shell chrome.</summary>
    Limited,

    /// <summary>Platform account class → Platform Administration menu.</summary>
    Platform,

    /// <summary>Organization account class → Organization Administration menu.</summary>
    Organization,

    /// <summary>Personal account class → Personal Scope menu.</summary>
    Personal
}

/// <summary>
/// Circuit-scoped identity + shell mode for the Ant Design Admin chrome.
/// Shell mode follows AccountClass from <c>GET /api/v1/platform/auth/me</c>, not selected-org UI state
/// and not Platform permission elevation.
/// </summary>
public sealed class AdminShellContext(
    IPlatformApiClient api,
    PlatformPermissionState permissions)
{
    private Task? _loadTask;

    public bool Loaded { get; private set; }
    public AdminShellMode Mode { get; private set; } = AdminShellMode.Limited;
    public string? AccountClass { get; private set; }
    public string? AllowedScope { get; private set; }
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
    public bool IsPersonalShell => Mode == AdminShellMode.Personal;

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
        string? displayName = null;
        string? username = null;
        Guid? selectedOrgId = null;
        string? selectedOrgName = null;
        var orgCount = 0;
        string? membershipRole = null;
        string? accountClass = null;
        string? allowedScope = null;

        var me = await api.GetAuthMeAsync();
        if (me.IsSuccess && me.Data is not null)
        {
            displayName = me.Data.DisplayName;
            username = me.Data.Username;
            selectedOrgId = me.Data.SelectedOrganizationId;
            selectedOrgName = me.Data.SelectedOrganizationDisplayName;
            orgCount = me.Data.ActiveOrganizationCount;
            accountClass = me.Data.AccountClass;
            allowedScope = me.Data.AllowedScope;
        }

        var isOrganizationAccount = string.Equals(accountClass, "Organization", StringComparison.OrdinalIgnoreCase);
        var isPlatformAccount = string.Equals(accountClass, "Platform", StringComparison.OrdinalIgnoreCase);
        var isPersonalAccount = string.Equals(accountClass, "Personal", StringComparison.OrdinalIgnoreCase);

        ApiCallResult<IReadOnlyList<EligibleOrganizationDto>>? orgs = null;
        if (isOrganizationAccount)
        {
            orgs = await api.GetEligibleOrganizationsAsync();
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
        }

        // Platform permission codes are only meaningful for Platform sessions.
        if (isPlatformAccount)
        {
            await permissions.EnsureLoadedAsync();
        }
        else
        {
            await permissions.EnsureLoadedForNonPlatformAsync();
        }

        DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName;
        Username = username;
        AccountClass = accountClass;
        AllowedScope = allowedScope;
        SelectedOrganizationId = selectedOrgId;
        SelectedOrganizationName = selectedOrgName;
        MembershipRole = membershipRole;
        OrganizationCount = orgCount;

        if (isPlatformAccount)
        {
            Mode = AdminShellMode.Platform;
            RoleLabel = ResolvePlatformRoleLabel();
        }
        else if (isOrganizationAccount)
        {
            Mode = AdminShellMode.Organization;
            RoleLabel = FormatMembershipRole(membershipRole) ?? "Organization";
        }
        else if (isPersonalAccount)
        {
            Mode = AdminShellMode.Personal;
            RoleLabel = "Personal";
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

    private static string? FormatMembershipRole(string? role) => role switch
    {
        "OrganizationOwner" => "Organization Owner",
        "OrganizationAdministrator" => "Organization Administrator",
        "OrganizationMember" => "Organization Member",
        _ => null
    };
}
