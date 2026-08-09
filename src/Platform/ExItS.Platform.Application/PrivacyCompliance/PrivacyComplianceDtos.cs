using ExItS.Platform.Domain.PrivacyCompliance;

namespace ExItS.Platform.Application.PrivacyCompliance;

public sealed record ComplianceRequirementDto(
    Guid Id,
    string Code,
    string Title,
    ComplianceItemCategory Category,
    string Description,
    ComplianceRequirementLevel RequirementLevel,
    ComplianceItemStatus Status,
    string OwnerRole,
    string Version,
    DateOnly? EffectiveDate,
    DateOnly? LastReviewedDate,
    DateOnly? NextReviewDate,
    string? Notes,
    string? SourceReference,
    bool RequiresDpoLegalVerification,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int EvidenceCount);

public sealed record ComplianceEvidenceDto(
    Guid Id,
    Guid RequirementId,
    ComplianceEvidenceKind Kind,
    string Label,
    string ReferencePath,
    string? Notes,
    DateTimeOffset CreatedAtUtc);

public sealed record ProcessingSystemDto(
    Guid Id,
    string Code,
    string SystemName,
    string Purpose,
    string DataSubjects,
    string PersonalDataCategories,
    string? SensitiveDataCategories,
    string StorageLocation,
    string? RecipientsProcessors,
    string? RetentionSummary,
    string? SecurityControls,
    string Owner,
    ProcessingSystemPiaStatus PiaStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PrivacyComplianceOverviewDto(
    int TotalRequirements,
    int TotalSystems,
    int TotalEvidence,
    IReadOnlyDictionary<string, int> RequirementsByStatus,
    IReadOnlyDictionary<string, int> RequirementsByCategory,
    DateTimeOffset? LastUpdatedUtc);

public sealed record EnsurePrivacyComplianceCatalogResultDto(
    int RequirementsAdded,
    int SystemsAdded,
    int EvidenceAdded);

public sealed record UpdateComplianceRequirementStatusRequest(ComplianceItemStatus Status);

public sealed record UpdateComplianceRequirementDetailsRequest(
    string? Notes = null,
    string? Version = null,
    DateOnly? EffectiveDate = null,
    DateOnly? LastReviewedDate = null,
    DateOnly? NextReviewDate = null);

public sealed record AddComplianceEvidenceRequest(
    Guid RequirementId,
    ComplianceEvidenceKind Kind,
    string Label,
    string ReferencePath,
    string? Notes = null);

public sealed record ExportComplianceRequirementPdfResult(
    byte[] Content,
    string FileName,
    string ContentType);
