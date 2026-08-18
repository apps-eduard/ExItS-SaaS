namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Platform-agnostic abstraction over device network reachability. MAUI implementations
/// typically wrap <c>Microsoft.Maui.Networking.IConnectivity</c>; test/host implementations
/// can stub this without depending on MAUI.
/// </summary>
public interface IConnectivityService
{
    /// <summary>Checks current network reachability without relying on cached state.</summary>
    Task<bool> IsConnectedAsync(CancellationToken ct = default);

    /// <summary>
    /// True when the OS reports no network interface (airplane / radios off).
    /// Debug Local Validation may still return true from <see cref="IsConnectedAsync"/> for
    /// <c>NetworkAccess.None</c> so password sign-in and session restore can attempt Tailscale/LAN
    /// APIs. Offline PIN shortcuts must use this instead of inverting <see cref="IsConnectedAsync"/>.
    /// Default: inverse of <see cref="IsConnectedAsync"/> (hosts/tests without an OS radio).
    /// </summary>
    Task<bool> HasNoNetworkInterfaceAsync(CancellationToken ct = default)
        => InvertConnectedAsync(ct);

    /// <summary>Raised whenever the device's connectivity state transitions.</summary>
    event EventHandler<ConnectivityStatus>? ConnectivityChanged;

    private async Task<bool> InvertConnectedAsync(CancellationToken ct) =>
        !await IsConnectedAsync(ct).ConfigureAwait(false);
}
