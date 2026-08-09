using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Flushes the offline outbox when connectivity returns, after login while already online,
/// and once at startup if already online. Debounced + single-flight so flaky networks do not stampede.
/// </summary>
public interface IOfflineReconnectAutoSync : IDisposable
{
    /// <summary>Subscribes to connectivity/auth and schedules an initial online catch-up.</summary>
    void Start();

    /// <summary>Runs pending sync when online (auto path — does not reclaim permanent failures).</summary>
    Task TrySyncNowAsync(CancellationToken ct = default);

    /// <summary>
    /// User-initiated Retry: reclaim permanent/conflict rows to Pending, then sync.
    /// </summary>
    Task RetryIncludingFailedAsync(CancellationToken ct = default);
}

public sealed class OfflineReconnectAutoSyncService : IOfflineReconnectAutoSync
{
    public static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(1000);
    public static readonly TimeSpan StartupCatchUpDelay = TimeSpan.FromMilliseconds(1500);

    private readonly IConnectivityService _connectivity;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILocalContextManager _contextManager;
    private readonly IOfflineOperationQueue _queue;
    private readonly IPersonalOfflineSyncService _personalSync;
    private readonly ICustomerCreditOfflineSyncService _customerCreditSync;
    private readonly IOfflineQueueProcessor _queueProcessor;
    private readonly IPosSyncStatusService _syncStatus;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _debounceDelay;
    private readonly TimeSpan _startupCatchUpDelay;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private ConnectivityStatus _lastStatus = ConnectivityStatus.Unknown;
    private CancellationTokenSource? _debounceCts;
    private int _started;
    private bool _disposed;

