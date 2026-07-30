using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Truthful sync-status resolver for P7-WP02: Online/Offline/Reconnect plus queue-driven states.
/// </summary>
public sealed class PosSyncStatusService : IPosSyncStatusService, IDisposable
{
    private readonly IConnectivityService _connectivity;
    private readonly IProtectedShellAccessPolicy _accessPolicy;
    private readonly IOfflineOperationQueue? _queue;
    private ConnectivityStatus _connectivityStatus = ConnectivityStatus.Unknown;
    private bool _reconnectRequired;

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

        if (counts is not null)
        {
            if (counts.Syncing > 0)
            {
                Current = new PosSyncStatusSnapshot(PosSyncStatusKind.Syncing, PendingCount: counts.PendingSyncDisplay);
                return;
            }

            if (counts.PermanentFailure > 0 || counts.Conflict > 0)
            {
                Current = new PosSyncStatusSnapshot(
                    PosSyncStatusKind.SyncFailed,
                    PendingCount: counts.PendingSyncDisplay + counts.PermanentFailure + counts.Conflict,
                    SafeDetailKey: "SyncStatus_Failed");
                return;
            }

            if (counts.PendingSyncDisplay > 0 || counts.RetryableFailure > 0)
            {
                Current = new PosSyncStatusSnapshot(
                    PosSyncStatusKind.PendingSync,
                    PendingCount: counts.Pending + counts.RetryableFailure + counts.BlockedByAccess);
                return;
            }

            if (lastSynced is not null && _connectivityStatus == ConnectivityStatus.Online)
            {
                Current = new PosSyncStatusSnapshot(
                    PosSyncStatusKind.LastSynced,
                    LastSyncedAtUtc: lastSynced,
                    SafeDetailKey: "SyncStatus_LastSynced");
                return;
            }
        }

        Current = _connectivityStatus switch
        {
            ConnectivityStatus.Online => new PosSyncStatusSnapshot(PosSyncStatusKind.Online),
            ConnectivityStatus.Offline => new PosSyncStatusSnapshot(PosSyncStatusKind.Offline),
            _ => new PosSyncStatusSnapshot(PosSyncStatusKind.Offline)
        };
    }

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
