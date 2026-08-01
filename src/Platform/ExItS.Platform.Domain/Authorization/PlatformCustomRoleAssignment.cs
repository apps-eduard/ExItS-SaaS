using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Assigns a custom <see cref="PlatformRoleDefinition"/> to a Platform User (platform-wide).
/// Built-in system roles continue to use <see cref="PlatformRoleAssignment"/>.
/// </summary>
public sealed class PlatformCustomRoleAssignment
{
    public PlatformCustomRoleAssignmentId Id { get; }
    public PlatformUserId PlatformUserId { get; }
    public PlatformRoleDefinitionId RoleDefinitionId { get; }
    public PlatformRoleAssignmentStatus Status { get; private set; }
    public string GrantedByActor { get; }
    public DateTimeOffset GrantedAtUtc { get; }
    public string? Reason { get; private set; }
    public string? RevokedByActor { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokeReason { get; private set; }

    private PlatformCustomRoleAssignment(
        PlatformCustomRoleAssignmentId id,
        PlatformUserId platformUserId,
        PlatformRoleDefinitionId roleDefinitionId,
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
        RoleDefinitionId = roleDefinitionId;
        Status = status;
        GrantedByActor = grantedByActor;
        GrantedAtUtc = grantedAtUtc;
        Reason = reason;
        RevokedByActor = revokedByActor;
        RevokedAtUtc = revokedAtUtc;
        RevokeReason = revokeReason;
    }

    public static PlatformCustomRoleAssignment Grant(
        PlatformUserId platformUserId,
        PlatformRoleDefinitionId roleDefinitionId,
        string grantedByActor,
        DateTimeOffset utcNow,
        string? reason = null,
        PlatformCustomRoleAssignmentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(platformUserId);
        ArgumentNullException.ThrowIfNull(roleDefinitionId);
        DomainTime.EnsureUtc(utcNow);
        return new PlatformCustomRoleAssignment(
            id ?? PlatformCustomRoleAssignmentId.New(),
            platformUserId,
            roleDefinitionId,
            PlatformRoleAssignmentStatus.Active,
            NormalizeActor(grantedByActor),
            utcNow,
            NormalizeOptionalText(reason),
            null,
            null,
            null);
    }

    public static PlatformCustomRoleAssignment Rehydrate(
        PlatformCustomRoleAssignmentId id,
        PlatformUserId platformUserId,
        PlatformRoleDefinitionId roleDefinitionId,
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
            roleDefinitionId,
            status,
            grantedByActor,
            grantedAtUtc,
            reason,
            revokedByActor,
            revokedAtUtc,
            revokeReason);

    public void Revoke(string revokedByActor, string? reason, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == PlatformRoleAssignmentStatus.Revoked)
        {
            return;
        }

        Status = PlatformRoleAssignmentStatus.Revoked;
        RevokedByActor = NormalizeActor(revokedByActor);
        RevokedAtUtc = utcNow;
        RevokeReason = NormalizeOptionalText(reason);
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

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 512)
        {
            throw new DomainException(
                DomainErrorCodes.OverrideReasonRequired,
                "Value must be at most 512 characters.");
        }

        return trimmed;
    }
}