    public OfflineReconnectAutoSyncService(
        IConnectivityService connectivity,
        ICurrentUserContext currentUser,
        ILocalContextManager contextManager,
        IOfflineOperationQueue queue,
        IPersonalOfflineSyncService personalSync,
        ICustomerCreditOfflineSyncService customerCreditSync,
        IOfflineQueueProcessor queueProcessor,
        IPosSyncStatusService syncStatus,
        TimeProvider? timeProvider = null,
        TimeSpan? debounceDelay = null,
        TimeSpan? startupCatchUpDelay = null)
    {
        _connectivity = connectivity;
        _currentUser = currentUser;
        _contextManager = contextManager;
        _queue = queue;
        _personalSync = personalSync;
        _customerCreditSync = customerCreditSync;
        _queueProcessor = queueProcessor;
        _syncStatus = syncStatus;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _debounceDelay = debounceDelay ?? DebounceDelay;
        _startupCatchUpDelay = startupCatchUpDelay ?? StartupCatchUpDelay;
    }

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1 || _disposed)
        {
            return;
        }

        _connectivity.ConnectivityChanged += OnConnectivityChanged;
        _currentUser.Changed += OnCurrentUserChangedAsync;
        _ = InitializeLastStatusThenCatchUpAsync();
    }

    private async Task InitializeLastStatusThenCatchUpAsync()
    {
        try
        {
            _lastStatus = await _connectivity.IsConnectedAsync().ConfigureAwait(false)
                ? ConnectivityStatus.Online
                : ConnectivityStatus.Offline;
        }
        catch
        {
            _lastStatus = ConnectivityStatus.Unknown;
        }

        await CatchUpIfOnlineAsync().ConfigureAwait(false);
    }

    private Task OnCurrentUserChangedAsync()
    {
        if (_disposed || !_currentUser.IsAuthenticated)
        {
            return Task.CompletedTask;
        }

        // Login / session restore while already online — no Offline→Online event fires.
        ScheduleDebouncedSync();
        return Task.CompletedTask;
    }

    public Task TrySyncNowAsync(CancellationToken ct = default) =>
        SyncInternalAsync(reclaimFailed: false, ct);

    public Task RetryIncludingFailedAsync(CancellationToken ct = default) =>
        SyncInternalAsync(reclaimFailed: true, ct);

    private async Task SyncInternalAsync(bool reclaimFailed, CancellationToken ct)
    {
        if (_disposed)
        {
            return;
        }

        if (!await _connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        if (!_currentUser.IsAuthenticated)
        {
            return;
        }

        if (!await _gate.WaitAsync(0, ct).ConfigureAwait(false))
        {
            return; // already syncing
        }

        try
        {
            // Open personal/org local DB first so reclaim targets the right outbox.
            await EnsureActiveContextAsync(ct).ConfigureAwait(false);

            if (reclaimFailed)
            {
                await SafeReclaimFailedAsync(ct).ConfigureAwait(false);
            }

            await RunSyncCoreAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureActiveContextAsync(CancellationToken ct)
    {
        var active = _contextManager.ActiveContext;
        var isPersonal = active is not null
            && PersonalLocalScope.IsPersonalContext(active.Identity.OrganizationId, active.Identity.ProductCode);

        if (isPersonal)
        {
            return;
        }

        var session = _currentUser.Session;
        if (session is null || session.OrganizationId is not null)
        {
            return;
        }

        try
        {
            // Open SQLite only — do not flush yet so manual reclaim can run first.
            await _contextManager.OpenPersonalAsync(session.UserId, ct).ConfigureAwait(false);
        }
        catch
        {
            // RunSyncCore / personal sync will retry open.
        }
    }

    private async Task SafeReclaimFailedAsync(CancellationToken ct)
    {
        try
        {
            await _queue.ReclaimFailedForManualRetryAsync(ct).ConfigureAwait(false);
            await _queue.ReclaimBlockedByAccessAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // No active context yet — nothing to reclaim.
        }
    }

    private async Task CatchUpIfOnlineAsync()
    {
        try
        {
            // Tests pass InfiniteTimeSpan to disable startup catch-up.
            if (_startupCatchUpDelay == Timeout.InfiniteTimeSpan || _startupCatchUpDelay < TimeSpan.Zero)
            {
                return;
            }

            if (_startupCatchUpDelay > TimeSpan.Zero)
            {
                await Task.Delay(_startupCatchUpDelay, _timeProvider).ConfigureAwait(false);
            }

            if (_disposed)
            {
                return;
            }

            if (await _connectivity.IsConnectedAsync().ConfigureAwait(false))
            {
                await TrySyncNowAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // Best-effort startup catch-up; reconnect / auth path remains.
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityStatus status)
    {
        if (_disposed)
        {
            return;
        }

        var previous = _lastStatus;
        _lastStatus = status;
        if (status != ConnectivityStatus.Online || previous == ConnectivityStatus.Online)
        {
            return;
        }

        ScheduleDebouncedSync();
    }

    private void ScheduleDebouncedSync()
    {
        var prior = Interlocked.Exchange(ref _debounceCts, new CancellationTokenSource());
        try
        {
            prior?.Cancel();
            prior?.Dispose();
        }
        catch
        {
            // ignore dispose races
        }

        var cts = _debounceCts;
        if (cts is null)
        {
            return;
        }

        _ = DebounceThenSyncAsync(cts);
    }

    private async Task DebounceThenSyncAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_debounceDelay, _timeProvider, cts.Token).ConfigureAwait(false);
            await TrySyncNowAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Newer reconnect won the debounce.
        }
        catch
        {
            // Keep pending outbox; user can Retry.
        }
    }

    private async Task RunSyncCoreAsync(CancellationToken ct)
    {
        var active = _contextManager.ActiveContext;
        var isPersonal = active is not null
            && PersonalLocalScope.IsPersonalContext(active.Identity.OrganizationId, active.Identity.ProductCode);

        // Personal session (no org) or active personal DB → personal outbox.
        if (isPersonal || (_currentUser.Session?.OrganizationId is null && _currentUser.IsAuthenticated))
        {
            await _personalSync.TrySyncPendingAsync(ct).ConfigureAwait(false);
            active = _contextManager.ActiveContext;
        }

        // Organization POS context → credit/sale outbox + incremental download.
        // Skip when membership exists but POS operate access is not active (Org essentials).
        if (!isPersonal
            && _currentUser.Session?.OrganizationId is not null
            && _currentUser.HasPosAccess)
        {
            await _customerCreditSync.ReconcileOnReconnectAsync(ct).ConfigureAwait(false);
        }
        else if (!isPersonal && active?.Status == LocalContextInitStatus.Ready && _currentUser.HasPosAccess)
        {
            await _queueProcessor.ProcessAvailableAsync(ct).ConfigureAwait(false);
        }
        else if (!isPersonal && _currentUser.IsAuthenticated && !_currentUser.HasPosAccess)
        {
            // Online Org essentials: keep chip truthful (Online), never Reconnect.
            _syncStatus.SetReconnectRequired(false);
        }

        _syncStatus.Refresh();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_started == 1)
        {
            _connectivity.ConnectivityChanged -= OnConnectivityChanged;
            _currentUser.Changed -= OnCurrentUserChangedAsync;
        }

        try
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }
        catch
        {
            // ignore
        }

        _gate.Dispose();
    }
}
