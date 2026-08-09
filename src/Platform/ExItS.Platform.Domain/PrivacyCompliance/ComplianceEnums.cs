namespace ExItS.Platform.Domain.PrivacyCompliance;

public enum ComplianceItemCategory
{
    CustomerFacing = 0,
    Internal = 1,
    RegulatoryReadiness = 2,
    ProcessingSystem = 3,
    PrivacyImpactAssessment = 4,
    DataInventory = 5,
    Retention = 6,
    IncidentBreach = 7,
    VendorProcessor = 8,
    DpoNpc = 9,
    Evidence = 10
}

public enum ComplianceRequirementLevel
{
    Required = 0,
    Conditional = 1,
    Optional = 2
}

public enum ComplianceItemStatus
{
    NotStarted = 0,
    InProgress = 1,
    ReadyForReview = 2,
    Approved = 3,
    NeedsUpdate = 4
}

public enum ProcessingSystemPiaStatus
{
    NotStarted = 0,
    InProgress = 1,
    ReadyForReview = 2,
    Approved = 3,
    NeedsUpdate = 4,
    NotApplicable = 5
}

public enum ComplianceEvidenceKind
{
    PhaseDoc = 0,
    ArchitectureDoc = 1,
    Report = 2,
    Test = 3,
    SecurityControl = 4,
    Implementation = 5,
    Other = 6
}
