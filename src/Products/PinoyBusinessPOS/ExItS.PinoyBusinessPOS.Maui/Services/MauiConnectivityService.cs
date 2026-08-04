using ExItS.PinoyBusinessPOS.Application.Abstractions;
using Microsoft.Maui.Networking;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// MAUI implementation of <see cref="IConnectivityService"/> wrapping
/// <see cref="Microsoft.Maui.Networking.Connectivity"/>. Reports coarse reachability only —
/// "connected" means the OS reports a usable network interface, not that the POS API itself is
/// reachable (that distinction is left to <c>IPosApiClient</c> call outcomes).
/// </summary>
public sealed class MauiConnectivityService : IConnectivityService, IDisposable
{
    public event EventHandler<ConnectivityStatus>? ConnectivityChanged;

    public MauiConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += OnPlatformConnectivityChanged;
    }

    public Task<bool> IsConnectedAsync(CancellationToken ct = default)
    {
        return Task.FromResult(IsUsablyConnected(Connectivity.Current.NetworkAccess));
    }

    private void OnPlatformConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var status = IsUsablyConnected(e.NetworkAccess)
            ? ConnectivityStatus.Online
            : ConnectivityStatus.Offline;
        ConnectivityChanged?.Invoke(this, status);
    }

    /// <summary>
    /// Emulator Local Validation often reports <see cref="NetworkAccess.None"/> even when
    /// <c>adb reverse</c> makes <c>127.0.0.1:8091/8092</c> reachable. Short-circuiting to
    /// Offline then blocks every catalog/ops call while the host APIs are healthy.
    /// </summary>
    private static bool IsUsablyConnected(NetworkAccess access)
    {
        if (access is NetworkAccess.Internet
            or NetworkAccess.ConstrainedInternet
            or NetworkAccess.Local
            or NetworkAccess.Unknown)
        {
            return true;
        }

#if DEBUG
        // Debug APKs target Local Validation (emulator reverse or PhysicalDevice Tailscale).
        // Prefer attempting the HTTP call over trusting Android's coarse NetworkAccess=None.
        return access is NetworkAccess.None;
#else
        return false;
#endif
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnPlatformConnectivityChanged;
    }
}
