using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryOrganizationMembershipBranchAssignmentRepository : IOrganizationMembershipBranchAssignmentRepository
{
    private readonly List<OrganizationMembershipBranchAssignment> _items = [];

    public Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByMembershipAsync(
        OrganizationMembershipId membershipId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationMembershipBranchAssignment>>(
            _items.Where(x => x.MembershipId == membershipId).ToList());

    public Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationMembershipBranchAssignment>>(
            _items.Where(x => x.OrganizationId == organizationId).ToList());

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
        CancellationToken cancellationToken = default)
    {
        var assignment = OrganizationMembershipBranchAssignment.Create(
            organizationId,
            membershipId,
            OrganizationBranchId.New(),
            utcNow,
            actorReference: actorReference);
        _items.Add(assignment);
        return Task.CompletedTask;
    }
}
