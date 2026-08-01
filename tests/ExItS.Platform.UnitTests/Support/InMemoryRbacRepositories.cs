using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformRoleDefinitionRepository : IPlatformRoleDefinitionRepository
{
    private readonly Dictionary<Guid, PlatformRoleDefinition> _byId = new();

    public Task<PlatformRoleDefinition?> GetByIdAsync(PlatformRoleDefinitionId id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var item);
        return Task.FromResult(item);
    }

    public Task<PlatformRoleDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var item = _byId.Values.FirstOrDefault(d => string.Equals(d.Code, code, StringComparison.Ordinal));
        return Task.FromResult(item);
    }

    public Task<(IReadOnlyList<PlatformRoleDefinition> Items, int TotalCount)> ListAsync(
        PlatformRoleKind? kind,
        PlatformRoleLifecycleStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.AsEnumerable();
        if (kind is not null) query = query.Where(d => d.Kind == kind);
        if (status is not null) query = query.Where(d => d.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                || d.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderBy(d => d.Code, StringComparer.Ordinal).ToList();
        return Task.FromResult<(IReadOnlyList<PlatformRoleDefinition>, int)>((ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task AddAsync(PlatformRoleDefinition definition, CancellationToken cancellationToken = default)
    {
        _byId[definition.Id.Value] = definition;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformRoleDefinition definition, int? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (!_byId.TryGetValue(definition.Id.Value, out var existing))
        {
            throw new PersistenceConflictException(ApplicationErrorCodes.RoleDefinitionNotFound, "Not found.");
        }

        if (expectedVersion is not null && existing.Version != expectedVersion.Value)
        {
            throw new PersistenceConflictException(ApplicationErrorCodes.ConcurrencyConflict, "Conflict.");
        }

        _byId[definition.Id.Value] = definition;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPlatformCustomRoleAssignmentRepository : IPlatformCustomRoleAssignmentRepository
{
    private readonly Dictionary<Guid, PlatformCustomRoleAssignment> _byId = new();

    public Task<PlatformCustomRoleAssignment?> GetByIdAsync(PlatformCustomRoleAssignmentId id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var item);
        return Task.FromResult(item);
    }

    public Task<PlatformCustomRoleAssignment?> FindActiveAsync(
        PlatformUserId userId,
        PlatformRoleDefinitionId roleDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var item = _byId.Values.FirstOrDefault(a =>
            a.PlatformUserId == userId
            && a.RoleDefinitionId == roleDefinitionId
            && a.Status == PlatformRoleAssignmentStatus.Active);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<PlatformCustomRoleAssignment>> ListActiveByUserAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlatformCustomRoleAssignment> list = _byId.Values
            .Where(a => a.PlatformUserId == userId && a.Status == PlatformRoleAssignmentStatus.Active)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<(IReadOnlyList<PlatformCustomRoleAssignment> Items, int TotalCount)> ListAsync(
        PlatformUserId? userId,
        PlatformRoleDefinitionId? roleDefinitionId,
        PlatformRoleAssignmentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.AsEnumerable();
        if (userId is not null) query = query.Where(a => a.PlatformUserId == userId);
        if (roleDefinitionId is not null) query = query.Where(a => a.RoleDefinitionId == roleDefinitionId);
        if (status is not null) query = query.Where(a => a.Status == status);
        var ordered = query.OrderByDescending(a => a.GrantedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<PlatformCustomRoleAssignment>, int)>((ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task AddAsync(PlatformCustomRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _byId[assignment.Id.Value] = assignment;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformCustomRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _byId[assignment.Id.Value] = assignment;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryOrganizationRoleDefinitionRepository : IOrganizationRoleDefinitionRepository
{
    private readonly Dictionary<Guid, OrganizationRoleDefinition> _byId = new();

    public Task<OrganizationRoleDefinition?> GetByIdAsync(OrganizationRoleDefinitionId id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var item);
        return Task.FromResult(item);
    }

    public Task<OrganizationRoleDefinition?> GetByOrgAndCodeAsync(
        PlatformOrganizationId organizationId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var item = _byId.Values.FirstOrDefault(d =>
            d.OrganizationId == organizationId && string.Equals(d.Code, code, StringComparison.Ordinal));
        return Task.FromResult(item);
    }

    public Task<(IReadOnlyList<OrganizationRoleDefinition> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        PlatformRoleLifecycleStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.Where(d => d.OrganizationId == organizationId);
        if (status is not null) query = query.Where(d => d.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                || d.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderBy(d => d.Code, StringComparer.Ordinal).ToList();
        return Task.FromResult<(IReadOnlyList<OrganizationRoleDefinition>, int)>((ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task AddAsync(OrganizationRoleDefinition definition, CancellationToken cancellationToken = default)
    {
        _byId[definition.Id.Value] = definition;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OrganizationRoleDefinition definition, int? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (!_byId.TryGetValue(definition.Id.Value, out var existing))
        {
            throw new PersistenceConflictException(ApplicationErrorCodes.OrganizationRoleDefinitionNotFound, "Not found.");
        }

        if (expectedVersion is not null && existing.Version != expectedVersion.Value)
        {
            throw new PersistenceConflictException(ApplicationErrorCodes.ConcurrencyConflict, "Conflict.");
        }

        _byId[definition.Id.Value] = definition;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryOrganizationCustomRoleAssignmentRepository : IOrganizationCustomRoleAssignmentRepository
{
    private readonly Dictionary<Guid, OrganizationCustomRoleAssignment> _byId = new();

    public Task<OrganizationCustomRoleAssignment?> GetByIdAsync(
        OrganizationCustomRoleAssignmentId id,
        CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var item);
        return Task.FromResult(item);
    }

    public Task<OrganizationCustomRoleAssignment?> FindActiveAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        OrganizationRoleDefinitionId roleDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var item = _byId.Values.FirstOrDefault(a =>
            a.OrganizationId == organizationId
            && a.PlatformUserId == userId
            && a.RoleDefinitionId == roleDefinitionId
            && a.Status == PlatformRoleAssignmentStatus.Active);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<OrganizationCustomRoleAssignment>> ListActiveByUserAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrganizationCustomRoleAssignment> list = _byId.Values
            .Where(a => a.OrganizationId == organizationId
                        && a.PlatformUserId == userId
                        && a.Status == PlatformRoleAssignmentStatus.Active)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<(IReadOnlyList<OrganizationCustomRoleAssignment> Items, int TotalCount)> ListAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId? userId,
        OrganizationRoleDefinitionId? roleDefinitionId,
        PlatformRoleAssignmentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.Where(a => a.OrganizationId == organizationId);
        if (userId is not null) query = query.Where(a => a.PlatformUserId == userId);
        if (roleDefinitionId is not null) query = query.Where(a => a.RoleDefinitionId == roleDefinitionId);
        if (status is not null) query = query.Where(a => a.Status == status);
        var ordered = query.OrderByDescending(a => a.GrantedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<OrganizationCustomRoleAssignment>, int)>((ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task AddAsync(OrganizationCustomRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _byId[assignment.Id.Value] = assignment;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OrganizationCustomRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _byId[assignment.Id.Value] = assignment;
        return Task.CompletedTask;
    }
}
