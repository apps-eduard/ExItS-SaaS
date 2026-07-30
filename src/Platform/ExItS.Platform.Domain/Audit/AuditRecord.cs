using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Domain.Audit;

/// <summary>
/// Append-only Platform audit record. Records who did what to what, when, and with what outcome.
/// Never mutated or deleted after creation — there is intentionally no Update method.
/// </summary>
public sealed class AuditRecord
{
    private const int ActorIdentifierMaxLength = 256;
    private const int ActionCodeMaxLength = 128;
    private const int TargetTypeMaxLength = 64;
    private const int TargetIdMaxLength = 128;
    private const int CorrelationIdMaxLength = 128;
    private const int ReasonMaxLength = 512;
    private const int SummaryMaxLength = 2000;

    public AuditRecordId Id { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public string ActorIdentifier { get; }
    public AuditActorType ActorType { get; }
    public string ActionCode { get; }
    public string TargetType { get; }
    public string TargetId { get; }
    public PlatformOrganizationId? OrganizationId { get; }
    public ProductCode? ProductCode { get; }
    public string? CorrelationId { get; }
    public AuditOutcome Outcome { get; }
    public string? Reason { get; }

    /// <summary>Safe before/after summary text. Must never contain secrets, PHI, or payment credentials.</summary>
    public string? Summary { get; }

    private AuditRecord(
        AuditRecordId id,
        DateTimeOffset occurredAtUtc,
        string actorIdentifier,
        AuditActorType actorType,
        string actionCode,
        string targetType,
        string targetId,
        PlatformOrganizationId? organizationId,
        ProductCode? productCode,
        string? correlationId,
        AuditOutcome outcome,
        string? reason,
        string? summary)
    {
        Id = id;
        OccurredAtUtc = occurredAtUtc;
        ActorIdentifier = actorIdentifier;
        ActorType = actorType;
        ActionCode = actionCode;
        TargetType = targetType;
        TargetId = targetId;
        OrganizationId = organizationId;
        ProductCode = productCode;
        CorrelationId = correlationId;
        Outcome = outcome;
        Reason = reason;
        Summary = summary;
    }

    public static AuditRecord Create(
        DateTimeOffset occurredAtUtc,
        string actorIdentifier,
        AuditActorType actorType,
        string actionCode,
        string targetType,
        string targetId,
        AuditOutcome outcome,
        PlatformOrganizationId? organizationId = null,
        ProductCode? productCode = null,
        string? correlationId = null,
        string? reason = null,
        string? summary = null,
        AuditRecordId? id = null)
    {
        EnsureUtc(occurredAtUtc);
        EnsureDefinedActorType(actorType);
        EnsureDefinedOutcome(outcome);

        return new AuditRecord(
            id ?? AuditRecordId.New(),
            occurredAtUtc,
            NormalizeRequired(actorIdentifier, ActorIdentifierMaxLength, DomainErrorCodes.InvalidAuditActorIdentifier, "Actor identifier"),
            actorType,
            NormalizeRequired(actionCode, ActionCodeMaxLength, DomainErrorCodes.InvalidAuditActionCode, "Action code"),
            NormalizeRequired(targetType, TargetTypeMaxLength, DomainErrorCodes.InvalidAuditTargetType, "Target type"),
            NormalizeRequired(targetId, TargetIdMaxLength, DomainErrorCodes.InvalidAuditTargetId, "Target id"),
            organizationId,
            productCode,
            NormalizeOptional(correlationId, CorrelationIdMaxLength, DomainErrorCodes.InvalidAuditCorrelationId, "Correlation id"),
            outcome,
            NormalizeOptional(reason, ReasonMaxLength, DomainErrorCodes.InvalidAuditReason, "Reason"),
            NormalizeOptional(summary, SummaryMaxLength, DomainErrorCodes.InvalidAuditSummary, "Summary"));
    }

    /// <summary>Rehydrate from persistence.</summary>
    public static AuditRecord Rehydrate(
        AuditRecordId id,
        DateTimeOffset occurredAtUtc,
        string actorIdentifier,
        AuditActorType actorType,
        string actionCode,
        string targetType,
        string targetId,
        PlatformOrganizationId? organizationId,
        ProductCode? productCode,
        string? correlationId,
        AuditOutcome outcome,
        string? reason,
        string? summary) =>
        new(
            id,
            occurredAtUtc,
            actorIdentifier,
            actorType,
            actionCode,
            targetType,
            targetId,
            organizationId,
            productCode,
            correlationId,
            outcome,
            reason,
            summary);

    private static string NormalizeRequired(string value, int maxLength, string errorCode, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(errorCode, $"{fieldLabel} cannot be blank.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{fieldLabel} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string errorCode, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{fieldLabel} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static void EnsureDefinedActorType(AuditActorType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidAuditActorType, "Actor type is not defined.");
        }
    }

    private static void EnsureDefinedOutcome(AuditOutcome value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidAuditOutcome, "Outcome is not defined.");
        }
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
