namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Central classification of important POS routes and actions for offline/online UX.
/// Prefer this over ad-hoc page connectivity checks.
/// </summary>
public interface IPosOfflineCapabilityPolicy
{
    /// <summary>Important routes that must have an explicit classification (coverage tests).</summary>
    IReadOnlyDictionary<string, PosConnectivityRequirement> ImportantRoutes { get; }

    /// <summary>Important actions that must have an explicit classification (coverage tests).</summary>
    IReadOnlyDictionary<string, PosConnectivityRequirement> ImportantActions { get; }

    PosConnectivityRequirement GetRouteRequirement(string relativePath);

    PosConnectivityRequirement GetActionRequirement(string actionKey);

    bool RequiresOnlineForRoute(string relativePath);

    bool RequiresOnlineForAction(string actionKey);
}
