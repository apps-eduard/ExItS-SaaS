using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Shared online-required action guard. Preserves the offline session; does not redirect to Reconnect.
/// </summary>
public sealed class OnlineRequiredGuard(IConnectivityService connectivity)
{
    public bool IsDialogVisible { get; private set; }

    public event Func<Task>? Changed;

    /// <summary>
    /// Returns true when online. When offline, shows the shared Internet-required dialog and returns false.
    /// </summary>
    public async Task<bool> EnsureOnlineAsync(CancellationToken ct = default)
    {
        if (await connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return true;
        }

        if (!IsDialogVisible)
        {
            IsDialogVisible = true;
            await RaiseChangedAsync().ConfigureAwait(false);
        }

        return false;
    }

    public async Task DismissAsync()
    {
        if (!IsDialogVisible)
        {
            return;
        }

        IsDialogVisible = false;
        await RaiseChangedAsync().ConfigureAwait(false);
    }

    private Task RaiseChangedAsync()
    {
        var handler = Changed;
        return handler is null ? Task.CompletedTask : handler();
    }
}
