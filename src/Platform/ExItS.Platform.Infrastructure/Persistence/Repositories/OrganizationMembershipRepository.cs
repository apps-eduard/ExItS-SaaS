using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationMembershipRepository : IOrganizationMembershipRepository
{
    private readonly PlatformDbContext _db;

    public OrganizationMembershipRepository(PlatformDbContext db) => _db = db;

    public async Task<OrganizationMembership?> GetByIdAsync(
        OrganizationMembershipId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationMemberships.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : IdentityAccessEntityMapper.ToMembershipDomain(record);
    }

    public async Task<OrganizationMembership?> FindActiveByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(MembershipStatus.Active);
        var record = await _db.OrganizationMemberships.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.UserId == userId.Value
                     && m.OrganizationId == organizationId.Value
                     && m.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : IdentityAccessEntityMapper.ToMembershipDomain(record);
    }

    public async Task<OrganizationMembership?> FindCurrentByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var removed = nameof(MembershipStatus.Removed);
        var record = await _db.OrganizationMemberships.AsNoTracking()
            .Where(m => m.UserId == userId.Value
                        && m.OrganizationId == organizationId.Value
                        && m.Status != removed)
            .OrderByDescending(m => m.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : IdentityAccessEntityMapper.ToMembershipDomain(record);
    }

    public async Task<(IReadOnlyList<OrganizationMembership> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        MembershipStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.OrganizationMemberships.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId.Value);
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(m => m.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(IdentityAccessEntityMapper.ToMembershipDomain).ToList(), total);
    }

    public async Task<(IReadOnlyList<OrganizationMembership> Items, int TotalCount)> ListByUserAsync(
        PlatformUserId userId,
        MembershipStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.OrganizationMemberships.AsNoTracking()
            .Where(m => m.UserId == userId.Value);
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(m => m.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(IdentityAccessEntityMapper.ToMembershipDomain).ToList(), total);
    }

    public async Task<int> CountActiveGoverningAdminsAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(MembershipStatus.Active);
        var owner = nameof(OrganizationRole.OrganizationOwner);
        return await _db.OrganizationMemberships.AsNoTracking()
            .CountAsync(
                m => m.OrganizationId == organizationId.Value
                     && m.Status == active
                     && m.Role == owner,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task AddAsync(OrganizationMembership membership, CancellationToken cancellationToken = default)
    {
        _db.OrganizationMemberships.Add(IdentityAccessEntityMapper.ToMembershipRecord(membership));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(OrganizationMembership membership, CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.Id == membership.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        IdentityAccessEntityMapper.ApplyToMembershipRecord(membership, record);
    }
}
