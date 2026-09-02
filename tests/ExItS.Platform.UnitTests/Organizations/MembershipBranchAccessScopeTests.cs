using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class MembershipBranchAccessScopeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_defaults_ordinary_membership_to_Explicit()
    {
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationMember,
            T0);

        Assert.Equal(BranchAccessScope.Explicit, membership.BranchAccessScope);
    }

    [Fact]
    public async Task Existing_explicit_staff_does_not_gain_newly_created_branch()
    {
        var org = PlatformOrganizationId.New();
        var staff = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var membership = OrganizationMembership.Create(org, staff, OrganizationRole.OrganizationMember, T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership);
        var assignments = new MutableAssignmentRepo([
            OrganizationMembershipBranchAssignment.Create(org, membership.Id, main.Id, T0)
        ]);
        var branches = new MutableBranchRepo([main]);
        var sut = new OrganizationBranchAccessService(memberships, branches, assignments);

        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);
        await branches.AddAsync(north);

        var accessible = await sut.ResolveAccessibleActiveBranchIdsAsync(staff, org);
        Assert.NotNull(accessible);
        Assert.Single(accessible!);
        Assert.Contains(main.Id.Value, accessible);
        Assert.DoesNotContain(north.Id.Value, accessible!);
        Assert.False(await sut.CanAccessBranchAsync(staff, org, north.Id));
    }

    [Fact]
    public async Task AllActive_staff_automatically_gains_newly_created_Active_branch()
    {
        var org = PlatformOrganizationId.New();
        var staff = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var membership = OrganizationMembership.Create(org, staff, OrganizationRole.OrganizationMember, T0);
        membership.SetBranchAccessScope(BranchAccessScope.AllActive, T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership);
        var assignments = new MutableAssignmentRepo([]);
        var branches = new MutableBranchRepo([main]);
        var sut = new OrganizationBranchAccessService(memberships, branches, assignments);

        Assert.Null(await sut.ResolveAccessibleActiveBranchIdsAsync(staff, org));

        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);
        await branches.AddAsync(north);

        Assert.Null(await sut.ResolveAccessibleActiveBranchIdsAsync(staff, org));
        Assert.True(await sut.CanAccessBranchAsync(staff, org, north.Id));
    }

    [Fact]
    public async Task Switching_Explicit_to_AllActive_clears_assignment_rows()
    {
        var org = PlatformOrganizationId.New();
        var staff = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);
        var membership = OrganizationMembership.Create(org, staff, OrganizationRole.OrganizationMember, T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership);
        var assignments = new MutableAssignmentRepo([
            OrganizationMembershipBranchAssignment.Create(org, membership.Id, main.Id, T0)
        ]);
        var branches = new MutableBranchRepo([main, north]);
        var uow = new RecordingUnitOfWork();
        var clock = new FixedClock(T0);
        var set = new SetMembershipBranchAssignments(memberships, branches, assignments, uow, clock);

        var result = await set.ExecuteAsync(
            org,
            membership.Id,
            new SetMembershipBranchAssignmentsCommand("AllActive", null),
            "actor");

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(BranchAccessScope.AllActive), result.Value!.Scope);
        Assert.Empty(await assignments.ListByMembershipAsync(membership.Id));
        var updated = await memberships.GetByIdAsync(membership.Id);
        Assert.Equal(BranchAccessScope.AllActive, updated!.BranchAccessScope);
    }

    [Fact]
    public async Task Switching_AllActive_to_Explicit_saves_exact_chosen_rows()
    {
        var org = PlatformOrganizationId.New();
        var staff = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);
        var membership = OrganizationMembership.Create(
            org,
            staff,
            OrganizationRole.OrganizationMember,
            T0,
            branchAccessScope: BranchAccessScope.AllActive);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership);
        var assignments = new MutableAssignmentRepo([]);
        var branches = new MutableBranchRepo([main, north]);
        var set = new SetMembershipBranchAssignments(
            memberships,
            branches,
            assignments,
            new RecordingUnitOfWork(),
            new FixedClock(T0));

        var result = await set.ExecuteAsync(
            org,
            membership.Id,
            new SetMembershipBranchAssignmentsCommand("Explicit", [north.Id.Value]),
            "actor");

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(BranchAccessScope.Explicit), result.Value!.Scope);
        Assert.Single(result.Value.Branches);
        Assert.Equal(north.Id.Value, result.Value.Branches[0].BranchId);
        var rows = await assignments.ListByMembershipAsync(membership.Id);
        Assert.Single(rows);
        Assert.Equal(north.Id, rows[0].BranchId);
        var updated = await memberships.GetByIdAsync(membership.Id);
        Assert.Equal(BranchAccessScope.Explicit, updated!.BranchAccessScope);
    }

    [Fact]
    public async Task Owner_implicit_all_active_ignores_stored_scope_and_cannot_set()
    {
        var org = PlatformOrganizationId.New();
        var owner = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);
        var membership = OrganizationMembership.Create(
            org,
            owner,
            OrganizationRole.OrganizationOwner,
            T0,
            branchAccessScope: BranchAccessScope.Explicit);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership);
        var assignments = new MutableAssignmentRepo([]);
        var branches = new MutableBranchRepo([main, north]);
        var sut = new OrganizationBranchAccessService(memberships, branches, assignments);

        Assert.Null(await sut.ResolveAccessibleActiveBranchIdsAsync(owner, org));
        Assert.True(await sut.CanAccessBranchAsync(owner, org, north.Id));

        var set = new SetMembershipBranchAssignments(
            memberships,
            branches,
            assignments,
            new RecordingUnitOfWork(),
            new FixedClock(T0));
        var denied = await set.ExecuteAsync(
            org,
            membership.Id,
            new SetMembershipBranchAssignmentsCommand("Explicit", [main.Id.Value]),
            "actor");
        Assert.False(denied.IsSuccess);
        Assert.Equal(Domain.Common.DomainErrorCodes.InvalidOrganizationRole, denied.ErrorCode);
    }

    [Fact]
    public async Task List_returns_persisted_scope_not_inferred_from_branch_count()
    {
        var org = PlatformOrganizationId.New();
        var staff = PlatformUserId.New();
        var actor = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);
        var staffMembership = OrganizationMembership.Create(org, staff, OrganizationRole.OrganizationMember, T0);
        var actorMembership = OrganizationMembership.Create(org, actor, OrganizationRole.OrganizationOwner, T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(staffMembership);
        await memberships.AddAsync(actorMembership);
        var assignments = new MutableAssignmentRepo([
            OrganizationMembershipBranchAssignment.Create(org, staffMembership.Id, main.Id, T0),
            OrganizationMembershipBranchAssignment.Create(org, staffMembership.Id, north.Id, T0)
        ]);
        var branches = new MutableBranchRepo([main, north]);
        var list = new ListMembershipBranchAssignments(
            memberships,
            branches,
            assignments,
            new OrganizationBranchAccessService(memberships, branches, assignments));

        var result = await list.ExecuteAsync(org, staffMembership.Id, actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(BranchAccessScope.Explicit), result.Value!.Scope);
        Assert.Equal(2, result.Value.Branches.Count);
    }

    [Fact]
    public async Task Role_change_path_does_not_require_branch_set()
    {
        // Proven at domain level: ChangeRole leaves BranchAccessScope untouched.
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationMember,
            T0,
            branchAccessScope: BranchAccessScope.AllActive);
        membership.ChangeRole(OrganizationRole.OrganizationMember, T0.AddMinutes(1));
        Assert.Equal(BranchAccessScope.AllActive, membership.BranchAccessScope);
        await Task.CompletedTask;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class RecordingUnitOfWork : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MutableBranchRepo(List<OrganizationBranch> items) : IOrganizationBranchRepository
    {
        public Task<OrganizationBranch?> GetByIdAsync(OrganizationBranchId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(b => b.Id == id));

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(
                items.Where(b => b.OrganizationId == organizationId).ToList());

        public Task<int> CountActiveAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.Count(b => b.OrganizationId == organizationId && b.Status == OrganizationBranchStatus.Active));

        public Task<OrganizationBranch?> GetPrimaryAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(b => b.OrganizationId == organizationId && b.IsPrimary));

        public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default)
        {
            items.Add(branch);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MutableAssignmentRepo(List<OrganizationMembershipBranchAssignment> items)
        : IOrganizationMembershipBranchAssignmentRepository
    {
        public Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByMembershipAsync(
            OrganizationMembershipId membershipId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationMembershipBranchAssignment>>(
                items.Where(x => x.MembershipId == membershipId).ToList());

        public Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationMembershipBranchAssignment>>(
                items.Where(x => x.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByBranchAsync(
            PlatformOrganizationId organizationId,
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationMembershipBranchAssignment>>(
                items.Where(x => x.OrganizationId == organizationId && x.BranchId == branchId).ToList());

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
            items.RemoveAll(x => x.MembershipId == membershipId);
            foreach (var branchId in branchIds)
            {
                items.Add(OrganizationMembershipBranchAssignment.Create(
                    organizationId, membershipId, branchId, utcNow, actorReference: actorReference));
            }
            return Task.CompletedTask;
        }

        public Task AssignPrimaryBranchForNewStaffAsync(
            PlatformOrganizationId organizationId,
            OrganizationMembershipId membershipId,
            DateTimeOffset utcNow,
            string? actorReference,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
