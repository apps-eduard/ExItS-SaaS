using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Application.Support;

/// <summary>
/// Scope-specific diagnostics capture. Implementations must never read the other scope's local DB.
/// </summary>
public interface ISupportDiagnosticsProvider
{
    SupportDiagnosticsScope Scope { get; }

    Task<SupportDiagnosticsAccessKind> EvaluateAccessAsync(
        AuthSession? session,
        CancellationToken ct = default);

    Task<SupportDiagnosticsSnapshot> CaptureAsync(
        AuthSession session,
        CancellationToken ct = default);

    /// <summary>Retries sync for this scope only via existing sync services.</summary>
    Task RetrySyncAsync(CancellationToken ct = default);
}

/// <summary>
/// Facade that selects Personal vs Organization provider and enforces access before capture.
/// </summary>
public interface ISupportDiagnosticsService
{
    Task<SupportDiagnosticsCaptureResult> CaptureForCurrentSessionAsync(CancellationToken ct = default);

    Task<SupportDiagnosticsAccessKind> EvaluateAccessForCurrentSessionAsync(CancellationToken ct = default);

    Task RetrySyncForCurrentSessionAsync(CancellationToken ct = default);

    string FormatReport(SupportDiagnosticsSnapshot snapshot);
}
