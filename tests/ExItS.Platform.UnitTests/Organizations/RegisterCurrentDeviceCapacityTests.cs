using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class RegisterCurrentDeviceCapacityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 22, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Register_consumes_exactly_one_active_slot_and_reload_does_not()
    {
        var harness = await Harness.CreateAsync(maxActivePosDevices: 3);

        var first = await harness.Register.ExecuteAsync(
            harness.Org.Id,
            new RegisterPosDeviceCommand(harness.Branch.Id.Value, "install-a", "Shop PC"));
        Assert.True(first.IsSuccess);
        Assert.Equal(1, await harness.Devices.CountActiveAsync(harness.Org.Id));

        var reload = await harness.Register.ExecuteAsync(
            harness.Org.Id,
            new RegisterPosDeviceCommand(harness.Branch.Id.Value, "install-a", "Shop PC"));
        Assert.True(reload.IsSuccess);
        Assert.Equal(1, await harness.Devices.CountActiveAsync(harness.Org.Id));
    }

    [Fact]
    public async Task Final_slot_allows_one_register_and_blocks_the_next_distinct_install()
    {
        var harness = await Harness.CreateAsync(maxActivePosDevices: 1);

        var first = await harness.Register.ExecuteAsync(
            harness.Org.Id,
            new RegisterPosDeviceCommand(harness.Branch.Id.Value, "install-1", "Counter"));
        Assert.True(first.IsSuccess);

        var second = await harness.Register.ExecuteAsync(
            harness.Org.Id,
            new RegisterPosDeviceCommand(harness.Branch.Id.Value, "install-2", "Laptop"));
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PosDeviceCapacityExceeded, second.ErrorCode);
        Assert.Equal(1, await harness.Devices.CountActiveAsync(harness.Org.Id));
    }

    [Fact]
    public async Task Register_reactivates_revoked_device_with_reactivated_kind()
    {
        var harness = await Harness.CreateAsync(maxActivePosDevices: 3);
        const string installId = "install-reactivate";
        var device = PosDevice.Register(
            harness.Org.Id,
            harness.Branch.Id,
            installId,
            "Counter",
            T0);
        device.Revoke(PlatformUserId.New(), T0);
        await harness.Devices.AddAsync(device);

        var result = await harness.Register.ExecuteAsync(
            harness.Org.Id,
            new RegisterPosDeviceCommand(harness.Branch.Id.Value, installId, "Counter"));

        Assert.True(result.IsSuccess);
        Assert.Equal(PosDeviceRegisterKind.Reactivated, result.Value!.Kind);
        Assert.Equal(PosDeviceStatus.Active, result.Value.Device.Status);
        Assert.Equal(1, await harness.Devices.CountActiveAsync(harness.Org.Id));
    }

    [Fact]
    public void RegisterCurrentDevice_serializes_capacity_under_organization_lock()
    {
        var text = File.ReadAllText(
            Path.Combine(
                FindRepoRoot(),
                "src",
                "Platform",
                "ExItS.Platform.Application",
                "Organizations",
                "PosDeviceUseCases.cs"));
        Assert.Contains("ExecuteWithOrganizationLockAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authorize_missing_installation_returns_registration_required()
    {
        var harness = await Harness.CreateAsync(maxActivePosDevices: 3);
        var result = await new AuthorizeForTransactions(harness.Devices)
            .ExecuteAsync(harness.Org.Id, "never-registered-install");
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PosDeviceRegistrationRequired, result.ErrorCode);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class Harness
    {
        public required InMemoryPosDeviceRepository Devices { get; init; }
        public required RegisterCurrentDevice Register { get; init; }
        public required PlatformOrganization Org { get; init; }
        public required OrganizationBranch Branch { get; init; }

        public static async Task<Harness> CreateAsync(int maxActivePosDevices)
        {
            var clock = new FixedClock(T0);
            var devices = new InMemoryPosDeviceRepository();
            var branches = new InMemoryOrganizationBranchRepository();
            var plan = Plan.CreateDraft(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                PlanCode.Create("starter"),
                "Starter",
                T0,
                maxBranches: 3,
                maxActiveStaff: 10,
                maxActivePosDevices: maxActivePosDevices);
            var plans = new StubPlanRepository(plan);
            var subscriptions = new StubSubscriptionRepository(plan.Id);

            var org = PlatformOrganization.Create("Org A", "org-a", T0);
            var branch = OrganizationBranch.CreateMainBranch(org.Id, T0);
            await branches.AddAsync(branch);
            subscriptions.Register(org.Id);

            return new Harness
            {
                Devices = devices,
                Register = new RegisterCurrentDevice(devices, branches, subscriptions, plans, new NoOpUnitOfWork(), clock),
                Org = org,
                Branch = branch
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
            if (index >= 0) _items[index] = device;
            else _items.Add(device);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOrganizationBranchRepository : IOrganizationBranchRepository
    {
        private readonly Dictionary<Guid, OrganizationBranch> _byId = new();

        public Task<OrganizationBranch?> GetByIdAsync(OrganizationBranchId id, CancellationToken cancellationToken = default)
        {
            _byId.TryGetValue(id.Value, out var branch);
            return Task.FromResult(branch);
        }

        public Task<OrganizationBranch?> GetPrimaryAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.Values.FirstOrDefault(x => x.OrganizationId == organizationId && x.IsPrimary));

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(_byId.Values.Where(x => x.OrganizationId == organizationId).ToList());

        public Task<int> CountActiveAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
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
            ProductCode? productCode, PlanStatus? status, string? search, CatalogListSortBy sortBy, bool sortDescending, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Plan>, int)>(([plan], 1));
        public Task AddAsync(Plan entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<PlanVersion?> GetVersionByIdAsync(PlanVersionId id, CancellationToken cancellationToken = default) => Task.FromResult<PlanVersion?>(null);
        public Task<PlanVersion?> GetVersionByPlanAndNumberAsync(PlanId planId, int versionNumber, CancellationToken cancellationToken = default) => Task.FromResult<PlanVersion?>(null);
        public Task<IReadOnlyList<PlanVersion>> ListVersionsAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlanVersion>>([]);
        public Task<PlanVersion?> GetLatestPublishedVersionAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult<PlanVersion?>(null);
        public Task<int> GetMaxVersionNumberAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task AddVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubSubscriptionRepository(PlanId planId) : ISubscriptionRepository
    {
        private readonly HashSet<Guid> _orgs = [];
        public void Register(PlatformOrganizationId organizationId) => _orgs.Add(organizationId.Value);
        public Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default) => Task.FromResult<Subscription?>(null);
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
            PlatformOrganizationId organizationId, SubscriptionStatus? status, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));
        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByProductAsync(
            ProductCode productCode, SubscriptionStatus? status, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));
        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListExpiringTrialsAsync(
            DateTimeOffset asOfUtc, DateTimeOffset throughUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));
        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByStatusAsync(
            SubscriptionStatus status, int skip, int take, CancellationToken cancellationToken = default) =>
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
