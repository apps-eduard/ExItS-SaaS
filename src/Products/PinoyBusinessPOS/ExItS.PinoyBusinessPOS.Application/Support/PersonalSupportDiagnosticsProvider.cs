using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Application.Support;

/// <summary>
/// Personal-scope diagnostics. Never opens or reads an Organization local database.
/// </summary>
public sealed class PersonalSupportDiagnosticsProvider(
    IConnectivityService connectivity,
    IDeviceIdentityProvider deviceIdentity,
    IAppInfoService appInfo,
    ILocalContextManager localContext,
    ILocalPersonalUtangStore personalStore,
    IOfflineOperationQueue queue,
    IOfflineOperatingGrantService offlineGrant,
    IPersonalDiagnosticsSyncRetry syncRetry,
    TimeProvider time) : ISupportDiagnosticsProvider
{
    public SupportDiagnosticsScope Scope => SupportDiagnosticsScope.Personal;

    public Task<SupportDiagnosticsAccessKind> EvaluateAccessAsync(
        AuthSession? session,
        CancellationToken ct = default)
    {
        if (session is null)
        {
            return Task.FromResult(SupportDiagnosticsAccessKind.NotAuthenticated);
        }

        if (!IsPersonalSession(session))
        {
            return Task.FromResult(SupportDiagnosticsAccessKind.Forbidden);
        }

        return Task.FromResult(SupportDiagnosticsAccessKind.Allowed);
    }

    public async Task<SupportDiagnosticsSnapshot> CaptureAsync(
        AuthSession session,
        CancellationToken ct = default)
    {
        var now = time.GetUtcNow();
        if (!IsPersonalSession(session))
        {
            return SupportDiagnosticsSnapshot.EmptyDenied(Scope, now);
        }

        await personalStore.EnsurePersonalContextAsync(ct).ConfigureAwait(false);

        var active = localContext.ActiveContext;
        if (active is null
            || !PersonalLocalScope.IsPersonalContext(active.Identity.OrganizationId, active.Identity.ProductCode)
            || active.Identity.UserId != session.UserId)
        {
            return SupportDiagnosticsSnapshot.EmptyDenied(Scope, now);
        }

        var isConnected = await connectivity.IsConnectedAsync(ct).ConfigureAwait(false);
        string deviceId;
        try
        {
            deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            deviceId = string.Empty;
        }

        OfflineQueueCounts counts;
        DateTimeOffset? lastSynced;
        try
        {
            counts = await queue.GetCountsAsync(ct).ConfigureAwait(false);
            lastSynced = await queue.GetLastSyncedUtcAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            counts = new OfflineQueueCounts(0, 0, 0, 0, 0, 0, 0);
            lastSynced = null;
        }

        var grant = session.UserId != Guid.Empty
            ? await offlineGrant.PeekStoredGrantAsync(session.UserId, ct).ConfigureAwait(false)
            : await offlineGrant.PeekStoredGrantAsync(ct).ConfigureAwait(false);
        if (grant is not null
            && (!grant.IsPersonalScope || grant.UserId != session.UserId))
        {
            grant = null;
        }

        var pinConfigured = await offlineGrant.HasPinConfiguredAsync(session.UserId, ct).ConfigureAwait(false);

        return new SupportDiagnosticsSnapshot(
            CapturedAtUtc: now,
            Scope: Scope,
            ConnectionState: SupportDiagnosticsShared.ConnectionLabel(isConnected),
            DeviceIdShort: SupportDiagnosticsShared.ShortenDeviceId(deviceId),
            AppVersion: appInfo.Version,
            ApiServerStatus: null,
            LocalSchemaVersion: active.SchemaVersion,
            LastSuccessfulSyncUtc: lastSynced,
            PendingSyncCount: SupportDiagnosticsShared.PendingCount(counts),
            FailedSyncCount: SupportDiagnosticsShared.FailedCount(counts),
            OfflineGrantStatus: SupportDiagnosticsShared.DescribeGrantStatus(
                grant, offlineGrant.IsUnlockedThisProcess, now),
            OfflineGrantExpiresAtUtc: grant?.ExpiresAtUtc,
            OfflinePinConfigured: pinConfigured,
            LastServerContactUtc: grant?.LastOnlineValidatedAtUtc,
            UserId: session.UserId,
            PersonalProfileId: session.AccountProfileId,
            OrganizationId: null,
            PublicOrganizationId: null,
            CurrentRole: null,
            OrganizationDisplayName: null);
    }

    public Task RetrySyncAsync(CancellationToken ct = default) =>
        syncRetry.RetryAsync(ct);

    internal static bool IsPersonalSession(AuthSession session) =>
        !session.OrganizationContextLocked
        && session.OrganizationId is null
        && !session.HasPosAccess
        && (string.IsNullOrWhiteSpace(session.AccountClass)
            || string.Equals(session.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase));
}
