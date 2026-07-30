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

    /// <summary>Raised whenever the device's connectivity state transitions.</summary>
    event EventHandler<ConnectivityStatus>? ConnectivityChanged;
}
