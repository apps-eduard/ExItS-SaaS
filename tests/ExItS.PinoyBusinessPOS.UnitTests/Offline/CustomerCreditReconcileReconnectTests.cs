using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class CustomerCreditReconcileReconnectTests
{
    [Fact]
    public async Task Reconcile_online_without_pos_access_clears_reconnect_flag()
    {
        var connectivity = new FakeConnectivity(online: true);
        var current = new CurrentUserContext();
        current.Set(new AuthSession(
            Guid.NewGuid(), "Staff", "staff1", "staff1@ORG000001.exits.local",
            Guid.NewGuid(), "Romel Store",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: false,
            AccessReasonCode: "product_local_role_missing",
            AccountClass: "Organization",
            OrganizationContextLocked: true));

        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();
        var sync = new TrackingSyncStatus();
        sync.SetReconnectRequired(true);

        // Unused collaborators are never touched on the early-return path.
        var sut = new CustomerCreditOfflineSyncService(
            api: null!,
            store: null!,
            contextManager: null!,
            accessPolicy: policy,
            connectivity: connectivity,
            currentUser: current,
            queueProcessor: null!,
            syncStatus: sync);

        await sut.ReconcileOnReconnectAsync();

        Assert.False(policy.CanEnterProtectedShell);
        Assert.False(policy.RequiresReconnectToVerifyAccess);
        Assert.False(sync.ReconnectRequired);
        Assert.True(sync.RefreshInvocations >= 1);
    }

    [Fact]
    public async Task Reconcile_offline_without_validated_session_sets_reconnect()
    {
        var connectivity = new FakeConnectivity(online: false);
        var current = new CurrentUserContext();
        current.Set(new AuthSession(
            Guid.NewGuid(), "Staff", "staff1", "staff1@ORG000001.exits.local",
            Guid.NewGuid(), "Romel Store",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: true,
            AccessReasonCode: "allowed",
            AccountClass: "Organization",
            OrganizationContextLocked: true));

        var policy = new ProtectedShellAccessPolicy(current, connectivity);
        await policy.InitializeAsync();
        var sync = new TrackingSyncStatus();

        var sut = new CustomerCreditOfflineSyncService(
            api: null!,
            store: null!,
            contextManager: null!,
            accessPolicy: policy,
            connectivity: connectivity,
            currentUser: current,
            queueProcessor: null!,
            syncStatus: sync);

        await sut.ReconcileOnReconnectAsync();

        Assert.True(policy.RequiresReconnectToVerifyAccess);
        Assert.True(sync.ReconnectRequired);
    }

    private sealed class FakeConnectivity(bool online) : IConnectivityService
    {
        public event EventHandler<ConnectivityStatus>? ConnectivityChanged;
        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(online);
    }

    private sealed class TrackingSyncStatus : IPosSyncStatusService
    {
        public PosSyncStatusSnapshot Current { get; private set; } = new(PosSyncStatusKind.Offline);
        public bool ReconnectRequired { get; private set; }
        public int RefreshInvocations { get; private set; }
        public event Func<Task>? Changed;
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void SetReconnectRequired(bool required) => ReconnectRequired = required;
        public void SetRecoveryRequired(bool required) { }
        public void Refresh() => RefreshInvocations++;
    }
}
