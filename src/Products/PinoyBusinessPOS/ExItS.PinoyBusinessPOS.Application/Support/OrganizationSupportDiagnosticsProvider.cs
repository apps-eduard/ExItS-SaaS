using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Application.Support;

/// <summary>
/// Organization-scope diagnostics for the current org only. Owner-class access required.
/// Never opens Personal local DB or another organization's context.
/// </summary>
public sealed class OrganizationSupportDiagnosticsProvider(
    IConnectivityService connectivity,
    IDeviceIdentityProvider deviceIdentity,
    IAppInfoService appInfo,
    ILocalContextManager localContext,
    IOfflineOperationQueue queue,
    IOfflineOperatingGrantService offlineGrant,
    IOrganizationDiagnosticsSyncRetry syncRetry,
    IOrganizationOwnerProbe ownerProbe,
    ISupportDiagnosticsRoleReader roleReader,
    TimeProvider time) : ISupportDiagnosticsProvider
{
    public SupportDiagnosticsScope Scope => SupportDiagnosticsScope.Organization;

    public async Task<SupportDiagnosticsAccessKind> EvaluateAccessAsync(
        AuthSession? session,
        CancellationToken ct = default)
    {
        if (session is null)
        {
            return SupportDiagnosticsAccessKind.NotAuthenticated;
        }

        if (session.OrganizationId is null)
        {
            return SupportDiagnosticsAccessKind.WrongScope;
        }

        return await ownerProbe.IsOwnerAsync(session, session.OrganizationId.Value, ct)
            .ConfigureAwait(false)
            ? SupportDiagnosticsAccessKind.Allowed
            : SupportDiagnosticsAccessKind.Forbidden;
    }

    public async Task<SupportDiagnosticsSnapshot> CaptureAsync(
        AuthSession session,
        CancellationToken ct = default)
    {
        var now = time.GetUtcNow();
        if (session.OrganizationId is not Guid orgId)
        {
            return SupportDiagnosticsSnapshot.EmptyDenied(Scope, now);
        }

        if (!await ownerProbe.IsOwnerAsync(session, orgId, ct).ConfigureAwait(false))
        {
            return SupportDiagnosticsSnapshot.EmptyDenied(Scope, now);
        }

        var role = await roleReader.GetCurrentRoleLabelAsync(ct).ConfigureAwait(false);
        var active = localContext.ActiveContext;
        if (active is null
            || PersonalLocalScope.IsPersonalContext(active.Identity.OrganizationId, active.Identity.ProductCode)
            || active.Identity.OrganizationId != orgId
            || active.Identity.UserId != session.UserId)
        {
            return await BuildSnapshotAsync(
                    session,
                    orgId,
                    schemaVersion: null,
                    counts: new OfflineQueueCounts(0, 0, 0, 0, 0, 0, 0),
                    lastSynced: null,
                    role,
                    now,
                    ct)
                .ConfigureAwait(false);
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

        return await BuildSnapshotAsync(
                session,
                orgId,
                active.SchemaVersion,
                counts,
                lastSynced,
                role,
                now,
                ct)
            .ConfigureAwait(false);
    }

    public Task RetrySyncAsync(CancellationToken ct = default) =>
        syncRetry.RetryAsync(ct);

    private async Task<SupportDiagnosticsSnapshot> BuildSnapshotAsync(
        AuthSession session,
        Guid orgId,
        int? schemaVersion,
        OfflineQueueCounts counts,
        DateTimeOffset? lastSynced,
        string? role,
        DateTimeOffset now,
        CancellationToken ct)
    {
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

        var grant = session.UserId != Guid.Empty
            ? await offlineGrant.PeekStoredGrantAsync(session.UserId, ct).ConfigureAwait(false)
            : await offlineGrant.PeekStoredGrantAsync(ct).ConfigureAwait(false);
        if (grant is not null
            && (!grant.IsOrganizationScope
                || grant.OrganizationId != orgId
                || grant.UserId != session.UserId))
        {
            grant = null;
        }

        var pinConfigured = await offlineGrant.HasPinConfiguredAsync(session.UserId, ct).ConfigureAwait(false);
        var publicOrgId = SupportDiagnosticsPublicIds.TryExtractPublicOrganizationId(session.Username)
            ?? SupportDiagnosticsPublicIds.TryExtractPublicOrganizationId(session.Email);

        return new SupportDiagnosticsSnapshot(
            CapturedAtUtc: now,
            Scope: Scope,
            ConnectionState: SupportDiagnosticsShared.ConnectionLabel(isConnected),
            DeviceIdShort: SupportDiagnosticsShared.ShortenDeviceId(deviceId),
            AppVersion: appInfo.Version,
            ApiServerStatus: null,
            LocalSchemaVersion: schemaVersion,
            LastSuccessfulSyncUtc: lastSynced,
            PendingSyncCount: SupportDiagnosticsShared.PendingCount(counts),
            FailedSyncCount: SupportDiagnosticsShared.FailedCount(counts),
            OfflineGrantStatus: SupportDiagnosticsShared.DescribeGrantStatus(
                grant, offlineGrant.IsUnlockedThisProcess, now),
            OfflineGrantExpiresAtUtc: grant?.ExpiresAtUtc,
            OfflinePinConfigured: pinConfigured,
            LastServerContactUtc: grant?.LastOnlineValidatedAtUtc,
            UserId: session.UserId,
            PersonalProfileId: null,
            OrganizationId: orgId,
            PublicOrganizationId: publicOrgId,
            CurrentRole: role,
            OrganizationDisplayName: session.OrganizationDisplayName);
    }
}
