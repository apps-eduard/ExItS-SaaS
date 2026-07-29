using ExItS.Platform.Application.Contracts;
using ExItS.Platform.Application.Integration.HealthCare;
using ExItS.Platform.Application.Projections;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Contracts;

public sealed class ContractEnvelopeAndVersionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ContractVersion_rejects_non_positive_major()
    {
        Assert.Throws<ContractException>(() => ContractVersion.Create(0));
        Assert.Throws<ContractException>(() => ContractVersion.Create(-1));
        var v = ContractVersion.Create(1, 2);
        Assert.Equal(1, v.Major);
        Assert.Equal(2, v.Minor);
        Assert.Equal(v, ContractVersion.Create(1, 2));
        Assert.True(ContractVersion.Create(1).IsCompatibleWith(ContractVersion.Create(1)));
        Assert.False(ContractVersion.Create(2).IsCompatibleWith(ContractVersion.Create(1)));
    }

    [Fact]
    public void Envelope_rejects_empty_message_id_and_non_utc()
    {
        var payload = new PlatformUserProjection(
            PlatformUserId.New(), "Ada", "ada@example.com", AccountStatus.Active, T0, 1);

        Assert.Throws<ContractException>(() =>
            ContractEnvelope<PlatformUserProjection>.Create(
                ContractNames.PlatformUserProjection,
                ContractVersion.V1,
                Guid.Empty,
                Guid.NewGuid(),
                T0, T0,
                ContractSourceSystems.ExItsPlatform,
                Guid.NewGuid().ToString("D"),
                1,
                payload));

        var local = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.FromHours(3));
        Assert.Throws<ContractException>(() =>
            ContractEnvelope<PlatformUserProjection>.Create(
                ContractNames.PlatformUserProjection,
                ContractVersion.V1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                local, T0,
                ContractSourceSystems.ExItsPlatform,
                Guid.NewGuid().ToString("D"),
                1,
                payload));
    }

    [Fact]
    public void Envelope_rejects_invalid_source_system_and_blank_aggregate_id()
    {
        var payload = new PlatformUserProjection(
            PlatformUserId.New(), "Ada", "ada@example.com", AccountStatus.Active, T0, 1);

        Assert.Throws<ContractException>(() =>
            ContractEnvelope<PlatformUserProjection>.Create(
                ContractNames.PlatformUserProjection,
                ContractVersion.V1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                T0, T0,
                "other-system",
                Guid.NewGuid().ToString("D"),
                1,
                payload));

        Assert.Throws<ContractException>(() =>
            ContractEnvelope<PlatformUserProjection>.Create(
                ContractNames.PlatformUserProjection,
                ContractVersion.V1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                T0, T0,
                ContractSourceSystems.ExItsPlatform,
                " ",
                1,
                payload));
    }
}

public sealed class ProjectionContractTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Membership_preserves_platform_org_role_only()
    {
        var projection = new OrganizationMembershipProjection(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            MembershipStatus.Active,
            OrganizationRole.OrganizationAdministrator,
            T0,
            1);

        Assert.Equal(OrganizationRole.OrganizationAdministrator, projection.OrganizationRole);
        Assert.DoesNotContain("Doctor", Enum.GetNames<OrganizationRole>());
    }

    [Fact]
    public void Organization_mapping_allows_multiple_external_ids_for_one_platform_org()
    {
        var org = PlatformOrganizationId.New();
        var product = ProductCode.Create(ProductCode.HealthCare);
        var a = new OrganizationMappingProjection(
            Guid.NewGuid(), org, product, "clinic-A", OrganizationMappingStatus.Active, T0, T0, 1);
        var b = new OrganizationMappingProjection(
            Guid.NewGuid(), org, product, "clinic-B", OrganizationMappingStatus.Active, T0, T0, 2);

        Assert.Equal(a.PlatformOrganizationId, b.PlatformOrganizationId);
        Assert.NotEqual(a.ExternalOrganizationId, b.ExternalOrganizationId);
        Assert.Throws<ContractException>(() =>
            new OrganizationMappingProjection(
                Guid.NewGuid(), org, ProductCode.Create(ProductCode.PinoyBusinessPos), "x",
                OrganizationMappingStatus.Active, T0, T0, 1));
    }

    [Fact]
    public void Product_access_revocation_requires_timestamp()
    {
        Assert.Throws<ContractException>(() =>
            new ProductAccessProjection(
                PlatformOrganizationId.New(),
                ProductCode.Create(ProductCode.HealthCare),
                ProductAccessStatus.Revoked,
                T0,
                1));

        var revoked = new ProductAccessProjection(
            PlatformOrganizationId.New(),
            ProductCode.Create(ProductCode.HealthCare),
            ProductAccessStatus.Revoked,
            T0,
            1,
            revokedAtUtc: T0.AddMinutes(1));
        Assert.Equal(ProductAccessStatus.Revoked, revoked.AccessStatus);
    }

    [Fact]
    public void Entitlement_projection_rejects_duplicate_feature_codes()
    {
        var grant = new FeatureGrantProjection(
            FeatureCode.Create("max-users"),
            FeatureValueType.NumericLimit,
            true,
            EntitlementGrantSource.Plan,
            T0,
            5);

        Assert.Throws<ContractException>(() =>
            new EntitlementSnapshotProjection(
                Guid.NewGuid(), 1, ContractVersion.V1,
                PlatformOrganizationId.New(),
                ProductCode.Create(ProductCode.HealthCare),
                SubscriptionId.New(),
                SubscriptionStatus.Active,
                PlanCode.Create("basic"),
                1, T0, T0, T0.AddHours(1), false, 1,
                new[] { grant, grant }));
    }
}

