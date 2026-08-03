using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>Tracks Owner/Manager temporary selling mode without changing the real POS role.</summary>
public sealed class SellingModeService
{
    public bool IsSellingMode { get; private set; }

    public string? ReturnRoute { get; private set; }

    public event Func<Task>? Changed;

    public void Enter(string returnRoute)
    {
        IsSellingMode = true;
        ReturnRoute = string.IsNullOrWhiteSpace(returnRoute) ? RoleHomeResolver.OwnerHome : returnRoute;
        _ = NotifyAsync();
    }

    public void Exit()
    {
        IsSellingMode = false;
        ReturnRoute = null;
        _ = NotifyAsync();
    }

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

/// <summary>Resolves the Mobile home route from the effective POS role.</summary>
public sealed class RoleHomeResolver(IPosPermissionClient permissions)
{
    public const string OwnerHome = "/owner";
    public const string ManagerHome = "/manager";
    public const string CashierHome = "/cashier";
    public const string AccessDenied = "/access-denied";
    public const string PersonalHome = "/personal";
    public const string OrgEssentials = "/org";

    public async Task<string> ResolvePosHomeAsync(CancellationToken ct = default)
    {
        var effective = await permissions.GetEffectiveAsync(ct).ConfigureAwait(false);
        if (!effective.IsSuccess || effective.Data is null)
        {
            return AccessDenied;
        }

        var data = effective.Data;
        if (!string.Equals(data.Status, "Active", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(data.Role))
        {
            return AccessDenied;
        }

        if (!PosRoleCodes.TryParse(data.Role, out var role))
        {
            return AccessDenied;
        }

        return role switch
        {
            PosRole.Owner or PosRole.Admin => OwnerHome,
            PosRole.StoreManager => ManagerHome,
            PosRole.Cashier => CashierHome,
            _ => AccessDenied
        };
    }
}
