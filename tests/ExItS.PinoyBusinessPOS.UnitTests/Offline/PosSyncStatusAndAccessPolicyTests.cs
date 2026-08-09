using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class PosSyncStatusAndAccessPolicyTests
{
    [Fact]
    public async Task Sync_status_maps_online_and_offline_only_in_wp01()
    {
        var connectivity = new FakeConnectivity(online: true);
        var current = new CurrentUserContext();
        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();
        var sync = new PosSyncStatusService(connectivity, policy);
        await sync.InitializeAsync();

        Assert.Equal(PosSyncStatusKind.Online, sync.Current.Kind);
        Assert.Null(sync.Current.PendingCount);
        Assert.Null(sync.Current.LastSyncedAtUtc);

        connectivity.SetOnline(false);
        await Task.Delay(10);
        Assert.Equal(PosSyncStatusKind.Offline, sync.Current.Kind);
        Assert.NotEqual(PosSyncStatusKind.PendingSync, sync.Current.Kind);
        Assert.NotEqual(PosSyncStatusKind.Syncing, sync.Current.Kind);
        Assert.NotEqual(PosSyncStatusKind.SyncFailed, sync.Current.Kind);
        Assert.NotEqual(PosSyncStatusKind.LastSynced, sync.Current.Kind);
    }

    [Fact]
    public async Task Reconnect_required_when_authenticated_offline()
    {
        var connectivity = new FakeConnectivity(online: false);
        var current = new CurrentUserContext();
        current.Set(new AuthSession(
            Guid.NewGuid(), "User", "user", "u@example.com",
            Guid.NewGuid(), "Org",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: true, AccessReasonCode: "allowed"));

        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();

        Assert.True(policy.RequiresReconnectToVerifyAccess);
        Assert.False(policy.CanEnterProtectedShell);

        var sync = new PosSyncStatusService(connectivity, policy);
        await sync.InitializeAsync();
        Assert.Equal(PosSyncStatusKind.ReconnectRequired, sync.Current.Kind);
    }

    [Fact]
    public async Task Protected_shell_requires_online_validated_access()
    {
        var connectivity = new FakeConnectivity(online: true);
        var current = new CurrentUserContext();
        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();
        Assert.False(policy.CanEnterProtectedShell);

        current.Set(new AuthSession(
            Guid.NewGuid(), "User", "user", "u@example.com",
            Guid.NewGuid(), "Org",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: true, AccessReasonCode: "allowed"));

        Assert.True(policy.CanEnterProtectedShell);
        Assert.False(policy.RequiresReconnectToVerifyAccess);
    }

    [Fact]
    public void NotifySessionAccessChanged_rearms_shell_after_clear_without_initialize()
    {
        // Quick Login → Owner: SelectOrganization clears process validation, then bind succeeds
        // before NavigationGate/Initialize has run — Notify must re-arm CanEnterProtectedShell.
        var connectivity = new FakeConnectivity(online: true);
        var current = new CurrentUserContext();
        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        policy.ClearProcessValidation();

        current.Set(new AuthSession(
            Guid.NewGuid(), "User", "user", "u@example.com",
            Guid.NewGuid(), "Org",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: true, AccessReasonCode: "allowed"));

        Assert.False(policy.CanEnterProtectedShell);

        policy.NotifySessionAccessChanged();

        Assert.True(policy.CanEnterProtectedShell);
        Assert.False(policy.RequiresReconnectToVerifyAccess);
    }

    [Fact]
    public async Task Continuous_session_allows_offline_shell_after_online_validation()
    {
        var connectivity = new FakeConnectivity(online: true);
        var current = new CurrentUserContext();
        current.Set(new AuthSession(
            Guid.NewGuid(), "User", "user", "u@example.com",
            Guid.NewGuid(), "Org",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: true, AccessReasonCode: "allowed"));

        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();

        Assert.True(policy.CanEnterProtectedShell);
        Assert.True(policy.AllowsOfflineMutation);
        Assert.False(policy.RequiresReconnectToVerifyAccess);

        connectivity.SetOnline(false);
        await Task.Delay(10);

        Assert.True(policy.CanEnterProtectedShell);
        Assert.True(policy.AllowsOfflineMutation);
        Assert.False(policy.RequiresReconnectToVerifyAccess);
    }

    [Fact]
    public async Task RecoveryRequired_when_queue_has_Conflict()
    {
        var sync = await CreateSyncWithQueueAsync(new OfflineQueueCounts(0, 0, 0, 0, 0, Conflict: 1, 0));
        Assert.Equal(PosSyncStatusKind.RecoveryRequired, sync.Current.Kind);
        Assert.Equal("SyncStatus_RecoveryRequired", sync.Current.SafeDetailKey);
    }

    [Fact]
    public async Task RecoveryRequired_when_queue_has_PermanentFailure()
    {
        var sync = await CreateSyncWithQueueAsync(new OfflineQueueCounts(0, 0, 0, 0, PermanentFailure: 2, 0, 0));
        Assert.Equal(PosSyncStatusKind.RecoveryRequired, sync.Current.Kind);
        Assert.Equal("SyncStatus_RecoveryRequired", sync.Current.SafeDetailKey);
    }

    [Fact]
    public async Task RecoveryRequired_when_queue_has_BlockedByAccess()
    {
        var sync = await CreateSyncWithQueueAsync(new OfflineQueueCounts(0, 0, 0, 0, 0, 0, BlockedByAccess: 1));
        Assert.Equal(PosSyncStatusKind.RecoveryRequired, sync.Current.Kind);
        Assert.Equal("SyncStatus_RecoveryRequired", sync.Current.SafeDetailKey);
    }

    [Fact]
    public async Task RecoveryRequired_via_SetRecoveryRequired_uses_KeyUnavailable_detail()
    {
        var connectivity = new FakeConnectivity(online: true);
        var current = new CurrentUserContext();
        current.Set(CreateSession());
        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();
        var sync = new PosSyncStatusService(connectivity, policy, new FakeQueue(new OfflineQueueCounts(0, 0, 0, 0, 0, 0, 0)));
        await sync.InitializeAsync();

        Assert.Equal(PosSyncStatusKind.Online, sync.Current.Kind);

        sync.SetRecoveryRequired(true);
        await Task.Delay(20);

        Assert.Equal(PosSyncStatusKind.RecoveryRequired, sync.Current.Kind);
        Assert.Equal("SyncStatus_KeyUnavailable", sync.Current.SafeDetailKey);
    }

    [Fact]
    public async Task SyncFailed_for_RetryableFailure_only()
    {
        var sync = await CreateSyncWithQueueAsync(new OfflineQueueCounts(0, 0, 0, RetryableFailure: 3, 0, 0, 0));
        Assert.Equal(PosSyncStatusKind.SyncFailed, sync.Current.Kind);
        Assert.Equal("SyncStatus_Failed", sync.Current.SafeDetailKey);
        Assert.Equal(3, sync.Current.PendingCount);
    }

    [Fact]
    public async Task PendingSync_when_Pending_only()
    {
        var sync = await CreateSyncWithQueueAsync(new OfflineQueueCounts(Pending: 2, 0, 0, 0, 0, 0, 0));
        Assert.Equal(PosSyncStatusKind.PendingSync, sync.Current.Kind);
        Assert.Equal(2, sync.Current.PendingCount);
    }

    [Fact]
    public void PendingSyncDisplay_excludes_BlockedByAccess()
    {
        var counts = new OfflineQueueCounts(Pending: 2, 0, 0, RetryableFailure: 1, 0, 0, BlockedByAccess: 5);
        Assert.Equal(3, counts.PendingSyncDisplay);
        Assert.Equal(8, counts.UnsyncedWork);
    }

    [Fact]
    public async Task Reconnect_still_wins_over_RecoveryRequired()
    {
        var connectivity = new FakeConnectivity(online: true);
        var current = new CurrentUserContext();
        current.Set(CreateSession());
        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();
        var queue = new FakeQueue(new OfflineQueueCounts(0, 0, 0, 0, PermanentFailure: 1, Conflict: 1, BlockedByAccess: 1));
        var sync = new PosSyncStatusService(connectivity, policy, queue);
        await sync.InitializeAsync();

        Assert.Equal(PosSyncStatusKind.RecoveryRequired, sync.Current.Kind);

        sync.SetReconnectRequired(true);
        await Task.Delay(20);

        Assert.Equal(PosSyncStatusKind.ReconnectRequired, sync.Current.Kind);
    }

    [Fact]
    public async Task Reconnect_wins_over_SetRecoveryRequired_key_flag()
    {
        var connectivity = new FakeConnectivity(online: true);
        var current = new CurrentUserContext();
        current.Set(CreateSession());
        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();
        var sync = new PosSyncStatusService(connectivity, policy);
        await sync.InitializeAsync();

        sync.SetRecoveryRequired(true);
        await Task.Delay(20);
        Assert.Equal(PosSyncStatusKind.RecoveryRequired, sync.Current.Kind);

        sync.SetReconnectRequired(true);
        await Task.Delay(20);
        Assert.Equal(PosSyncStatusKind.ReconnectRequired, sync.Current.Kind);
    }

    private static async Task<PosSyncStatusService> CreateSyncWithQueueAsync(OfflineQueueCounts counts)
    {
        var connectivity = new FakeConnectivity(online: true);
        var current = new CurrentUserContext();
        current.Set(CreateSession());
        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();
        var sync = new PosSyncStatusService(connectivity, policy, new FakeQueue(counts));
        await sync.InitializeAsync();
        return sync;
    }

    private static AuthSession CreateSession() =>
        new(
            Guid.NewGuid(), "User", "user", "u@example.com",
            Guid.NewGuid(), "Org",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: true, AccessReasonCode: "allowed");

    private sealed class FakeConnectivity(bool online) : IConnectivityService
    {
        private bool _online = online;

        public event EventHandler<ConnectivityStatus>? ConnectivityChanged;

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(_online);

        public void SetOnline(bool value)
        {
            _online = value;
            ConnectivityChanged?.Invoke(this, value ? ConnectivityStatus.Online : ConnectivityStatus.Offline);
        }
    }

    private sealed class FakeQueue(OfflineQueueCounts counts) : IOfflineOperationQueue
    {
        public Task EnqueueAsync(OfflineEnqueueRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task RecoverAbandonedSyncingAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task ReclaimBlockedByAccessAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task ReclaimFailedForManualRetryAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<OfflineOperationEnvelope?> TryClaimNextAsync(string claimToken, CancellationToken ct = default) =>
            Task.FromResult<OfflineOperationEnvelope?>(null);

        public Task MarkSucceededAsync(Guid operationId, string? serverReference, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task MarkFailureAsync(
            Guid operationId,
            OfflineFailureClass failureClass,
            string failureCode,
            string? failureSummary,
            DateTimeOffset? nextAttemptUtc,
            int attemptCount,
            CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<OfflineQueueCounts> GetCountsAsync(CancellationToken ct = default) =>
            Task.FromResult(counts);

        public Task<IReadOnlyList<OfflineOperationEnvelope>> ListSafeMetadataAsync(int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OfflineOperationEnvelope>>([]);

        public Task<bool> HasUnsyncedWorkAsync(CancellationToken ct = default) =>
            Task.FromResult(counts.UnsyncedWork > 0);

        public Task SetLastSyncedUtcAsync(DateTimeOffset utc, CancellationToken ct = default) => Task.CompletedTask;

        public Task<DateTimeOffset?> GetLastSyncedUtcAsync(CancellationToken ct = default) =>
            Task.FromResult<DateTimeOffset?>(null);

        public Task<(OfflineOperationEnvelope Envelope, EncryptedPayload Encrypted)?> TryLoadEncryptedAsync(
            Guid operationId,
            CancellationToken ct = default) =>
            Task.FromResult<(OfflineOperationEnvelope Envelope, EncryptedPayload Encrypted)?>(null);
    }
}
