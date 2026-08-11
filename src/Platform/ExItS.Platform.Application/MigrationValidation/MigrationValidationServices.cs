using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.MigrationValidation;

public interface IMigrationPreflightValidator
{
    PreflightValidationResult Validate(MigrationSimulationInput input);
}

public interface IMigrationCompatibilityReporter
{
    CompatibilityReport Build(MigrationSimulationInput input, IReadOnlyList<MigrationFinding> findings, RollbackReadinessStatus rollback);
}

public interface IMigrationSimulationService
{
    MigrationSimulationResult Simulate(MigrationSimulationInput input, RollbackEvidence? rollbackEvidence = null);
}

public interface IRollbackReadinessValidator
{
    RollbackReadinessResult Validate(
        RollbackEvidence? evidence,
        bool requireEvidence = false,
        bool requireBackupReference = false);
}

public sealed partial class MigrationPreflightValidator : IMigrationPreflightValidator
{
    private static readonly Regex EmailLike = CreateEmailLike();
    private static readonly HashSet<string> ForbiddenMetadataKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "PasswordHash", "RefreshToken", "MfaSecret", "Otp", "Cookie",
        "Patient", "MedicalNote", "Diagnosis", "Prescription"
    };

    private static readonly HashSet<string> ForbiddenRoleTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "Doctor", "Nurse", "Patient", "ClinicAdmin", "ClinicAdministrator", "Clinical"
    };

    public PreflightValidationResult Validate(MigrationSimulationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var findings = new List<MigrationFinding>();

        if (input.ValidationTimestampUtc.Offset != TimeSpan.Zero)
        {
            findings.Add(Finding(
                MigrationFindingCodes.NonUtcTimestamp,
                ValidationFindingSeverity.Blocked,
                "Validation timestamp must be UTC."));
        }

        if (input.ContractVersion.Major > 1)
        {
            findings.Add(Finding(
                MigrationFindingCodes.UnsupportedContractVersion,
                ValidationFindingSeverity.Blocked,
                $"Unsupported contract major {input.ContractVersion.Major}."));
        }

        if (input.PriorSourceVersion is int prior
            && input.IncomingSourceVersion is int incoming
            && incoming < prior)
        {
            findings.Add(Finding(
                MigrationFindingCodes.SourceVersionRegression,
                ValidationFindingSeverity.Conflict,
                "Incoming source version regresses prior checkpoint."));
        }

        if (input.OpaqueMetadataProbe is not null)
        {
            foreach (var key in input.OpaqueMetadataProbe.Keys)
            {
                if (ForbiddenMetadataKeys.Contains(key)
                    || ForbiddenMetadataKeys.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase)))
                {
                    findings.Add(Finding(
                        MigrationFindingCodes.SensitiveFieldDetected,
                        ValidationFindingSeverity.Blocked,
                        $"Sensitive metadata key '{key}' is not allowed.",
                        key));
                }
            }
        }

        ValidateIdentities(input.IdentityMappings, findings);
        ValidateOrganizations(input.OrganizationMappings, findings);
        ValidateMemberships(input, findings);
        ValidateEntitlementFeatures(input.EntitlementFeatureCodes, findings);

        return new PreflightValidationResult(Classify(findings), findings);
    }

    private static void ValidateIdentities(
        IReadOnlyList<IdentityMappingCandidate> identities,
        List<MigrationFinding> findings)
    {
        var byPlatform = new Dictionary<Guid, IdentityMappingCandidate>();
        var byExternal = new Dictionary<string, IdentityMappingCandidate>(StringComparer.Ordinal);
        var byNormalized = new Dictionary<string, List<IdentityMappingCandidate>>(StringComparer.Ordinal);

        foreach (var candidate in identities)
        {
            if (string.IsNullOrWhiteSpace(candidate.ExternalUserId))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.EmptyIdentifier,
                    ValidationFindingSeverity.Blocked,
                    "External user ID is required."));
                continue;
            }

            if (!EmailLike.IsMatch(candidate.NormalizedLoginIdentifier)
                && candidate.NormalizedLoginIdentifier.Length < 3)
            {
                findings.Add(Finding(
                    MigrationFindingCodes.InvalidNormalizedIdentifier,
                    ValidationFindingSeverity.Blocked,
                    "Normalized login identifier is invalid.",
                    candidate.ExternalUserId));
            }

            if (!byPlatform.TryAdd(candidate.PlatformUserId.Value, candidate))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.PlatformUserAlreadyMapped,
                    ValidationFindingSeverity.Conflict,
                    "Duplicate PlatformUserId mapping.",
                    candidate.PlatformUserId.ToString()));
            }

            if (!byExternal.TryAdd(candidate.ExternalUserId, candidate))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.ExternalUserAlreadyMapped,
                    ValidationFindingSeverity.Conflict,
                    "Duplicate external user mapping.",
                    candidate.ExternalUserId));
            }

            if (!byNormalized.TryGetValue(candidate.NormalizedLoginIdentifier, out var list))
            {
                list = new List<IdentityMappingCandidate>();
                byNormalized[candidate.NormalizedLoginIdentifier] = list;
            }

            list.Add(candidate);

            if (candidate.MatchClassification is IdentityMatchClassification.ManualReviewRequired
                or IdentityMatchClassification.NoMatch)
            {
                findings.Add(Finding(
                    MigrationFindingCodes.AmbiguousIdentityMatch,
                    ValidationFindingSeverity.ManualReviewRequired,
                    "Identity match requires manual review.",
                    candidate.ExternalUserId));
            }

            if (candidate.MatchClassification == IdentityMatchClassification.Conflict)
            {
                findings.Add(Finding(
                    MigrationFindingCodes.AmbiguousIdentityMatch,
                    ValidationFindingSeverity.Conflict,
                    "Identity match classified as conflict.",
                    candidate.ExternalUserId));
            }

            if (candidate.MatchClassification == IdentityMatchClassification.ExactNormalizedIdentifier
                && candidate.Status == MappingCandidateStatus.Accepted)
            {
                findings.Add(Finding(
                    MigrationFindingCodes.AmbiguousIdentityMatch,
                    ValidationFindingSeverity.Warning,
                    "Exact normalized identifier alone is not always safe; confirm before cutover.",
                    candidate.ExternalUserId));
            }
        }

        foreach (var (normalized, list) in byNormalized)
        {
            if (list.Count > 1)
            {
                findings.Add(Finding(
                    MigrationFindingCodes.DuplicateNormalizedIdentifier,
                    ValidationFindingSeverity.Conflict,
                    "Duplicate normalized identifier across identity candidates.",
                    normalized));
            }
        }
    }

    private static void ValidateOrganizations(
        IReadOnlyList<OrganizationMappingCandidate> organizations,
        List<MigrationFinding> findings)
    {
        var seenPairs = new HashSet<string>(StringComparer.Ordinal);
        var externalToPlatform = new Dictionary<string, PlatformOrganizationId>(StringComparer.Ordinal);

        foreach (var candidate in organizations)
        {
            if (string.IsNullOrWhiteSpace(candidate.ExternalOrganizationId))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.EmptyIdentifier,
                    ValidationFindingSeverity.Blocked,
                    "External organization ID is required."));
                continue;
            }

            var pair = $"{candidate.PlatformOrganizationId}:{candidate.ExternalOrganizationId}";
            if (!seenPairs.Add(pair))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.DuplicateOrganizationMapping,
                    ValidationFindingSeverity.Conflict,
                    "Duplicate organization mapping pair.",
                    pair));
            }

            if (externalToPlatform.TryGetValue(candidate.ExternalOrganizationId, out var existing)
                && existing != candidate.PlatformOrganizationId)
            {
                findings.Add(Finding(
                    MigrationFindingCodes.ExternalOrganizationMappedElsewhere,
                    ValidationFindingSeverity.Conflict,
                    "External organization is mapped to multiple Platform organizations.",
                    candidate.ExternalOrganizationId));
            }
            else
            {
                externalToPlatform[candidate.ExternalOrganizationId] = candidate.PlatformOrganizationId;
            }

            if (candidate.MappingClassification is IdentityMatchClassification.ManualReviewRequired
                or IdentityMatchClassification.Conflict
                or IdentityMatchClassification.NoMatch)
            {
                findings.Add(Finding(
                    MigrationFindingCodes.AmbiguousIdentityMatch,
                    candidate.MappingClassification == IdentityMatchClassification.Conflict
                        ? ValidationFindingSeverity.Conflict
                        : ValidationFindingSeverity.ManualReviewRequired,
                    "Organization mapping requires review or is in conflict.",
                    candidate.ExternalOrganizationId));
            }
        }

        // Duplicate same external appearing twice with same platform is already pair-detected;
        // also flag when same external appears more than once even if identical (already covered).
        _ = findings;
    }

    private static void ValidateMemberships(
        MigrationSimulationInput input,
        List<MigrationFinding> findings)
    {
        var knownUsers = input.IdentityMappings.Select(i => i.PlatformUserId.Value).ToHashSet();
        var knownOrgs = input.OrganizationMappings.Select(o => o.PlatformOrganizationId.Value).ToHashSet();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in input.MembershipMappings)
        {
            var key = $"{candidate.PlatformUserId}:{candidate.PlatformOrganizationId}:{candidate.ExternalMembershipReference}";
            if (!seen.Add(key))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.DuplicateMembershipMapping,
                    ValidationFindingSeverity.Conflict,
                    "Duplicate membership mapping.",
                    key));
            }

            if (!knownUsers.Contains(candidate.PlatformUserId.Value))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.MissingPlatformUser,
                    ValidationFindingSeverity.Blocked,
                    "Membership references a Platform user not present in identity candidates.",
                    candidate.PlatformUserId.ToString()));
            }

            if (!knownOrgs.Contains(candidate.PlatformOrganizationId.Value))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.MissingPlatformOrganization,
                    ValidationFindingSeverity.Blocked,
                    "Membership references a Platform organization not present in organization candidates.",
                    candidate.PlatformOrganizationId.ToString()));
            }

            if (candidate.MembershipStatus != MembershipStatus.Active)
            {
                findings.Add(Finding(
                    MigrationFindingCodes.SuspendedMembershipNotActive,
                    ValidationFindingSeverity.Warning,
                    "Non-active Platform membership must not be treated as active product access.",
                    key));
            }

            var roleName = candidate.PlatformOrganizationRole.ToString();
            if (ForbiddenRoleTokens.Any(t => roleName.Contains(t, StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.ClinicalRoleProhibited,
                    ValidationFindingSeverity.Blocked,
                    "Clinical or product-local roles cannot be assigned from Platform membership.",
                    roleName));
            }

            // Platform roles are only Owner/Admin/Member — any other enum cast fails at construction.
            if (!Enum.IsDefined(candidate.PlatformOrganizationRole))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.ClinicalRoleProhibited,
                    ValidationFindingSeverity.Blocked,
                    "Undefined organization role.",
                    roleName));
            }
        }
    }

    private static void ValidateEntitlementFeatures(
        IReadOnlyList<string> featureCodes,
        List<MigrationFinding> findings)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in featureCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.EntitlementSnapshotInvalid,
                    ValidationFindingSeverity.Blocked,
                    "Entitlement feature code cannot be blank."));
                continue;
            }

            if (!seen.Add(code))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.EntitlementSnapshotInvalid,
                    ValidationFindingSeverity.Conflict,
                    "Duplicate entitlement feature code in snapshot.",
                    code));
            }

            if (ForbiddenRoleTokens.Any(t => code.Contains(t, StringComparison.OrdinalIgnoreCase))
                || ForbiddenMetadataKeys.Any(t => code.Contains(t, StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add(Finding(
                    MigrationFindingCodes.EntitlementSnapshotInvalid,
                    ValidationFindingSeverity.Blocked,
                    "Feature code resembles clinical or sensitive content.",
                    code));
            }
        }
    }

    internal static CompatibilityStatus Classify(IReadOnlyList<MigrationFinding> findings)
    {
        if (findings.Any(f => f.Severity is ValidationFindingSeverity.Blocked or ValidationFindingSeverity.Conflict))
        {
            return CompatibilityStatus.Failed;
        }

        if (findings.Any(f => f.Severity == ValidationFindingSeverity.ManualReviewRequired))
        {
            return CompatibilityStatus.ManualReviewRequired;
        }

        if (findings.Any(f => f.Severity == ValidationFindingSeverity.Warning))
        {
            return CompatibilityStatus.PassedWithWarnings;
        }

        return CompatibilityStatus.Passed;
    }

    private static MigrationFinding Finding(
        string code,
        ValidationFindingSeverity severity,
        string message,
        string? subject = null) =>
        new(code, severity, message, subject);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateEmailLike();
}

