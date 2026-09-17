using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationOnlineSupplierPaymentsCapabilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateDefault_is_disabled()
    {
        var capability = OrganizationOnlineSupplierPaymentsCapability.CreateDefault(
            PlatformOrganizationId.New(),
            Now);

        Assert.Equal(OrganizationOnlineSupplierPaymentsStatuses.Disabled, capability.Status);
        Assert.Null(capability.UpdatedByActorReference);
        Assert.Null(capability.Reason);
    }

    [Fact]
    public async Task Missing_capability_reads_as_disabled()
    {
        var repository = new InMemoryCapabilityRepository();
        var organizationId = PlatformOrganizationId.New();

        var result = await new GetOrganizationOnlineSupplierPaymentsCapability(repository)
            .ExecuteAsync(organizationId);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrganizationOnlineSupplierPaymentsStatuses.Disabled, result.Value!.Status);
        Assert.Null(result.Value.UpdatedAtUtc);
        Assert.Null(result.Value.Reason);
    }

    [Fact]
    public void Enable_disable_suspend_restore_transitions_are_allowed()
    {
        var capability = OrganizationOnlineSupplierPaymentsCapability.CreateDefault(
            PlatformOrganizationId.New(),
            Now);

        Assert.True(capability.Enable("admin", Now.AddMinutes(1), "enable payments"));
        Assert.Equal(OrganizationOnlineSupplierPaymentsStatuses.Available, capability.Status);
        Assert.Equal("enable payments", capability.Reason);

        Assert.True(capability.Suspend("admin", Now.AddMinutes(2), "risk review"));
        Assert.Equal(OrganizationOnlineSupplierPaymentsStatuses.Suspended, capability.Status);

        Assert.True(capability.Restore("admin", Now.AddMinutes(3)));
        Assert.Equal(OrganizationOnlineSupplierPaymentsStatuses.Available, capability.Status);

        Assert.True(capability.Disable("admin", Now.AddMinutes(4), "org request"));
        Assert.Equal(OrganizationOnlineSupplierPaymentsStatuses.Disabled, capability.Status);
        Assert.Equal("org request", capability.Reason);
    }

    [Fact]
    public void Invalid_transitions_throw()
    {
        var capability = OrganizationOnlineSupplierPaymentsCapability.CreateDefault(
            PlatformOrganizationId.New(),
            Now);

        Assert.Throws<InvalidOperationException>(() =>
            capability.Suspend("admin", Now.AddMinutes(1)));

        Assert.Throws<ArgumentException>(() =>
            capability.Transition("NotARealStatus", "admin", Now.AddMinutes(2)));
    }

    [Fact]
    public async Task Set_use_case_enables_and_audits()
    {
        var fixture = new Fixture();
        var organizationId = PlatformOrganizationId.New();

        var enable = await fixture.Set.ExecuteAsync(
            organizationId,
            OrganizationOnlineSupplierPaymentsStatuses.Available,
            "platform-admin",
            "ready");

        Assert.True(enable.IsSuccess);
        Assert.Equal(OrganizationOnlineSupplierPaymentsStatuses.Available, enable.Value!.Status);
        Assert.Equal("ready", enable.Value.Reason);
        Assert.Contains(
            PlatformAuditActions.OrganizationOnlineSupplierPaymentsEnabled,
            fixture.Audit.Entries.Select(e => e.Action));

        var suspend = await fixture.Set.ExecuteAsync(
            organizationId,
            OrganizationOnlineSupplierPaymentsStatuses.Suspended,
            "platform-admin",
            "hold");

        Assert.True(suspend.IsSuccess);
        Assert.Equal(OrganizationOnlineSupplierPaymentsStatuses.Suspended, suspend.Value!.Status);
        Assert.Contains(
            PlatformAuditActions.OrganizationOnlineSupplierPaymentsSuspended,
            fixture.Audit.Entries.Select(e => e.Action));
    }

    [Fact]
    public async Task Set_use_case_rejects_invalid_transition()
    {
        var fixture = new Fixture();
        var organizationId = PlatformOrganizationId.New();

        var result = await fixture.Set.ExecuteAsync(
            organizationId,
            OrganizationOnlineSupplierPaymentsStatuses.Suspended,
            "platform-admin");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OnlineSupplierPaymentsInvalidTransition, result.ErrorCode);
    }

    private sealed class Fixture
    {
        public InMemoryCapabilityRepository Capabilities { get; } = new();
        public RecordingAuditWriter Audit { get; } = new();
        public SetOrganizationOnlineSupplierPaymentsCapability Set { get; }

        public Fixture()
        {
            var clock = new FixedClock(Now);
            var unitOfWork = new NoOpUnitOfWork();
            var ensure = new EnsureOrganizationOnlineSupplierPaymentsCapability(
                Capabilities,
                unitOfWork,
                clock);
            Set = new SetOrganizationOnlineSupplierPaymentsCapability(
                ensure,
                Capabilities,
                unitOfWork,
                clock,
                Audit);
        }
    }

    private sealed class InMemoryCapabilityRepository : IOrganizationOnlineSupplierPaymentsCapabilityRepository
    {
        private readonly Dictionary<Guid, OrganizationOnlineSupplierPaymentsCapability> _items = [];

        public Task<OrganizationOnlineSupplierPaymentsCapability?> GetByOrganizationIdAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(organizationId.Value, out var value);
            return Task.FromResult(value);
        }

        public Task AddAsync(
            OrganizationOnlineSupplierPaymentsCapability capability,
            CancellationToken cancellationToken = default)
        {
            _items[capability.OrganizationId.Value] = capability;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            OrganizationOnlineSupplierPaymentsCapability capability,
            CancellationToken cancellationToken = default)
        {
            _items[capability.OrganizationId.Value] = capability;
            return Task.CompletedTask;
        }
    }
}
