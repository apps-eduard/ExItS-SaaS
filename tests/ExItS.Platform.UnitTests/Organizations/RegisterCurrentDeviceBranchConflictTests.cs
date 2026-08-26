using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class RegisterCurrentDeviceBranchConflictTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Register_rejects_active_device_bound_to_other_branch()
    {
        var harness = await Harness.CreateAsync();
        const string installId = "install-active-conflict";
        await harness.Devices.AddAsync(
            PosDevice.Register(harness.Org.Id, harness.BranchA.Id, installId, "Counter A", T0));

        var result = await harness.Register.ExecuteAsync(
            harness.Org.Id,
            new RegisterPosDeviceCommand(harness.BranchB.Id.Value, installId, "Counter B"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PosDeviceBranchConflict, result.ErrorCode);
    }

    [Fact]
    public async Task Register_rejects_revoked_device_rebinding_to_other_branch()
    {
        var harness = await Harness.CreateAsync();
        const string installId = "install-revoked-conflict";
        var device = PosDevice.Register(harness.Org.Id, harness.BranchA.Id, installId, "Counter A", T0);
        device.Revoke(PlatformUserId.New(), T0);
        await harness.Devices.AddAsync(device);

        var result = await harness.Register.ExecuteAsync(
            harness.Org.Id,
            new RegisterPosDeviceCommand(harness.BranchB.Id.Value, installId, "Counter B"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PosDeviceBranchConflict, result.ErrorCode);
    }

    [Fact]
    public async Task Register_same_branch_active_device_touches_last_seen()
    {
        var harness = await Harness.CreateAsync();
        const string installId = "install-same-branch";
        var device = PosDevice.Register(harness.Org.Id, harness.BranchA.Id, installId, "Counter A", T0);
        await harness.Devices.AddAsync(device);
        var originalLastSeen = device.LastSeenAtUtc;

        harness.Clock.UtcNow = T0.AddMinutes(5);

        var result = await harness.Register.ExecuteAsync(
            harness.Org.Id,
            new RegisterPosDeviceCommand(harness.BranchA.Id.Value, installId, "Counter A"));

        Assert.True(result.IsSuccess);
        Assert.Equal(PosDeviceRegisterKind.Reload, result.Value!.Kind);
        Assert.Equal(installId, result.Value.Device.InstallationDeviceId);
        Assert.True(result.Value.Device.LastSeenAtUtc > originalLastSeen);
        Assert.Equal(harness.Clock.UtcNow, result.Value.Device.LastSeenAtUtc);

        var stored = await harness.Devices.GetByInstallationDeviceIdAsync(harness.Org.Id, installId);
        Assert.NotNull(stored);
        Assert.Equal(harness.Clock.UtcNow, stored!.LastSeenAtUtc);
    }

    private sealed class Harness
    {
        public required InMemoryPosDeviceRepository Devices { get; init; }
        public required FixedClock Clock { get; init; }
        public required RegisterCurrentDevice Register { get; init; }
        public required PlatformOrganization Org { get; init; }
        public required OrganizationBranch BranchA { get; init; }
        public required OrganizationBranch BranchB { get; init; }

        public static async Task<Harness> CreateAsync()
        {
            var clock = new FixedClock(T0);
            var devices = new InMemoryPosDeviceRepository();
            var branches = new InMemoryOrganizationBranchRepository();
            var uow = new NoOpUnitOfWork();
            var plan = Plan.CreateDraft(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                PlanCode.Create("starter"),
                "Starter",
                T0,
                maxBranches: 3,
                maxActiveStaff: 10,
                maxActivePosDevices: 5);
            var plans = new StubPlanRepository(plan);
            var subscriptions = new StubSubscriptionRepository(plan.Id);

            var org = PlatformOrganization.Create("Org A", "org-a", T0);
            var branchA = OrganizationBranch.CreateMainBranch(org.Id, T0);
            var branchB = OrganizationBranch.Create(org.Id, "SEC", "Secondary", T0);
            await branches.AddAsync(branchA);
            await branches.AddAsync(branchB);
            subscriptions.Register(org.Id);

            return new Harness
            {
                Devices = devices,
                Clock = clock,
                Register = new RegisterCurrentDevice(devices, branches, subscriptions, plans, uow, clock),
                Org = org,
                BranchA = branchA,
                BranchB = branchB
            };
        }
    }

    private sealed class InMemoryPosDeviceRepository : IPosDeviceRepository
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

        public Task UpdateAsync(PosDevice device, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(x => x.Id == device.Id);
            if (index >= 0)
            {
                _items[index] = device;
            }
            else
            {
                _items.Add(device);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOrganizationBranchRepository : IOrganizationBranchRepository
    {
        private readonly Dictionary<Guid, OrganizationBranch> _byId = new();

        public Task<OrganizationBranch?> GetByIdAsync(
            OrganizationBranchId id,
            CancellationToken cancellationToken = default)
        {
            _byId.TryGetValue(id.Value, out var branch);
            return Task.FromResult(branch);
        }

        public Task<OrganizationBranch?> GetPrimaryAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.Values.FirstOrDefault(x => x.OrganizationId == organizationId && x.IsPrimary));

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(
                _byId.Values.Where(x => x.OrganizationId == organizationId).ToList());

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.Values.Count(x =>
                x.OrganizationId == organizationId && x.Status == OrganizationBranchStatus.Active));

        public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default)
        {
            _byId[branch.Id.Value] = branch;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default)
        {
            _byId[branch.Id.Value] = branch;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPlanRepository(Plan plan) : IPlanRepository
    {
        public Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(plan.Id == id ? plan : null);

        public Task<Plan?> GetByProductAndCodeAsync(ProductCode productCode, PlanCode planCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<Plan?>(plan);

        public Task<IReadOnlyList<Plan>> ListByProductAsync(ProductCode productCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Plan>>([plan]);

        public Task<(IReadOnlyList<Plan> Items, int TotalCount)> ListAsync(
            ProductCode? productCode,
            PlanStatus? status,
            string? search,
            CatalogListSortBy sortBy,
            bool sortDescending,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Plan>, int)>(([plan], 1));

        public Task AddAsync(Plan entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<PlanVersion?> GetVersionByIdAsync(PlanVersionId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PlanVersion?>(null);
        public Task<PlanVersion?> GetVersionByPlanAndNumberAsync(PlanId planId, int versionNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<PlanVersion?>(null);
        public Task<IReadOnlyList<PlanVersion>> ListVersionsAsync(PlanId planId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlanVersion>>([]);
        public Task<PlanVersion?> GetLatestPublishedVersionAsync(PlanId planId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PlanVersion?>(null);
        public Task<int> GetMaxVersionNumberAsync(PlanId planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
        public Task AddVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubSubscriptionRepository(PlanId planId) : ISubscriptionRepository
    {
        private readonly HashSet<Guid> _orgs = [];

        public void Register(PlatformOrganizationId organizationId) => _orgs.Add(organizationId.Value);

        public Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Subscription?>(null);

        public Task<Subscription?> GetCurrentForOrganizationProductAsync(
            PlatformOrganizationId organizationId,
            ProductCode productCode,
            CancellationToken cancellationToken = default)
        {
            if (!_orgs.Contains(organizationId.Value))
            {
                return Task.FromResult<Subscription?>(null);
            }

            return Task.FromResult<Subscription?>(Subscription.Rehydrate(
                SubscriptionId.New(),
                organizationId,
                productCode,
                planId,
                PlanVersionId.New(),
                TrialDefinitionId.New(),
                SubscriptionStatus.Trialing,
                T0,
                T0.AddDays(14),
                paidPeriodStartUtc: null,
                paidPeriodEndUtc: null,
                gracePeriodEndUtc: null,
                suspendedAtUtc: null,
                cancelledAtUtc: null,
                pastDueAtUtc: null,
                expiredAtUtc: null,
                billingCycle: BillingCycle.Monthly,
                agreedPrice: 0m,
                currencyCode: "PHP",
                priceEffectiveFromUtc: null,
                pendingPlanId: null,
                pendingPlanEffectiveAtUtc: null,
                createdAtUtc: T0,
                updatedAtUtc: T0,
                version: 1));
        }

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            SubscriptionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByProductAsync(
            ProductCode productCode,
            SubscriptionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListExpiringTrialsAsync(
            DateTimeOffset asOfUtc,
            DateTimeOffset throughUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByStatusAsync(
            SubscriptionStatus status,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListAsync(
            PlatformOrganizationId? organizationId,
            ProductCode? productCode,
            SubscriptionStatus? status,
            string? search,
            bool? isTrial,
            Guid? planId,
            SubscriptionListSortBy sortBy,
            bool sortDescending,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<bool> ExistsActiveLikeAsync(
            PlatformOrganizationId organizationId,
            ProductCode productCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_orgs.Contains(organizationId.Value));

        public Task<bool> HasConsumedTrialAsync(
            PlatformOrganizationId organizationId,
            ProductCode productCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<Subscription>> ListDuePendingPlanChangesAsync(
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subscription>>([]);

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
