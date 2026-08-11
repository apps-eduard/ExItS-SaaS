using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.MigrationValidation;

public enum MigrationType
{
    IdentityMapping = 1,
    OrganizationMapping = 2,
    MembershipMapping = 3,
    ProductAccessMapping = 4,
    EntitlementProjection = 5
}

/// <summary>Validation-batch lifecycle only. Does not imply a production migration completed.</summary>
public enum MigrationBatchStatus
{
    Created = 1,
    Validating = 2,
    Validated = 3,
    Failed = 4,
    Cancelled = 5
}

public enum IdentityMatchClassification
{
    ExplicitApprovedMapping = 1,
    ExactNormalizedIdentifier = 2,
    ManualReviewRequired = 3,
    Conflict = 4,
    NoMatch = 5
}

public enum MappingCandidateStatus
{
    Proposed = 1,
    Accepted = 2,
    Warning = 3,
    Conflict = 4,
    Blocked = 5,
    ManualReviewRequired = 6
}

public enum ValidationFindingSeverity
{
    Valid = 1,
    Warning = 2,
    Conflict = 3,
    Blocked = 4,
    ManualReviewRequired = 5,
    NotApplicable = 6
}

public enum CompatibilityStatus
{
    Passed = 1,
    PassedWithWarnings = 2,
    Failed = 3,
    ManualReviewRequired = 4
}

public enum RollbackReadinessStatus
{
    Ready = 1,
    NotReady = 2,
    NotApplicable = 3
}

public sealed class MigrationFinding
{
    public string Code { get; }
    public ValidationFindingSeverity Severity { get; }
    public string Message { get; }
    public string? SubjectKey { get; }

    public MigrationFinding(
        string code,
        ValidationFindingSeverity severity,
        string message,
        string? subjectKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Severity = severity;
        Message = message;
        SubjectKey = subjectKey;
    }
}

/// <summary>Immutable validation-only migration batch. Status never means production cutover completed.</summary>
public sealed class MigrationBatch
{
    public Guid MigrationBatchId { get; }
    public MigrationType MigrationType { get; }
    public ProductCode ProductCode { get; }
    public DateTimeOffset StartedAtUtc { get; }
    public DateTimeOffset ValidatedAtUtc { get; }
    public Guid CorrelationId { get; }
    public string SourceSystem { get; }
    public string TargetSystem { get; }
    public MigrationBatchStatus Status { get; }
    public int RecordCount { get; }
    public string ValidationSummary { get; }

    public MigrationBatch(
        Guid migrationBatchId,
        MigrationType migrationType,
        ProductCode productCode,
        DateTimeOffset startedAtUtc,
        DateTimeOffset validatedAtUtc,
        Guid correlationId,
        string sourceSystem,
        string targetSystem,
        MigrationBatchStatus status,
        int recordCount,
        string validationSummary)
    {
        ArgumentNullException.ThrowIfNull(productCode);

        if (migrationBatchId == Guid.Empty)
        {
            throw new ArgumentException("Migration batch ID cannot be empty.", nameof(migrationBatchId));
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation ID cannot be empty.", nameof(correlationId));
        }

        if (startedAtUtc.Offset != TimeSpan.Zero || validatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Migration batch timestamps must be UTC.");
        }

        if (recordCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(recordCount));
        }

        if (string.IsNullOrWhiteSpace(sourceSystem) || string.IsNullOrWhiteSpace(targetSystem))
        {
            throw new ArgumentException("Source and target systems are required.");
        }

        if (string.IsNullOrWhiteSpace(validationSummary))
        {
            throw new ArgumentException("Validation summary is required.", nameof(validationSummary));
        }

        MigrationBatchId = migrationBatchId;
        MigrationType = migrationType;
        ProductCode = productCode;
        StartedAtUtc = startedAtUtc;
        ValidatedAtUtc = validatedAtUtc;
        CorrelationId = correlationId;
        SourceSystem = sourceSystem.Trim();
        TargetSystem = targetSystem.Trim();
        Status = status;
        RecordCount = recordCount;
        ValidationSummary = validationSummary.Trim();
    }
}

public sealed class IdentityMappingCandidate
{
    public string ExternalUserId { get; }
    public PlatformUserId PlatformUserId { get; }
    public string NormalizedLoginIdentifier { get; }
    public string MatchReason { get; }
    public IdentityMatchClassification MatchClassification { get; }
    public MappingCandidateStatus Status { get; }
    public Guid MigrationBatchId { get; }
    public Guid CorrelationId { get; }

