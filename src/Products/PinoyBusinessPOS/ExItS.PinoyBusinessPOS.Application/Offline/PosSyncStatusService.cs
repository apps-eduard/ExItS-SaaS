using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Truthful sync-status resolver for P7-WP02+: Online/Offline/Reconnect plus queue-driven states.
/// </summary>
public sealed class PosSyncStatusService : IPosSyncStatusService, IDisposable
{
    private readonly IConnectivityService _connectivity;
    private readonly IProtectedShellAccessPolicy _accessPolicy;
    private readonly IOfflineOperationQueue? _queue;
    private ConnectivityStatus _connectivityStatus = ConnectivityStatus.Unknown;
    private bool _reconnectRequired;
    private bool _recoveryRequired;
    private bool? _apiReachable;

    public PosSyncStatusService(
        IConnectivityService connectivity,
        IProtectedShellAccessPolicy accessPolicy,
        IOfflineOperationQueue? queue = null)
    {
        _connectivity = connectivity;
        _accessPolicy = accessPolicy;
        _queue = queue;
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
        Current = new PosSyncStatusSnapshot(PosSyncStatusKind.Offline);
    }

    public PosSyncStatusSnapshot Current { get; private set; }

    public event Func<Task>? Changed;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _connectivityStatus = await _connectivity.IsConnectedAsync(ct).ConfigureAwait(false)
            ? ConnectivityStatus.Online
            : ConnectivityStatus.Offline;
        await RecomputeAsync(ct).ConfigureAwait(false);
        await NotifyAsync().ConfigureAwait(false);
    }

    public void SetReconnectRequired(bool required)
    {
        if (_reconnectRequired == required)
        {
            return;
        }

        _reconnectRequired = required;
        Refresh();
    }

    public void SetRecoveryRequired(bool required)
    {
        if (_recoveryRequired == required)
        {
            return;
        }

        _recoveryRequired = required;
        Refresh();
    }

    public void NotifyApiReachability(bool reachable)
    {
        if (_apiReachable == reachable)
        {
            return;
        }

        _apiReachable = reachable;
        Refresh();
    }

    public void Refresh()
    {
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        await RecomputeAsync(CancellationToken.None).ConfigureAwait(false);
        await NotifyAsync().ConfigureAwait(false);
    }

    private async Task RecomputeAsync(CancellationToken ct)
    {
        if (_reconnectRequired || _accessPolicy.RequiresReconnectToVerifyAccess)
        {
            Current = new PosSyncStatusSnapshot(PosSyncStatusKind.ReconnectRequired, SafeDetailKey: "SyncStatus_Reconnect");
            return;
        }

        OfflineQueueCounts? counts = null;
        DateTimeOffset? lastSynced = null;
        if (_queue is not null)
        {
            try
            {
                counts = await _queue.GetCountsAsync(ct).ConfigureAwait(false);
                lastSynced = await _queue.GetLastSyncedUtcAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                counts = null;
            }
        }

        var queueNeedsRecovery = counts is not null
            && (counts.Conflict > 0 || counts.PermanentFailure > 0 || counts.BlockedByAccess > 0);

        if (_recoveryRequired || queueNeedsRecovery)
        {
            var pendingCount = counts is null
                ? (int?)null
                : counts.PendingSyncDisplay + counts.PermanentFailure + counts.Conflict + counts.BlockedByAccess;
            Current = new PosSyncStatusSnapshot(
                PosSyncStatusKind.RecoveryRequired,
                PendingCount: pendingCount,
                SafeDetailKey: _recoveryRequired ? "SyncStatus_KeyUnavailable" : "SyncStatus_RecoveryRequired");
            return;
        }

        var effectivelyOnline = IsEffectivelyOnline();

        if (counts is not null)
        {
            if (effectivelyOnline && counts.Syncing > 0)
            {
                Current = new PosSyncStatusSnapshot(PosSyncStatusKind.Syncing, PendingCount: counts.PendingSyncDisplay);
                return;
            }

            if (counts.RetryableFailure > 0)
            {
                Current = new PosSyncStatusSnapshot(
                    PosSyncStatusKind.SyncFailed,
                    PendingCount: counts.PendingSyncDisplay,
                    SafeDetailKey: "SyncStatus_Failed");
                return;
            }

            if (effectivelyOnline && counts.Pending > 0)
            {
                Current = new PosSyncStatusSnapshot(
                    PosSyncStatusKind.PendingSync,
                    PendingCount: counts.Pending);
                return;
            }

            if (lastSynced is not null && effectivelyOnline)
            {
                Current = new PosSyncStatusSnapshot(
                    PosSyncStatusKind.LastSynced,
                    LastSyncedAtUtc: lastSynced,
                    SafeDetailKey: "SyncStatus_LastSynced");
                return;
            }
        }

        Current = effectivelyOnline
            ? new PosSyncStatusSnapshot(PosSyncStatusKind.Online)
            : new PosSyncStatusSnapshot(PosSyncStatusKind.Offline);
    }

    private bool IsEffectivelyOnline() =>
        _connectivityStatus == ConnectivityStatus.Online && _apiReachable != false;

    private async void OnConnectivityChanged(object? sender, ConnectivityStatus status)
    {
        _connectivityStatus = status;
        await RecomputeAsync(CancellationToken.None).ConfigureAwait(false);
        await NotifyAsync().ConfigureAwait(false);
    }

    private async Task NotifyAsync()
    {
        if (Changed is not null)
        {
            await Changed.Invoke().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}
