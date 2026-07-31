using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Permissions;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class PosRoleAssignmentRepository(PosDbContext db) : IPosRoleAssignmentRepository
{
    private const string LockSql = "SELECT pg_advisory_xact_lock({0})";

    public async Task AcquireOrganizationLockAsync(PosOrganizationId organizationId, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync(LockSql, [OrganizationLockKey(organizationId)], ct)
            .ConfigureAwait(false);
    }

    public async Task<PosRoleAssignment?> GetByIdAsync(
        PosOrganizationId organizationId,
        PosRoleAssignmentId id,
        CancellationToken ct = default)
    {
        var record = await db.PosRoleAssignments.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id.Value && r.OrganizationId == organizationId.Value, ct)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<PosRoleAssignment?> GetActiveForActorAsync(
        PosOrganizationId organizationId,
        Guid actorId,
        CancellationToken ct = default)
    {
        var record = await db.PosRoleAssignments.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value
                    && r.ActorId == actorId
                    && r.Status == PosRoleAssignmentStatusCodes.Active,
                ct)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task<int> CountActiveOwnersAsync(PosOrganizationId organizationId, CancellationToken ct = default) =>
        db.PosRoleAssignments.AsNoTracking()
            .CountAsync(
                r => r.OrganizationId == organizationId.Value
                    && r.Status == PosRoleAssignmentStatusCodes.Active
                    && r.Role == PosRoleCodes.Owner,
                ct);

    public Task<bool> HasAnyAssignmentsAsync(PosOrganizationId organizationId, CancellationToken ct = default) =>
        db.PosRoleAssignments.AsNoTracking()
            .AnyAsync(r => r.OrganizationId == organizationId.Value, ct);

    public async Task<(IReadOnlyList<PosRoleAssignment> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        PosRoleAssignmentStatus? status,
        Guid? actorId,
        PosRole? role,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.PosRoleAssignments.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value);

        if (status is not null)
        {
            var code = PosRoleAssignmentStatusCodes.ToCode(status.Value);
            query = query.Where(r => r.Status == code);
        }

        if (actorId is Guid aid)
        {
            query = query.Where(r => r.ActorId == aid);
        }

        if (role is PosRole posRole)
        {
            var code = PosRoleCodes.ToCode(posRole);
            query = query.Where(r => r.Role == code);
        }

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(r => r.AssignedAtUtc)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (records.Select(ToDomain).ToList(), total);
    }

    public async Task AddAsync(PosRoleAssignment assignment, CancellationToken ct = default)
    {
        await db.PosRoleAssignments.AddAsync(ToRecord(assignment), ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(PosRoleAssignment assignment, CancellationToken ct = default)
    {
        var record = await db.PosRoleAssignments
            .FirstAsync(r => r.Id == assignment.Id.Value && r.OrganizationId == assignment.OrganizationId.Value, ct)
            .ConfigureAwait(false);

        record.Status = PosRoleAssignmentStatusCodes.ToCode(assignment.Status);
        record.RevokedAtUtc = assignment.RevokedAtUtc;
        record.RevokedBy = assignment.RevokedBy;
        record.RevocationReason = assignment.RevocationReason;
        record.UpdatedAtUtc = assignment.UpdatedAtUtc;
    }

    private static PosRoleAssignmentRecord ToRecord(PosRoleAssignment a) => new()
    {
        Id = a.Id.Value,
        OrganizationId = a.OrganizationId.Value,
        ActorId = a.ActorId,
        Role = PosRoleCodes.ToCode(a.Role),
        Status = PosRoleAssignmentStatusCodes.ToCode(a.Status),
        AssignedAtUtc = a.AssignedAtUtc,
        AssignedBy = a.AssignedBy,
        RevokedAtUtc = a.RevokedAtUtc,
        RevokedBy = a.RevokedBy,
        RevocationReason = a.RevocationReason,
        UpdatedAtUtc = a.UpdatedAtUtc
    };

    private static PosRoleAssignment ToDomain(PosRoleAssignmentRecord r)
    {
        if (!PosRoleCodes.TryParse(r.Role, out var role)
            || !PosRoleAssignmentStatusCodes.TryParse(r.Status, out var status))
        {
            throw new InvalidOperationException($"Corrupt role assignment row {r.Id:D}.");
        }

        return PosRoleAssignment.Rehydrate(
            PosRoleAssignmentId.From(r.Id),
            PosOrganizationId.From(r.OrganizationId),
            r.ActorId,
            role,
            status,
            r.AssignedAtUtc,
            r.AssignedBy,
            r.RevokedAtUtc,
            r.RevokedBy,
            r.RevocationReason,
            r.UpdatedAtUtc);
    }

    private static long OrganizationLockKey(PosOrganizationId organizationId)
    {
        Span<byte> bytes = stackalloc byte[17];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        bytes[16] = 0x50; // role assignment lock namespace
        unchecked
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            return (long)hash;
        }
    }
}