    public IdentityMappingCandidate(
        string externalUserId,
        PlatformUserId platformUserId,
        string normalizedLoginIdentifier,
        string matchReason,
        IdentityMatchClassification matchClassification,
        MappingCandidateStatus status,
        Guid migrationBatchId,
        Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(platformUserId);

        if (string.IsNullOrWhiteSpace(externalUserId))
        {
            throw new ArgumentException("External user ID cannot be empty.", nameof(externalUserId));
        }

        if (string.IsNullOrWhiteSpace(normalizedLoginIdentifier))
        {
            throw new ArgumentException("Normalized login identifier cannot be empty.", nameof(normalizedLoginIdentifier));
        }

        if (string.IsNullOrWhiteSpace(matchReason))
        {
            throw new ArgumentException("Match reason is required.", nameof(matchReason));
        }

        if (migrationBatchId == Guid.Empty || correlationId == Guid.Empty)
        {
            throw new ArgumentException("Batch and correlation IDs are required.");
        }

        ExternalUserId = externalUserId.Trim();
        PlatformUserId = platformUserId;
        NormalizedLoginIdentifier = normalizedLoginIdentifier.Trim().ToLowerInvariant();
        MatchReason = matchReason.Trim();
        MatchClassification = matchClassification;
        Status = status;
        MigrationBatchId = migrationBatchId;
        CorrelationId = correlationId;
    }
}

public sealed class OrganizationMappingCandidate
{
    public PlatformOrganizationId PlatformOrganizationId { get; }
    public string ExternalOrganizationId { get; }
    public IdentityMatchClassification MappingClassification { get; }
    public MappingCandidateStatus Status { get; }
    public Guid MigrationBatchId { get; }
    public string ValidationReason { get; }

    public OrganizationMappingCandidate(
        PlatformOrganizationId platformOrganizationId,
        string externalOrganizationId,
        IdentityMatchClassification mappingClassification,
        MappingCandidateStatus status,
        Guid migrationBatchId,
        string validationReason)
    {
        ArgumentNullException.ThrowIfNull(platformOrganizationId);

        if (string.IsNullOrWhiteSpace(externalOrganizationId))
        {
            throw new ArgumentException("External organization ID cannot be empty.", nameof(externalOrganizationId));
        }

        if (migrationBatchId == Guid.Empty)
        {
            throw new ArgumentException("Migration batch ID cannot be empty.", nameof(migrationBatchId));
        }

        if (string.IsNullOrWhiteSpace(validationReason))
        {
            throw new ArgumentException("Validation reason is required.", nameof(validationReason));
        }

        PlatformOrganizationId = platformOrganizationId;
        ExternalOrganizationId = externalOrganizationId.Trim();
        MappingClassification = mappingClassification;
        Status = status;
        MigrationBatchId = migrationBatchId;
        ValidationReason = validationReason.Trim();
    }
}

public sealed class MembershipMappingCandidate
{
    public PlatformUserId PlatformUserId { get; }
    public PlatformOrganizationId PlatformOrganizationId { get; }
    public string ExternalMembershipReference { get; }
    public OrganizationRole PlatformOrganizationRole { get; }
    public MembershipStatus MembershipStatus { get; }
    public MappingCandidateStatus Status { get; }
    public Guid MigrationBatchId { get; }

    public MembershipMappingCandidate(
        PlatformUserId platformUserId,
        PlatformOrganizationId platformOrganizationId,
        string externalMembershipReference,
        OrganizationRole platformOrganizationRole,
        MembershipStatus membershipStatus,
        MappingCandidateStatus status,
        Guid migrationBatchId)
    {
        ArgumentNullException.ThrowIfNull(platformUserId);
        ArgumentNullException.ThrowIfNull(platformOrganizationId);

        if (string.IsNullOrWhiteSpace(externalMembershipReference))
        {
            throw new ArgumentException("External membership reference cannot be empty.", nameof(externalMembershipReference));
        }

        if (!Enum.IsDefined(platformOrganizationRole))
        {
            throw new ArgumentOutOfRangeException(nameof(platformOrganizationRole));
        }

        if (!Enum.IsDefined(membershipStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(membershipStatus));
        }

        if (migrationBatchId == Guid.Empty)
        {
            throw new ArgumentException("Migration batch ID cannot be empty.", nameof(migrationBatchId));
        }

        PlatformUserId = platformUserId;
        PlatformOrganizationId = platformOrganizationId;
        ExternalMembershipReference = externalMembershipReference.Trim();
        PlatformOrganizationRole = platformOrganizationRole;
        MembershipStatus = membershipStatus;
        Status = status;
        MigrationBatchId = migrationBatchId;
    }
}

