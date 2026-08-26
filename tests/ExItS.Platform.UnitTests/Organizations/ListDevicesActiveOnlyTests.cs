using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class ListDevicesActiveOnlyTests
{
    [Fact]
    public async Task ListDevices_returns_active_devices_only()
    {
        var org = PlatformOrganizationId.From(Guid.Parse("11111111-1111-4111-8111-111111111111"));
        var branch = OrganizationBranchId.From(Guid.Parse("22222222-2222-4222-8222-222222222222"));
        var actor = PlatformUserId.From(Guid.Parse("33333333-3333-4333-8333-333333333333"));
        var now = DateTimeOffset.Parse("2026-08-22T00:00:00Z");

        var active = PosDevice.Register(org, branch, "install-active", "Shop PC", now, "Windows", "Chrome", "1.0");
        var revoked = PosDevice.Register(org, branch, "install-revoked", "Old Phone", now, "Android", "Chrome", "1.0");
        revoked.Revoke(actor, now.AddHours(1));

        var repo = new InMemoryPosDeviceRepository([active, revoked]);
        var listed = await new ListDevices(repo).ExecuteAsync(org);

        Assert.Single(listed);
        Assert.Equal("Shop PC", listed[0].FriendlyName);
        Assert.Equal(PosDeviceStatus.Active, listed[0].Status);

        var history = await new ListAllDevices(repo).ExecuteAsync(org);
        Assert.Equal(2, history.Count);
    }

    private sealed class InMemoryPosDeviceRepository(IReadOnlyList<PosDevice> seed) : IPosDeviceRepository
    {
        private readonly List<PosDevice> _items = [.. seed];

        public Task<PosDevice?> GetByIdAsync(PosDeviceId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<PosDevice?> GetByInstallationDeviceIdAsync(
            PlatformOrganizationId organizationId,
            string installationDeviceId,
            CancellationToken cancellationToken = default)
        {
            var value = PosDevice.NormalizeInstallationDeviceId(installationDeviceId);
            return Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.InstallationDeviceId == value));
        }

        public Task<PosDevice?> FindByInstallationDeviceIdAsync(
            string installationDeviceId,
            CancellationToken cancellationToken = default)
        {
            var value = PosDevice.NormalizeInstallationDeviceId(installationDeviceId);
            return Task.FromResult(_items.FirstOrDefault(x => x.InstallationDeviceId == value));
        }

        public Task<IReadOnlyList<PosDevice>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PosDevice>>(_items.Where(x => x.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<PosDevice>> ListActiveByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PosDevice>>(
                _items.Where(x => x.OrganizationId == organizationId && x.Status == PosDeviceStatus.Active).ToList());

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(x => x.OrganizationId == organizationId && x.Status == PosDeviceStatus.Active));

        public Task AddAsync(PosDevice device, CancellationToken cancellationToken = default)
        {
            _items.Add(device);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PosDevice device, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
