namespace ExItS.Platform.Domain.PrivacyCompliance;

/// <summary>
/// Operational privacy readiness (not legal/NPC certification).
/// Never treat evidence count or Approved status as regulatory compliance.
/// </summary>
public enum PrivacyReadinessOverallStatus
{
    NotStarted = 0,
    ActionNeeded = 1,
    InProgress = 2,
    ReadyForReview = 3,
    VerifiedInternally = 4,
    ExternalLegalReviewRequired = 5,
    NotAssessed = 6
}

/// <summary>Display grouping for the Platform Admin readiness dashboard.</summary>
public enum PrivacyReadinessCategoryGroup
{
    NoticesAndConsent = 0,
    Governance = 1,
    DataInventory = 2,
    RetentionAndDeletion = 3,
    DataSubjectRequests = 4,
    SecurityAndAccess = 5,
    IncidentResponse = 6,
    BusinessContinuity = 7,
    VendorsAndProcessors = 8,
    DpoRegulatoryReadiness = 9,
    PrivacyImpact = 10,
    Other = 11
}

/// <summary>Evidence presentation labels — technical evidence never implies legal verification.</summary>
public enum PrivacyEvidenceDisplayKind
{
    TechnicalEvidence = 0,
    OperationalEvidence = 1,
    ManualEvidence = 2,
    LegalReviewEvidence = 3,
    RegulatoryEvidence = 4
}