public sealed class RollbackEvidence
{
    public string SourceIdentifier { get; }
    public string TargetIdentifier { get; }
    public Guid MigrationBatchId { get; }
    public Guid CorrelationId { get; }
    public string BeforeStateReference { get; }
    public string AfterStateReference { get; }
    public string ReverseMappingReference { get; }
    public string? BackupVerificationReference { get; }
    public DateTimeOffset ValidatedAtUtc { get; }
    public string ApprovalStatusPlaceholder { get; }

    public RollbackEvidence(
        string sourceIdentifier,
        string targetIdentifier,
        Guid migrationBatchId,
        Guid correlationId,
        string beforeStateReference,
        string afterStateReference,
        string reverseMappingReference,
        DateTimeOffset validatedAtUtc,
        string approvalStatusPlaceholder,
        string? backupVerificationReference = null)
    {
        if (string.IsNullOrWhiteSpace(sourceIdentifier) || string.IsNullOrWhiteSpace(targetIdentifier))
        {
            throw new ArgumentException("Source and target identifiers are required.");
        }

        if (migrationBatchId == Guid.Empty || correlationId == Guid.Empty)
        {
            throw new ArgumentException("Batch and correlation IDs are required.");
        }

        if (string.IsNullOrWhiteSpace(beforeStateReference)
            || string.IsNullOrWhiteSpace(afterStateReference)
            || string.IsNullOrWhiteSpace(reverseMappingReference))
        {
            throw new ArgumentException("Before/after/reverse mapping references are required.");
        }

        if (validatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("ValidatedAtUtc must be UTC.");
        }

        if (string.IsNullOrWhiteSpace(approvalStatusPlaceholder))
        {
            throw new ArgumentException("Approval status placeholder is required.");
        }

        SourceIdentifier = sourceIdentifier.Trim();
        TargetIdentifier = targetIdentifier.Trim();
        MigrationBatchId = migrationBatchId;
        CorrelationId = correlationId;
        BeforeStateReference = beforeStateReference.Trim();
        AfterStateReference = afterStateReference.Trim();
        ReverseMappingReference = reverseMappingReference.Trim();
        BackupVerificationReference = string.IsNullOrWhiteSpace(backupVerificationReference)
            ? null
            : backupVerificationReference.Trim();
        ValidatedAtUtc = validatedAtUtc;
        ApprovalStatusPlaceholder = approvalStatusPlaceholder.Trim();
    }
}

public sealed class CompatibilityReport
{
    public CompatibilityStatus Status { get; }
    public bool ContractSchemaSupported { get; }
    public bool SourceVersionContinuous { get; }
    public bool IdentifierComplete { get; }
    public bool OrganizationMappingComplete { get; }
    public bool MembershipMappingComplete { get; }
    public bool ProductAccessCompatible { get; }
    public bool SubscriptionCompatible { get; }
    public bool EntitlementCompatible { get; }
    public bool SecurityShapeCompatible { get; }
    public RollbackReadinessStatus RollbackReadiness { get; }
    public IReadOnlyList<MigrationFinding> Findings { get; }

    public CompatibilityReport(
        CompatibilityStatus status,
        bool contractSchemaSupported,
        bool sourceVersionContinuous,
        bool identifierComplete,
        bool organizationMappingComplete,
        bool membershipMappingComplete,
        bool productAccessCompatible,
        bool subscriptionCompatible,
        bool entitlementCompatible,
        bool securityShapeCompatible,
        RollbackReadinessStatus rollbackReadiness,
        IReadOnlyList<MigrationFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        Status = status;
        ContractSchemaSupported = contractSchemaSupported;
        SourceVersionContinuous = sourceVersionContinuous;
        IdentifierComplete = identifierComplete;
        OrganizationMappingComplete = organizationMappingComplete;
        MembershipMappingComplete = membershipMappingComplete;
        ProductAccessCompatible = productAccessCompatible;
        SubscriptionCompatible = subscriptionCompatible;
        EntitlementCompatible = entitlementCompatible;
        SecurityShapeCompatible = securityShapeCompatible;
        RollbackReadiness = rollbackReadiness;
        Findings = findings;
    }
}

