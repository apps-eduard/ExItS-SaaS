using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformRoleAssignmentRepository : IPlatformRoleAssignmentRepository
{
    private readonly Dictionary<Guid, PlatformRoleAssignment> _byId = new();

    public Task<PlatformRoleAssignment?> GetByIdAsync(
        PlatformRoleAssignmentId id,
        CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var assignment);
        return Task.FromResult(assignment);
    }

    public Task<PlatformRoleAssignment?> FindActiveAsync(
        PlatformUserId userId,
        PlatformSystemRole role,
        PlatformOrganizationId? organizationId,
        CancellationToken cancellationToken = default)
    {
        var match = _byId.Values.FirstOrDefault(a =>
            a.PlatformUserId == userId
            && a.Role == role
            && a.OrganizationId == organizationId
            && a.Status == PlatformRoleAssignmentStatus.Active);
        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<PlatformRoleAssignment>> ListActiveByUserAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlatformRoleAssignment> list = _byId.Values
            .Where(a => a.PlatformUserId == userId && a.Status == PlatformRoleAssignmentStatus.Active)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<int> CountActivePlatformAdministratorsAsync(CancellationToken cancellationToken = default)
    {
        var count = _byId.Values.Count(a =>
            a.Status == PlatformRoleAssignmentStatus.Active
            && a.Role == PlatformSystemRole.PlatformAdministrator
            && a.OrganizationId is null);
        return Task.FromResult(count);
    }

    public Task<(IReadOnlyList<PlatformRoleAssignment> Items, int TotalCount)> ListAsync(
        PlatformUserId? userId,
        PlatformSystemRole? role,
        PlatformOrganizationId? organizationId,
        PlatformRoleAssignmentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.AsEnumerable();
        if (userId is not null)
        {
            query = query.Where(a => a.PlatformUserId == userId);
        }

        if (role is not null)
        {
            query = query.Where(a => a.Role == role);
        }

        if (organizationId is not null)
        {
            query = query.Where(a => a.OrganizationId == organizationId);
        }

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        var ordered = query.OrderByDescending(a => a.GrantedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<PlatformRoleAssignment>, int)>(
            (ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task AddAsync(PlatformRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _byId[assignment.Id.Value] = assignment;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _byId[assignment.Id.Value] = assignment;
        return Task.CompletedTask;
    }
}
