using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Infrastructure.Persistence.Audit;
using ExItS.Platform.Infrastructure.Persistence.Authorization;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class AuthorizationAuditEntityMapper
{
    public static PlatformRoleAssignment ToDomain(PlatformRoleAssignmentRecord record) =>
        PlatformRoleAssignment.Rehydrate(
            PlatformRoleAssignmentId.From(record.Id),
            PlatformUserId.From(record.PlatformUserId),
            Enum.Parse<PlatformSystemRole>(record.Role),
            record.OrganizationId.HasValue ? PlatformOrganizationId.From(record.OrganizationId.Value) : null,
            Enum.Parse<PlatformRoleAssignmentStatus>(record.Status),
            record.GrantedByActor,
            record.GrantedAtUtc,
            record.Reason,
            record.RevokedByActor,
            record.RevokedAtUtc,
            record.RevokeReason);

    public static PlatformRoleAssignmentRecord ToRecord(PlatformRoleAssignment assignment) =>
        new()
        {
            Id = assignment.Id.Value,
            PlatformUserId = assignment.PlatformUserId.Value,
            Role = assignment.Role.ToString(),
            OrganizationId = assignment.OrganizationId?.Value,
            Status = assignment.Status.ToString(),
            GrantedByActor = assignment.GrantedByActor,
            GrantedAtUtc = assignment.GrantedAtUtc,
            Reason = assignment.Reason,
            RevokedByActor = assignment.RevokedByActor,
            RevokedAtUtc = assignment.RevokedAtUtc,
            RevokeReason = assignment.RevokeReason
        };

    public static void ApplyToRecord(PlatformRoleAssignment assignment, PlatformRoleAssignmentRecord record)
    {
        record.Status = assignment.Status.ToString();
        record.Reason = assignment.Reason;
        record.RevokedByActor = assignment.RevokedByActor;
        record.RevokedAtUtc = assignment.RevokedAtUtc;
        record.RevokeReason = assignment.RevokeReason;
    }

    public static AuditRecord ToDomain(AuditRecordRecord record) =>
        AuditRecord.Rehydrate(
            AuditRecordId.From(record.Id),
            record.OccurredAtUtc,
            record.ActorIdentifier,
            Enum.Parse<AuditActorType>(record.ActorType),
            record.ActionCode,
            record.TargetType,
            record.TargetId,
            record.OrganizationId.HasValue ? PlatformOrganizationId.From(record.OrganizationId.Value) : null,
            record.ProductCode is null ? null : ProductCode.Create(record.ProductCode),
            record.CorrelationId,
            Enum.Parse<AuditOutcome>(record.Outcome),
            record.Reason,
            record.Summary);

    public static AuditRecordRecord ToRecord(AuditRecord record) =>
        new()
        {
            Id = record.Id.Value,
            OccurredAtUtc = record.OccurredAtUtc,
            ActorIdentifier = record.ActorIdentifier,
            ActorType = record.ActorType.ToString(),
            ActionCode = record.ActionCode,
            TargetType = record.TargetType,
            TargetId = record.TargetId,
            OrganizationId = record.OrganizationId?.Value,
            ProductCode = record.ProductCode?.Value,
            CorrelationId = record.CorrelationId,
            Outcome = record.Outcome.ToString(),
            Reason = record.Reason,
            Summary = record.Summary
        };
}
