using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class GetOrganizationPrimaryBranchTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 1, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task H1_ACL_05_structural_lookup_returns_only_branch_id()
    {
        var org = PlatformOrganizationId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var remote = OrganizationBranch.Create(org, "REMOTE", "Remote", T0);
        var sut = new GetOrganizationPrimaryBranch(new FakeBranchRepo([main, remote]));

        var result = await sut.ExecuteAsync(org);

        Assert.True(result.IsSuccess);
        Assert.Equal(main.Id.Value, result.Value!.BranchId);
    }

    [Fact]
    public async Task Remote_only_staff_accessible_set_excludes_main_but_structural_primary_is_main()
    {
        var org = PlatformOrganizationId.New();
        var staff = PlatformUserId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var remote = OrganizationBranch.Create(org, "REMOTE", "Remote", T0);
        var membership = OrganizationMembership.Create(org, staff, OrganizationRole.OrganizationMember, T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership, CancellationToken.None);
        var assignments = new[]
        {
            OrganizationMembershipBranchAssignment.Create(org, membership.Id, remote.Id, T0)
        };
        var branchAccess = new OrganizationBranchAccessService(
            memberships,
            new FakeBranchRepo([main, remote]),
            new FakeAssignmentRepo(assignments));
        var structural = new GetOrganizationPrimaryBranch(new FakeBranchRepo([main, remote]));

        var accessible = await branchAccess.ResolveAccessibleActiveBranchIdsAsync(staff, org);
        var primary = await structural.ExecuteAsync(org);

        Assert.NotNull(accessible);
        Assert.Single(accessible!);
        Assert.Contains(remote.Id.Value, accessible);
        Assert.DoesNotContain(main.Id.Value, accessible);
        Assert.True(primary.IsSuccess);
        Assert.Equal(main.Id.Value, primary.Value!.BranchId);
    }

    [Fact]
    public async Task Missing_primary_returns_not_found()
    {
        var org = PlatformOrganizationId.New();
        var remote = OrganizationBranch.Create(org, "REMOTE", "Remote", T0);
        var sut = new GetOrganizationPrimaryBranch(new FakeBranchRepo([remote]));

        var result = await sut.ExecuteAsync(org);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BranchNotFound, result.ErrorCode);
    }

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
            Task.FromResult(items.Count(b => b.OrganizationId == organizationId && b.Status == OrganizationBranchStatus.Active));

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
