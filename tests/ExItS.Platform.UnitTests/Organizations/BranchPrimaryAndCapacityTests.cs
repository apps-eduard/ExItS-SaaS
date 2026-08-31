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

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class BranchPrimaryAndCapacityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SetPrimaryBranch_atomically_switches_primary()
    {
        var orgId = PlatformOrganizationId.New();
        var branches = new InMemoryOrganizationBranchRepository();
        var main = OrganizationBranch.CreateMainBranch(orgId, T0);
        var east = OrganizationBranch.Create(orgId, "east", "East Branch", T0);
        await branches.AddAsync(main);
        await branches.AddAsync(east);

        var useCase = new SetPrimaryBranch(
            branches,
            new NoOpDeliveryPolicyRepository(),
            new NoOpUnitOfWork(),
            new FixedClock(T0.AddMinutes(1)));
        var result = await useCase.ExecuteAsync(orgId, east.Id);

        Assert.True(result.IsSuccess);
        Assert.True((await branches.GetByIdAsync(east.Id))!.IsPrimary);
        Assert.False((await branches.GetByIdAsync(main.Id))!.IsPrimary);
        Assert.Equal(east.Id, (await branches.GetPrimaryAsync(orgId))!.Id);
    }

    [Fact]
    public async Task ReactivateBranch_rejects_when_active_count_at_MaxBranches()
    {
        var orgId = PlatformOrganizationId.New();
        var branches = new InMemoryOrganizationBranchRepository();
        var main = OrganizationBranch.CreateMainBranch(orgId, T0);
        var east = OrganizationBranch.Create(orgId, "east", "East Branch", T0);
        await branches.AddAsync(main);
        await branches.AddAsync(east);
        east.Suspend(PlatformUserId.New(), "Temporary renovation work", T0.AddMinutes(1));
        await branches.UpdateAsync(east);

        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("starter"),
            "Starter",
            T0,
            maxBranches: 1,
            maxActiveStaff: 10,
            maxActivePosDevices: 1);
        var plans = new StubPlanRepository(plan);
        var subscriptions = new StubSubscriptionRepository(plan.Id);
        subscriptions.Register(orgId);

        var useCase = new ReactivateBranch(
            branches,
            new NoOpDeliveryPolicyRepository(),
            subscriptions,
            plans,
            new NoOpUnitOfWork(),
            new FixedClock(T0.AddMinutes(2)));

        var result = await useCase.ExecuteAsync(orgId, east.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BranchCapacityExceeded, result.ErrorCode);
        Assert.Equal(OrganizationBranchStatus.Inactive, (await branches.GetByIdAsync(east.Id))!.Status);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class NoOpUnitOfWork : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpDeliveryPolicyRepository : IBranchDeliveryPolicyRepository
    {
        public Task<BranchDeliveryPolicy?> GetByBranchIdAsync(OrganizationBranchId branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BranchDeliveryPolicy?>(null);

        public Task<IReadOnlyList<BranchDeliveryPolicy>> ListByOrganizationAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchDeliveryPolicy>>([]);

        public Task AddAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
            Task.FromResult(_byId.Values.Count(x => x.OrganizationId == organizationId && x.Status == OrganizationBranchStatus.Active));

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
