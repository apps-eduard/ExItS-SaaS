namespace ExItS.PinoyBusinessPOS.Application.Support;

/// <summary>Which local scope a diagnostics snapshot describes.</summary>
public enum SupportDiagnosticsScope
{
    Personal = 0,
    Organization = 1
}

/// <summary>Result of an access check before capturing diagnostics.</summary>
public enum SupportDiagnosticsAccessKind
{
    Allowed = 0,
    NotAuthenticated = 1,
    Forbidden = 2,
    WrongScope = 3
}

/// <summary>
/// Safe, device-local support snapshot. Never includes tokens, PIN verifiers, keys, or payloads.
/// </summary>
public sealed record SupportDiagnosticsSnapshot(
    DateTimeOffset CapturedAtUtc,
    SupportDiagnosticsScope Scope,
    string ConnectionState,
    string DeviceIdShort,
    string AppVersion,
    string? ApiServerStatus,
    int? LocalSchemaVersion,
    DateTimeOffset? LastSuccessfulSyncUtc,
    int PendingSyncCount,
    int FailedSyncCount,
    string OfflineGrantStatus,
    DateTimeOffset? OfflineGrantExpiresAtUtc,
    bool OfflinePinConfigured,
    DateTimeOffset? LastServerContactUtc,
    Guid UserId,
    Guid? PersonalProfileId,
    Guid? OrganizationId,
    string? PublicOrganizationId,
    string? CurrentRole,
    string? OrganizationDisplayName)
{
    public static SupportDiagnosticsSnapshot EmptyDenied(
        SupportDiagnosticsScope scope,
        DateTimeOffset utcNow) =>
        new(
            CapturedAtUtc: utcNow,
            Scope: scope,
            ConnectionState: "Unknown",
            DeviceIdShort: "(unavailable)",
            AppVersion: string.Empty,
            ApiServerStatus: null,
            LocalSchemaVersion: null,
            LastSuccessfulSyncUtc: null,
            PendingSyncCount: 0,
            FailedSyncCount: 0,
            OfflineGrantStatus: "Unavailable",
            OfflineGrantExpiresAtUtc: null,
            OfflinePinConfigured: false,
            LastServerContactUtc: null,
            UserId: Guid.Empty,
            PersonalProfileId: null,
            OrganizationId: null,
            PublicOrganizationId: null,
            CurrentRole: null,
            OrganizationDisplayName: null);
}

public sealed record SupportDiagnosticsCaptureResult(
    SupportDiagnosticsAccessKind Access,
    SupportDiagnosticsSnapshot? Snapshot);

/// <summary>Formats a support-safe plain-text report (no secrets / payloads).</summary>
public static class SupportDiagnosticsReportFormatter
{
    public static string Format(SupportDiagnosticsSnapshot s)
    {
        var lines = new List<string>
        {
            "ExItS Support Diagnostics",
            $"Captured (UTC): {s.CapturedAtUtc:O}",
            $"Scope: {s.Scope}",
            $"Connection: {s.ConnectionState}",
            $"Device ID: {s.DeviceIdShort}",
            $"App version: {s.AppVersion}",
            $"API/server status: {s.ApiServerStatus ?? "—"}",
            $"Local DB schema: {s.LocalSchemaVersion?.ToString() ?? "—"}",
            $"Last successful sync (UTC): {FormatTime(s.LastSuccessfulSyncUtc)}",
            $"Pending sync: {s.PendingSyncCount}",
            $"Failed sync: {s.FailedSyncCount}",
            $"Offline grant: {s.OfflineGrantStatus}",
            $"Offline grant expiry (UTC): {FormatTime(s.OfflineGrantExpiresAtUtc)}",
            $"Offline PIN configured: {(s.OfflinePinConfigured ? "Yes" : "No")}",
            $"Last server contact (UTC): {FormatTime(s.LastServerContactUtc)}",
            $"User ID: {s.UserId:D}",
        };

        if (s.Scope == SupportDiagnosticsScope.Personal)
        {
            lines.Add($"Personal profile ID: {s.PersonalProfileId?.ToString("D") ?? "—"}");
        }
        else
        {
            lines.Add($"Organization ID: {s.OrganizationId?.ToString("D") ?? "—"}");
            lines.Add($"Public organization ID: {s.PublicOrganizationId ?? "—"}");
            lines.Add($"Organization name: {s.OrganizationDisplayName ?? "—"}");
            lines.Add($"Current role: {s.CurrentRole ?? "—"}");
        }

        lines.Add(string.Empty);
        lines.Add("This report omits credentials, tokens, PIN material, encryption keys, and transaction contents.");
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatTime(DateTimeOffset? value) =>
        value is null ? "—" : value.Value.ToString("O");
}
