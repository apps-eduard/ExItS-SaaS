using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class EntitlementPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<(PlatformOrganizationId organizationId, PlanId planId, PlanVersionId versionId, TrialDefinitionId trialId, ProductCode productCode)>
        SeedTrialEligibleOrganizationAsync(IServiceProvider provider, string prefix)
    {
        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync($"{prefix} Org", Unique(prefix)).ConfigureAwait(false)).Value!;

        var candidate = Unique(prefix);
        var productCode = candidate[..Math.Min(30, candidate.Length)];
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createFeature = provider.GetRequiredService<CreateFeatureDefinition>();
        var createPlan = provider.GetRequiredService<CreatePlan>();
        var activatePlan = provider.GetRequiredService<ActivatePlan>();
        var createVersion = provider.GetRequiredService<CreateDraftPlanVersion>();
        var publish = provider.GetRequiredService<PublishExistingPlanVersion>();
        var createTrial = provider.GetRequiredService<CreateTrialDefinition>();

        await createProduct.ExecuteAsync(productCode, "POS").ConfigureAwait(false);
        await createFeature.ExecuteAsync(
            productCode, FeatureCode.CustomerCreditView, "View", FeatureValueType.Boolean).ConfigureAwait(false);
        await createFeature.ExecuteAsync(
            productCode, FeatureCode.CustomerCreditCreate, "Create", FeatureValueType.Boolean).ConfigureAwait(false);

        var plan = (await createPlan.ExecuteAsync(productCode, "utang", "Utang").ConfigureAwait(false)).Value!;
        await activatePlan.ExecuteAsync(plan.Id).ConfigureAwait(false);

        var grants = new[]
        {
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditCreate), true)
        };
        var version = (await createVersion
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true, grants, T0)
            .ConfigureAwait(false)).Value!;
        await publish.ExecuteAsync(plan.Id, 1).ConfigureAwait(false);

        var trial = (await createTrial
            .ExecuteAsync(productCode, "Trial", TimeSpan.FromDays(21), grants, Array.Empty<FeatureGrantSpec>())
            .ConfigureAwait(false)).Value!;

        return (org.Id, plan.Id, version.Id, trial.Id, ProductCode.Create(productCode));
    }

    private static async Task<(PlatformOrganizationId organizationId, SubscriptionId subscriptionId, ProductCode productCode)>
        SeedActiveSubscriptionAsync(IServiceProvider provider, string prefix)
    {
        var (organizationId, planId, versionId, trialId, productCode) =
            await SeedTrialEligibleOrganizationAsync(provider, prefix).ConfigureAwait(false);

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var subscription = (await startTrial.ExecuteAsync(organizationId, planId, versionId, trialId)
            .ConfigureAwait(false)).Value!;

        return (organizationId, subscription.Id, productCode);
    }

    [Fact]
    public async Task CreateFeatureOverride_persists_and_reloads_with_revocation_metadata()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync(provider, "persist-override");

        var createOverride = provider.GetRequiredService<CreateFeatureOverride>();
        var revokeOverride = provider.GetRequiredService<RevokeFeatureOverride>();
        var overrides = provider.GetRequiredService<IFeatureOverrideRepository>();

        var created = await createOverride.ExecuteAsync(
            organizationId,
            productCode,
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            enabled: false,
            reason: "Fraud investigation hold",
            createdByUserId: PlatformUserId.New(),
            numericLimit: null,
            expiresAtUtc: T0.AddDays(7));
        Assert.True(created.IsSuccess);

        var reloaded = await overrides.GetByIdAsync(created.Value!.Id, default);
        Assert.NotNull(reloaded);
        Assert.Equal(organizationId, reloaded!.OrganizationId);
        Assert.Equal(productCode, reloaded.ProductCode);
        Assert.Equal(FeatureCode.CustomerCreditCreate, reloaded.FeatureCode.Value);
        Assert.False(reloaded.Enabled);
        Assert.Equal("Fraud investigation hold", reloaded.Reason);
        Assert.Equal(FeatureOverrideStatus.Active, reloaded.Status);
        Assert.Null(reloaded.RevokedAtUtc);

        var revokedBy = PlatformUserId.New();
        var revoked = await revokeOverride.ExecuteAsync(
            created.Value.Id, "Investigation closed", revokedBy);
        Assert.True(revoked.IsSuccess);

        var reloadedAfterRevoke = await overrides.GetByIdAsync(created.Value.Id, default);
        Assert.Equal(FeatureOverrideStatus.Revoked, reloadedAfterRevoke!.Status);
        Assert.Equal(revokedBy, reloadedAfterRevoke.RevokedByUserId);
        Assert.Equal("Investigation closed", reloadedAfterRevoke.RevocationReason);
        Assert.NotNull(reloadedAfterRevoke.RevokedAtUtc);
    }

    [Fact]
    public async Task CreateFeatureOverride_rejects_a_second_active_override_for_the_same_feature()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync(provider, "override-conflict");

        var createOverride = provider.GetRequiredService<CreateFeatureOverride>();
        var first = await createOverride.ExecuteAsync(
            organizationId,
            productCode,
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            enabled: false,
            reason: "Support hold",
            createdByUserId: PlatformUserId.New());
        Assert.True(first.IsSuccess);

        var second = await createOverride.ExecuteAsync(
            organizationId,
            productCode,
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            enabled: true,
            reason: "Trying to re-enable",
            createdByUserId: PlatformUserId.New());
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.FeatureOverrideConflict, second.ErrorCode);
    }

    [Fact]
    public async Task GenerateEntitlementSnapshot_persists_grants_and_allocates_sequential_versions()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, subscriptionId, productCode) = await SeedActiveSubscriptionAsync(provider, "gen-snapshot");

        var generate = provider.GetRequiredService<GenerateEntitlementSnapshot>();
        var snapshots = provider.GetRequiredService<IEntitlementSnapshotRepository>();

        var first = await generate.ExecuteAsync(organizationId, productCode);
        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value!.SnapshotVersion);

        var second = await generate.ExecuteAsync(organizationId, productCode);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, second.Value!.SnapshotVersion);

        var reloaded = await snapshots.GetByIdAsync(first.Value.Id, default);
        Assert.NotNull(reloaded);
        Assert.Equal(subscriptionId, reloaded!.SubscriptionId);
        Assert.Equal(productCode, reloaded.ProductCode);
        Assert.NotEmpty(reloaded.Grants);
        Assert.Contains(reloaded.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditView && g.Enabled);
        Assert.Contains(reloaded.Grants, g => g.FeatureCode.Value == FeatureCode.CustomerCreditCreate && g.Enabled);

        var byVersion2 = await snapshots.GetByVersionAsync(organizationId, productCode, 2, default);
        Assert.NotNull(byVersion2);
        Assert.Equal(second.Value.Id, byVersion2!.Id);

        var latest = await snapshots.GetLatestForOrganizationProductAsync(organizationId, productCode, default);
        Assert.Equal(second.Value.Id, latest!.Id);

        var (items, totalCount) = await snapshots.ListHistoryAsync(organizationId, productCode, 0, 10, default);
        Assert.Equal(2, totalCount);
        Assert.Equal(2, items[0].SnapshotVersion);
        Assert.Equal(1, items[1].SnapshotVersion);
    }

    [Fact]
    public async Task GenerateEntitlementSnapshot_with_stale_expected_version_returns_version_conflict()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync(provider, "stale-expected-version");

        var generate = provider.GetRequiredService<GenerateEntitlementSnapshot>();
        var first = await generate.ExecuteAsync(organizationId, productCode, expectedNextVersion: 1);
        Assert.True(first.IsSuccess);

        var conflict = await generate.ExecuteAsync(organizationId, productCode, expectedNextVersion: 1);
        Assert.False(conflict.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SnapshotVersionConflict, conflict.ErrorCode);
    }

    [Fact]
    public async Task Duplicate_snapshot_version_that_bypasses_the_application_check_is_rejected_by_the_database()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, subscriptionId, productCode) = await SeedActiveSubscriptionAsync(provider, "dup-db-snapshot");

        var snapshots = provider.GetRequiredService<IEntitlementSnapshotRepository>();
        var unitOfWork = provider.GetRequiredService<IPlatformUnitOfWork>();

        var grant = new EntitlementGrant(
            FeatureCode.Create(FeatureCode.CustomerCreditView), true, EntitlementGrantSource.Plan, T0);

        var first = EntitlementSnapshot.Create(
            organizationId, productCode, subscriptionId, PlanCode.Create("utang"), 1, 1,
            SubscriptionStatus.Trialing, false, T0, T0, T0.AddHours(24), 1, new[] { grant });
        await snapshots.AddAsync(first, default);
        await unitOfWork.SaveChangesAsync();

        var second = EntitlementSnapshot.Create(
            organizationId, productCode, subscriptionId, PlanCode.Create("utang"), 1, 1,
            SubscriptionStatus.Trialing, false, T0, T0, T0.AddHours(24), 1, new[] { grant });
        await snapshots.AddAsync(second, default);

        var ex = await Assert.ThrowsAsync<PersistenceConflictException>(() => unitOfWork.SaveChangesAsync());
        Assert.Equal(ApplicationErrorCodes.SnapshotVersionConflict, ex.ErrorCode);
    }

            [Fact]
            public async Task GetLatestSnapshotVersionAsync_returns_null_then_the_correct_version_after_inserts()
            {
                await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
                var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync(provider, "latest-version");

                var snapshots = provider.GetRequiredService<IEntitlementSnapshotRepository>();
                var generate = provider.GetRequiredService<GenerateEntitlementSnapshot>();

                var beforeAny = await snapshots.GetLatestSnapshotVersionAsync(organizationId, productCode, default);
                Assert.Null(beforeAny);

                await generate.ExecuteAsync(organizationId, productCode);
                await generate.ExecuteAsync(organizationId, productCode);

                var afterTwo = await snapshots.GetLatestSnapshotVersionAsync(organizationId, productCode, default);
                Assert.Equal(2, afterTwo);
            }

            [Fact]
            public async Task ListActiveForOrganizationProductAsync_excludes_expired_and_revoked_overrides()
            {
                await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
                var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync(provider, "list-active-overrides");

                var createOverride = provider.GetRequiredService<CreateFeatureOverride>();
                var revokeOverride = provider.GetRequiredService<RevokeFeatureOverride>();
                var overrides = provider.GetRequiredService<IFeatureOverrideRepository>();

                var stillActive = (await createOverride.ExecuteAsync(
                    organizationId, productCode, FeatureCode.Create(FeatureCode.CustomerCreditCreate),
                    enabled: false, reason: "Active hold", createdByUserId: PlatformUserId.New())).Value!;

                var toBeRevoked = (await createOverride.ExecuteAsync(
                    organizationId, productCode, FeatureCode.Create(FeatureCode.CustomerCreditView),
                    enabled: false, reason: "Temporary hold", createdByUserId: PlatformUserId.New())).Value!;
                await revokeOverride.ExecuteAsync(toBeRevoked.Id, "Resolved", PlatformUserId.New());

                var active = await overrides.ListActiveForOrganizationProductAsync(
                    organizationId, productCode, T0.AddMinutes(1), default);

                Assert.Single(active);
                Assert.Equal(stillActive.Id, active[0].Id);
            }

            [Fact]
            public async Task ReconcileEntitlementSnapshot_creates_a_new_snapshot_version_without_mutating_history()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync(provider, "reconcile-snapshot");

        var generate = provider.GetRequiredService<GenerateEntitlementSnapshot>();
        var reconcile = provider.GetRequiredService<ReconcileEntitlementSnapshot>();
        var snapshots = provider.GetRequiredService<IEntitlementSnapshotRepository>();

        var initial = await generate.ExecuteAsync(organizationId, productCode);
        Assert.True(initial.IsSuccess);

        var reconciled = await reconcile.ExecuteAsync(organizationId, productCode, "manual correction");
        Assert.True(reconciled.IsSuccess);
        Assert.Equal(2, reconciled.Value!.SnapshotVersion);
        Assert.NotEqual(initial.Value!.Id, reconciled.Value.Id);

        var originalStillExists = await snapshots.GetByVersionAsync(organizationId, productCode, 1, default);
        Assert.NotNull(originalStillExists);
        Assert.Equal(initial.Value.Id, originalStillExists!.Id);
    }

    [Fact]
    public async Task Concurrent_revocations_of_the_same_feature_override_surface_a_concurrency_conflict()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync(provider, "xmin-override");

        var createOverride = provider.GetRequiredService<CreateFeatureOverride>();
        var created = (await createOverride.ExecuteAsync(
            organizationId,
            productCode,
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            enabled: false,
            reason: "Support hold",
            createdByUserId: PlatformUserId.New())).Value!;

        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();

        var overridesA = scopeA.ServiceProvider.GetRequiredService<IFeatureOverrideRepository>();
        var overridesB = scopeB.ServiceProvider.GetRequiredService<IFeatureOverrideRepository>();
        var uowA = scopeA.ServiceProvider.GetRequiredService<IPlatformUnitOfWork>();
        var uowB = scopeB.ServiceProvider.GetRequiredService<IPlatformUnitOfWork>();

        var overrideA = await overridesA.GetByIdAsync(created.Id, default);
        var overrideB = await overridesB.GetByIdAsync(created.Id, default);
        Assert.NotNull(overrideA);
        Assert.NotNull(overrideB);

        overrideA!.Revoke("Reason A", PlatformUserId.New(), T0.AddMinutes(1));
        overrideB!.Revoke("Reason B", PlatformUserId.New(), T0.AddMinutes(2));

        await overridesA.UpdateAsync(overrideA, default);
        await overridesB.UpdateAsync(overrideB, default);

        await uowA.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<PersistenceConflictException>(() => uowB.SaveChangesAsync());
        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, ex.ErrorCode);

        await using var verifyScope = provider.CreateAsyncScope();
        var reloaded = await verifyScope.ServiceProvider
            .GetRequiredService<IFeatureOverrideRepository>()
            .GetByIdAsync(created.Id, default);
        Assert.NotNull(reloaded);
        Assert.Equal(FeatureOverrideStatus.Revoked, reloaded!.Status);
        Assert.Equal("Reason A", reloaded.RevocationReason);
    }

    [Fact]
    public async Task Expiry_before_effective_check_constraint_rejects_an_invalid_row_at_the_database_level()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, _, productCode) = await SeedActiveSubscriptionAsync(provider, "override-check-constraint");

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO platform.feature_overrides (
                id, organization_id, product_code, feature_code, enabled, numeric_limit, reason,
                effective_from_utc, expires_at_utc, status, created_at_utc, created_by_user_id, updated_at_utc)
            VALUES (
                @id, @organizationId, @productCode, @featureCode, false, NULL, 'invalid expiry',
                @effectiveFrom, @expiresAt, 'Active', @now, @createdBy, @now)
            """,
            connection);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("organizationId", organizationId.Value);
        command.Parameters.AddWithValue("productCode", productCode.Value);
        command.Parameters.AddWithValue("featureCode", FeatureCode.CustomerCreditCreate);
        command.Parameters.AddWithValue("effectiveFrom", T0);
        command.Parameters.AddWithValue("expiresAt", T0.AddDays(-1));
        command.Parameters.AddWithValue("now", T0);
        command.Parameters.AddWithValue("createdBy", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Contains("ck_feature_overrides_expiry_range", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
