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

/// <summary>
/// POS-AREA-01 governance and staff scope.
/// An Area groups branches for access, navigation, and reporting. It never owns stock,
/// reservations, registers, shifts, sales, or receiving — those stay on the branch.
/// </summary>
public sealed class OrganizationAreaGovernanceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AREA01_01_new_and_rehydrated_branches_start_without_an_area()
    {
        var org = PlatformOrganizationId.New();

        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);

        Assert.Null(main.AreaId);
        Assert.Null(north.AreaId);
    }

    [Fact]
    public async Task AREA01_02_area_is_created_while_under_the_plan_limit()
    {
        var fixture = new AreaFixture(maxAreas: 3);

        var result = await fixture.CreateArea().ExecuteAsync(
            fixture.Org,
            new CreateOrganizationAreaCommand("Metro North", "NCR-NORTH"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Metro North", result.Value!.Name);
        Assert.Equal("NCR-NORTH", result.Value.Code);
        Assert.Equal(OrganizationAreaStatus.Active, result.Value.Status);
        Assert.Equal(0, result.Value.BranchCount);
        Assert.Single(fixture.Areas.Items);
    }

    [Fact]
    public async Task AREA01_03_creating_past_MaxAreas_is_rejected_with_a_capacity_error()
    {
        var fixture = new AreaFixture(maxAreas: 1);
        var create = fixture.CreateArea();
        Assert.True((await create.ExecuteAsync(fixture.Org, new CreateOrganizationAreaCommand("Metro North"))).IsSuccess);

        var second = await create.ExecuteAsync(fixture.Org, new CreateOrganizationAreaCommand("Metro South"));

        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AreaCapacityExceeded, second.ErrorCode);
        Assert.Single(fixture.Areas.Items);
    }

    [Fact]
    public async Task AREA01_04_assign_move_and_unassign_change_grouping_only()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var south = fixture.SeedArea("Metro South");
        var branch = fixture.SeedBranch("NORTH", "North");
        var setArea = fixture.SetBranchArea();

        var assigned = await setArea.ExecuteAsync(fixture.Org, branch.Id, north.Id);
        Assert.True(assigned.IsSuccess);
        Assert.Equal(north.Id, branch.AreaId);

        var moved = await setArea.ExecuteAsync(fixture.Org, branch.Id, south.Id);
        Assert.True(moved.IsSuccess);
        Assert.Equal(south.Id, branch.AreaId);

        var cleared = await setArea.ExecuteAsync(fixture.Org, branch.Id, null);
        Assert.True(cleared.IsSuccess);
        Assert.Null(branch.AreaId);

        // Grouping only: the branch keeps its own identity and operational status throughout.
        Assert.Equal(OrganizationBranchStatus.Active, branch.Status);
        Assert.Equal("NORTH", branch.Code);
    }

    [Fact]
    public async Task AREA01_05_branch_cannot_be_placed_in_another_organizations_area()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var branch = fixture.SeedBranch("NORTH", "North");
        var foreignOrg = PlatformOrganizationId.New();
        var foreignArea = OrganizationArea.Create(foreignOrg, "Foreign Area", T0);
        await fixture.Areas.AddAsync(foreignArea);

        var result = await fixture.SetBranchArea().ExecuteAsync(fixture.Org, branch.Id, foreignArea.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AreaNotFound, result.ErrorCode);
        Assert.Null(branch.AreaId);
    }

    [Fact]
    public async Task AREA01_06_assigning_a_second_area_moves_the_branch_instead_of_duplicating_it()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var south = fixture.SeedArea("Metro South");
        var branch = fixture.SeedBranch("NORTH", "North");
        var setArea = fixture.SetBranchArea();

        await setArea.ExecuteAsync(fixture.Org, branch.Id, north.Id);
        await setArea.ExecuteAsync(fixture.Org, branch.Id, south.Id);

        Assert.Equal(south.Id, branch.AreaId);
        Assert.Equal(0, await UpdateOrganizationArea.CountAssignedBranchesAsync(
            fixture.Branches, fixture.Org, north.Id, CancellationToken.None));
        Assert.Equal(1, await UpdateOrganizationArea.CountAssignedBranchesAsync(
            fixture.Branches, fixture.Org, south.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AREA01_07_area_scope_resolves_only_branches_inside_the_granted_areas()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var south = fixture.SeedArea("Metro South");
        var inNorth = fixture.SeedBranch("NORTH", "North", north.Id);
        var inSouth = fixture.SeedBranch("SOUTH", "South", south.Id);
        var (user, membership) = await fixture.SeedStaffAsync();
        await fixture.GrantAreasAsync(membership, north.Id);

        var accessible = await fixture.AccessService().ResolveAccessibleActiveBranchIdsAsync(user, fixture.Org);

        Assert.NotNull(accessible);
        Assert.Single(accessible!);
        Assert.Contains(inNorth.Id.Value, accessible);
        Assert.DoesNotContain(inSouth.Id.Value, accessible);
    }

    [Fact]
    public async Task AREA01_08_branch_added_to_a_granted_area_becomes_accessible()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        fixture.SeedBranch("NORTH", "North", north.Id);
        var (user, membership) = await fixture.SeedStaffAsync();
        await fixture.GrantAreasAsync(membership, north.Id);
        var sut = fixture.AccessService();

        var opened = fixture.SeedBranch("NORTH2", "North Annex");
        Assert.False(await sut.CanAccessBranchAsync(user, fixture.Org, opened.Id));

        await fixture.SetBranchArea().ExecuteAsync(fixture.Org, opened.Id, north.Id);

        Assert.True(await sut.CanAccessBranchAsync(user, fixture.Org, opened.Id));
    }

    [Fact]
    public async Task AREA01_09_branch_moved_out_of_a_granted_area_loses_access()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var south = fixture.SeedArea("Metro South");
        var branch = fixture.SeedBranch("NORTH", "North", north.Id);
        var (user, membership) = await fixture.SeedStaffAsync();
        await fixture.GrantAreasAsync(membership, north.Id);
        var sut = fixture.AccessService();
        Assert.True(await sut.CanAccessBranchAsync(user, fixture.Org, branch.Id));

        await fixture.SetBranchArea().ExecuteAsync(fixture.Org, branch.Id, south.Id);

        Assert.False(await sut.CanAccessBranchAsync(user, fixture.Org, branch.Id));
    }

    [Fact]
    public async Task AREA01_10_staff_of_the_destination_area_gain_the_moved_branch()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var south = fixture.SeedArea("Metro South");
        var branch = fixture.SeedBranch("NORTH", "North", north.Id);
        var (southUser, southMembership) = await fixture.SeedStaffAsync();
        await fixture.GrantAreasAsync(southMembership, south.Id);
        var sut = fixture.AccessService();
        Assert.False(await sut.CanAccessBranchAsync(southUser, fixture.Org, branch.Id));

        await fixture.SetBranchArea().ExecuteAsync(fixture.Org, branch.Id, south.Id);

        Assert.True(await sut.CanAccessBranchAsync(southUser, fixture.Org, branch.Id));
    }

    [Fact]
    public async Task AREA01_11_branches_without_an_area_are_excluded_from_area_scope()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var inNorth = fixture.SeedBranch("NORTH", "North", north.Id);
        var orphan = fixture.SeedBranch("WAREHOUSE", "Warehouse");
        var (user, membership) = await fixture.SeedStaffAsync();
        await fixture.GrantAreasAsync(membership, north.Id);

        var sut = fixture.AccessService();
        var accessible = await sut.ResolveAccessibleActiveBranchIdsAsync(user, fixture.Org);

        Assert.NotNull(accessible);
        Assert.Contains(inNorth.Id.Value, accessible!);
        Assert.DoesNotContain(orphan.Id.Value, accessible!);
        Assert.False(await sut.CanAccessBranchAsync(user, fixture.Org, orphan.Id));
    }

    [Fact]
    public async Task AREA01_12_explicit_scope_is_unchanged_by_areas()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var inNorth = fixture.SeedBranch("NORTH", "North", north.Id);
        var warehouse = fixture.SeedBranch("WAREHOUSE", "Warehouse");
        var (user, membership) = await fixture.SeedStaffAsync(BranchAccessScope.Explicit);
        await fixture.Assignments.ReplaceForMembershipAsync(
            fixture.Org, membership.Id, [warehouse.Id], T0, "actor");

        var sut = fixture.AccessService();
        var accessible = await sut.ResolveAccessibleActiveBranchIdsAsync(user, fixture.Org);

        Assert.NotNull(accessible);
        Assert.Single(accessible!);
        Assert.Contains(warehouse.Id.Value, accessible);
        Assert.DoesNotContain(inNorth.Id.Value, accessible);
    }

    [Fact]
    public async Task AREA01_13_all_active_scope_is_unchanged_by_areas()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        fixture.SeedBranch("NORTH", "North", north.Id);
        var orphan = fixture.SeedBranch("WAREHOUSE", "Warehouse");
        var (user, _) = await fixture.SeedStaffAsync(BranchAccessScope.AllActive);

        var sut = fixture.AccessService();

        Assert.Null(await sut.ResolveAccessibleActiveBranchIdsAsync(user, fixture.Org));
        Assert.True(await sut.CanAccessBranchAsync(user, fixture.Org, orphan.Id));
    }

    [Fact]
    public async Task AREA01_14_owner_and_administrator_keep_implicit_access_to_every_branch()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        fixture.SeedBranch("NORTH", "North", north.Id);
        var orphan = fixture.SeedBranch("WAREHOUSE", "Warehouse");
        var (owner, _) = await fixture.SeedStaffAsync(role: OrganizationRole.OrganizationOwner);
        var (admin, _) = await fixture.SeedStaffAsync(role: OrganizationRole.OrganizationAdministrator);
        var sut = fixture.AccessService();

        Assert.Null(await sut.ResolveAccessibleActiveBranchIdsAsync(owner, fixture.Org));
        Assert.Null(await sut.ResolveAccessibleActiveBranchIdsAsync(admin, fixture.Org));
        Assert.True(await sut.CanAccessBranchAsync(owner, fixture.Org, orphan.Id));
        Assert.True(await sut.CanAccessBranchAsync(admin, fixture.Org, orphan.Id));
    }

    [Fact]
    public async Task AREA01_15_switching_away_from_area_scope_clears_stale_area_grants()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var inNorth = fixture.SeedBranch("NORTH", "North", north.Id);
        var warehouse = fixture.SeedBranch("WAREHOUSE", "Warehouse");
        var (_, membership) = await fixture.SeedStaffAsync();
        var set = fixture.SetAssignments();

        var toAreas = await set.ExecuteAsync(
            fixture.Org,
            membership.Id,
            new SetMembershipBranchAssignmentsCommand("Areas", null, [north.Id.Value]),
            "actor");
        Assert.True(toAreas.IsSuccess);
        Assert.Equal(nameof(BranchAccessScope.Areas), toAreas.Value!.Scope);
        Assert.Single(toAreas.Value.Areas!);
        Assert.Equal(inNorth.Id.Value, Assert.Single(toAreas.Value.Branches).BranchId);
        Assert.Single(await fixture.AreaAssignments.ListByMembershipAsync(membership.Id));

        var toExplicit = await set.ExecuteAsync(
            fixture.Org,
            membership.Id,
            new SetMembershipBranchAssignmentsCommand("Explicit", [warehouse.Id.Value]),
            "actor");
        Assert.True(toExplicit.IsSuccess);
        Assert.Empty(await fixture.AreaAssignments.ListByMembershipAsync(membership.Id));

        var backToAreas = await set.ExecuteAsync(
            fixture.Org,
            membership.Id,
            new SetMembershipBranchAssignmentsCommand("Areas", null, [north.Id.Value]),
            "actor");
        Assert.True(backToAreas.IsSuccess);
        Assert.Empty(await fixture.Assignments.ListByMembershipAsync(membership.Id));

        var toAllActive = await set.ExecuteAsync(
            fixture.Org,
            membership.Id,
            new SetMembershipBranchAssignmentsCommand("AllActive", null),
            "actor");
        Assert.True(toAllActive.IsSuccess);
        Assert.Empty(await fixture.AreaAssignments.ListByMembershipAsync(membership.Id));
        Assert.Empty(await fixture.Assignments.ListByMembershipAsync(membership.Id));
    }

    [Fact]
    public async Task AREA01_16_archive_is_blocked_while_branches_or_staff_grants_remain()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var branch = fixture.SeedBranch("NORTH", "North", north.Id);
        var (_, membership) = await fixture.SeedStaffAsync();
        await fixture.GrantAreasAsync(membership, north.Id);
        var archive = fixture.ArchiveArea();

        var blockedByBranch = await archive.ExecuteAsync(fixture.Org, north.Id);
        Assert.False(blockedByBranch.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AreaArchiveBlocked, blockedByBranch.ErrorCode);

        await fixture.SetBranchArea().ExecuteAsync(fixture.Org, branch.Id, null);

        var blockedByStaff = await archive.ExecuteAsync(fixture.Org, north.Id);
        Assert.False(blockedByStaff.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AreaArchiveBlocked, blockedByStaff.ErrorCode);

        await fixture.AreaAssignments.ReplaceForMembershipAsync(fixture.Org, membership.Id, [], T0, "actor");

        var archived = await archive.ExecuteAsync(fixture.Org, north.Id);
        Assert.True(archived.IsSuccess);
        Assert.Equal(OrganizationAreaStatus.Archived, archived.Value!.Status);

        // Archiving never cascades: the branch survives untouched.
        Assert.Equal(OrganizationBranchStatus.Active, branch.Status);
    }

    /// <summary>
    /// AREAH-17: a grant held by staff outside the first roster page still blocks the archive.
    /// The check asks whether any granted membership is active rather than paging the roster.
    /// </summary>
    [Fact]
    public async Task AREAH_17_archive_sees_grants_held_beyond_the_first_roster_page()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        for (var i = 0; i < 640; i++)
        {
            await fixture.SeedStaffAsync();
        }

        var (_, lateJoiner) = await fixture.SeedStaffAsync();
        await fixture.GrantAreasAsync(lateJoiner, north.Id);
        var archive = fixture.ArchiveArea();

        var blocked = await archive.ExecuteAsync(fixture.Org, north.Id);

        Assert.False(blocked.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AreaArchiveBlocked, blocked.ErrorCode);
        Assert.Equal(0, fixture.Memberships.ListByOrganizationCallCount);
        Assert.Equal(1, fixture.Memberships.AnyActiveCallCount);
    }

    /// <summary>
    /// AREAH-18: a grant left behind by staff who have since been removed must not block the archive.
    /// </summary>
    [Fact]
    public async Task AREAH_18_grants_from_inactive_staff_do_not_block_the_archive()
    {
        var fixture = new AreaFixture(maxAreas: 5);
        var north = fixture.SeedArea("Metro North");
        var (_, membership) = await fixture.SeedStaffAsync();
        await fixture.GrantAreasAsync(membership, north.Id);
        membership.Remove(T0, "actor");
        await fixture.Memberships.UpdateAsync(membership);

        var archived = await fixture.ArchiveArea().ExecuteAsync(fixture.Org, north.Id);

        Assert.True(archived.IsSuccess);
        Assert.Equal(OrganizationAreaStatus.Archived, archived.Value!.Status);
    }

    [Fact]
    public void AREA01_17_no_area_inventory_or_operational_types_are_introduced()
    {
        var forbiddenTypeNames = new[]
        {
            "AreaInventoryBalance",
            "AreaInventoryReservation",
            "AreaStockBalance",
            "AreaInventoryMovement",
            "AreaRegister",
            "AreaShift",
            "AreaSale",
            "AreaReceiving",
        };

        foreach (var assembly in new[]
                 {
                     typeof(OrganizationArea).Assembly,
                     typeof(ListOrganizationAreas).Assembly,
                 })
        {
            foreach (var forbidden in forbiddenTypeNames)
            {
                Assert.DoesNotContain(
                    assembly.GetTypes(),
                    type => type.Name.Contains(forbidden, StringComparison.Ordinal));
            }
        }

        var areaSources = new[]
        {
            Path.Combine("src", "Platform", "ExItS.Platform.Domain", "Organizations", "OrganizationArea.cs"),
            Path.Combine("src", "Platform", "ExItS.Platform.Application", "Organizations", "AreaUseCases.cs"),
        };
        var forbiddenTokens = new[] { "Inventory", "Stock", "Reservation", "Register", "Shift", "Receiving" };
        foreach (var relativePath in areaSources)
        {
            var text = File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath));
            foreach (var token in forbiddenTokens)
            {
                // Prose in the "no operational authority" comments is allowed; declarations are not.
                Assert.DoesNotContain($"class Area{token}", text, StringComparison.Ordinal);
                Assert.DoesNotContain($"record Area{token}", text, StringComparison.Ordinal);
                Assert.DoesNotContain($"AreaId {token}", text, StringComparison.Ordinal);
            }
        }
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

    private sealed class AreaFixture
    {
        private readonly Plan _plan;

        public AreaFixture(int maxAreas)
        {
            Org = PlatformOrganizationId.New();
            _plan = Plan.CreateDraft(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                PlanCode.Create("starter"),
                "Starter",
                T0,
                maxBranches: 20,
                maxActiveStaff: 20,
                maxActivePosDevices: 20,
                maxAreas: maxAreas);
            Plans = new StubPlanRepository(_plan);
            Subscriptions = new StubSubscriptionRepository(_plan.Id);
            Subscriptions.Register(Org);
        }

        public PlatformOrganizationId Org { get; }
        public InMemoryOrganizationAreaRepository Areas { get; } = new();
        public MutableBranchRepo Branches { get; } = new();
        public InMemoryOrganizationMembershipRepository Memberships { get; } = new();
        public InMemoryOrganizationMembershipBranchAssignmentRepository Assignments { get; } = new();
        public InMemoryOrganizationMembershipAreaAssignmentRepository AreaAssignments { get; } = new();
        public StubPlanRepository Plans { get; }
        public StubSubscriptionRepository Subscriptions { get; }

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

        public async Task<(PlatformUserId User, OrganizationMembership Membership)> SeedStaffAsync(
            BranchAccessScope scope = BranchAccessScope.Areas,
            OrganizationRole role = OrganizationRole.OrganizationMember)
        {
            var user = PlatformUserId.New();
            var membership = OrganizationMembership.Create(Org, user, role, T0, branchAccessScope: scope);
            await Memberships.AddAsync(membership);
            return (user, membership);
        }

        public Task GrantAreasAsync(OrganizationMembership membership, params OrganizationAreaId[] areaIds) =>
            AreaAssignments.ReplaceForMembershipAsync(Org, membership.Id, areaIds, T0, "actor");

        public OrganizationBranchAccessService AccessService() =>
            new(Memberships, Branches, Assignments, Areas, AreaAssignments);

        public CreateOrganizationArea CreateArea() =>
            new(Areas, Subscriptions, Plans, new NoOpUnitOfWork(), new FixedClock(T0));

        public ArchiveOrganizationArea ArchiveArea() =>
            new(Areas, Branches, AreaAssignments, Memberships, new NoOpUnitOfWork(), new FixedClock(T0));

        public SetBranchArea SetBranchArea() =>
            new(Branches, Areas, new NoOpUnitOfWork(), new FixedClock(T0));

        public SetMembershipBranchAssignments SetAssignments() =>
            new(Memberships, Branches, Assignments, Areas, AreaAssignments, new NoOpUnitOfWork(), new FixedClock(T0));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class NoOpUnitOfWork : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    internal sealed class MutableBranchRepo : IOrganizationBranchRepository
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
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(
                _byId.Values.Where(x => x.OrganizationId == organizationId).ToList());

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

    internal sealed class StubPlanRepository(Plan plan) : IPlanRepository
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

    internal sealed class StubSubscriptionRepository(PlanId planId) : ISubscriptionRepository
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
