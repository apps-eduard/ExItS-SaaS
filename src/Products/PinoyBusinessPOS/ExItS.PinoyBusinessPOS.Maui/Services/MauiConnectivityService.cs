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
    /// Local Validation Debug (emulator + PhysicalDevice/Tailscale) often reports
    /// <see cref="NetworkAccess.None"/> or captive Wi‑Fi with "!" while host APIs on
    /// <c>100.x</c> / <c>10.0.2.2</c> remain reachable. Treating that as Offline hid Test users
    /// and blocked Sign in even when Platform/POS APIs were healthy.
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
        // Debug Local Validation: prefer attempting API calls; real offline still fails at HttpClient.
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
