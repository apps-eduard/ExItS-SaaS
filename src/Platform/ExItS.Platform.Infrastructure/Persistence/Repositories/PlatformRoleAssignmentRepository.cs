using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformRoleAssignmentRepository : IPlatformRoleAssignmentRepository
{
    private readonly PlatformDbContext _db;

    public PlatformRoleAssignmentRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformRoleAssignment?> GetByIdAsync(
        PlatformRoleAssignmentId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformRoleAssignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : AuthorizationAuditEntityMapper.ToDomain(record);
    }

    public async Task<PlatformRoleAssignment?> FindActiveAsync(
        PlatformUserId userId,
        PlatformSystemRole role,
        PlatformOrganizationId? organizationId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(PlatformRoleAssignmentStatus.Active);
        var roleName = role.ToString();
        var orgValue = organizationId?.Value;
        var record = await _db.PlatformRoleAssignments.AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.PlatformUserId == userId.Value
                     && a.Role == roleName
                     && a.Status == active
                     && a.OrganizationId == orgValue,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : AuthorizationAuditEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<PlatformRoleAssignment>> ListActiveByUserAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(PlatformRoleAssignmentStatus.Active);
        var records = await _db.PlatformRoleAssignments.AsNoTracking()
            .Where(a => a.PlatformUserId == userId.Value && a.Status == active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(AuthorizationAuditEntityMapper.ToDomain).ToList();
    }

    public async Task<int> CountActivePlatformAdministratorsAsync(CancellationToken cancellationToken = default)
    {
        var active = nameof(PlatformRoleAssignmentStatus.Active);
        var role = nameof(PlatformSystemRole.PlatformAdministrator);
        return await _db.PlatformRoleAssignments.AsNoTracking()
            .CountAsync(
                a => a.Status == active
                     && a.Role == role
                     && a.OrganizationId == null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<PlatformRoleAssignment> Items, int TotalCount)> ListAsync(
        PlatformUserId? userId,
        PlatformSystemRole? role,
        PlatformOrganizationId? organizationId,
        PlatformRoleAssignmentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PlatformRoleAssignments.AsNoTracking().AsQueryable();

        if (userId is not null)
        {
            var userValue = userId.Value;
            query = query.Where(a => a.PlatformUserId == userValue);
        }

        if (role is not null)
        {
            var roleName = role.Value.ToString();
            query = query.Where(a => a.Role == roleName);
        }

        if (organizationId is not null)
        {
            var orgValue = organizationId.Value;
            query = query.Where(a => a.OrganizationId == orgValue);
        }

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(a => a.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(a => a.GrantedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(AuthorizationAuditEntityMapper.ToDomain).ToList(), total);
    }

    public Task AddAsync(PlatformRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _db.PlatformRoleAssignments.Add(AuthorizationAuditEntityMapper.ToRecord(assignment));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformRoleAssignments
            .FirstOrDefaultAsync(a => a.Id == assignment.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.RoleAssignmentNotFound,
                "Platform role assignment was not found.");
        }

        AuthorizationAuditEntityMapper.ApplyToRecord(assignment, record);
    }
}
