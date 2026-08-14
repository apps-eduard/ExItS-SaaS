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

    public Task<OrganizationMembership?> FindActiveOwnerByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var match = _byId.Values
            .Where(m => m.OrganizationId == organizationId
                        && m.Status == MembershipStatus.Active
                        && m.Role == OrganizationRole.OrganizationOwner)
            .OrderByDescending(m => m.UpdatedAtUtc)
            .FirstOrDefault();
        return Task.FromResult(match);
    }

    public Task<OrganizationMembership?> FindCurrentByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var match = _byId.Values
            .Where(m => m.UserId == userId
                        && m.OrganizationId == organizationId
                        && m.Status != MembershipStatus.Removed)
            .OrderByDescending(m => m.UpdatedAtUtc)
            .FirstOrDefault();
        return Task.FromResult(match);
    }

    public Task<(IReadOnlyList<OrganizationMembership> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        MembershipStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.Where(m => m.OrganizationId == organizationId);
        if (status is not null)
        {
            query = query.Where(m => m.Status == status);
        }

        var ordered = query.OrderByDescending(m => m.CreatedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<OrganizationMembership>, int)>(
            (ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task<(IReadOnlyList<OrganizationMembership> Items, int TotalCount)> ListByUserAsync(
        PlatformUserId userId,
        MembershipStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.Where(m => m.UserId == userId);
        if (status is not null)
        {
            query = query.Where(m => m.Status == status);
        }

        var ordered = query.OrderByDescending(m => m.CreatedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<OrganizationMembership>, int)>(
            (ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task<int> CountActiveGoverningAdminsAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var count = _byId.Values.Count(m =>
            m.OrganizationId == organizationId
            && m.Status == MembershipStatus.Active
            && OrganizationMembershipGuard.IsProtectedGoverningSeat(m.Role));
        return Task.FromResult(count);
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
