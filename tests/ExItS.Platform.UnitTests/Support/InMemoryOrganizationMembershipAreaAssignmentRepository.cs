using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryOrganizationMembershipAreaAssignmentRepository
    : IOrganizationMembershipAreaAssignmentRepository
{
    private readonly List<OrganizationMembershipAreaAssignment> _items = [];

    public InMemoryOrganizationMembershipAreaAssignmentRepository(
        params OrganizationMembershipAreaAssignment[] seed) => _items.AddRange(seed);

    public IReadOnlyList<OrganizationMembershipAreaAssignment> Items => _items;

    public Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByMembershipAsync(
        OrganizationMembershipId membershipId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationMembershipAreaAssignment>>(
            _items.Where(x => x.MembershipId == membershipId).ToList());

    public Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationMembershipAreaAssignment>>(
            _items.Where(x => x.OrganizationId == organizationId).ToList());

    public Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByAreaAsync(
        PlatformOrganizationId organizationId,
        OrganizationAreaId areaId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationMembershipAreaAssignment>>(
            _items.Where(x => x.OrganizationId == organizationId && x.AreaId == areaId).ToList());

    public Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationMembershipAreaAssignment>>([]);

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