public sealed class MigrationCompatibilityReporter : IMigrationCompatibilityReporter
{
    public CompatibilityReport Build(
        MigrationSimulationInput input,
        IReadOnlyList<MigrationFinding> findings,
        RollbackReadinessStatus rollback)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(findings);

        var status = MigrationPreflightValidator.Classify(findings);
        var hasBlock = findings.Any(f =>
            f.Severity is ValidationFindingSeverity.Blocked or ValidationFindingSeverity.Conflict);

        if (hasBlock && status == CompatibilityStatus.Passed)
        {
            status = CompatibilityStatus.Failed;
        }

        bool HasCode(string code) => findings.Any(f => f.Code == code);

        return new CompatibilityReport(
            status,
            contractSchemaSupported: !HasCode(MigrationFindingCodes.UnsupportedContractVersion),
            sourceVersionContinuous: !HasCode(MigrationFindingCodes.SourceVersionRegression),
            identifierComplete: !HasCode(MigrationFindingCodes.EmptyIdentifier)
                && !HasCode(MigrationFindingCodes.MissingPlatformUser)
                && !HasCode(MigrationFindingCodes.MissingPlatformOrganization),
            organizationMappingComplete: !HasCode(MigrationFindingCodes.ExternalOrganizationMappedElsewhere)
                && !HasCode(MigrationFindingCodes.DuplicateOrganizationMapping)
                && !HasCode(MigrationFindingCodes.DuplicateExternalOrganization),
            membershipMappingComplete: !HasCode(MigrationFindingCodes.MembershipConflict)
                && !HasCode(MigrationFindingCodes.DuplicateMembershipMapping),
            productAccessCompatible: !HasCode(MigrationFindingCodes.ProductCodeMismatch),
            subscriptionCompatible: !HasCode(MigrationFindingCodes.SourceVersionRegression),
            entitlementCompatible: !HasCode(MigrationFindingCodes.EntitlementSnapshotInvalid),
            securityShapeCompatible: !HasCode(MigrationFindingCodes.SensitiveFieldDetected)
                && !HasCode(MigrationFindingCodes.ClinicalRoleProhibited),
            rollbackReadiness: rollback,
            findings: findings);
    }
}

