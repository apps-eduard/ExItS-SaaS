using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// P7-WP01 sync-status resolver: Online, Offline, ReconnectRequired only.
/// Does not invent pending counts, sync progress, failures, or last-synced times.
/// </summary>
public sealed class PosSyncStatusService : IPosSyncStatusService, IDisposable
{
    private readonly IConnectivityService _connectivity;
    private readonly IProtectedShellAccessPolicy _accessPolicy;
    private ConnectivityStatus _connectivityStatus = ConnectivityStatus.Unknown;
    private bool _reconnectRequired;

    public PosSyncStatusService(IConnectivityService connectivity, IProtectedShellAccessPolicy accessPolicy)
    {
        _connectivity = connectivity;
        _accessPolicy = accessPolicy;
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
        Recompute();
        await NotifyAsync().ConfigureAwait(false);
    }

    public void SetReconnectRequired(bool required)
    {
        if (_reconnectRequired == required)
        {
            return;
        }

        _reconnectRequired = required;
        Recompute();
        _ = NotifyAsync();
    }

    public void Refresh()
    {
        Recompute();
        _ = NotifyAsync();
    }

    private void Recompute()
    {
        if (_reconnectRequired || _accessPolicy.RequiresReconnectToVerifyAccess)
        {
            Current = new PosSyncStatusSnapshot(PosSyncStatusKind.ReconnectRequired, SafeDetailKey: "SyncStatus_Reconnect");
            return;
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
        Recompute();
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
