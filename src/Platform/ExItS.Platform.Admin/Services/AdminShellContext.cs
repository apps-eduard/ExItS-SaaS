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
    private readonly object _gate = new();
    private Task? _loadTask;
    private int _loadGeneration;

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

    /// <summary>
    /// Fired when <see cref="Loaded"/> / shell identity fields change.
    /// AdminNav must re-sync <c>_ready</c> from this — Permissions.Changed alone is not enough
    /// when shell recovers after an earlier failed nav load (header Platform, sidenav Spin+Retry).
    /// </summary>
    public event Action? Changed;

    public Task EnsureLoadedAsync()
    {
        lock (_gate)
        {
            // Reuse an in-flight load, or a completed successful load. Never reuse a completed
            // failure (Loaded=false) — that permanently collapsed AdminNav after hard refresh.
            if (_loadTask is not null && (Loaded || !_loadTask.IsCompleted))
            {
                return _loadTask;
            }

            _loadTask = StartLoadLocked();
            return _loadTask;
        }
    }

    public async Task RefreshAsync()
    {
        Task load;
        lock (_gate)
        {
            Loaded = false;
            _loadTask = StartLoadLocked();
            load = _loadTask;
        }

        RaiseChanged();
        await load;
    }

    private Task StartLoadLocked()
    {
        var generation = ++_loadGeneration;
        return LoadAsync(generation);
    }

    private async Task LoadAsync(int generation)
    {
        try
        {
            await LoadCoreAsync(generation);
        }
        catch
        {
            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            Mode = AdminShellMode.Limited;
            Loaded = false;
            ClearTaskIfCurrent(generation);
            RaiseChanged();
            throw;
        }
    }

    private async Task LoadCoreAsync(int generation)
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
        if (!IsCurrentGeneration(generation))
        {
            return;
        }

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
        else
        {
            // /auth/me can fail (deserialization/transient) while other Platform APIs succeed with the
            // same session. Recover shell mode from authorization/me instead of leaving Loaded=false
            // (which stuck AdminNav on Spin+Retry and left catalog pages spinning).
            var recovered = await TryRecoverFromAuthorizationAsync(generation);
            if (recovered || !IsCurrentGeneration(generation))
            {
                return;
            }

            Mode = AdminShellMode.Limited;
            RoleLabel = "Signed in";
            Loaded = false;
            ClearTaskIfCurrent(generation);
            RaiseChanged();
            return;
        }

        var isOrganizationAccount = string.Equals(accountClass, "Organization", StringComparison.OrdinalIgnoreCase);
        var isPlatformAccount = string.Equals(accountClass, "Platform", StringComparison.OrdinalIgnoreCase);
        var isPersonalAccount = string.Equals(accountClass, "Personal", StringComparison.OrdinalIgnoreCase);

        if (isOrganizationAccount)
        {
            var orgs = await api.GetEligibleOrganizationsAsync();
            if (!IsCurrentGeneration(generation))
            {
                return;
            }

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

        if (!IsCurrentGeneration(generation))
        {
            return;
        }

        ApplyLoadedState(
            displayName,
            username,
            accountClass,
            allowedScope,
            selectedOrgId,
            selectedOrgName,
            membershipRole,
            orgCount,
            isPlatformAccount,
            isOrganizationAccount,
            isPersonalAccount);
    }

    private async Task<bool> TryRecoverFromAuthorizationAsync(int generation)
    {
        var authz = await api.GetMyAuthorizationAsync();
        if (!IsCurrentGeneration(generation))
        {
            return true;
        }

        if (!authz.IsSuccess || authz.Data is null)
        {
            return false;
        }

        var actorType = authz.Data.ActorType?.Trim();
        var isPlatform = string.Equals(actorType, "Platform", StringComparison.OrdinalIgnoreCase);
        var isOrganization = string.Equals(actorType, "Organization", StringComparison.OrdinalIgnoreCase);
        var isPersonal = string.Equals(actorType, "Personal", StringComparison.OrdinalIgnoreCase);
        if (!isPlatform && !isOrganization && !isPersonal)
        {
            return false;
        }

        if (isPlatform)
        {
            await permissions.RefreshAsync();
        }
        else
        {
            await permissions.EnsureLoadedForNonPlatformAsync();
        }

        if (!IsCurrentGeneration(generation))
        {
            return true;
        }

        ApplyLoadedState(
            displayName: authz.Data.ActorIdentifier,
            username: authz.Data.ActorIdentifier,
            accountClass: actorType,
            allowedScope: actorType,
            selectedOrgId: authz.Data.OrganizationId,
            selectedOrgName: null,
            membershipRole: null,
            orgCount: authz.Data.OrganizationId is null ? 0 : 1,
            isPlatformAccount: isPlatform,
            isOrganizationAccount: isOrganization,
            isPersonalAccount: isPersonal);
        return true;
    }

    private void ApplyLoadedState(
        string? displayName,
        string? username,
        string? accountClass,
        string? allowedScope,
        Guid? selectedOrgId,
        string? selectedOrgName,
        string? membershipRole,
        int orgCount,
        bool isPlatformAccount,
        bool isOrganizationAccount,
        bool isPersonalAccount)
    {
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
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        Changed?.Invoke();
    }

    private bool IsCurrentGeneration(int generation)
    {
        lock (_gate)
        {
            return generation == _loadGeneration;
        }
    }

    private void ClearTaskIfCurrent(int generation)
    {
        lock (_gate)
        {
            if (generation == _loadGeneration)
            {
                _loadTask = null;
            }
        }
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
            return "Platform Billing Administrator";
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
        "OrganizationMember" => "Organization Staff",
        "OrganizationAdministrator" => "Organization Staff",
        _ => null
    };
}
