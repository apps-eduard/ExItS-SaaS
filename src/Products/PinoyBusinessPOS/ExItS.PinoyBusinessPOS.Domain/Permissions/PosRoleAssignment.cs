using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Permissions;

/// <summary>
/// Organization-scoped POS role assignment. Active rows are mutable only via Revoke;
/// revoked rows are immutable history.
/// </summary>
public sealed class PosRoleAssignment
{
    public const int RevocationReasonMaxLength = 512;

    public PosRoleAssignmentId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public Guid ActorId { get; }
    public PosRole Role { get; }
    public PosRoleAssignmentStatus Status { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; }
    public Guid AssignedBy { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public string? RevocationReason { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private PosRoleAssignment(
        PosRoleAssignmentId id,
        PosOrganizationId organizationId,
        Guid actorId,
        PosRole role,
        PosRoleAssignmentStatus status,
        DateTimeOffset assignedAtUtc,
        Guid assignedBy,
        DateTimeOffset? revokedAtUtc,
        Guid? revokedBy,
        string? revocationReason,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ActorId = actorId;
        Role = role;
        Status = status;
        AssignedAtUtc = assignedAtUtc;
        AssignedBy = assignedBy;
        RevokedAtUtc = revokedAtUtc;
        RevokedBy = revokedBy;
        RevocationReason = revocationReason;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static PosRoleAssignment Assign(
        PosOrganizationId organizationId,
        Guid actorId,
        PosRole role,
        Guid assignedBy,
        DateTimeOffset utcNow,
        PosRoleAssignmentId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        SaleMoney.EnsureActor(assignedBy);

        return new PosRoleAssignment(
            id ?? PosRoleAssignmentId.New(),
            organizationId,
            actorId,
            role,
            PosRoleAssignmentStatus.Active,
            utcNow,
            assignedBy,
            revokedAtUtc: null,
            revokedBy: null,
            revocationReason: null,
            utcNow);
    }

    public void Revoke(Guid revokedBy, DateTimeOffset utcNow, string? reason = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(revokedBy);

        if (Status != PosRoleAssignmentStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.PosRoleAssignmentAlreadyRevoked,
                "Only an active role assignment can be revoked.");
        }

        string? normalizedReason = null;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            normalizedReason = reason.Trim();
            if (normalizedReason.Length > RevocationReasonMaxLength)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPosRoleRevocationReason,
                    $"Revocation reason cannot exceed {RevocationReasonMaxLength} characters.");
            }
        }

        Status = PosRoleAssignmentStatus.Revoked;
        RevokedAtUtc = utcNow;
        RevokedBy = revokedBy;
        RevocationReason = normalizedReason;
        UpdatedAtUtc = utcNow;
    }

    public static PosRoleAssignment Rehydrate(
        PosRoleAssignmentId id,
        PosOrganizationId organizationId,
        Guid actorId,
        PosRole role,
        PosRoleAssignmentStatus status,
        DateTimeOffset assignedAtUtc,
        Guid assignedBy,
        DateTimeOffset? revokedAtUtc,
        Guid? revokedBy,
        string? revocationReason,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            actorId,
            role,
            status,
            assignedAtUtc,
            assignedBy,
            revokedAtUtc,
            revokedBy,
            revocationReason,
            updatedAtUtc);
}
