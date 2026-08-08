using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Support;

/// <summary>
/// Selects the Personal or Organization diagnostics provider from the current session.
/// </summary>
public sealed class SupportDiagnosticsService(
    ICurrentUserContext currentUser,
    IEnumerable<ISupportDiagnosticsProvider> providers) : ISupportDiagnosticsService
{
    public async Task<SupportDiagnosticsCaptureResult> CaptureForCurrentSessionAsync(
        CancellationToken ct = default)
    {
        var session = currentUser.Session;
        var provider = ResolveProvider(session);
        if (provider is null || session is null)
        {
            return new SupportDiagnosticsCaptureResult(
                SupportDiagnosticsAccessKind.NotAuthenticated,
                Snapshot: null);
        }

        var access = await provider.EvaluateAccessAsync(session, ct).ConfigureAwait(false);
        if (access != SupportDiagnosticsAccessKind.Allowed)
        {
            return new SupportDiagnosticsCaptureResult(access, Snapshot: null);
        }

        var snapshot = await provider.CaptureAsync(session, ct).ConfigureAwait(false);
        return new SupportDiagnosticsCaptureResult(SupportDiagnosticsAccessKind.Allowed, snapshot);
    }

    public async Task<SupportDiagnosticsAccessKind> EvaluateAccessForCurrentSessionAsync(
        CancellationToken ct = default)
    {
        var session = currentUser.Session;
        var provider = ResolveProvider(session);
        if (provider is null || session is null)
        {
            return SupportDiagnosticsAccessKind.NotAuthenticated;
        }

        return await provider.EvaluateAccessAsync(session, ct).ConfigureAwait(false);
    }

    public async Task RetrySyncForCurrentSessionAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session;
        var provider = ResolveProvider(session);
        if (provider is null || session is null)
        {
            return;
        }

        if (await provider.EvaluateAccessAsync(session, ct).ConfigureAwait(false)
            != SupportDiagnosticsAccessKind.Allowed)
        {
            return;
        }

        await provider.RetrySyncAsync(ct).ConfigureAwait(false);
    }

    public string FormatReport(SupportDiagnosticsSnapshot snapshot) =>
        SupportDiagnosticsReportFormatter.Format(snapshot);

    private ISupportDiagnosticsProvider? ResolveProvider(Auth.AuthSession? session)
    {
        if (session is null)
        {
            return null;
        }

        var want = session.OrganizationId is null
            ? SupportDiagnosticsScope.Personal
            : SupportDiagnosticsScope.Organization;

        return providers.FirstOrDefault(p => p.Scope == want);
    }
}
