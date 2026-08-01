using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformRoleDefinitionRepository : IPlatformRoleDefinitionRepository
{
    private readonly PlatformDbContext _db;

    public PlatformRoleDefinitionRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformRoleDefinition?> GetByIdAsync(PlatformRoleDefinitionId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformRoleDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RbacEntityMapper.ToDomain(record);
    }

    public async Task<PlatformRoleDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformRoleDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RbacEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<PlatformRoleDefinition> Items, int TotalCount)> ListAsync(
        PlatformRoleKind? kind,
        PlatformRoleLifecycleStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PlatformRoleDefinitions.AsNoTracking().AsQueryable();
        if (kind is not null)
        {
            var kindName = kind.Value.ToString();
            query = query.Where(r => r.Kind == kindName);
        }

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(r => r.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.Code.ToLower().Contains(term)
                || r.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.OrderBy(r => r.Code).Skip(skip).Take(take).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(RbacEntityMapper.ToDomain).ToList(), total);
    }

    public Task AddAsync(PlatformRoleDefinition definition, CancellationToken cancellationToken = default)
    {
        _db.PlatformRoleDefinitions.Add(RbacEntityMapper.ToRecord(definition));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformRoleDefinition definition, int? expectedVersion, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformRoleDefinitions
            .FirstOrDefaultAsync(r => r.Id == definition.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.RoleDefinitionNotFound,
                "Platform role definition was not found.");
        }

        if (expectedVersion is not null && record.Version != expectedVersion.Value)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ConcurrencyConflict,
                "Platform role definition was modified by another request.");
        }

        RbacEntityMapper.ApplyToRecord(definition, record);
    }
}

internal sealed class PlatformCustomRoleAssignmentRepository : IPlatformCustomRoleAssignmentRepository
{
    private readonly PlatformDbContext _db;

    public PlatformCustomRoleAssignmentRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformCustomRoleAssignment?> GetByIdAsync(
        PlatformCustomRoleAssignmentId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformCustomRoleAssignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RbacEntityMapper.ToDomain(record);
    }

    public async Task<PlatformCustomRoleAssignment?> FindActiveAsync(
        PlatformUserId userId,
        PlatformRoleDefinitionId roleDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(PlatformRoleAssignmentStatus.Active);
        var record = await _db.PlatformCustomRoleAssignments.AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.PlatformUserId == userId.Value
                     && a.RoleDefinitionId == roleDefinitionId.Value
                     && a.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RbacEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<PlatformCustomRoleAssignment>> ListActiveByUserAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(PlatformRoleAssignmentStatus.Active);
        var records = await _db.PlatformCustomRoleAssignments.AsNoTracking()
            .Where(a => a.PlatformUserId == userId.Value && a.Status == active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(RbacEntityMapper.ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<PlatformCustomRoleAssignment> Items, int TotalCount)> ListAsync(
        PlatformUserId? userId,
        PlatformRoleDefinitionId? roleDefinitionId,
        PlatformRoleAssignmentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PlatformCustomRoleAssignments.AsNoTracking().AsQueryable();
        if (userId is not null)
        {
            query = query.Where(a => a.PlatformUserId == userId.Value);
        }

        if (roleDefinitionId is not null)
        {
            query = query.Where(a => a.RoleDefinitionId == roleDefinitionId.Value);
        }

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(a => a.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.OrderByDescending(a => a.GrantedAtUtc).Skip(skip).Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(RbacEntityMapper.ToDomain).ToList(), total);
    }

    public Task AddAsync(PlatformCustomRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _db.PlatformCustomRoleAssignments.Add(RbacEntityMapper.ToRecord(assignment));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformCustomRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformCustomRoleAssignments
            .FirstOrDefaultAsync(a => a.Id == assignment.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CustomRoleAssignmentNotFound,
                "Platform custom role assignment was not found.");
        }

        RbacEntityMapper.ApplyToRecord(assignment, record);
    }
}

internal sealed class OrganizationRoleDefinitionRepository : IOrganizationRoleDefinitionRepository
{
    private readonly PlatformDbContext _db;

