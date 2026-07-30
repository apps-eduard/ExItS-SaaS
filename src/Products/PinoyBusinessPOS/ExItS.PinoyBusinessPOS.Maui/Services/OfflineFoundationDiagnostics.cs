using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Development/Testing diagnostics snapshot for offline foundation. Never includes tokens or payloads.
/// </summary>
public sealed class OfflineFoundationDiagnostics(
    IDeviceIdentityProvider deviceIdentity,
    ILocalContextManager localContext,
    IAppInfoService appInfo)
{
    public bool IsAvailable =>
        string.Equals(appInfo.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase)
        || string.Equals(appInfo.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);

    public async Task<OfflineFoundationDiagnosticsSnapshot> CaptureAsync(CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            return OfflineFoundationDiagnosticsSnapshot.Unavailable;
        }

        string deviceId;
        try
        {
            deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            deviceId = string.Empty;
        }

        var shortened = string.IsNullOrWhiteSpace(deviceId)
            ? "(unavailable)"
            : deviceId.Length <= 8
                ? deviceId
                : $"{deviceId[..8]}…";

        var active = localContext.ActiveContext;
        return new OfflineFoundationDiagnosticsSnapshot(
            Available: true,
            DeviceIdShort: shortened,
            ContextHash: active?.Identity.ContextHash,
            UserId: active?.Identity.UserId.ToString("D"),
            OrganizationId: active?.Identity.OrganizationId.ToString("D"),
            ProductCode: active?.Identity.ProductCode,
            DatabaseFileName: active?.DatabaseFileName,
            SchemaVersion: active?.SchemaVersion,
            InitStatus: active?.Status.ToString() ?? LocalContextInitStatus.NotInitialized.ToString(),
            LastOpenedAtUtc: active?.OpenedAtUtc);
    }
}

public sealed record OfflineFoundationDiagnosticsSnapshot(
    bool Available,
    string? DeviceIdShort = null,
    string? ContextHash = null,
    string? UserId = null,
    string? OrganizationId = null,
    string? ProductCode = null,
    string? DatabaseFileName = null,
    int? SchemaVersion = null,
    string? InitStatus = null,
    DateTimeOffset? LastOpenedAtUtc = null)
{
    public static OfflineFoundationDiagnosticsSnapshot Unavailable { get; } = new(false);
}