public sealed class RollbackReadinessValidator : IRollbackReadinessValidator
{
    public RollbackReadinessResult Validate(
        RollbackEvidence? evidence,
        bool requireEvidence = false,
        bool requireBackupReference = false)
    {
        var findings = new List<MigrationFinding>();

        if (evidence is null)
        {
            if (!requireEvidence)
            {
                return new RollbackReadinessResult(RollbackReadinessStatus.NotApplicable, findings);
            }

            findings.Add(new MigrationFinding(
                MigrationFindingCodes.RollbackDataMissing,
                ValidationFindingSeverity.Blocked,
                "Rollback evidence is missing."));
            return new RollbackReadinessResult(RollbackReadinessStatus.NotReady, findings);
        }

        if (string.IsNullOrWhiteSpace(evidence.ReverseMappingReference))
        {
            findings.Add(new MigrationFinding(
                MigrationFindingCodes.RollbackDataMissing,
                ValidationFindingSeverity.Blocked,
                "Reverse-mapping reference is required."));
        }

        if (evidence.MigrationBatchId == Guid.Empty || evidence.CorrelationId == Guid.Empty)
        {
            findings.Add(new MigrationFinding(
                MigrationFindingCodes.RollbackDataMissing,
                ValidationFindingSeverity.Blocked,
                "Batch and correlation IDs are required for rollback readiness."));
        }

        if (requireBackupReference && string.IsNullOrWhiteSpace(evidence.BackupVerificationReference))
        {
            findings.Add(new MigrationFinding(
                MigrationFindingCodes.RollbackDataMissing,
                ValidationFindingSeverity.Warning,
                "Backup verification reference is missing; cutover must not proceed without restore rehearsal."));
        }

        if (findings.Any(f => f.Severity == ValidationFindingSeverity.Blocked))
        {
            return new RollbackReadinessResult(RollbackReadinessStatus.NotReady, findings);
        }

        return new RollbackReadinessResult(RollbackReadinessStatus.Ready, findings);
    }
}

