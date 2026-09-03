using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

/// <summary>
/// POS-AREA-HARDENING-CLOSEOUT. Branch staff counts must reflect every scope that actually reaches a
/// branch, and area archiving must see the whole roster rather than the first page of it.
/// </summary>
public sealed class BranchAreaHardeningTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AREAH_01_explicit_staff_count_only_on_their_assigned_branches()
    {
        var fixture = new SummaryFixture();
        var main = fixture.SeedBranch("MAIN", "Main");
        var iloilo = fixture.SeedBranch("ILO", "Iloilo");
        var membership = await fixture.SeedStaffAsync(BranchAccessScope.Explicit);
        await fixture.GrantBranchesAsync(membership, main.Id);

        var summaries = await fixture.Execute();

        Assert.Equal(1, Count(summaries, main));
        Assert.Equal(0, Count(summaries, iloilo));
    }

    [Fact]
    public async Task AREAH_02_area_scoped_staff_count_on_every_branch_of_a_granted_active_area()
    {
        var fixture = new SummaryFixture();
        var panay = fixture.SeedArea("PANAY");
        var main = fixture.SeedBranch("MAIN", "Main", panay.Id);
        var iloilo = fixture.SeedBranch("ILO", "Iloilo", panay.Id);
        var cebu = fixture.SeedBranch("CEB", "Cebu");
        var membership = await fixture.SeedStaffAsync(BranchAccessScope.Areas);
        await fixture.GrantAreasAsync(membership, panay.Id);

        var summaries = await fixture.Execute();

        Assert.Equal(1, Count(summaries, main));
        Assert.Equal(1, Count(summaries, iloilo));
        Assert.Equal(0, Count(summaries, cebu));
    }

    [Fact]
    public async Task AREAH_03_area_grant_to_an_archived_area_grants_nothing()
    {
        var fixture = new SummaryFixture();
        var panay = fixture.SeedArea("PANAY");
        var main = fixture.SeedBranch("MAIN", "Main", panay.Id);
        var membership = await fixture.SeedStaffAsync(BranchAccessScope.Areas);
        await fixture.GrantAreasAsync(membership, panay.Id);
        panay.Archive(T0);
        await fixture.Areas.UpdateAsync(panay);

        var summaries = await fixture.Execute();

        Assert.Equal(0, Count(summaries, main));
    }

    [Fact]
    public async Task AREAH_04_all_active_staff_count_on_every_active_branch_but_not_inactive_ones()
    {
        var fixture = new SummaryFixture();
        var main = fixture.SeedBranch("MAIN", "Main");
        var iloilo = fixture.SeedBranch("ILO", "Iloilo");
        var closed = fixture.SeedBranch("OLD", "Old");
        closed.Deactivate(T0);
        await fixture.SeedStaffAsync(BranchAccessScope.AllActive);

        var summaries = await fixture.Execute();

        Assert.Equal(1, Count(summaries, main));
        Assert.Equal(1, Count(summaries, iloilo));
        Assert.Equal(0, Count(summaries, closed));
    }

    [Fact]
    public async Task AREAH_05_moving_a_branch_between_areas_moves_the_staff_count_with_it()
    {
        var fixture = new SummaryFixture();
        var panay = fixture.SeedArea("PANAY");
        var visayas = fixture.SeedArea("VISAYAS");
        var iloilo = fixture.SeedBranch("ILO", "Iloilo", panay.Id);
        var cebu = fixture.SeedBranch("CEB", "Cebu", visayas.Id);
        var membership = await fixture.SeedStaffAsync(BranchAccessScope.Areas);
        await fixture.GrantAreasAsync(membership, panay.Id);

        var before = await fixture.Execute();
        Assert.Equal(1, Count(before, iloilo));
        Assert.Equal(0, Count(before, cebu));

        iloilo.AssignArea(visayas.Id, T0);
        cebu.AssignArea(panay.Id, T0);

        var after = await fixture.Execute();

        Assert.Equal(0, Count(after, iloilo));
        Assert.Equal(1, Count(after, cebu));
    }

    [Fact]
    public async Task AREAH_06_owner_and_administrator_seats_stay_out_of_the_branch_staff_count()
    {
        var fixture = new SummaryFixture();
        var main = fixture.SeedBranch("MAIN", "Main");
        await fixture.SeedStaffAsync(BranchAccessScope.AllActive, OrganizationRole.OrganizationOwner);
        await fixture.SeedStaffAsync(BranchAccessScope.AllActive, OrganizationRole.OrganizationAdministrator);
        await fixture.SeedStaffAsync(BranchAccessScope.AllActive);

        var summaries = await fixture.Execute();

        Assert.Equal(1, Count(summaries, main));
    }

    [Fact]
    public async Task AREAH_07_staff_beyond_the_first_page_of_the_roster_are_still_counted()
    {
        var fixture = new SummaryFixture();
        var main = fixture.SeedBranch("MAIN", "Main");
        for (var i = 0; i < 620; i++)
        {
            await fixture.SeedStaffAsync(BranchAccessScope.AllActive);
        }

        var summaries = await fixture.Execute();

        Assert.Equal(620, Count(summaries, main));
        // The roster is read whole, never through the paged first-500 window.
        Assert.Equal(1, fixture.Memberships.ListActiveCallCount);
        Assert.Equal(0, fixture.Memberships.ListByOrganizationCallCount);
    }

    [Fact]
    public async Task AREAH_08_counting_reads_each_source_once_regardless_of_members_or_branches()
    {
        var fixture = new SummaryFixture();
        var panay = fixture.SeedArea("PANAY");
        for (var i = 0; i < 12; i++)
        {
            fixture.SeedBranch($"BR{i:00}", $"Branch {i:00}", panay.Id);
        }

        for (var i = 0; i < 30; i++)
        {
            var membership = await fixture.SeedStaffAsync(BranchAccessScope.Areas);
            await fixture.GrantAreasAsync(membership, panay.Id);
        }

        await fixture.Execute();

        Assert.Equal(1, fixture.Memberships.ListActiveCallCount);
        Assert.Equal(1, fixture.Assignments.ListByOrganizationCallCount);
        Assert.Equal(1, fixture.AreaAssignments.ListByOrganizationCallCount);
    }

    private static int Count(IReadOnlyList<BranchManagementSummaryItemDto> summaries, OrganizationBranch branch) =>
        summaries.Single(s => s.Id == branch.Id.Value).AssignedStaffCount;

    private sealed class SummaryFixture
    {
        public PlatformOrganizationId Org { get; } = PlatformOrganizationId.New();
        public InMemoryOrganizationAreaRepository Areas { get; } = new();
        public OrganizationAreaGovernanceTests.MutableBranchRepo Branches { get; } = new();
        public InMemoryOrganizationMembershipRepository Memberships { get; } = new();
        public CountingBranchAssignmentRepository Assignments { get; } = new();
        public CountingAreaAssignmentRepository AreaAssignments { get; } = new();

        public OrganizationArea SeedArea(string name)
        {
            var area = OrganizationArea.Create(Org, name, T0);
            Areas.AddAsync(area).GetAwaiter().GetResult();
            return area;
        }

        public OrganizationBranch SeedBranch(string code, string name, OrganizationAreaId? areaId = null)
        {
            var branch = OrganizationBranch.Create(Org, code, name, T0);
            if (areaId is not null)
            {
                branch.AssignArea(areaId, T0);
            }

            Branches.AddAsync(branch).GetAwaiter().GetResult();
            return branch;
        }

        public async Task<OrganizationMembership> SeedStaffAsync(
            BranchAccessScope scope,
            OrganizationRole role = OrganizationRole.OrganizationMember)
        {
            var membership = OrganizationMembership.Create(
                Org,
                PlatformUserId.New(),
                role,
                T0,
                branchAccessScope: scope);
            await Memberships.AddAsync(membership);
            return membership;
        }

        public Task GrantBranchesAsync(OrganizationMembership membership, params OrganizationBranchId[] branchIds) =>
            Assignments.ReplaceForMembershipAsync(Org, membership.Id, branchIds, T0, "actor");

        public Task GrantAreasAsync(OrganizationMembership membership, params OrganizationAreaId[] areaIds) =>
            AreaAssignments.ReplaceForMembershipAsync(Org, membership.Id, areaIds, T0, "actor");

        public async Task<IReadOnlyList<BranchManagementSummaryItemDto>> Execute()
        {
            var organizations = new InMemoryPlatformOrganizationRepository();
            await organizations.AddAsync(PlatformOrganization.Create("Test Org", "test-org", T0));
            var listBranches = new ListBranches(
                Branches,
                new NoPolicyRepository(),
                new NoHoursRepository(),
                new NoServiceAreaRepository(),
                new EmptyPhilippineLocalityDirectory(),
                organizations,
                new EntitlementQueryService(new InMemoryEntitlementSnapshotRepository()),
                new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator()),
                new AllBranchesVisible(),
                Areas,
                new FixedClock(T0));

            var summaries = new ListBranchManagementSummaries(
                listBranches,
                Assignments,
                new NoDeviceRepository(),
                Memberships,
                Areas,
                AreaAssignments);

            var result = await summaries.ExecuteAsync(Org, PlatformUserId.New());
            Assert.True(result.IsSuccess);
            return result.Value!;
        }
    }

    internal sealed class CountingBranchAssignmentRepository : IOrganizationMembershipBranchAssignmentRepository
    {
        private readonly List<OrganizationMembershipBranchAssignment> _items = [];

        public int ListByOrganizationCallCount { get; private set; }

        public Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByMembershipAsync(
            OrganizationMembershipId membershipId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationMembershipBranchAssignment>>(
                _items.Where(x => x.MembershipId == membershipId).ToList());

        public Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            ListByOrganizationCallCount++;
            return Task.FromResult<IReadOnlyList<OrganizationMembershipBranchAssignment>>(
                _items.Where(x => x.OrganizationId == organizationId).ToList());
        }

        public Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByBranchAsync(
            PlatformOrganizationId organizationId,
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationMembershipBranchAssignment>>(
                _items.Where(x => x.OrganizationId == organizationId && x.BranchId == branchId).ToList());

        public Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByUserAndOrganizationAsync(
            PlatformUserId userId,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationMembershipBranchAssignment>>([]);

        public Task ReplaceForMembershipAsync(
            PlatformOrganizationId organizationId,
            OrganizationMembershipId membershipId,
            IReadOnlyCollection<OrganizationBranchId> branchIds,
            DateTimeOffset utcNow,
            string? actorReference,
            CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(x => x.MembershipId == membershipId);
            foreach (var branchId in branchIds)
            {
                _items.Add(OrganizationMembershipBranchAssignment.Create(
                    organizationId,
                    membershipId,
                    branchId,
                    utcNow,
                    actorReference: actorReference));
            }

            return Task.CompletedTask;
        }

        public Task AssignPrimaryBranchForNewStaffAsync(
            PlatformOrganizationId organizationId,
            OrganizationMembershipId membershipId,
            DateTimeOffset utcNow,
            string? actorReference,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    internal sealed class CountingAreaAssignmentRepository : IOrganizationMembershipAreaAssignmentRepository
    {
        private readonly List<OrganizationMembershipAreaAssignment> _items = [];

        public int ListByOrganizationCallCount { get; private set; }

        public Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByMembershipAsync(
            OrganizationMembershipId membershipId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationMembershipAreaAssignment>>(
                _items.Where(x => x.MembershipId == membershipId).ToList());

        public Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            ListByOrganizationCallCount++;
            return Task.FromResult<IReadOnlyList<OrganizationMembershipAreaAssignment>>(
                _items.Where(x => x.OrganizationId == organizationId).ToList());
        }

        public Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByUserAndOrganizationAsync(
            PlatformUserId userId,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationMembershipAreaAssignment>>([]);

        public Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByAreaAsync(
            PlatformOrganizationId organizationId,
            OrganizationAreaId areaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationMembershipAreaAssignment>>(
                _items.Where(x => x.OrganizationId == organizationId && x.AreaId == areaId).ToList());

        public Task ReplaceForMembershipAsync(
            PlatformOrganizationId organizationId,
            OrganizationMembershipId membershipId,
            IReadOnlyCollection<OrganizationAreaId> areaIds,
            DateTimeOffset utcNow,
            string? actorReference,
            CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(x => x.MembershipId == membershipId);
            foreach (var areaId in areaIds)
            {
                _items.Add(OrganizationMembershipAreaAssignment.Create(
                    organizationId,
                    membershipId,
                    areaId,
                    utcNow,
                    actorReference: actorReference));
            }

            return Task.CompletedTask;
        }
    }

    private sealed class AllBranchesVisible : IOrganizationBranchAccessService
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

    private sealed class NoPolicyRepository : IBranchDeliveryPolicyRepository
    {
        public Task<BranchDeliveryPolicy?> GetByBranchIdAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BranchDeliveryPolicy?>(null);

        public Task<IReadOnlyList<BranchDeliveryPolicy>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchDeliveryPolicy>>([]);

        public Task AddAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoHoursRepository : IBranchOperatingHoursRepository
    {
        public Task<BranchOperatingHoursSchedule?> GetByBranchIdAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BranchOperatingHoursSchedule?>(null);

        public Task<IReadOnlyDictionary<Guid, BranchOperatingHoursSchedule>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, BranchOperatingHoursSchedule>>(
                new Dictionary<Guid, BranchOperatingHoursSchedule>());

        public Task UpsertAsync(
            BranchOperatingHoursSchedule schedule,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoServiceAreaRepository : IBranchDeliveryServiceAreaRepository
    {
        public Task<BranchDeliveryServiceArea?> GetByIdAsync(
            BranchDeliveryServiceAreaId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BranchDeliveryServiceArea?>(null);

        public Task<IReadOnlyList<BranchDeliveryServiceArea>> ListByBranchAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchDeliveryServiceArea>>([]);

        public Task<IReadOnlyList<BranchDeliveryServiceArea>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchDeliveryServiceArea>>([]);

        public Task<IReadOnlyDictionary<Guid, int>> CountActiveByBranchIdsAsync(
            PlatformOrganizationId organizationId,
            IReadOnlyCollection<OrganizationBranchId> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());

        public Task AddAsync(BranchDeliveryServiceArea area, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(BranchDeliveryServiceArea area, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoDeviceRepository : IPosDeviceRepository
    {
        public Task<PosDevice?> GetByIdAsync(PosDeviceId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PosDevice?>(null);

        public Task<PosDevice?> GetByInstallationDeviceIdAsync(
            PlatformOrganizationId organizationId,
            string installationDeviceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PosDevice?>(null);

        public Task<IReadOnlyList<PosDevice>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PosDevice>>([]);

        public Task<IReadOnlyList<PosDevice>> ListActiveByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PosDevice>>([]);

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task AddAsync(PosDevice device, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(PosDevice device, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<PosDevice?> FindByInstallationDeviceIdAsync(
            string installationDeviceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PosDevice?>(null);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
