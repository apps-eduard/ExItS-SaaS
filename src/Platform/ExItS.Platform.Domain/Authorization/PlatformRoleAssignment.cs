using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Grants a Platform system role (<see cref="PlatformSystemRole"/>) to a Platform User, either
/// platform-wide (OrganizationId is null) or scoped to one organization. Assignments are always to
/// a Platform User — never to a product-local role. Persistence enforces one active assignment per
/// (user, role, organization scope).
/// </summary>
public sealed class PlatformRoleAssignment
{
    public PlatformRoleAssignmentId Id { get; }
    public PlatformUserId PlatformUserId { get; }
    public PlatformSystemRole Role { get; }
    public PlatformOrganizationId? OrganizationId { get; }
    public PlatformRoleAssignmentStatus Status { get; private set; }
    public string GrantedByActor { get; }
    public DateTimeOffset GrantedAtUtc { get; }
    public string? Reason { get; private set; }
    public string? RevokedByActor { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokeReason { get; private set; }

    private PlatformRoleAssignment(
        PlatformRoleAssignmentId id,
        PlatformUserId platformUserId,
        PlatformSystemRole role,
        PlatformOrganizationId? organizationId,
        PlatformRoleAssignmentStatus status,
        string grantedByActor,
        DateTimeOffset grantedAtUtc,
        string? reason,
        string? revokedByActor,
        DateTimeOffset? revokedAtUtc,
        string? revokeReason)
    {
        Id = id;
        PlatformUserId = platformUserId;
        Role = role;
        OrganizationId = organizationId;
        Status = status;
        GrantedByActor = grantedByActor;
        GrantedAtUtc = grantedAtUtc;
        Reason = reason;
        RevokedByActor = revokedByActor;
        RevokedAtUtc = revokedAtUtc;
        RevokeReason = revokeReason;
    }

    public static PlatformRoleAssignment Grant(
        PlatformUserId platformUserId,
        PlatformSystemRole role,
        PlatformOrganizationId? organizationId,
        string grantedByActor,
        DateTimeOffset utcNow,
        string? reason = null,
        PlatformRoleAssignmentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(platformUserId);
        EnsureUtc(utcNow);
        EnsureDefinedRole(role);
        var actor = NormalizeActor(grantedByActor);

        return new PlatformRoleAssignment(
            id ?? PlatformRoleAssignmentId.New(),
            platformUserId,
            role,
            organizationId,
            PlatformRoleAssignmentStatus.Active,
            actor,
            utcNow,
            NormalizeOptionalText(reason, 512, DomainErrorCodes.OverrideReasonRequired),
            null,
            null,
            null);
    }

    /// <summary>Rehydrate from persistence.</summary>
    public static PlatformRoleAssignment Rehydrate(
        PlatformRoleAssignmentId id,
        PlatformUserId platformUserId,
        PlatformSystemRole role,
        PlatformOrganizationId? organizationId,
        PlatformRoleAssignmentStatus status,
        string grantedByActor,
        DateTimeOffset grantedAtUtc,
        string? reason,
        string? revokedByActor,
        DateTimeOffset? revokedAtUtc,
        string? revokeReason) =>
        new(
            id,
            platformUserId,
            role,
            organizationId,
            status,
            grantedByActor,
            grantedAtUtc,
            reason,
            revokedByActor,
            revokedAtUtc,
            revokeReason);

    public void Revoke(string revokedByActor, string? reason, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == PlatformRoleAssignmentStatus.Revoked)
        {
            return;
        }

        Status = PlatformRoleAssignmentStatus.Revoked;
        RevokedByActor = NormalizeActor(revokedByActor);
        RevokedAtUtc = utcNow;
        RevokeReason = NormalizeOptionalText(reason, 512, DomainErrorCodes.OverrideReasonRequired);
    }

    private static void EnsureDefinedRole(PlatformSystemRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformSystemRole,
                "Platform system role is not defined.");
        }
    }

    private static string NormalizeActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new DomainException(
                DomainErrorCodes.ActorReferenceRequired,
                "Actor reference is required.");
        }

        var trimmed = actor.Trim();
        if (trimmed.Length > 128)
        {
            throw new DomainException(
                DomainErrorCodes.ActorReferenceRequired,
                "Actor reference must be at most 128 characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"Value must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamps must be UTC (offset zero).");
        }
    }
}
