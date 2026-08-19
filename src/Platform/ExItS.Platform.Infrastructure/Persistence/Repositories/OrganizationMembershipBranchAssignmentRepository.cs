using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationMembershipBranchAssignmentRepository(
    PlatformDbContext db,
    IOrganizationBranchRepository branches) : IOrganizationMembershipBranchAssignmentRepository
{
    public async Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByMembershipAsync(
        OrganizationMembershipId membershipId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.OrganizationMembershipBranchAssignments.AsNoTracking()
            .Where(x => x.MembershipId == membershipId.Value)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await (
                from assignment in db.OrganizationMembershipBranchAssignments.AsNoTracking()
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
        IReadOnlyCollection<OrganizationBranchId> branchIds,
        DateTimeOffset utcNow,
        string? actorReference,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.OrganizationMembershipBranchAssignments
            .Where(x => x.MembershipId == membershipId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (existing.Count > 0)
        {
            db.OrganizationMembershipBranchAssignments.RemoveRange(existing);
        }

        foreach (var branchId in branchIds)
        {
            var assignment = OrganizationMembershipBranchAssignment.Create(
                organizationId,
                membershipId,
                branchId,
                utcNow,
                actorReference: actorReference);
            db.OrganizationMembershipBranchAssignments.Add(ToRecord(assignment));
        }
    }

    public async Task AssignPrimaryBranchForNewStaffAsync(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        DateTimeOffset utcNow,
        string? actorReference,
        CancellationToken cancellationToken = default)
    {
        var primary = await branches.GetPrimaryAsync(organizationId, cancellationToken).ConfigureAwait(false)
            ?? (await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(b => b.Status == OrganizationBranchStatus.Active);
        if (primary is null)
        {
            return;
        }

        var assignment = OrganizationMembershipBranchAssignment.Create(
            organizationId,
            membershipId,
            primary.Id,
            utcNow,
            actorReference: actorReference);
        db.OrganizationMembershipBranchAssignments.Add(ToRecord(assignment));
    }

    private static OrganizationMembershipBranchAssignment ToDomain(
        OrganizationMembershipBranchAssignmentRecord record) =>
        OrganizationMembershipBranchAssignment.Rehydrate(
            OrganizationMembershipBranchAssignmentId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            OrganizationMembershipId.From(record.MembershipId),
            OrganizationBranchId.From(record.BranchId),
            record.CreatedAtUtc,
            record.ActorReference);

    private static OrganizationMembershipBranchAssignmentRecord ToRecord(
        OrganizationMembershipBranchAssignment assignment) =>
        new()
        {
            Id = assignment.Id.Value,
            OrganizationId = assignment.OrganizationId.Value,
            MembershipId = assignment.MembershipId.Value,
            BranchId = assignment.BranchId.Value,
            CreatedAtUtc = assignment.CreatedAtUtc,
            ActorReference = assignment.ActorReference
        };
}
