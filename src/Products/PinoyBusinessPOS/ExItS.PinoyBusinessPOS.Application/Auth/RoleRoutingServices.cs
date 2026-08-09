using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>
/// Owner working-as / temporary selling mode without changing the real Platform POS role.
/// Preferred home is chosen at organization entry (Owner / Manager / Cashier UI).
/// </summary>
public sealed class SellingModeService
{
    public bool IsSellingMode { get; private set; }

    public string? ReturnRoute { get; private set; }

    /// <summary>Owner-selected Mobile home route while still holding the real Owner POS role.</summary>
    public string? PreferredHomeRoute { get; private set; }

    public event Func<Task>? Changed;

    public void EnterWorkingAs(string homeRoute)
    {
        PreferredHomeRoute = NormalizeHome(homeRoute);
        IsSellingMode = false;
        ReturnRoute = RoleHomeResolver.OwnerHome;
        _ = NotifyAsync();
    }

    public void Enter(string returnRoute)
    {
        IsSellingMode = true;
        ReturnRoute = string.IsNullOrWhiteSpace(returnRoute) ? RoleHomeResolver.OwnerHome : returnRoute;
        PreferredHomeRoute ??= ReturnRoute;
        _ = NotifyAsync();
    }

    public void Exit()
    {
        IsSellingMode = false;
        // Keep PreferredHomeRoute so Owner stays in the chosen working home after a sale.
        ReturnRoute = PreferredHomeRoute ?? RoleHomeResolver.OwnerHome;
        _ = NotifyAsync();
    }

    public void Clear()
    {
        IsSellingMode = false;
        ReturnRoute = null;
        PreferredHomeRoute = null;
        _ = NotifyAsync();
    }

    private static string NormalizeHome(string homeRoute) =>
        homeRoute switch
        {
            RoleHomeResolver.ManagerHome => RoleHomeResolver.ManagerHome,
            RoleHomeResolver.CashierHome => RoleHomeResolver.CashierHome,
            _ => RoleHomeResolver.OwnerHome
        };

    private async Task NotifyAsync()
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            await handler().ConfigureAwait(false);
        }
    }
}

/// <summary>Resolves the Mobile home route from the effective POS role (and Owner working-as preference).</summary>
public sealed class RoleHomeResolver(
    IPosPermissionClient permissions,
    SellingModeService sellingMode,
    ICurrentUserContext currentUser,
    IOfflineOperatingGrantService? offlineGrant = null,
    IConnectivityService? connectivity = null)
{
    public const string OwnerHome = "/owner";
    public const string ManagerHome = "/manager";
    public const string CashierHome = "/cashier";
    public const string AccessDenied = "/access-denied";
    public const string PersonalHome = "/personal";
    public const string OrgEssentials = "/org";

    public async Task<string> ResolvePosHomeAsync(CancellationToken ct = default)
    {
        // Personal default workspace — never treat a stale/forged OrganizationId as org bind.
        if (AuthSessionWorkspace.IsPersonalDefault(currentUser.Session)
            || currentUser.Session?.OrganizationId is null)
        {
            return PersonalHome;
        }

        // Organization Owner membership, subscription and entitlement are separate concepts from
        // POS access assignment. Without confirmed POS access the Home destination is Org essentials.
        if (!currentUser.HasPosAccess)
        {
            return OrgEssentials;
        }

        // Offline PIN unlock / cold start: never block on permissions HTTP. Use grant role snapshot
        // and Owner working-as preference already held in-process.
        if (await ShouldResolveFromOfflineGrantSnapshotAsync(ct).ConfigureAwait(false))
        {
            return ResolveFromOfflineGrantSnapshot();
        }

        var effective = await permissions.GetEffectiveAsync(ct).ConfigureAwait(false);
        var preferred = sellingMode.PreferredHomeRoute;

        if (!effective.IsSuccess || effective.Data is null)
        {
            // Transport / POS API failures must not present as "commercial access denied".
            if (IsTransientRoleLookupFailure(effective.Status))
            {
                // Owner already chose working-as after a successful org bind; don't strand them
                // when effective role is briefly unavailable (Dev in-memory Owner / sync lag).
                return !string.IsNullOrWhiteSpace(preferred) ? preferred! : OrgEssentials;
            }

            // Server-side authorization failure on the role lookup stays a denial.
            return AccessDenied;
        }

        var data = effective.Data;

        // Revoked / suspended assignments are real denials; working-as must not override them.
        if (IsRevokedAssignmentStatus(data.Status))
        {
            return AccessDenied;
        }

        if (!string.Equals(data.Status, "Active", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(data.Role))
        {
            // POS access without an active POS role: no dashboard is authorized, and this is not
            // a commercial denial — Org essentials explains that role assignment is required.
            return OrgEssentials;
        }

        if (!PosRoleCodes.TryParse(data.Role, out var role))
        {
            return !string.IsNullOrWhiteSpace(preferred) ? preferred! : OrgEssentials;
        }

        // Organization Owners may work as Owner, Manager, or Cashier UI without changing the POS grant.
        if (role is PosRole.Owner or PosRole.Admin
            && !string.IsNullOrWhiteSpace(preferred))
        {
            return preferred!;
        }

        return role switch
        {
            PosRole.Owner or PosRole.Admin => OwnerHome,
            PosRole.StoreManager => ManagerHome,
            PosRole.Cashier => CashierHome,
            // Unknown POS roles (InventoryStaff, etc.): keep Owner working-as when already chosen.
            _ => !string.IsNullOrWhiteSpace(preferred) ? preferred! : OrgEssentials
        };
    }

    private static bool IsRevokedAssignmentStatus(string? status) =>
        string.Equals(status, "Revoked", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Suspended", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Denied", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Blocked", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase);

    private static bool IsTransientRoleLookupFailure(ApiCallStatus status) =>
        status is ApiCallStatus.Offline
            or ApiCallStatus.Unavailable
            or ApiCallStatus.Timeout
            or ApiCallStatus.RateLimited
            or ApiCallStatus.Failed
            or ApiCallStatus.Cancelled;

    private async Task<bool> ShouldResolveFromOfflineGrantSnapshotAsync(CancellationToken ct)
    {
        if (offlineGrant is { IsUnlockedThisProcess: true, ActiveUnlockedGrant: not null })
        {
            return true;
        }

        // Connectivity may report online incorrectly on some Debug emulator paths; when it
        // correctly reports offline, skip the permissions round-trip.
        if (connectivity is null)
        {
            return false;
        }

        try
        {
            return !await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Maps the durable offline grant role (and Owner working-as preference) without network I/O.
    /// </summary>
    public string ResolveFromOfflineGrantSnapshot()
    {
        var preferred = sellingMode.PreferredHomeRoute;
        var roleCode = offlineGrant?.ActiveUnlockedGrant?.RoleCode;
        if (!string.IsNullOrWhiteSpace(roleCode) && PosRoleCodes.TryParse(roleCode, out var role))
        {
            if (role is PosRole.Owner or PosRole.Admin && !string.IsNullOrWhiteSpace(preferred))
            {
                return preferred!;
            }

            return role switch
            {
                PosRole.Owner or PosRole.Admin => OwnerHome,
                PosRole.StoreManager => ManagerHome,
                PosRole.Cashier => CashierHome,
                _ => !string.IsNullOrWhiteSpace(preferred) ? preferred! : OwnerHome
            };
        }

        return !string.IsNullOrWhiteSpace(preferred) ? preferred! : OwnerHome;
    }
}
