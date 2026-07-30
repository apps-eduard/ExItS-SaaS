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

    public Task<bool> IsConnectedAsync(CancellationToken ct = default) =>
        Task.FromResult(Connectivity.Current.NetworkAccess == NetworkAccess.Internet);

    private void OnPlatformConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var status = e.NetworkAccess == NetworkAccess.Internet ? ConnectivityStatus.Online : ConnectivityStatus.Offline;
        ConnectivityChanged?.Invoke(this, status);
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnPlatformConnectivityChanged;
    }
}
