using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Assigns a custom organization role definition to a Platform User within one organization.
/// Does not grant platform roles or product-local POS roles.
/// </summary>
public sealed class OrganizationCustomRoleAssignment
{
    public OrganizationCustomRoleAssignmentId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId PlatformUserId { get; }
    public OrganizationRoleDefinitionId RoleDefinitionId { get; }
    public PlatformRoleAssignmentStatus Status { get; private set; }
    public string GrantedByActor { get; }
    public DateTimeOffset GrantedAtUtc { get; }
    public string? Reason { get; private set; }
    public string? RevokedByActor { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokeReason { get; private set; }

    private OrganizationCustomRoleAssignment(
        OrganizationCustomRoleAssignmentId id,
        PlatformOrganizationId organizationId,
        PlatformUserId platformUserId,
        OrganizationRoleDefinitionId roleDefinitionId,
        PlatformRoleAssignmentStatus status,
        string grantedByActor,
        DateTimeOffset grantedAtUtc,
        string? reason,
        string? revokedByActor,
        DateTimeOffset? revokedAtUtc,
        string? revokeReason)
    {
        Id = id;
        OrganizationId = organizationId;
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

    public static OrganizationCustomRoleAssignment Grant(
        PlatformOrganizationId organizationId,
        PlatformUserId platformUserId,
        OrganizationRoleDefinitionId roleDefinitionId,
        string grantedByActor,
        DateTimeOffset utcNow,
        string? reason = null,
        OrganizationCustomRoleAssignmentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(platformUserId);
        ArgumentNullException.ThrowIfNull(roleDefinitionId);
        DomainTime.EnsureUtc(utcNow);
        return new OrganizationCustomRoleAssignment(
            id ?? OrganizationCustomRoleAssignmentId.New(),
            organizationId,
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

    public static OrganizationCustomRoleAssignment Rehydrate(
        OrganizationCustomRoleAssignmentId id,
        PlatformOrganizationId organizationId,
        PlatformUserId platformUserId,
        OrganizationRoleDefinitionId roleDefinitionId,
        PlatformRoleAssignmentStatus status,
        string grantedByActor,
        DateTimeOffset grantedAtUtc,
        string? reason,
        string? revokedByActor,
        DateTimeOffset? revokedAtUtc,
        string? revokeReason) =>
        new(
            id,
            organizationId,
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
