namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Coarse network reachability state for the PinoyBusinessPOS client. <see cref="Unknown"/>
/// is the initial state before the first connectivity check has completed.
/// </summary>
public enum ConnectivityStatus
{
    Unknown,
    Online,
    Offline
}