public sealed class ProjectionApplicabilityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
    private readonly ProjectionApplicabilityEvaluator _evaluator = new();

    [Fact]
    public void Duplicate_message_id_ignored()
    {
        var msg = Guid.NewGuid();
        var checkpoint = ProjectionCheckpoint.Empty("hc", ContractNames.PlatformUserProjection)
            .WithApplied(1, msg, T0, null, null);
        var result = _evaluator.Evaluate(checkpoint, msg, 2, ContractVersion.V1, T0, null, null);
        Assert.Equal(ProjectionApplyOutcome.DuplicateIgnored, result.Outcome);
    }

    [Fact]
    public void Older_version_ignored_and_sequential_accepted()
    {
        var checkpoint = ProjectionCheckpoint.Empty("hc", ContractNames.PlatformUserProjection)
            .WithApplied(3, Guid.NewGuid(), T0, null, null);

        var older = _evaluator.Evaluate(checkpoint, Guid.NewGuid(), 2, ContractVersion.V1, T0, null, null);
        Assert.Equal(ProjectionApplyOutcome.OlderVersionIgnored, older.Outcome);

        var next = _evaluator.Evaluate(checkpoint, Guid.NewGuid(), 4, ContractVersion.V1, T0, null, null);
        Assert.Equal(ProjectionApplyOutcome.Applied, next.Outcome);
        Assert.Equal(4, next.UpdatedCheckpoint!.LastAppliedSourceVersion);
    }

    [Fact]
    public void Version_gap_and_same_version_conflict_and_unsupported_major()
    {
        var checkpoint = ProjectionCheckpoint.Empty("hc", ContractNames.EntitlementSnapshotProjection)
            .WithApplied(1, Guid.NewGuid(), T0, null, null);

        var gap = _evaluator.Evaluate(checkpoint, Guid.NewGuid(), 3, ContractVersion.V1, T0, null, null);
        Assert.Equal(ProjectionApplyOutcome.VersionGapDetected, gap.Outcome);
        Assert.Equal(ProjectionConsumerState.ReconciliationRequired, gap.ConsumerState);

        var conflict = _evaluator.Evaluate(checkpoint, Guid.NewGuid(), 1, ContractVersion.V1, T0, null, null);
        Assert.Equal(ProjectionApplyOutcome.Conflict, conflict.Outcome);

        var unsupported = _evaluator.Evaluate(
            checkpoint, Guid.NewGuid(), 2, ContractVersion.Create(2), T0, null, null);
        Assert.Equal(ProjectionApplyOutcome.UnsupportedVersion, unsupported.Outcome);
    }

    [Fact]
    public void Reconciliation_snapshot_can_bridge_gap()
    {
        var checkpoint = ProjectionCheckpoint.Empty("hc", ContractNames.EntitlementSnapshotProjection)
            .WithApplied(1, Guid.NewGuid(), T0, null, null);
        var result = _evaluator.Evaluate(
            checkpoint, Guid.NewGuid(), 5, ContractVersion.V1, T0, null, null, isReconciliationSnapshot: true);
        Assert.Equal(ProjectionApplyOutcome.Applied, result.Outcome);
        Assert.Equal(5, result.UpdatedCheckpoint!.LastAppliedSourceVersion);
    }
}

public sealed class ProjectionUseCaseAndReconciliationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private sealed class MemoryCheckpointStore : IProjectionCheckpointStore
    {
        public ProjectionCheckpoint? LastSaved { get; private set; }
        public int SaveCount { get; private set; }

        public Task<ProjectionCheckpoint?> GetAsync(
            string consumerName,
            string contractName,
            PlatformOrganizationId? organizationId,
            ProductCode? productCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LastSaved);

        public Task SaveAsync(ProjectionCheckpoint checkpoint, CancellationToken cancellationToken = default)
        {
            LastSaved = checkpoint;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Evaluate_saves_checkpoint_only_when_applied()
    {
        var store = new MemoryCheckpointStore();
        var useCase = new EvaluateProjectionApplicability(store, new ProjectionApplicabilityEvaluator(), new FixedClock(T0));

        var applied = await useCase.ExecuteAsync(
            "healthcare-api", ContractNames.PlatformUserProjection, Guid.NewGuid(), 1, ContractVersion.V1, null, null);
        Assert.True(applied.IsSuccess);
        Assert.Equal(ProjectionApplyOutcome.Applied, applied.Value!.Outcome);
        Assert.Equal(1, store.SaveCount);

        var older = await useCase.ExecuteAsync(
            "healthcare-api", ContractNames.PlatformUserProjection, Guid.NewGuid(), 1, ContractVersion.V1, null, null);
        Assert.Equal(ProjectionApplyOutcome.Conflict, older.Value!.Outcome);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Reconciliation_request_validates_and_reports_transport_unavailable()
    {
        var request = new ReconciliationRequest(
            "healthcare-api",
            PlatformOrganizationId.New(),
            ProductCode.Create(ProductCode.HealthCare),
            "version gap",
            Guid.NewGuid(),
            expectedSourceVersion: 5,
            currentSourceVersion: 2);

        var result = await new RequestProjectionReconciliation().ExecuteAsync(request);
        Assert.True(result.IsSuccess);
        Assert.Equal(ReconciliationOutcome.SourceUnavailable, result.Value!.Outcome);

        var noChange = await new RequestProjectionReconciliation().ExecuteAsync(
            new ReconciliationRequest(
                "healthcare-api",
                PlatformOrganizationId.New(),
                ProductCode.Create(ProductCode.HealthCare),
                "already current",
                Guid.NewGuid(),
                expectedSourceVersion: 3,
                currentSourceVersion: 3));
        Assert.Equal(ReconciliationOutcome.NoChange, noChange.Value!.Outcome);
    }
}

public sealed class ContractSecurityShapeTests
{
    [Fact]
    public void Outbound_contract_types_do_not_expose_secret_or_clinical_properties()
    {
        var forbidden = new[]
        {
            "Password", "PasswordHash", "RefreshToken", "MfaSecret", "Otp", "Cookie",
            "Patient", "MedicalNote", "Diagnosis", "Prescription"
        };

        var contractTypes = typeof(ContractEnvelope<>).Assembly.GetTypes()
            .Where(t => t.Namespace is not null
                        && (t.Namespace.Contains(".Contracts", StringComparison.Ordinal)
                            || t.Namespace.Contains(".Integration.HealthCare", StringComparison.Ordinal)))
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var type in contractTypes)
        {
            foreach (var prop in type.GetProperties())
            {
                Assert.DoesNotContain(forbidden, f =>
                    string.Equals(prop.Name, f, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void HealthCare_delivery_interfaces_do_not_reference_clinical_type_names()
    {
        var interfaces = new[]
        {
            typeof(IHealthCareUserProjectionDelivery),
            typeof(IHealthCareMembershipProjectionDelivery),
            typeof(IHealthCareOrganizationMappingDelivery),
            typeof(IHealthCareProductAccessProjectionDelivery),
            typeof(IHealthCareSubscriptionProjectionDelivery),
            typeof(IHealthCareEntitlementSnapshotDelivery),
            typeof(IPlatformProjectionReconciliationService)
        };

        foreach (var iface in interfaces)
        {
            Assert.DoesNotContain(iface.GetMethods().SelectMany(m => m.GetParameters()).Select(p => p.ParameterType.Name),
                n => n is "Patient" or "MedicalNote" or "DbContext" or "ApplicationUser");
        }
    }
}
