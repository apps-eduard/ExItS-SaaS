namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Connectivity classification for POS routes and actions.
/// Authorization / offline-grant checks remain separate and must not be weakened by this policy.
/// </summary>
public enum PosConnectivityRequirement
{
    /// <summary>Safe to use while offline (local cache / local PIN / static UI).</summary>
    OfflineCapable = 0,

    /// <summary>May mutate locally and sync later via outbox when supported.</summary>
    Queueable = 1,

    /// <summary>Requires a live server; show shared Internet-required dialog when offline.</summary>
    OnlineRequired = 2
}
