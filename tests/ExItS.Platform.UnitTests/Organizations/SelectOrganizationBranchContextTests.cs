using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class SelectOrganizationBranchContextTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId OwnerActor = PlatformUserId.New();

    [Fact]
    public async Task Owner_can_select_any_active_branch_in_the_same_organization()
    {
        var org = PlatformOrganizationId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var north = OrganizationBranch.Create(org, "NORTH", "Branch B", T0);
        var sut = new SelectOrganizationBranchContext(
            new FakeRepo([main, north]),
            new AllowAllBranchAccess());

        var result = await sut.ExecuteAsync(org, north.Id, OwnerActor);

        Assert.True(result.IsSuccess);
        Assert.Equal(north.Id.Value, result.Value!.BranchId);
        Assert.Equal("Branch B", result.Value.Name);
        Assert.False(result.Value.IsPrimary);
    }

    [Fact]
    public async Task Staff_without_assignment_is_denied()
    {
        var org = PlatformOrganizationId.New();
        var north = OrganizationBranch.Create(org, "NORTH", "Branch B", T0);
        var staff = PlatformUserId.New();
        var sut = new SelectOrganizationBranchContext(
            new FakeRepo([north]),
            new DenyBranchAccess());

        var result = await sut.ExecuteAsync(org, north.Id, staff);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BranchAccessDenied, result.ErrorCode);
    }

    [Fact]
    public async Task Foreign_organization_branch_is_not_found()
    {
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var foreign = OrganizationBranch.Create(orgB, "OTHER", "Other", T0);
        var sut = new SelectOrganizationBranchContext(
            new FakeRepo([foreign]),
            new AllowAllBranchAccess());

        var result = await sut.ExecuteAsync(orgA, foreign.Id, OwnerActor);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BranchNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Inactive_branch_cannot_be_selected()
    {
        var org = PlatformOrganizationId.New();
        var branch = OrganizationBranch.Create(org, "HOLD", "On Hold", T0);
        branch.Deactivate(T0.AddMinutes(1));
        var sut = new SelectOrganizationBranchContext(
            new FakeRepo([branch]),
            new AllowAllBranchAccess());

        var result = await sut.ExecuteAsync(org, branch.Id, OwnerActor);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BranchNotSelectable, result.ErrorCode);
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

    private sealed class DenyBranchAccess : IOrganizationBranchAccessService
    {
        public Task<bool> CanAccessBranchAsync(
            PlatformUserId userId,
            PlatformOrganizationId organizationId,
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlySet<Guid>?> ResolveAccessibleActiveBranchIdsAsync(
            PlatformUserId userId,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>?>(new HashSet<Guid>());
    }

    private sealed class FakeRepo(IReadOnlyList<OrganizationBranch> items) : IOrganizationBranchRepository
    {
        public Task<OrganizationBranch?> GetByIdAsync(
            OrganizationBranchId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(b => b.Id == id));

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(
                items.Where(b => b.OrganizationId == organizationId).ToList());

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
}
