using ExItS.Platform.Domain.PrivacyCompliance;
using ExItS.Platform.Infrastructure.Persistence.PrivacyCompliance;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class PrivacyComplianceEntityMapper
{
    public static ComplianceRequirement ToDomain(ComplianceRequirementRecord record) =>
        ComplianceRequirement.Rehydrate(
            record.Id,
            record.Code,
            record.Title,
            Enum.Parse<ComplianceItemCategory>(record.Category),
            record.Description,
            Enum.Parse<ComplianceRequirementLevel>(record.RequirementLevel),
            Enum.Parse<ComplianceItemStatus>(record.Status),
            record.OwnerRole,
            record.Version,
            record.EffectiveDate,
            record.LastReviewedDate,
            record.NextReviewDate,
            record.Notes,
            record.SourceReference,
            record.RequiresDpoLegalVerification,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static ComplianceRequirementRecord ToRecord(ComplianceRequirement requirement) =>
        new()
        {
            Id = requirement.Id,
            Code = requirement.Code,
            Title = requirement.Title,
            Category = requirement.Category.ToString(),
            Description = requirement.Description,
            RequirementLevel = requirement.RequirementLevel.ToString(),
            Status = requirement.Status.ToString(),
            OwnerRole = requirement.OwnerRole,
            Version = requirement.Version,
            EffectiveDate = requirement.EffectiveDate,
            LastReviewedDate = requirement.LastReviewedDate,
            NextReviewDate = requirement.NextReviewDate,
            Notes = requirement.Notes,
            SourceReference = requirement.SourceReference,
            RequiresDpoLegalVerification = requirement.RequiresDpoLegalVerification,
            CreatedAtUtc = requirement.CreatedAtUtc,
            UpdatedAtUtc = requirement.UpdatedAtUtc
        };

    public static void ApplyToRecord(ComplianceRequirement requirement, ComplianceRequirementRecord record)
    {
        record.Title = requirement.Title;
        record.Category = requirement.Category.ToString();
        record.Description = requirement.Description;
        record.RequirementLevel = requirement.RequirementLevel.ToString();
        record.Status = requirement.Status.ToString();
        record.OwnerRole = requirement.OwnerRole;
        record.Version = requirement.Version;
        record.EffectiveDate = requirement.EffectiveDate;
        record.LastReviewedDate = requirement.LastReviewedDate;
        record.NextReviewDate = requirement.NextReviewDate;
        record.Notes = requirement.Notes;
        record.SourceReference = requirement.SourceReference;
        record.RequiresDpoLegalVerification = requirement.RequiresDpoLegalVerification;
        record.UpdatedAtUtc = requirement.UpdatedAtUtc;
    }

    public static ComplianceEvidenceReference ToDomain(ComplianceEvidenceRecord record) =>
        ComplianceEvidenceReference.Rehydrate(
            record.Id,
            record.RequirementId,
            Enum.Parse<ComplianceEvidenceKind>(record.Kind),
            record.Label,
            record.ReferencePath,
            record.Notes,
            record.CreatedAtUtc);

    public static ComplianceEvidenceRecord ToRecord(ComplianceEvidenceReference evidence) =>
        new()
        {
            Id = evidence.Id,
            RequirementId = evidence.RequirementId,
            Kind = evidence.Kind.ToString(),
            Label = evidence.Label,
            ReferencePath = evidence.ReferencePath,
            Notes = evidence.Notes,
            CreatedAtUtc = evidence.CreatedAtUtc
        };

    public static ProcessingSystemRecord ToDomain(ProcessingSystemRecordEntity record) =>
        ProcessingSystemRecord.Rehydrate(
            record.Id,
            record.Code,
            record.SystemName,
            record.Purpose,
            record.DataSubjects,
            record.PersonalDataCategories,
            record.SensitiveDataCategories,
            record.StorageLocation,
            record.RecipientsProcessors,
            record.RetentionSummary,
            record.SecurityControls,
            record.Owner,
            Enum.Parse<ProcessingSystemPiaStatus>(record.PiaStatus),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static ProcessingSystemRecordEntity ToRecord(ProcessingSystemRecord system) =>
        new()
        {
            Id = system.Id,
            Code = system.Code,
            SystemName = system.SystemName,
            Purpose = system.Purpose,
            DataSubjects = system.DataSubjects,
            PersonalDataCategories = system.PersonalDataCategories,
            SensitiveDataCategories = system.SensitiveDataCategories,
            StorageLocation = system.StorageLocation,
            RecipientsProcessors = system.RecipientsProcessors,
            RetentionSummary = system.RetentionSummary,
            SecurityControls = system.SecurityControls,
            Owner = system.Owner,
            PiaStatus = system.PiaStatus.ToString(),
            CreatedAtUtc = system.CreatedAtUtc,
            UpdatedAtUtc = system.UpdatedAtUtc
        };

    public static void ApplyToRecord(ProcessingSystemRecord system, ProcessingSystemRecordEntity record)
    {
        record.SystemName = system.SystemName;
        record.Purpose = system.Purpose;
        record.DataSubjects = system.DataSubjects;
        record.PersonalDataCategories = system.PersonalDataCategories;
        record.SensitiveDataCategories = system.SensitiveDataCategories;
        record.StorageLocation = system.StorageLocation;
        record.RecipientsProcessors = system.RecipientsProcessors;
        record.RetentionSummary = system.RetentionSummary;
        record.SecurityControls = system.SecurityControls;
        record.Owner = system.Owner;
        record.PiaStatus = system.PiaStatus.ToString();
        record.UpdatedAtUtc = system.UpdatedAtUtc;
    }
}
