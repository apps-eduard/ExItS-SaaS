using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class OfflineReconnectAutoSyncServiceTests
{
    [Fact]
    public async Task Offline_to_online_triggers_personal_sync_after_debounce()
    {
        var connectivity = new FakeConnectivity(online: false);
        var personal = new FakePersonalSync();
        var credit = new FakeCreditSync();
        await using var harness = CreateHarness(
            connectivity, personal, credit, personalSession: true, debounce: TimeSpan.Zero, startup: Timeout.InfiniteTimeSpan);

        harness.Service.Start();
        connectivity.SetOnline(true);
        await WaitUntilAsync(() => personal.Calls >= 1);

        Assert.Equal(1, personal.Calls);
        Assert.Equal(0, credit.Calls);
    }

    [Fact]
    public async Task Login_while_already_online_triggers_sync()
    {
        var connectivity = new FakeConnectivity(online: true);
        var personal = new FakePersonalSync();
        var credit = new FakeCreditSync();
        var current = new CurrentUserContext();
        await using var harness = CreateHarness(
            connectivity, personal, credit, personalSession: true, authenticated: false,
            debounce: TimeSpan.Zero, startup: Timeout.InfiniteTimeSpan, currentUser: current);

        harness.Service.Start();
        current.Set(new AuthSession(
            Guid.NewGuid(), "User", "user", "u@example.com",
            null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: false, AccessReasonCode: null));

        await WaitUntilAsync(() => personal.Calls >= 1);
        Assert.True(personal.Calls >= 1);
    }

    [Fact]
    public async Task Already_online_connectivity_noise_does_not_retrigger()
    {
        var connectivity = new FakeConnectivity(online: true);
        var personal = new FakePersonalSync();
        var credit = new FakeCreditSync();
        await using var harness = CreateHarness(
            connectivity, personal, credit, personalSession: true, debounce: TimeSpan.Zero, startup: Timeout.InfiniteTimeSpan);
        harness.Service.Start();

        await harness.Service.TrySyncNowAsync();
        Assert.Equal(1, personal.Calls);

        connectivity.Raise(ConnectivityStatus.Online);
        await Task.Delay(50);

        Assert.Equal(1, personal.Calls);
    }

    [Fact]
    public async Task Organization_session_uses_credit_reconcile()
    {
        var connectivity = new FakeConnectivity(online: false);
        var personal = new FakePersonalSync();
        var credit = new FakeCreditSync();
        await using var harness = CreateHarness(
            connectivity, personal, credit, personalSession: false, debounce: TimeSpan.Zero, startup: Timeout.InfiniteTimeSpan);
        harness.Service.Start();

        connectivity.SetOnline(true);
        await WaitUntilAsync(() => credit.Calls >= 1);

        Assert.Equal(0, personal.Calls);
        Assert.Equal(1, credit.Calls);
    }

    [Fact]
    public async Task Unauthenticated_skips_sync()
    {
        var connectivity = new FakeConnectivity(online: true);
        var personal = new FakePersonalSync();
        var credit = new FakeCreditSync();
        await using var harness = CreateHarness(
            connectivity, personal, credit, personalSession: true, authenticated: false,
            debounce: TimeSpan.Zero, startup: Timeout.InfiniteTimeSpan);

        await harness.Service.TrySyncNowAsync();
        Assert.Equal(0, personal.Calls);
        Assert.Equal(0, credit.Calls);
    }

    [Fact]
    public async Task Manual_retry_reclaims_failed_then_syncs()
    {
        var connectivity = new FakeConnectivity(online: true);
        var personal = new FakePersonalSync();
        var credit = new FakeCreditSync();
        var queue = new FakeQueue();
        await using var harness = CreateHarness(
            connectivity, personal, credit, personalSession: true,
            debounce: TimeSpan.Zero, startup: Timeout.InfiniteTimeSpan, queue: queue);

        await harness.Service.RetryIncludingFailedAsync();

        Assert.True(queue.ReclaimFailedCalls >= 1);
        Assert.True(personal.Calls >= 1);
    }

    [Fact]
    public async Task Single_flight_skips_overlapping_try_sync()
    {
        var connectivity = new FakeConnectivity(online: true);
        var personal = new FakePersonalSync { Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        var credit = new FakeCreditSync();
        await using var harness = CreateHarness(
            connectivity, personal, credit, personalSession: true, debounce: TimeSpan.Zero, startup: Timeout.InfiniteTimeSpan);

        var first = harness.Service.TrySyncNowAsync();
        await WaitUntilAsync(() => personal.Calls >= 1);
        var second = harness.Service.TrySyncNowAsync();
        await second;
        personal.Hold.SetResult();
        await first;

        Assert.Equal(1, personal.Calls);
    }

    private static Harness CreateHarness(
        FakeConnectivity connectivity,
        FakePersonalSync personal,
        FakeCreditSync credit,
        bool personalSession,
        bool authenticated = true,
        TimeSpan? debounce = null,
        TimeSpan? startup = null,
        CurrentUserContext? currentUser = null,
        FakeQueue? queue = null)
    {
        var current = currentUser ?? new CurrentUserContext();
        if (authenticated && current.Session is null)
        {
            current.Set(new AuthSession(
                Guid.NewGuid(),
                "User",
                "user",
                "u@example.com",
                personalSession ? null : Guid.NewGuid(),
                personalSession ? null : "Org",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                HasPosAccess: !personalSession,
                AccessReasonCode: personalSession ? null : "allowed"));
        }

        var service = new OfflineReconnectAutoSyncService(
            connectivity,
            current,
            new FakeContextManager(),
            queue ?? new FakeQueue(),
            personal,
            credit,
            new FakeQueueProcessor(),
            new FakeSyncStatus(),
            timeProvider: null,
            debounceDelay: debounce,
            startupCatchUpDelay: startup);
        return new Harness(service);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 2000)
    {
        var start = Environment.TickCount64;
        while (!predicate())
        {
            if (Environment.TickCount64 - start > timeoutMs)
            {
                throw new TimeoutException("Condition was not met in time.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class Harness(OfflineReconnectAutoSyncService service) : IAsyncDisposable
    {
        public OfflineReconnectAutoSyncService Service { get; } = service;

        public ValueTask DisposeAsync()
        {
            Service.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeConnectivity(bool online) : IConnectivityService
    {
        private bool _online = online;

        public event EventHandler<ConnectivityStatus>? ConnectivityChanged;

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) =>
            Task.FromResult(_online);

        public void SetOnline(bool value)
        {
            _online = value;
            Raise(value ? ConnectivityStatus.Online : ConnectivityStatus.Offline);
        }

        public void Raise(ConnectivityStatus status) =>
            ConnectivityChanged?.Invoke(this, status);
    }

    private sealed class FakePersonalSync : IPersonalOfflineSyncService
    {
        public int Calls { get; private set; }
        public TaskCompletionSource? Hold { get; init; }

        public async Task<OfflineProcessBatchResult> TrySyncPendingAsync(CancellationToken ct = default)
        {
            Calls++;
            if (Hold is not null)
            {
                await Hold.Task.WaitAsync(ct).ConfigureAwait(false);
            }

            return new OfflineProcessBatchResult(0, 0, 0, null);
        }

        public Task<int> GetPendingCountAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class FakeCreditSync : ICustomerCreditOfflineSyncService
    {
        public int Calls { get; private set; }
        public bool CanMutateOffline => true;

        public Task DownloadIncrementalAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task ReconcileOnReconnectAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.CompletedTask;
        }

        public Task<ApiResultLikeCustomer> CreateCustomerAsync(
            string displayName, string? mobileNumber, string? address, string? notes, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<ApiResultLikeCustomer> UpdateCustomerAsync(
            Guid customerId, string displayName, string? mobileNumber, string? address, string? notes, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<ApiResultLikeCredit> CreateCreditAsync(
            Guid customerId, decimal amount, string remarks, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<ApiResultLikeRepayment> CreateRepaymentAsync(
            Guid customerId, decimal amount, string? remarks, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<ApiResultLikeCredit> ReverseCreditAsync(
            Guid customerId, Guid creditEntryId, string reason, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<ApiResultLikeRepayment> ReverseRepaymentAsync(
            Guid repaymentId, string reason, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<ApiResultLikeCredit> SetCreditDueDateAsync(
            Guid creditEntryId, DateOnly? dueDate, string reason, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task DiscardCustomerConflictAsync(Guid customerId, CancellationToken ct = default) => Task.CompletedTask;

        public Task ApplyServerCustomerAfterSuccessAsync(Guid customerId, LocalCustomerProjection server, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task ApplyServerCreditAfterSuccessAsync(LocalCreditProjection server, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task ApplyCreditRejectedAsync(Guid creditEntryId, string safeFailureCode, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task ApplyRepaymentRejectedAsync(Guid repaymentId, string safeFailureCode, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RefreshCustomerFinancialsFromServerAsync(Guid customerId, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeQueueProcessor : IOfflineQueueProcessor
    {
        public Task<OfflineProcessBatchResult> ProcessAvailableAsync(CancellationToken ct = default) =>
            Task.FromResult(new OfflineProcessBatchResult(0, 0, 0, null));
    }

    private sealed class FakeSyncStatus : IPosSyncStatusService
    {
        public PosSyncStatusSnapshot Current { get; private set; } = new(PosSyncStatusKind.Offline);
        public event Func<Task>? Changed;
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void SetReconnectRequired(bool required) { }
        public void SetRecoveryRequired(bool required) { }
        public void Refresh() => Current = new PosSyncStatusSnapshot(PosSyncStatusKind.Online);
    }

    private sealed class FakeContextManager : ILocalContextManager
    {
        private LocalContextSnapshot? _active;

        public LocalContextSnapshot? ActiveContext => _active;

        public Task CloseAsync(CancellationToken ct = default)
        {
            _active = null;
            return Task.CompletedTask;
        }

        public Task<LocalContextOpenResult> OpenAsync(
            Guid userId, Guid organizationId, string productCode, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<LocalContextOpenResult> OpenPersonalAsync(Guid userId, CancellationToken ct = default)
        {
            _active = new LocalContextSnapshot(
                new LocalContextIdentity(
                    "personal-hash",
                    userId,
                    PersonalLocalScope.PathIsolationMarker,
                    PersonalLocalScope.ProductCode),
                "personal.db",
                1,
                DateTimeOffset.UtcNow,
                LocalContextInitStatus.Ready);
            return Task.FromResult(new LocalContextOpenResult(true, _active));
        }
    }

    private sealed class FakeQueue : IOfflineOperationQueue
    {
        public int ReclaimFailedCalls { get; private set; }

        public Task EnqueueAsync(OfflineEnqueueRequest request, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecoverAbandonedSyncingAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task ReclaimBlockedByAccessAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task ReclaimFailedForManualRetryAsync(CancellationToken ct = default)
        {
            ReclaimFailedCalls++;
            return Task.CompletedTask;
        }

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
            CancellationToken ct = default) => Task.CompletedTask;

        public Task<OfflineQueueCounts> GetCountsAsync(CancellationToken ct = default) =>
            Task.FromResult(new OfflineQueueCounts(0, 0, 0, 0, 0, 0, 0));

        public Task<IReadOnlyList<OfflineOperationEnvelope>> ListSafeMetadataAsync(int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OfflineOperationEnvelope>>([]);

        public Task<bool> HasUnsyncedWorkAsync(CancellationToken ct = default) => Task.FromResult(false);
        public Task SetLastSyncedUtcAsync(DateTimeOffset utc, CancellationToken ct = default) => Task.CompletedTask;
        public Task<DateTimeOffset?> GetLastSyncedUtcAsync(CancellationToken ct = default) =>
            Task.FromResult<DateTimeOffset?>(null);

        public Task<(OfflineOperationEnvelope Envelope, EncryptedPayload Encrypted)?> TryLoadEncryptedAsync(
            Guid operationId, CancellationToken ct = default) =>
            Task.FromResult<(OfflineOperationEnvelope Envelope, EncryptedPayload Encrypted)?>(null);
    }
}
