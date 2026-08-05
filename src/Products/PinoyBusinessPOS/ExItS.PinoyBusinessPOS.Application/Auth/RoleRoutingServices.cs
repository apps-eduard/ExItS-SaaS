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
    ICurrentUserContext currentUser)
{
    public const string OwnerHome = "/owner";
    public const string ManagerHome = "/manager";
    public const string CashierHome = "/cashier";
    public const string AccessDenied = "/access-denied";
    public const string PersonalHome = "/personal";
    public const string OrgEssentials = "/org";

    public async Task<string> ResolvePosHomeAsync(CancellationToken ct = default)
    {
        // Personal Mobile area — never resolve Owner/Manager/Cashier homes without an org bind.
        if (currentUser.Session?.OrganizationId is null)
        {
            return PersonalHome;
        }

        var effective = await permissions.GetEffectiveAsync(ct).ConfigureAwait(false);
        var preferred = sellingMode.PreferredHomeRoute;

        if (!effective.IsSuccess || effective.Data is null)
        {
            // Owner already chose working-as after a successful org bind; don't strand on Access Denied
            // when effective role is briefly unavailable (Dev in-memory Owner / sync lag).
            if (!string.IsNullOrWhiteSpace(preferred))
            {
                return preferred!;
            }

            // Transport / POS API failures must not present as "commercial access denied".
            // Send the user back to org essentials so they can retry Enable POS / check Settings.
            return IsTransientRoleLookupFailure(effective.Status) ? OrgEssentials : AccessDenied;
        }

        var data = effective.Data;
        if (!string.Equals(data.Status, "Active", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(data.Role))
        {
            return !string.IsNullOrWhiteSpace(preferred) ? preferred! : AccessDenied;
        }

        if (!PosRoleCodes.TryParse(data.Role, out var role))
        {
            return !string.IsNullOrWhiteSpace(preferred) ? preferred! : AccessDenied;
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
            _ => !string.IsNullOrWhiteSpace(preferred) ? preferred! : AccessDenied
        };
    }

    private static bool IsTransientRoleLookupFailure(ApiCallStatus status) =>
        status is ApiCallStatus.Offline
            or ApiCallStatus.Unavailable
            or ApiCallStatus.Timeout
            or ApiCallStatus.Failed
            or ApiCallStatus.Cancelled;
}