    public OrganizationRoleDefinitionRepository(PlatformDbContext db) => _db = db;

    public async Task<OrganizationRoleDefinition?> GetByIdAsync(
        OrganizationRoleDefinitionId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationRoleDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RbacEntityMapper.ToDomain(record);
    }

    public async Task<OrganizationRoleDefinition?> GetByOrgAndCodeAsync(
        PlatformOrganizationId organizationId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationRoleDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId.Value && r.Code == code, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RbacEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<OrganizationRoleDefinition> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        PlatformRoleLifecycleStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.OrganizationRoleDefinitions.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value);
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(r => r.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(r => r.Code.ToLower().Contains(term) || r.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.OrderBy(r => r.Code).Skip(skip).Take(take).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(RbacEntityMapper.ToDomain).ToList(), total);
    }

    public Task AddAsync(OrganizationRoleDefinition definition, CancellationToken cancellationToken = default)
    {
        _db.OrganizationRoleDefinitions.Add(RbacEntityMapper.ToRecord(definition));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(OrganizationRoleDefinition definition, int? expectedVersion, CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationRoleDefinitions
            .FirstOrDefaultAsync(r => r.Id == definition.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.OrganizationRoleDefinitionNotFound,
                "Organization role definition was not found.");
        }

        if (expectedVersion is not null && record.Version != expectedVersion.Value)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ConcurrencyConflict,
                "Organization role definition was modified by another request.");
        }

        RbacEntityMapper.ApplyToRecord(definition, record);
    }
}

internal sealed class OrganizationCustomRoleAssignmentRepository : IOrganizationCustomRoleAssignmentRepository
{
    private readonly PlatformDbContext _db;

    public OrganizationCustomRoleAssignmentRepository(PlatformDbContext db) => _db = db;

    public async Task<OrganizationCustomRoleAssignment?> GetByIdAsync(
        OrganizationCustomRoleAssignmentId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationCustomRoleAssignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RbacEntityMapper.ToDomain(record);
    }

    public async Task<OrganizationCustomRoleAssignment?> FindActiveAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        OrganizationRoleDefinitionId roleDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(PlatformRoleAssignmentStatus.Active);
        var record = await _db.OrganizationCustomRoleAssignments.AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.OrganizationId == organizationId.Value
                     && a.PlatformUserId == userId.Value
                     && a.RoleDefinitionId == roleDefinitionId.Value
                     && a.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RbacEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<OrganizationCustomRoleAssignment>> ListActiveByUserAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(PlatformRoleAssignmentStatus.Active);
        var records = await _db.OrganizationCustomRoleAssignments.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value
                        && a.PlatformUserId == userId.Value
                        && a.Status == active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(RbacEntityMapper.ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<OrganizationCustomRoleAssignment> Items, int TotalCount)> ListAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId? userId,
        OrganizationRoleDefinitionId? roleDefinitionId,
        PlatformRoleAssignmentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.OrganizationCustomRoleAssignments.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value);
        if (userId is not null)
        {
            query = query.Where(a => a.PlatformUserId == userId.Value);
        }

        if (roleDefinitionId is not null)
        {
            query = query.Where(a => a.RoleDefinitionId == roleDefinitionId.Value);
        }

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(a => a.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.OrderByDescending(a => a.GrantedAtUtc).Skip(skip).Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(RbacEntityMapper.ToDomain).ToList(), total);
    }

    public Task AddAsync(OrganizationCustomRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _db.OrganizationCustomRoleAssignments.Add(RbacEntityMapper.ToRecord(assignment));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(OrganizationCustomRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationCustomRoleAssignments
            .FirstOrDefaultAsync(a => a.Id == assignment.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.OrganizationCustomRoleAssignmentNotFound,
                "Organization custom role assignment was not found.");
        }

        RbacEntityMapper.ApplyToRecord(assignment, record);
    }
}