public static class PrivacyReadinessDerivation
{
    public static PrivacyReadinessCategoryGroup ResolveCategoryGroup(string code, ComplianceItemCategory category)
    {
        if (category == ComplianceItemCategory.PrivacyImpactAssessment
            || code.StartsWith("PIA_", StringComparison.OrdinalIgnoreCase))
        {
            return PrivacyReadinessCategoryGroup.PrivacyImpact;
        }

        if (category == ComplianceItemCategory.RegulatoryReadiness
            || code.StartsWith("DPO_", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("DPS_", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("NPC_", StringComparison.OrdinalIgnoreCase)
            || code.Contains("BREACH_REPORTING", StringComparison.OrdinalIgnoreCase)
            || code.Contains("NPC_SUBMISSION", StringComparison.OrdinalIgnoreCase))
        {
            return PrivacyReadinessCategoryGroup.DpoRegulatoryReadiness;
        }

        return code.ToUpperInvariant() switch
        {
            "PRIVACY_NOTICE" or "TERMS_OF_SERVICE" or "CONSENT_NOTICES" =>
                PrivacyReadinessCategoryGroup.NoticesAndConsent,
            "PRIVACY_MANUAL" or "PIA" => PrivacyReadinessCategoryGroup.Governance,
            "DATA_INVENTORY_ROPA" => PrivacyReadinessCategoryGroup.DataInventory,
            "RETENTION_DISPOSAL" => PrivacyReadinessCategoryGroup.RetentionAndDeletion,
            "DSAR_PROCEDURE" => PrivacyReadinessCategoryGroup.DataSubjectRequests,
            "SECURITY_ACCESS_CONTROL" => PrivacyReadinessCategoryGroup.SecurityAndAccess,
            "INCIDENT_BREACH_RESPONSE" => PrivacyReadinessCategoryGroup.IncidentResponse,
            "BACKUP_BCP_PRIVACY" => PrivacyReadinessCategoryGroup.BusinessContinuity,
            "VENDOR_PROCESSOR_REGISTER" => PrivacyReadinessCategoryGroup.VendorsAndProcessors,
            "DPO_APPOINTMENT" => PrivacyReadinessCategoryGroup.DpoRegulatoryReadiness,
            _ => category switch
            {
                ComplianceItemCategory.CustomerFacing => PrivacyReadinessCategoryGroup.NoticesAndConsent,
                ComplianceItemCategory.DataInventory => PrivacyReadinessCategoryGroup.DataInventory,
                ComplianceItemCategory.Retention => PrivacyReadinessCategoryGroup.RetentionAndDeletion,
                ComplianceItemCategory.IncidentBreach => PrivacyReadinessCategoryGroup.IncidentResponse,
                ComplianceItemCategory.VendorProcessor => PrivacyReadinessCategoryGroup.VendorsAndProcessors,
                ComplianceItemCategory.DpoNpc => PrivacyReadinessCategoryGroup.DpoRegulatoryReadiness,
                _ => PrivacyReadinessCategoryGroup.Other
            }
        };
    }

    public static PrivacyEvidenceDisplayKind ResolveEvidenceDisplayKind(
        ComplianceEvidenceKind kind,
        bool requirementRequiresDpoLegalVerification,
        string requirementCode)
    {
        if (requirementRequiresDpoLegalVerification
            || requirementCode.Contains("NPC", StringComparison.OrdinalIgnoreCase)
            || requirementCode.StartsWith("DPO_REGISTRATION", StringComparison.OrdinalIgnoreCase)
            || requirementCode.StartsWith("DPS_REGISTRATION", StringComparison.OrdinalIgnoreCase))
        {
            return kind is ComplianceEvidenceKind.Test or ComplianceEvidenceKind.Implementation
                or ComplianceEvidenceKind.SecurityControl
                ? PrivacyEvidenceDisplayKind.TechnicalEvidence
                : PrivacyEvidenceDisplayKind.RegulatoryEvidence;
        }

        return kind switch
        {
            ComplianceEvidenceKind.Test
                or ComplianceEvidenceKind.Implementation
                or ComplianceEvidenceKind.SecurityControl => PrivacyEvidenceDisplayKind.TechnicalEvidence,
            ComplianceEvidenceKind.PhaseDoc
                or ComplianceEvidenceKind.ArchitectureDoc
                or ComplianceEvidenceKind.Report => PrivacyEvidenceDisplayKind.OperationalEvidence,
            ComplianceEvidenceKind.Other => PrivacyEvidenceDisplayKind.ManualEvidence,
            _ => PrivacyEvidenceDisplayKind.ManualEvidence
        };
    }

    public static bool IsReadyStatus(ComplianceItemStatus status) =>
        status is ComplianceItemStatus.Approved or ComplianceItemStatus.ReadyForReview;

    public static bool IsActionNeededStatus(ComplianceItemStatus status) =>
        status is ComplianceItemStatus.NotStarted or ComplianceItemStatus.NeedsUpdate;

    public static bool CountsAsExternalLegalReview(ComplianceRequirement requirement) =>
        requirement.RequiresDpoLegalVerification
        && requirement.Status != ComplianceItemStatus.Approved;

    /// <summary>
    /// Derives overall operational readiness. Never returns a "Compliant" / NPC-certified state.
    /// </summary>
    public static PrivacyReadinessOverallStatus DeriveOverall(
        IReadOnlyList<ComplianceRequirement> requirements)
    {
        if (requirements.Count == 0)
        {
            return PrivacyReadinessOverallStatus.NotAssessed;
        }

        var required = requirements
            .Where(r => r.RequirementLevel == ComplianceRequirementLevel.Required)
            .ToArray();

        if (required.Length == 0)
        {
            return PrivacyReadinessOverallStatus.NotAssessed;
        }

        if (required.All(r => r.Status == ComplianceItemStatus.NotStarted))
        {
            return PrivacyReadinessOverallStatus.NotStarted;
        }

        if (required.Any(r => IsActionNeededStatus(r.Status)))
        {
            return PrivacyReadinessOverallStatus.ActionNeeded;
        }

        if (requirements.Any(CountsAsExternalLegalReview))
        {
            return PrivacyReadinessOverallStatus.ExternalLegalReviewRequired;
        }

        if (required.All(r => r.Status == ComplianceItemStatus.Approved))
        {
            return PrivacyReadinessOverallStatus.VerifiedInternally;
        }

        if (required.All(r => r.Status is ComplianceItemStatus.ReadyForReview or ComplianceItemStatus.Approved))
        {
            return PrivacyReadinessOverallStatus.ReadyForReview;
        }

        return PrivacyReadinessOverallStatus.InProgress;
    }

    public static string ResolveDetailRoute(PrivacyReadinessCategoryGroup group) =>
        group switch
        {
            PrivacyReadinessCategoryGroup.NoticesAndConsent => "/admin/privacy-compliance/documents",
            PrivacyReadinessCategoryGroup.Governance => "/admin/privacy-compliance/documents",
            PrivacyReadinessCategoryGroup.DataInventory => "/admin/privacy-compliance/data-inventory",
            PrivacyReadinessCategoryGroup.RetentionAndDeletion => "/admin/privacy-compliance/retention",
            PrivacyReadinessCategoryGroup.DataSubjectRequests => "/admin/privacy-compliance/documents",
            PrivacyReadinessCategoryGroup.SecurityAndAccess => "/admin/privacy-compliance/systems",
            PrivacyReadinessCategoryGroup.IncidentResponse => "/admin/privacy-compliance/incidents",
            PrivacyReadinessCategoryGroup.BusinessContinuity => "/admin/privacy-compliance/documents",
            PrivacyReadinessCategoryGroup.VendorsAndProcessors => "/admin/privacy-compliance/vendors",
            PrivacyReadinessCategoryGroup.DpoRegulatoryReadiness => "/admin/privacy-compliance/dpo-npc",
            PrivacyReadinessCategoryGroup.PrivacyImpact => "/admin/privacy-compliance/pias",
            _ => "/admin/privacy-compliance/documents"
        };
}
