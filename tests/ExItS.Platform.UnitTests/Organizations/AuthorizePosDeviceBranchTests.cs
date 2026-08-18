using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class AuthorizePosDeviceBranchTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Device_registered_to_branch_a_is_authorized_for_branch_a()
    {
        var org = PlatformOrganizationId.New();
        var branchA = OrganizationBranchId.New();
        var device = PosDevice.Register(org, branchA, "install-a", "Counter A", T0);
        var repo = new InMemoryDevices();
        await repo.AddAsync(device);
        var sut = new AuthorizeForTransactions(repo);

        var result = await sut.ExecuteAsync(org, "install-a", branchA);

        Assert.True(result.IsSuccess);
        Assert.Equal(branchA.Value, result.Value!.BranchId);
    }

    [Fact]
    public async Task Device_registered_to_branch_a_is_denied_for_branch_b()
    {
        var org = PlatformOrganizationId.New();
        var branchA = OrganizationBranchId.New();
        var branchB = OrganizationBranchId.New();
        var device = PosDevice.Register(org, branchA, "install-a", "Counter A", T0);
        var repo = new InMemoryDevices();
        await repo.AddAsync(device);
        var sut = new AuthorizeForTransactions(repo);

        var result = await sut.ExecuteAsync(org, "install-a", branchB);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PosDeviceNotAuthorized, result.ErrorCode);
    }

    private sealed class InMemoryDevices : IPosDeviceRepository
    {
        private readonly List<PosDevice> _items = [];

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

        public Task<IReadOnlyList<PosDevice>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PosDevice>>(_items.Where(x => x.OrganizationId == organizationId).ToList());

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
