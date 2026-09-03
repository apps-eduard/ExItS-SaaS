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
            new EmptyPhilippineLocalityDirectory(),
            orgRepo,
            new EntitlementQueryService(new InMemoryEntitlementSnapshotRepository()),
            new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator()),
            new AllowAllBranchAccess(),
            new InMemoryOrganizationAreaRepository(),
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

    /// <summary>
    /// AREA02-15: the workspace chooser groups by area, so the branch list carries AreaId/AreaName from
    /// one area read instead of a per-branch lookup.
    /// </summary>
    [Fact]
    public async Task ListBranches_exposes_area_identity_from_a_single_area_read()
    {
        var panay = OrganizationArea.Create(Org, "PANAY", T0);
        var visayas = OrganizationArea.Create(Org, "VISAYAS", T0);
        var main = OrganizationBranch.Create(Org, "BR-MAIN", "Main", T0);
        var cebu = OrganizationBranch.Create(Org, "BR-CEB", "Cebu", T0.AddMinutes(1));
        var manila = OrganizationBranch.Create(Org, "BR-MNL", "Manila", T0.AddMinutes(2));
        main.AssignArea(panay.Id, T0);
        cebu.AssignArea(visayas.Id, T0);

        var areaRepository = new InMemoryOrganizationAreaRepository(panay, visayas);
        var result = await BuildUseCase([main, cebu, manila], areaRepository)
            .ExecuteAsync(Org, PlatformUserId.New());

        Assert.Equal(1, areaRepository.ListByOrganizationCallCount);
        var mainDto = result.Single(dto => dto.Id == main.Id.Value);
        Assert.Equal(panay.Id.Value, mainDto.AreaId);
        Assert.Equal("PANAY", mainDto.AreaName);
        Assert.Equal("VISAYAS", result.Single(dto => dto.Id == cebu.Id.Value).AreaName);
        var manilaDto = result.Single(dto => dto.Id == manila.Id.Value);
        Assert.Null(manilaDto.AreaId);
        Assert.Null(manilaDto.AreaName);
    }

    /// <summary>
    /// AREA02-16: branch access resolution stays the single filter. An area never widens what the
    /// chooser can see, so an inaccessible branch is absent along with its area label.
    /// </summary>
    [Fact]
    public async Task ListBranches_omits_branches_outside_resolved_branch_access()
    {
        var panay = OrganizationArea.Create(Org, "PANAY", T0);
        var main = OrganizationBranch.Create(Org, "BR-MAIN", "Main", T0);
        var iloilo = OrganizationBranch.Create(Org, "BR-ILO", "Iloilo", T0.AddMinutes(1));
        main.AssignArea(panay.Id, T0);
        iloilo.AssignArea(panay.Id, T0);

        var result = await BuildUseCase(
                [main, iloilo],
                new InMemoryOrganizationAreaRepository(panay),
                new ExplicitBranchAccess(main.Id.Value))
            .ExecuteAsync(Org, PlatformUserId.New());

        var only = Assert.Single(result);
        Assert.Equal(main.Id.Value, only.Id);
        Assert.Equal("PANAY", only.AreaName);
        Assert.DoesNotContain(result, dto => dto.Id == iloilo.Id.Value);
    }

    private static ListBranches BuildUseCase(
        IReadOnlyList<OrganizationBranch> branches,
        InMemoryOrganizationAreaRepository areaRepository,
        IOrganizationBranchAccessService? access = null)
    {
        var orgRepo = new InMemoryPlatformOrganizationRepository();
        orgRepo.AddAsync(PlatformOrganization.Create("Test Org", "test-org", T0)).GetAwaiter().GetResult();
        return new ListBranches(
            new FakeBranchRepository(branches),
            new FakePolicyRepository([]),
            new FakeHoursRepository(),
            new FakeAreasRepository(),
            new EmptyPhilippineLocalityDirectory(),
            orgRepo,
            new EntitlementQueryService(new InMemoryEntitlementSnapshotRepository()),
            new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator()),
            access ?? new AllowAllBranchAccess(),
            areaRepository,
            new FixedClock(T0));
    }

    private sealed class ExplicitBranchAccess(params Guid[] accessibleBranchIds) : IOrganizationBranchAccessService
    {
        public Task<bool> CanAccessBranchAsync(
            PlatformUserId userId,
            PlatformOrganizationId organizationId,
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(accessibleBranchIds.Contains(branchId.Value));

        public Task<IReadOnlySet<Guid>?> ResolveAccessibleActiveBranchIdsAsync(
            PlatformUserId userId,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>?>(accessibleBranchIds.ToHashSet());
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
