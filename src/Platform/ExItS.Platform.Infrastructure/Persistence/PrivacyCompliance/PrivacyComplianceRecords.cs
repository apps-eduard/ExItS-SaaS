namespace ExItS.Platform.Infrastructure.Persistence.PrivacyCompliance;

internal sealed class ComplianceRequirementRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequirementLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerRole { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateOnly? EffectiveDate { get; set; }
    public DateOnly? LastReviewedDate { get; set; }
    public DateOnly? NextReviewDate { get; set; }
    public string? Notes { get; set; }
    public string? SourceReference { get; set; }
    public bool RequiresDpoLegalVerification { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class ComplianceEvidenceRecord
{
    public Guid Id { get; set; }
    public Guid RequirementId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ReferencePath { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class ProcessingSystemRecordEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string DataSubjects { get; set; } = string.Empty;
    public string PersonalDataCategories { get; set; } = string.Empty;
    public string? SensitiveDataCategories { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public string? RecipientsProcessors { get; set; }
    public string? RetentionSummary { get; set; }
    public string? SecurityControls { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string PiaStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