public sealed class MigrationSimulationInput
{
    public ProductCode ProductCode { get; }
    public ContractVersionSupported ContractVersion { get; }
    public int? PriorSourceVersion { get; }
    public int? IncomingSourceVersion { get; }
    public DateTimeOffset ValidationTimestampUtc { get; }
    public IReadOnlyList<IdentityMappingCandidate> IdentityMappings { get; }
    public IReadOnlyList<OrganizationMappingCandidate> OrganizationMappings { get; }
    public IReadOnlyList<MembershipMappingCandidate> MembershipMappings { get; }
    public IReadOnlyList<string> EntitlementFeatureCodes { get; }
    public IReadOnlyDictionary<string, string>? OpaqueMetadataProbe { get; }

    public MigrationSimulationInput(
        ProductCode productCode,
        ContractVersionSupported contractVersion,
        DateTimeOffset validationTimestampUtc,
        IReadOnlyList<IdentityMappingCandidate> identityMappings,
        IReadOnlyList<OrganizationMappingCandidate> organizationMappings,
        IReadOnlyList<MembershipMappingCandidate> membershipMappings,
        IReadOnlyList<string>? entitlementFeatureCodes = null,
        int? priorSourceVersion = null,
        int? incomingSourceVersion = null,
        IReadOnlyDictionary<string, string>? opaqueMetadataProbe = null)
    {
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(identityMappings);
        ArgumentNullException.ThrowIfNull(organizationMappings);
        ArgumentNullException.ThrowIfNull(membershipMappings);

        ProductCode = productCode;
        ContractVersion = contractVersion;
        PriorSourceVersion = priorSourceVersion;
        IncomingSourceVersion = incomingSourceVersion;
        ValidationTimestampUtc = validationTimestampUtc;
        IdentityMappings = identityMappings;
        OrganizationMappings = organizationMappings;
        MembershipMappings = membershipMappings;
        EntitlementFeatureCodes = entitlementFeatureCodes ?? Array.Empty<string>();
        OpaqueMetadataProbe = opaqueMetadataProbe;
    }
}

/// <summary>Supported contract major for migration dry-run validation.</summary>
public readonly struct ContractVersionSupported : IEquatable<ContractVersionSupported>
{
    public int Major { get; }
    public int Minor { get; }

    public ContractVersionSupported(int major, int minor = 0)
    {
        if (major < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(major));
        }

        if (minor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minor));
        }

        Major = major;
        Minor = minor;
    }

    public static ContractVersionSupported V1 => new(1, 0);

    public bool Equals(ContractVersionSupported other) => Major == other.Major && Minor == other.Minor;

    public override bool Equals(object? obj) => obj is ContractVersionSupported other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Major, Minor);
}

public sealed class MigrationSimulationResult
{
    public IReadOnlyList<MigrationFinding> Findings { get; }
    public int ProposedMappingCount { get; }
    public int AcceptedCandidateCount { get; }
    public int WarningCount { get; }
    public int ConflictCount { get; }
    public int BlockedCount { get; }
    public int ManualReviewCount { get; }
    public CompatibilityReport Compatibility { get; }
    public RollbackReadinessStatus RollbackReadiness { get; }

    public MigrationSimulationResult(
        IReadOnlyList<MigrationFinding> findings,
        int proposedMappingCount,
        int acceptedCandidateCount,
        int warningCount,
        int conflictCount,
        int blockedCount,
        int manualReviewCount,
        CompatibilityReport compatibility,
        RollbackReadinessStatus rollbackReadiness)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(compatibility);
        Findings = findings;
        ProposedMappingCount = proposedMappingCount;
        AcceptedCandidateCount = acceptedCandidateCount;
        WarningCount = warningCount;
        ConflictCount = conflictCount;
        BlockedCount = blockedCount;
        ManualReviewCount = manualReviewCount;
        Compatibility = compatibility;
        RollbackReadiness = rollbackReadiness;
    }
}

public sealed class PreflightValidationResult
{
    public CompatibilityStatus OverallStatus { get; }
    public IReadOnlyList<MigrationFinding> Findings { get; }

    public PreflightValidationResult(CompatibilityStatus overallStatus, IReadOnlyList<MigrationFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        OverallStatus = overallStatus;
        Findings = findings;
    }

    public bool HasBlockingFindings =>
        Findings.Any(f =>
            f.Severity is ValidationFindingSeverity.Conflict
                or ValidationFindingSeverity.Blocked);
}

public sealed class RollbackReadinessResult
{
    public RollbackReadinessStatus Status { get; }
    public IReadOnlyList<MigrationFinding> Findings { get; }

    public RollbackReadinessResult(RollbackReadinessStatus status, IReadOnlyList<MigrationFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        Status = status;
        Findings = findings;
    }
}
