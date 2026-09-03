using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationAreaRepository(PlatformDbContext db) : IOrganizationAreaRepository
{
    public async Task<OrganizationArea?> GetByIdAsync(
        OrganizationAreaId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationAreas.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<OrganizationArea>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.OrganizationAreas.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task<int> CountActiveAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        db.OrganizationAreas.AsNoTracking()
            .CountAsync(
                x => x.OrganizationId == organizationId.Value
                     && x.Status == nameof(OrganizationAreaStatus.Active),
                cancellationToken);

    public Task AddAsync(OrganizationArea area, CancellationToken cancellationToken = default)
    {
        db.OrganizationAreas.Add(ToRecord(area));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(OrganizationArea area, CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationAreas
            .FirstOrDefaultAsync(x => x.Id == area.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.Name = area.Name;
        record.Code = area.Code;
        record.Status = area.Status.ToString();
        record.UpdatedAtUtc = area.UpdatedAtUtc;
    }

    private static OrganizationArea ToDomain(OrganizationAreaRecord record) =>
        OrganizationArea.Rehydrate(
            OrganizationAreaId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            record.Name,
            record.Code,
            Enum.Parse<OrganizationAreaStatus>(record.Status),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static OrganizationAreaRecord ToRecord(OrganizationArea area) =>
        new()
        {
            Id = area.Id.Value,
            OrganizationId = area.OrganizationId.Value,
            Name = area.Name,
            Code = area.Code,
            Status = area.Status.ToString(),
            CreatedAtUtc = area.CreatedAtUtc,
            UpdatedAtUtc = area.UpdatedAtUtc
        };
}

internal sealed class OrganizationMembershipAreaAssignmentRepository(PlatformDbContext db)
    : IOrganizationMembershipAreaAssignmentRepository
{
    public async Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByMembershipAsync(
        OrganizationMembershipId membershipId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.OrganizationMembershipAreaAssignments.AsNoTracking()
            .Where(x => x.MembershipId == membershipId.Value)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.OrganizationMembershipAreaAssignments.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByAreaAsync(
        PlatformOrganizationId organizationId,
        OrganizationAreaId areaId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.OrganizationMembershipAreaAssignments.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value && x.AreaId == areaId.Value)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await (
                from assignment in db.OrganizationMembershipAreaAssignments.AsNoTracking()
                join membership in db.OrganizationMemberships.AsNoTracking()
                    on assignment.MembershipId equals membership.Id
                where membership.UserId == userId.Value
                      && membership.OrganizationId == organizationId.Value
                      && membership.Status == nameof(MembershipStatus.Active)
                select assignment)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task ReplaceForMembershipAsync(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        IReadOnlyCollection<OrganizationAreaId> areaIds,
        DateTimeOffset utcNow,
        string? actorReference,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.OrganizationMembershipAreaAssignments
            .Where(x => x.MembershipId == membershipId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (existing.Count > 0)
        {
            db.OrganizationMembershipAreaAssignments.RemoveRange(existing);
        }

        foreach (var areaId in areaIds)
        {
            var assignment = OrganizationMembershipAreaAssignment.Create(
                organizationId,
                membershipId,
                areaId,
                utcNow,
                actorReference: actorReference);
            db.OrganizationMembershipAreaAssignments.Add(ToRecord(assignment));
        }
    }

    private static OrganizationMembershipAreaAssignment ToDomain(
        OrganizationMembershipAreaAssignmentRecord record) =>
        OrganizationMembershipAreaAssignment.Rehydrate(
            OrganizationMembershipAreaAssignmentId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            OrganizationMembershipId.From(record.MembershipId),
            OrganizationAreaId.From(record.AreaId),
            record.CreatedAtUtc,
            record.ActorReference);

    private static OrganizationMembershipAreaAssignmentRecord ToRecord(
        OrganizationMembershipAreaAssignment assignment) =>
        new()
        {
            Id = assignment.Id.Value,
            OrganizationId = assignment.OrganizationId.Value,
            MembershipId = assignment.MembershipId.Value,
            AreaId = assignment.AreaId.Value,
            CreatedAtUtc = assignment.CreatedAtUtc,
            ActorReference = assignment.ActorReference
        };
}
