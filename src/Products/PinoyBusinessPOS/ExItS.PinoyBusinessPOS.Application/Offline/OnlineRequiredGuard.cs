using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Shared online-required action/route guard. Preserves the offline session; does not redirect to Reconnect.
/// Does not replace offline-grant or authorization checks (unreachable ≠ denied).
/// </summary>
public sealed class OnlineRequiredGuard(
    IConnectivityService connectivity,
    IPosOfflineCapabilityPolicy policy)
{
    public bool IsDialogVisible { get; private set; }

    public string DialogTitleKey { get; private set; } = "OnlineRequired_Title";

    public string DialogMessageKey { get; private set; } = "OnlineRequired_Message";

    /// <summary>Optional format argument for message keys that include {0} (e.g. current org name).</summary>
    public string? DialogMessageArg { get; private set; }

    public bool ShowRetryAction { get; private set; } = true;

    public event Func<Task>? Changed;

    /// <summary>
    /// Returns true when online. When offline, shows the shared Internet-required dialog and returns false.
    /// </summary>
    public Task<bool> EnsureOnlineAsync(CancellationToken ct = default) =>
        EnsureOnlineCoreAsync(
            "OnlineRequired_Title",
            "OnlineRequired_Message",
            messageArg: null,
            showRetry: true,
            ct);

    public async Task<bool> EnsureOnlineForRouteAsync(string relativePath, CancellationToken ct = default)
    {
        if (!policy.RequiresOnlineForRoute(relativePath))
        {
            return true;
        }

        return await EnsureOnlineAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> EnsureOnlineForActionAsync(
        string actionKey,
        string? currentOrganizationDisplayName = null,
        CancellationToken ct = default)
    {
        if (!policy.RequiresOnlineForAction(actionKey))
        {
            return true;
        }

        if (string.Equals(actionKey, PosOfflineActionKeys.SwitchOrganization, StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionKey, PosOfflineActionKeys.SwitchToPersonal, StringComparison.OrdinalIgnoreCase))
        {
            var org = string.IsNullOrWhiteSpace(currentOrganizationDisplayName)
                ? "this organization"
                : currentOrganizationDisplayName.Trim();
            return await EnsureOnlineCoreAsync(
                    "OnlineRequired_Title",
                    "OnlineRequired_OrgSwitchMessage",
                    org,
                    showRetry: true,
                    ct)
                .ConfigureAwait(false);
        }

        return await EnsureOnlineAsync(ct).ConfigureAwait(false);
    }

    public async Task DismissAsync()
    {
        if (!IsDialogVisible)
        {
            return;
        }

        IsDialogVisible = false;
        DialogMessageArg = null;
        await RaiseChangedAsync().ConfigureAwait(false);
    }

    /// <summary>Re-checks connectivity. Dismisses the dialog when the network is back.</summary>
    public async Task<bool> RetryConnectionAsync(CancellationToken ct = default)
    {
        if (await connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            await DismissAsync().ConfigureAwait(false);
            return true;
        }

        // Keep dialog visible; caller may refresh sync status separately.
        await RaiseChangedAsync().ConfigureAwait(false);
        return false;
    }

    private async Task<bool> EnsureOnlineCoreAsync(
        string titleKey,
        string messageKey,
        string? messageArg,
        bool showRetry,
        CancellationToken ct)
    {
        if (await connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return true;
        }

        DialogTitleKey = titleKey;
        DialogMessageKey = messageKey;
        DialogMessageArg = messageArg;
        ShowRetryAction = showRetry;
        IsDialogVisible = true;
        await RaiseChangedAsync().ConfigureAwait(false);
        return false;
    }

    private Task RaiseChangedAsync()
    {
        var handler = Changed;
        return handler is null ? Task.CompletedTask : handler();
    }
}
