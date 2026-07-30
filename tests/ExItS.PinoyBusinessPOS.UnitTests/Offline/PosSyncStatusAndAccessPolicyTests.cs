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
}
