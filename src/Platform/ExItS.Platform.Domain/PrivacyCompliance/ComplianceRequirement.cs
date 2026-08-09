using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.PrivacyCompliance;

/// <summary>
/// Platform-owned compliance-management requirement. Status reflects documentation readiness,
/// never legal certification or NPC "Compliant" claims.
/// </summary>
public sealed class ComplianceRequirement
{
    public Guid Id { get; }
    public string Code { get; }
    public string Title { get; private set; }
    public ComplianceItemCategory Category { get; private set; }
    public string Description { get; private set; }
    public ComplianceRequirementLevel RequirementLevel { get; private set; }
    public ComplianceItemStatus Status { get; private set; }
    public string OwnerRole { get; private set; }
    public string Version { get; private set; }
    public DateOnly? EffectiveDate { get; private set; }
    public DateOnly? LastReviewedDate { get; private set; }
    public DateOnly? NextReviewDate { get; private set; }
    public string? Notes { get; private set; }
    public string? SourceReference { get; private set; }
    public bool RequiresDpoLegalVerification { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ComplianceRequirement(
        Guid id,
        string code,
        string title,
        ComplianceItemCategory category,
        string description,
        ComplianceRequirementLevel requirementLevel,
        ComplianceItemStatus status,
        string ownerRole,
        string version,
        DateOnly? effectiveDate,
        DateOnly? lastReviewedDate,
        DateOnly? nextReviewDate,
        string? notes,
        string? sourceReference,
        bool requiresDpoLegalVerification,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Code = code;
        Title = title;
        Category = category;
        Description = description;
        RequirementLevel = requirementLevel;
        Status = status;
        OwnerRole = ownerRole;
        Version = version;
        EffectiveDate = effectiveDate;
        LastReviewedDate = lastReviewedDate;
        NextReviewDate = nextReviewDate;
        Notes = notes;
        SourceReference = sourceReference;
        RequiresDpoLegalVerification = requiresDpoLegalVerification;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static ComplianceRequirement Create(
        string code,
        string title,
        ComplianceItemCategory category,
        string description,
        ComplianceRequirementLevel requirementLevel,
        string ownerRole,
        DateTimeOffset utcNow,
        ComplianceItemStatus status = ComplianceItemStatus.NotStarted,
        string version = "0.1.0",
        string? sourceReference = null,
        bool requiresDpoLegalVerification = false,
        Guid? id = null)
    {
        DomainTime.EnsureUtc(utcNow);
        code = NormalizeCode(code);
        title = NormalizeRequired(title, 200, nameof(title));
        description = NormalizeRequired(description, 4000, nameof(description));
        ownerRole = NormalizeRequired(ownerRole, 120, nameof(ownerRole));
        version = NormalizeRequired(version, 32, nameof(version));
        sourceReference = NormalizeOptional(sourceReference, 500);

        return new ComplianceRequirement(
            id ?? Guid.NewGuid(),
            code,
            title,
            category,
            description,
            requirementLevel,
            status,
            ownerRole,
            version,
            effectiveDate: null,
            lastReviewedDate: null,
            nextReviewDate: null,
            notes: null,
            sourceReference,
            requiresDpoLegalVerification,
            utcNow,
            utcNow);
    }

    public static ComplianceRequirement Rehydrate(
        Guid id,
        string code,
        string title,
        ComplianceItemCategory category,
        string description,
        ComplianceRequirementLevel requirementLevel,
        ComplianceItemStatus status,
        string ownerRole,
        string version,
        DateOnly? effectiveDate,
        DateOnly? lastReviewedDate,
        DateOnly? nextReviewDate,
        string? notes,
        string? sourceReference,
        bool requiresDpoLegalVerification,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            code,
            title,
            category,
            description,
            requirementLevel,
            status,
            ownerRole,
            version,
            effectiveDate,
            lastReviewedDate,
            nextReviewDate,
            notes,
            sourceReference,
            requiresDpoLegalVerification,
            createdAtUtc,
            updatedAtUtc);

    public void TransitionStatus(ComplianceItemStatus next, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (!ComplianceStatusRules.CanTransition(Status, next))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidComplianceStatusTransition,
                $"Cannot transition compliance requirement from {Status} to {next}.");
        }

        Status = next;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateDetails(
        string? notes,
        string? version,
        DateOnly? effectiveDate,
        DateOnly? lastReviewedDate,
        DateOnly? nextReviewDate,
        DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (notes is not null)
        {
            Notes = NormalizeOptional(notes, 4000);
        }

        if (version is not null)
        {
            Version = NormalizeRequired(version, 32, nameof(version));
        }

        EffectiveDate = effectiveDate ?? EffectiveDate;
        LastReviewedDate = lastReviewedDate ?? LastReviewedDate;
        NextReviewDate = nextReviewDate ?? NextReviewDate;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException(DomainErrorCodes.InvalidComplianceRequirementCode, "Code is required.");
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidComplianceRequirementCode, "Code is too long.");
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidComplianceRequirementField, $"{name} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new DomainException(DomainErrorCodes.InvalidComplianceRequirementField, $"{name} is too long.");
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new DomainException(DomainErrorCodes.InvalidComplianceRequirementField, "Field is too long.");
        }

        return trimmed;
    }
}
