namespace ExItS.Platform.Admin.Models;

public sealed record ComplianceRequirementDto(
    Guid Id,
    string Code,
    string Title,
    string Category,
    string Description,
    string RequirementLevel,
    string Status,
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
    string Kind,
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
    string PiaStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PrivacyComplianceOverviewDto(
    int TotalRequirements,
    int TotalSystems,
    int TotalEvidence,
    IReadOnlyDictionary<string, int> RequirementsByStatus,
    IReadOnlyDictionary<string, int> RequirementsByCategory,
    DateTimeOffset? LastUpdatedUtc,
    string OverallReadiness = "NotAssessed",
    int ReadyCount = 0,
    int ActionNeededCount = 0,
    int ExternalLegalReviewCount = 0,
    int RequirementsWithEvidenceCount = 0,
    string TechnicalSafeguardsSummary = "Partial",
    string GovernanceDocumentationSummary = "Unavailable",
    string LegalReviewSummary = "Required",
    string NpcVerificationSummary = "NotVerified",
    IReadOnlyList<PrivacyReadinessCategorySummaryDto>? CategorySummaries = null,
    IReadOnlyList<PrivacyImpactFollowUpDto>? PrivacyImpactFollowUps = null);

public sealed record PrivacyReadinessCategorySummaryDto(
    string Group,
    string DetailRoute,
    int RequirementCount,
    int ReadyCount,
    int ActionNeededCount,
    int EvidenceCoveredCount,
    DateOnly? LastReviewedDate,
    string Status,
    bool HasActionNeeded);

public sealed record PrivacyImpactFollowUpDto(
    string Code,
    string Title,
    string Status,
    bool RequiresDpoLegalVerification,
    int EvidenceCount,
    DateOnly? LastReviewedDate);

public sealed record EnsurePrivacyComplianceCatalogResultDto(
    int RequirementsAdded,
    int SystemsAdded,
    int EvidenceAdded);

public sealed record UpdateComplianceRequirementStatusRequest(string Status);

public sealed record UpdateComplianceRequirementDetailsRequest(
    string? Notes = null,
    string? Version = null,
    DateOnly? EffectiveDate = null,
    DateOnly? LastReviewedDate = null,
    DateOnly? NextReviewDate = null);

public sealed record AddComplianceEvidenceRequest(
    Guid RequirementId,
    string Kind,
    string Label,
    string ReferencePath,
    string? Notes = null);

public sealed record PrivacyComplianceEvidenceRow(
    ComplianceEvidenceDto Evidence,
    string RequirementCode,
    string RequirementTitle);
