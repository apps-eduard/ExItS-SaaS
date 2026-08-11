using ExItS.Platform.Application.MigrationValidation;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.MigrationValidation;

public sealed class MigrationValidationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
    private const string OtherProductCode = "other-product";
    private static readonly ProductCode OtherProduct = ProductCode.Create(OtherProductCode);

    private static Guid BatchId => Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static Guid Correlation => Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void Identity_explicit_mapping_is_valid()
    {
        var user = PlatformUserId.New();
        var candidate = new IdentityMappingCandidate(
            "hc-user-1",
            user,
            "ada@example.com",
            "explicit approved",
            IdentityMatchClassification.ExplicitApprovedMapping,
            MappingCandidateStatus.Accepted,
            BatchId,
            Correlation);

        var input = BaseInput(identities: new[] { candidate });
        var result = new MigrationPreflightValidator().Validate(input);

        Assert.Equal(CompatibilityStatus.Passed, result.OverallStatus);
        Assert.DoesNotContain(result.Findings, f => f.Severity is ValidationFindingSeverity.Conflict or ValidationFindingSeverity.Blocked);
    }

    [Fact]
    public void Identity_rejects_empty_external_and_platform_ids()
    {
        Assert.Throws<ArgumentException>(() =>
            new IdentityMappingCandidate(
                " ",
                PlatformUserId.New(),
                "ada@example.com",
                "reason",
                IdentityMatchClassification.ExplicitApprovedMapping,
                MappingCandidateStatus.Proposed,
                BatchId,
                Correlation));

        Assert.ThrowsAny<Exception>(() => PlatformUserId.From(Guid.Empty));
    }

    [Fact]
    public void Identity_duplicate_platform_and_external_detected()
    {
        var user = PlatformUserId.New();
        var a = Id("hc-1", user, "a@example.com");
        var b = Id("hc-2", user, "b@example.com");
        var c = Id("hc-1", PlatformUserId.New(), "c@example.com");

        var result = new MigrationPreflightValidator().Validate(BaseInput(identities: new[] { a, b, c }));

        Assert.Contains(result.Findings, f => f.Code == MigrationFindingCodes.PlatformUserAlreadyMapped);
        Assert.Contains(result.Findings, f => f.Code == MigrationFindingCodes.ExternalUserAlreadyMapped);
        Assert.Equal(CompatibilityStatus.Failed, result.OverallStatus);
    }

    [Fact]
    public void Identity_duplicate_normalized_identifier_and_ambiguous_review()
    {
        var a = Id("hc-1", PlatformUserId.New(), "same@example.com");
        var b = Id("hc-2", PlatformUserId.New(), "same@example.com");
        var review = new IdentityMappingCandidate(
            "hc-3",
            PlatformUserId.New(),
            "review@example.com",
            "weak match",
            IdentityMatchClassification.ManualReviewRequired,
            MappingCandidateStatus.ManualReviewRequired,
            BatchId,
            Correlation);

        var result = new MigrationPreflightValidator().Validate(BaseInput(identities: new[] { a, b, review }));

        Assert.Contains(result.Findings, f => f.Code == MigrationFindingCodes.DuplicateNormalizedIdentifier);
        Assert.Contains(result.Findings, f => f.Code == MigrationFindingCodes.AmbiguousIdentityMatch);
        Assert.NotEqual(CompatibilityStatus.Passed, result.OverallStatus);
    }

    [Fact]
    public void Organization_one_platform_to_many_external_allowed()
    {
        var org = PlatformOrganizationId.New();
        var mappings = new[]
        {
            Org(org, "clinic-a"),
            Org(org, "clinic-b")
        };

        var result = new MigrationPreflightValidator().Validate(BaseInput(orgs: mappings));
        Assert.Equal(CompatibilityStatus.Passed, result.OverallStatus);
        Assert.Equal(2, mappings.Length);
        Assert.Equal(mappings[0].PlatformOrganizationId, mappings[1].PlatformOrganizationId);
        Assert.NotEqual(mappings[0].ExternalOrganizationId, mappings[1].ExternalOrganizationId);
    }

    [Fact]
    public void Organization_same_external_to_different_platform_blocked()
    {
        var mappings = new[]
        {
            Org(PlatformOrganizationId.New(), "clinic-x"),
            Org(PlatformOrganizationId.New(), "clinic-x")
        };

        var result = new MigrationPreflightValidator().Validate(BaseInput(orgs: mappings));
        Assert.Contains(result.Findings, f => f.Code == MigrationFindingCodes.ExternalOrganizationMappedElsewhere);
        Assert.Equal(CompatibilityStatus.Failed, result.OverallStatus);
    }

    [Fact]
    public void Organization_duplicate_pair_and_empty_id_rejected()
    {
        var org = PlatformOrganizationId.New();
        Assert.Throws<ArgumentException>(() => Org(org, " "));

        var dup = new[] { Org(org, "clinic-1"), Org(org, "clinic-1") };
        var result = new MigrationPreflightValidator().Validate(BaseInput(orgs: dup));
        Assert.Contains(result.Findings, f => f.Code == MigrationFindingCodes.DuplicateOrganizationMapping);
    }

    [Fact]
    public void Membership_valid_and_duplicate_and_missing_refs()
    {
        var user = PlatformUserId.New();
        var org = PlatformOrganizationId.New();
        var identity = Id("hc-u", user, "u@example.com");
        var organization = Org(org, "clinic-1");
        var m1 = Mem(user, org, "wf-1");
        var mDup = Mem(user, org, "wf-1");
        var orphan = Mem(PlatformUserId.New(), PlatformOrganizationId.New(), "wf-2");

        var ok = new MigrationPreflightValidator().Validate(
            BaseInput(identities: new[] { identity }, orgs: new[] { organization }, memberships: new[] { m1 }));
        Assert.Equal(CompatibilityStatus.Passed, ok.OverallStatus);

        var bad = new MigrationPreflightValidator().Validate(
            BaseInput(identities: new[] { identity }, orgs: new[] { organization }, memberships: new[] { m1, mDup, orphan }));
        Assert.Contains(bad.Findings, f => f.Code == MigrationFindingCodes.DuplicateMembershipMapping);
        Assert.Contains(bad.Findings, f => f.Code == MigrationFindingCodes.MissingPlatformUser);
        Assert.Contains(bad.Findings, f => f.Code == MigrationFindingCodes.MissingPlatformOrganization);
    }

    [Fact]
    public void Membership_suspended_warns_and_clinical_role_impossible_via_enum()
    {
        var user = PlatformUserId.New();
        var org = PlatformOrganizationId.New();
        var suspended = new MembershipMappingCandidate(
            user,
            org,
            "wf-s",
            OrganizationRole.OrganizationMember,
            MembershipStatus.Suspended,
            MappingCandidateStatus.Warning,
            BatchId);

        var result = new MigrationPreflightValidator().Validate(
            BaseInput(
                identities: new[] { Id("hc-u", user, "u@example.com") },
                orgs: new[] { Org(org, "c1") },
                memberships: new[] { suspended }));

        Assert.Contains(result.Findings, f => f.Code == MigrationFindingCodes.SuspendedMembershipNotActive);
        Assert.Equal(CompatibilityStatus.PassedWithWarnings, result.OverallStatus);

        var roles = Enum.GetNames<OrganizationRole>();
        Assert.DoesNotContain(roles, r => r.Contains("Doctor", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(roles, r => r.Contains("Patient", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Preflight_product_version_sensitive_and_non_utc_fail()
    {
        var local = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.FromHours(3));
        var badTime = new MigrationSimulationInput(
            OtherProduct,
            ContractVersionSupported.V1,
            local,
            Array.Empty<IdentityMappingCandidate>(),
            Array.Empty<OrganizationMappingCandidate>(),
            Array.Empty<MembershipMappingCandidate>());
        Assert.Equal(CompatibilityStatus.Failed, new MigrationPreflightValidator().Validate(badTime).OverallStatus);

        // Preflight is product-agnostic: any valid ProductCode is accepted (no ProductCodeMismatch).
        var posProduct = new MigrationSimulationInput(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            ContractVersionSupported.V1,
            T0,
            Array.Empty<IdentityMappingCandidate>(),
            Array.Empty<OrganizationMappingCandidate>(),
            Array.Empty<MembershipMappingCandidate>());
        Assert.DoesNotContain(
            new MigrationPreflightValidator().Validate(posProduct).Findings,
            f => f.Code == MigrationFindingCodes.ProductCodeMismatch);

        var unsupported = new MigrationSimulationInput(
            OtherProduct,
            new ContractVersionSupported(2),
            T0,
            Array.Empty<IdentityMappingCandidate>(),
            Array.Empty<OrganizationMappingCandidate>(),
            Array.Empty<MembershipMappingCandidate>());
        Assert.Contains(
            new MigrationPreflightValidator().Validate(unsupported).Findings,
            f => f.Code == MigrationFindingCodes.UnsupportedContractVersion);

        var sensitive = new MigrationSimulationInput(
            OtherProduct,
            ContractVersionSupported.V1,
            T0,
            Array.Empty<IdentityMappingCandidate>(),
            Array.Empty<OrganizationMappingCandidate>(),
            Array.Empty<MembershipMappingCandidate>(),
            opaqueMetadataProbe: new Dictionary<string, string> { ["PasswordHash"] = "x" });
        Assert.Contains(
            new MigrationPreflightValidator().Validate(sensitive).Findings,
            f => f.Code == MigrationFindingCodes.SensitiveFieldDetected);

        var features = new MigrationSimulationInput(
            OtherProduct,
            ContractVersionSupported.V1,
            T0,
            Array.Empty<IdentityMappingCandidate>(),
            Array.Empty<OrganizationMappingCandidate>(),
            Array.Empty<MembershipMappingCandidate>(),
            entitlementFeatureCodes: new[] { "feat-a", "feat-a" });
        Assert.Contains(
            new MigrationPreflightValidator().Validate(features).Findings,
            f => f.Code == MigrationFindingCodes.EntitlementSnapshotInvalid);
    }

    [Fact]
    public void Simulation_is_deterministic_and_does_not_mutate_input()
    {
        var user = PlatformUserId.New();
        var org = PlatformOrganizationId.New();
        var identities = new List<IdentityMappingCandidate>
        {
            Id("hc-u", user, "u@example.com")
        };
        var orgs = new List<OrganizationMappingCandidate> { Org(org, "c1") };
        var memberships = new List<MembershipMappingCandidate>
        {
            Mem(user, org, "wf-1")
        };
        var input = BaseInput(identities, orgs, memberships);
        var evidence = CompleteRollback("hc-u", user.ToString());

        var service = new MigrationSimulationService();
        var first = service.Simulate(input, evidence);
        var second = service.Simulate(input, evidence);

        Assert.Equal(first.ProposedMappingCount, second.ProposedMappingCount);
        Assert.Equal(first.AcceptedCandidateCount, second.AcceptedCandidateCount);
        Assert.Equal(first.Compatibility.Status, second.Compatibility.Status);
        Assert.Single(identities);
        Assert.Single(orgs);
        Assert.Single(memberships);
        Assert.Equal(CompatibilityStatus.Passed, first.Compatibility.Status);
        Assert.Equal(RollbackReadinessStatus.Ready, first.RollbackReadiness);
    }

    [Fact]
    public void Simulation_hides_accepted_when_blocked_and_manual_review_not_passed()
    {
        var review = new IdentityMappingCandidate(
            "hc-1",
            PlatformUserId.New(),
            "a@example.com",
            "weak",
            IdentityMatchClassification.ManualReviewRequired,
            MappingCandidateStatus.Accepted,
            BatchId,
            Correlation);

        var sim = new MigrationSimulationService().Simulate(BaseInput(identities: new[] { review }));
        Assert.Equal(CompatibilityStatus.ManualReviewRequired, sim.Compatibility.Status);
        Assert.True(sim.ManualReviewCount > 0);
        Assert.NotEqual(CompatibilityStatus.Passed, sim.Compatibility.Status);

        var conflict = new IdentityMappingCandidate(
            "hc-2",
            PlatformUserId.New(),
            "b@example.com",
            "conflict",
            IdentityMatchClassification.Conflict,
            MappingCandidateStatus.Accepted,
            BatchId,
            Correlation);
        var blocked = new MigrationSimulationService().Simulate(BaseInput(identities: new[] { conflict }));
        Assert.Equal(0, blocked.AcceptedCandidateCount);
        Assert.Equal(CompatibilityStatus.Failed, blocked.Compatibility.Status);
    }

    [Fact]
    public void Rollback_readiness_requires_reverse_mapping_and_ids()
    {
        var validator = new RollbackReadinessValidator();
        Assert.Equal(RollbackReadinessStatus.NotApplicable, validator.Validate(null).Status);
        Assert.Equal(RollbackReadinessStatus.NotReady, validator.Validate(null, requireEvidence: true).Status);

        var ready = CompleteRollback("ext-1", "plat-1");
        Assert.Equal(RollbackReadinessStatus.Ready, validator.Validate(ready).Status);

        var withoutBackup = new RollbackEvidence(
            "ext-1",
            "plat-1",
            BatchId,
            Correlation,
            "before-ref",
            "after-ref",
            "reverse-map-ref",
            T0,
            "pending-approval");
        var missingBackup = validator.Validate(withoutBackup, requireBackupReference: true);
        Assert.Equal(RollbackReadinessStatus.Ready, missingBackup.Status);
        Assert.Contains(missingBackup.Findings, f => f.Code == MigrationFindingCodes.RollbackDataMissing);

        Assert.Throws<ArgumentException>(() =>
            new RollbackEvidence(
                "s",
                "t",
                BatchId,
                Correlation,
                "before",
                "after",
                " ",
                T0,
                "pending-approval"));
    }

    [Fact]
    public void Migration_batch_rejects_non_utc_and_preserves_validated_status_semantics()
    {
        var batch = new MigrationBatch(
            BatchId,
            MigrationType.IdentityMapping,
            OtherProduct,
            T0,
            T0,
            Correlation,
            "product-opaque-export",
            "exits-platform",
            MigrationBatchStatus.Validated,
            2,
            "dry-run validated only");

        Assert.Equal(MigrationBatchStatus.Validated, batch.Status);
        Assert.DoesNotContain("Migrated", batch.Status.ToString(), StringComparison.OrdinalIgnoreCase);

        Assert.Throws<ArgumentException>(() =>
            new MigrationBatch(
                BatchId,
                MigrationType.IdentityMapping,
                OtherProduct,
                new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.FromHours(2)),
                T0,
                Correlation,
                "src",
                "tgt",
                MigrationBatchStatus.Validated,
                0,
                "x"));
    }

    [Fact]
    public void Migration_models_have_no_credential_or_clinical_properties()
    {
        var types = new[]
        {
            typeof(IdentityMappingCandidate),
            typeof(OrganizationMappingCandidate),
            typeof(MembershipMappingCandidate),
            typeof(MigrationBatch),
            typeof(RollbackEvidence),
            typeof(MigrationSimulationInput),
            typeof(MigrationSimulationResult)
        };

        var forbidden = new[]
        {
            "Password", "PasswordHash", "RefreshToken", "MfaSecret", "Otp", "Cookie",
            "Patient", "MedicalNote", "Diagnosis", "Prescription"
        };

        foreach (var type in types)
        {
            foreach (var prop in type.GetProperties())
            {
                Assert.DoesNotContain(forbidden, f =>
                    prop.Name.Equals(f, StringComparison.OrdinalIgnoreCase)
                    || prop.Name.Contains(f, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private static MigrationSimulationInput BaseInput(
        IReadOnlyList<IdentityMappingCandidate>? identities = null,
        IReadOnlyList<OrganizationMappingCandidate>? orgs = null,
        IReadOnlyList<MembershipMappingCandidate>? memberships = null) =>
        new(
            OtherProduct,
            ContractVersionSupported.V1,
            T0,
            identities ?? Array.Empty<IdentityMappingCandidate>(),
            orgs ?? Array.Empty<OrganizationMappingCandidate>(),
            memberships ?? Array.Empty<MembershipMappingCandidate>());

    private static IdentityMappingCandidate Id(string external, PlatformUserId user, string email) =>
        new(
            external,
            user,
            email,
            "explicit",
            IdentityMatchClassification.ExplicitApprovedMapping,
            MappingCandidateStatus.Accepted,
            BatchId,
            Correlation);

    private static OrganizationMappingCandidate Org(PlatformOrganizationId org, string external) =>
        new(
            org,
            external,
            IdentityMatchClassification.ExplicitApprovedMapping,
            MappingCandidateStatus.Accepted,
            BatchId,
            "explicit mapping");

    private static MembershipMappingCandidate Mem(
        PlatformUserId user,
        PlatformOrganizationId org,
        string external) =>
        new(
            user,
            org,
            external,
            OrganizationRole.OrganizationMember,
            MembershipStatus.Active,
            MappingCandidateStatus.Accepted,
            BatchId);

    private static RollbackEvidence CompleteRollback(string source, string target) =>
        new(
            source,
            target,
            BatchId,
            Correlation,
            "before-ref",
            "after-ref",
            "reverse-map-ref",
            T0,
            "pending-approval",
            backupVerificationReference: "backup-verify-ref");
}