public sealed class MigrationSimulationService : IMigrationSimulationService
{
    private readonly IMigrationPreflightValidator _preflight;
    private readonly IMigrationCompatibilityReporter _compatibility;
    private readonly IRollbackReadinessValidator _rollback;

    public MigrationSimulationService(
        IMigrationPreflightValidator? preflight = null,
        IMigrationCompatibilityReporter? compatibility = null,
        IRollbackReadinessValidator? rollback = null)
    {
        _preflight = preflight ?? new MigrationPreflightValidator();
        _compatibility = compatibility ?? new MigrationCompatibilityReporter();
        _rollback = rollback ?? new RollbackReadinessValidator();
    }

    public MigrationSimulationResult Simulate(MigrationSimulationInput input, RollbackEvidence? rollbackEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Defensive copy counts from immutable views — never mutate caller collections.
        var identityCount = input.IdentityMappings.Count;
        var orgCount = input.OrganizationMappings.Count;
        var membershipCount = input.MembershipMappings.Count;
        var proposed = identityCount + orgCount + membershipCount;

        var preflight = _preflight.Validate(input);
        var findings = preflight.Findings.ToList();

        var rollback = rollbackEvidence is null
            ? _rollback.Validate(null)
            : _rollback.Validate(rollbackEvidence, requireEvidence: true, requireBackupReference: false);
        findings.AddRange(rollback.Findings);

        var accepted = CountByStatus(input, MappingCandidateStatus.Accepted);
        var warnings = findings.Count(f => f.Severity == ValidationFindingSeverity.Warning);
        var conflicts = findings.Count(f => f.Severity == ValidationFindingSeverity.Conflict);
        var blocked = findings.Count(f => f.Severity == ValidationFindingSeverity.Blocked);
        var manual = findings.Count(f => f.Severity == ValidationFindingSeverity.ManualReviewRequired);

        // Partial success must not hide blocks: accepted count is candidate-declared Accepted only when no blocks.
        if (blocked > 0 || conflicts > 0)
        {
            accepted = 0;
        }

        var compatibility = _compatibility.Build(input, findings, rollback.Status);

        return new MigrationSimulationResult(
            findings,
            proposed,
            accepted,
            warnings,
            conflicts,
            blocked,
            manual,
            compatibility,
            rollback.Status);
    }

    private static int CountByStatus(MigrationSimulationInput input, MappingCandidateStatus status)
    {
        var count = input.IdentityMappings.Count(i => i.Status == status);
        count += input.OrganizationMappings.Count(o => o.Status == status);
        count += input.MembershipMappings.Count(m => m.Status == status);
        return count;
    }
}
