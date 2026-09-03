using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationBranchAccessServiceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 19, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Owner_resolves_null_accessible_set_for_all_active_branches()
    {
        var org = PlatformOrganizationId.New();
        var owner = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(
            OrganizationMembership.Create(org, owner, OrganizationRole.OrganizationOwner, T0),
            CancellationToken.None);
        var sut = CreateSut(memberships, [main, north], []);

        var accessible = await sut.ResolveAccessibleActiveBranchIdsAsync(owner, org);

        Assert.Null(accessible);
        Assert.True(await sut.CanAccessBranchAsync(owner, org, north.Id));
    }

    [Fact]
    public async Task Staff_with_main_only_sees_main_branch()
    {
        var org = PlatformOrganizationId.New();
        var staff = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);
        var membership = OrganizationMembership.Create(org, staff, OrganizationRole.OrganizationMember, T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership, CancellationToken.None);
        var assignments = new[]
        {
            OrganizationMembershipBranchAssignment.Create(org, membership.Id, main.Id, T0)
        };
        var sut = CreateSut(memberships, [main, north], assignments);

        var accessible = await sut.ResolveAccessibleActiveBranchIdsAsync(staff, org);

        Assert.NotNull(accessible);
        Assert.Single(accessible!);
        Assert.Contains(main.Id.Value, accessible);
        Assert.True(await sut.CanAccessBranchAsync(staff, org, main.Id));
        Assert.False(await sut.CanAccessBranchAsync(staff, org, north.Id));
    }

    [Fact]
    public async Task Staff_with_two_assignments_sees_only_those_branches()
    {
        var org = PlatformOrganizationId.New();
        var staff = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var north = OrganizationBranch.Create(org, "NORTH", "North", T0);
        var south = OrganizationBranch.Create(org, "SOUTH", "South", T0);
        var membership = OrganizationMembership.Create(org, staff, OrganizationRole.OrganizationMember, T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership, CancellationToken.None);
        var assignments = new[]
        {
            OrganizationMembershipBranchAssignment.Create(org, membership.Id, main.Id, T0),
            OrganizationMembershipBranchAssignment.Create(org, membership.Id, north.Id, T0)
        };
        var sut = CreateSut(memberships, [main, north, south], assignments);

        var accessible = await sut.ResolveAccessibleActiveBranchIdsAsync(staff, org);

        Assert.NotNull(accessible);
        Assert.Equal(2, accessible!.Count);
        Assert.Contains(main.Id.Value, accessible);
        Assert.Contains(north.Id.Value, accessible);
        Assert.DoesNotContain(south.Id.Value, accessible);
    }

    [Fact]
    public async Task Staff_with_AllActive_scope_sees_all_active_without_assignment_rows()
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
        await memberships.AddAsync(membership, CancellationToken.None);
        var sut = CreateSut(memberships, [main, north], []);

        Assert.Null(await sut.ResolveAccessibleActiveBranchIdsAsync(staff, org));
        Assert.True(await sut.CanAccessBranchAsync(staff, org, north.Id));
    }

    [Fact]
    public async Task Foreign_organization_branch_is_denied()
    {
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var staff = PlatformUserId.New();
        var foreign = OrganizationBranch.Create(orgB, "OTHER", "Other", T0);
        var membership = OrganizationMembership.Create(orgA, staff, OrganizationRole.OrganizationMember, T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership, CancellationToken.None);
        var sut = CreateSut(memberships, [foreign], []);

        Assert.False(await sut.CanAccessBranchAsync(staff, orgA, foreign.Id));
    }

    private static OrganizationBranchAccessService CreateSut(
        InMemoryOrganizationMembershipRepository memberships,
        IReadOnlyList<OrganizationBranch> branches,
        IReadOnlyList<OrganizationMembershipBranchAssignment> assignments) =>
        new(
            memberships,
            new FakeBranchRepo(branches),
            new FakeAssignmentRepo(assignments),
            new InMemoryOrganizationAreaRepository(),
            new InMemoryOrganizationMembershipAreaAssignmentRepository());

    private sealed class FakeBranchRepo(IReadOnlyList<OrganizationBranch> items) : IOrganizationBranchRepository
    {
        public Task<OrganizationBranch?> GetByIdAsync(OrganizationBranchId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(b => b.Id == id));

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(
                items.Where(b => b.OrganizationId == organizationId).ToList());

        public Task<int> CountActiveAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<OrganizationBranch?> GetPrimaryAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(b => b.OrganizationId == organizationId && b.IsPrimary));

        public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeAssignmentRepo(IReadOnlyList<OrganizationMembershipBranchAssignment> items)
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
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AssignPrimaryBranchForNewStaffAsync(
            PlatformOrganizationId organizationId,
            OrganizationMembershipId membershipId,
            DateTimeOffset utcNow,
            string? actorReference,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
