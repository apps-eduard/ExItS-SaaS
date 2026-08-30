using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class BranchListBulkPolicyTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlatformOrganizationId Org = PlatformOrganizationId.From(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    [Fact]
    public async Task ListBranches_loads_policies_once_via_ListByOrganizationAsync()
    {
        var branchA = OrganizationBranch.Create(Org, "BR-A", "Branch A", T0);
        var branchB = OrganizationBranch.Create(Org, "BR-B", "Branch B", T0.AddMinutes(1));
        var policyA = BranchDeliveryPolicy.CreateDefault(branchA.Id, Org, T0);
        var policyB = BranchDeliveryPolicy.CreateDefault(branchB.Id, Org, T0);

        var branches = new FakeBranchRepository([branchA, branchB]);
        var policies = new FakePolicyRepository([policyA, policyB]);
        var hours = new FakeHoursRepository();
        var areas = new FakeAreasRepository();
        var orgRepo = new InMemoryPlatformOrganizationRepository();
        await orgRepo.AddAsync(PlatformOrganization.Create("Test Org", "test-org", T0));
        var useCase = new ListBranches(
            branches,
            policies,
            hours,
            areas,
            orgRepo,
            new EntitlementQueryService(new InMemoryEntitlementSnapshotRepository()),
            new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator()),
            new AllowAllBranchAccess(),
            new FixedClock(T0));

        var result = await useCase.ExecuteAsync(Org, PlatformUserId.New());

        Assert.Equal(2, result.Count);
        Assert.Equal(1, policies.ListByOrganizationCallCount);
        Assert.Equal(0, policies.GetByBranchIdCallCount);
        Assert.Equal(1, hours.ListByOrganizationCallCount);
        Assert.Equal(0, hours.GetByBranchIdCallCount);
        Assert.Equal(1, areas.CountActiveByBranchIdsCallCount);
        Assert.All(result, dto => Assert.NotNull(dto.DeliveryPolicy));
    }

    private sealed class FakeBranchRepository(IReadOnlyList<OrganizationBranch> items) : IOrganizationBranchRepository
    {
        public Task<OrganizationBranch?> GetByIdAsync(
            OrganizationBranchId id,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(items);

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<OrganizationBranch?> GetPrimaryAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakePolicyRepository(IReadOnlyList<BranchDeliveryPolicy> items) : IBranchDeliveryPolicyRepository
    {
        public int ListByOrganizationCallCount { get; private set; }
        public int GetByBranchIdCallCount { get; private set; }

        public Task<BranchDeliveryPolicy?> GetByBranchIdAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default)
        {
            GetByBranchIdCallCount++;
            return Task.FromResult(items.FirstOrDefault(p => p.BranchId == branchId));
        }

        public Task<IReadOnlyList<BranchDeliveryPolicy>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            ListByOrganizationCallCount++;
            return Task.FromResult(items);
        }

        public Task AddAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeHoursRepository : IBranchOperatingHoursRepository
    {
        public int ListByOrganizationCallCount { get; private set; }
        public int GetByBranchIdCallCount { get; private set; }

        public Task<BranchOperatingHoursSchedule?> GetByBranchIdAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default)
        {
            GetByBranchIdCallCount++;
            return Task.FromResult<BranchOperatingHoursSchedule?>(null);
        }

        public Task<IReadOnlyDictionary<Guid, BranchOperatingHoursSchedule>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            ListByOrganizationCallCount++;
            return Task.FromResult<IReadOnlyDictionary<Guid, BranchOperatingHoursSchedule>>(
                new Dictionary<Guid, BranchOperatingHoursSchedule>());
        }

        public Task UpsertAsync(
            BranchOperatingHoursSchedule schedule,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeAreasRepository : IBranchDeliveryServiceAreaRepository
    {
        public int CountActiveByBranchIdsCallCount { get; private set; }

        public Task<BranchDeliveryServiceArea?> GetByIdAsync(
            BranchDeliveryServiceAreaId id,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<BranchDeliveryServiceArea>> ListByBranchAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<BranchDeliveryServiceArea>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchDeliveryServiceArea>>([]);

        public Task<IReadOnlyDictionary<Guid, int>> CountActiveByBranchIdsAsync(
            PlatformOrganizationId organizationId,
            IReadOnlyCollection<OrganizationBranchId> branchIds,
            CancellationToken cancellationToken = default)
        {
            CountActiveByBranchIdsCallCount++;
            return Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());
        }

        public Task AddAsync(BranchDeliveryServiceArea area, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(BranchDeliveryServiceArea area, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class AllowAllBranchAccess : IOrganizationBranchAccessService
    {
        public Task<bool> CanAccessBranchAsync(
            PlatformUserId userId,
            PlatformOrganizationId organizationId,
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlySet<Guid>?> ResolveAccessibleActiveBranchIdsAsync(
            PlatformUserId userId,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>?>(null);
    }
}
