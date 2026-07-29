using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryOrganizationMembershipRepository : IOrganizationMembershipRepository
{
    private readonly Dictionary<Guid, OrganizationMembership> _byId = new();

    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<OrganizationMembership?> GetByIdAsync(OrganizationMembershipId id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var membership);
        return Task.FromResult(membership);
    }

    public Task<OrganizationMembership?> FindActiveByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var match = _byId.Values.FirstOrDefault(m =>
            m.UserId == userId
            && m.OrganizationId == organizationId
            && m.Status == MembershipStatus.Active);
        return Task.FromResult(match);
    }

    public Task AddAsync(OrganizationMembership membership, CancellationToken cancellationToken = default)
    {
        _byId[membership.Id.Value] = membership;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OrganizationMembership membership, CancellationToken cancellationToken = default)
    {
        _byId[membership.Id.Value] = membership;
        UpdateCount++;
        return Task.CompletedTask;
    }
}
