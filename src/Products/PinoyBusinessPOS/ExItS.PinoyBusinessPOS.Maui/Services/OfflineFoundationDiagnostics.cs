using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Development/Testing diagnostics for offline foundation + queue. Never includes tokens or payloads.
/// </summary>
public sealed class OfflineFoundationDiagnostics(
    IDeviceIdentityProvider deviceIdentity,
    ILocalContextManager localContext,
    IOfflineOperationQueue queue,
    ILocalCustomerCreditStore customerCreditStore,
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
        OfflineQueueCounts counts;
        DateTimeOffset? lastSynced = null;
        IReadOnlyList<OfflineOperationEnvelope> sample = [];
        LocalEntityStateCounts entityCounts = LocalEntityStateCounts.Empty;
        try
        {
            counts = await queue.GetCountsAsync(ct).ConfigureAwait(false);
            lastSynced = await queue.GetLastSyncedUtcAsync(ct).ConfigureAwait(false);
            sample = await queue.ListSafeMetadataAsync(5, ct).ConfigureAwait(false);
            entityCounts = await customerCreditStore.GetEntityStateCountsAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            counts = new OfflineQueueCounts(0, 0, 0, 0, 0, 0, 0);
        }

        var claimed = sample.FirstOrDefault(o => o.QueueState == OfflineQueueState.Syncing);
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
            LastOpenedAtUtc: active?.OpenedAtUtc,
            Pending: counts.Pending,
            Syncing: counts.Syncing,
            Succeeded: counts.Succeeded,
            RetryableFailure: counts.RetryableFailure,
            PermanentFailure: counts.PermanentFailure,
            Conflict: counts.Conflict,
            BlockedByAccess: counts.BlockedByAccess,
            ClaimedOperationIdShort: claimed is null ? null : claimed.OperationId.ToString("N")[..8] + "…",
            ClaimedAttemptCount: claimed?.AttemptCount,
            ClaimedNextAttemptUtc: claimed?.NextAttemptUtc,
            LastSyncedAtUtc: lastSynced,
            SampleFailureCode: sample.FirstOrDefault(o => o.FailureCode is not null)?.FailureCode,
            LocalCustomerEntityCounts: entityCounts.Customers,
            LocalCreditEntityCounts: entityCounts.Credits,
            LocalRepaymentEntityCounts: entityCounts.Repayments);
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
    DateTimeOffset? LastOpenedAtUtc = null,
    int Pending = 0,
    int Syncing = 0,
    int Succeeded = 0,
    int RetryableFailure = 0,
    int PermanentFailure = 0,
    int Conflict = 0,
    int BlockedByAccess = 0,
    string? ClaimedOperationIdShort = null,
    int? ClaimedAttemptCount = null,
    DateTimeOffset? ClaimedNextAttemptUtc = null,
    DateTimeOffset? LastSyncedAtUtc = null,
    string? SampleFailureCode = null,
    IReadOnlyDictionary<LocalEntitySyncState, int>? LocalCustomerEntityCounts = null,
    IReadOnlyDictionary<LocalEntitySyncState, int>? LocalCreditEntityCounts = null,
    IReadOnlyDictionary<LocalEntitySyncState, int>? LocalRepaymentEntityCounts = null)
{
    public static OfflineFoundationDiagnosticsSnapshot Unavailable { get; } = new(false);
}
