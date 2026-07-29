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
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Application;

public sealed class EntitlementUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private sealed record Fixture(
        FixedClock Clock,
        NoOpUnitOfWork UnitOfWork,
        InMemoryPlatformOrganizationRepository Organizations,
        InMemoryProductRepository Products,
        InMemoryFeatureDefinitionRepository Features,
        InMemoryPlanRepository Plans,
        InMemoryTrialDefinitionRepository Trials,
        InMemorySubscriptionRepository Subscriptions,
        InMemoryFeatureOverrideRepository Overrides,
        InMemoryEntitlementSnapshotRepository Snapshots,
        ProvisionalEntitlementRefreshPolicy RefreshPolicy,
        PlatformOrganizationId OrganizationId,
        SubscriptionId SubscriptionId,
        ProductCode ProductCode);

    private static async Task<Fixture> BuildActiveSubscriptionFixtureAsync()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var products = new InMemoryProductRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();
        var overrides = new InMemoryFeatureOverrideRepository();
        var snapshots = new InMemoryEntitlementSnapshotRepository();
        var refreshPolicy = new ProvisionalEntitlementRefreshPolicy();

        var org = (await new CreatePlatformOrganization(orgs, uow, clock)
            .ExecuteAsync("Acme Group", "acme-group-" + Guid.NewGuid().ToString("N")[..8])).Value!;

        var productCode = ProductCode.Create(ProductCode.PinoyBusinessPos);
        await new CreateProduct(products, uow, clock).ExecuteAsync(ProductCode.PinoyBusinessPos, "POS");

        foreach (var (code, name) in new[]
                 {
                     (FeatureCode.CustomerCreditView, "View Credit"),
                     (FeatureCode.CustomerCreditRepay, "Repay Credit"),
                     (FeatureCode.CustomerCreditCreate, "Create Credit")
                 })
        {
            await features.AddAsync(FeatureDefinition.Create(
                productCode, FeatureCode.Create(code), name, FeatureValueType.Boolean, T0));
        }

        var plan = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "utang", "Utang")).Value!;
        plan.Activate(T0);

        var grants = new[]
        {
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditRepay), true),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditCreate), true)
        };
        var version = (await new PublishPlanVersion(plans, features, uow, clock)
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true, grants)).Value!;

        var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), plan.Id);
        await trials.AddAsync(trial);

        var subscription = (await new StartTrialSubscription(orgs, products, plans, trials, subscriptions, uow, clock)
            .ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id)).Value!;
        subscription.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));
        await subscriptions.UpdateAsync(subscription);

        return new Fixture(
            clock, uow, orgs, products, features, plans, trials, subscriptions, overrides, snapshots,
            refreshPolicy, org.Id, subscription.Id, productCode);
    }

    private static GenerateEntitlementSnapshot BuildGenerateUseCase(Fixture f) => new(
        f.Subscriptions, f.Plans, f.Trials, f.Overrides, f.Snapshots, f.RefreshPolicy, f.UnitOfWork, f.Clock);

    private static ReconcileEntitlementSnapshot BuildReconcileUseCase(Fixture f) => new(
        f.Subscriptions, f.Plans, f.Trials, f.Overrides, f.Snapshots, f.RefreshPolicy, f.UnitOfWork, f.Clock);

    private static CreateFeatureOverride BuildCreateOverrideUseCase(Fixture f) => new(
        f.Organizations, f.Features, f.Overrides, f.UnitOfWork, f.Clock);

    private static RevokeFeatureOverride BuildRevokeOverrideUseCase(Fixture f) => new(
        f.Overrides, f.UnitOfWork, f.Clock);

    [Fact]
    public void ProvisionalEntitlementRefreshPolicy_returns_a_uniform_24_hour_window_and_no_expiry()
    {
        var policy = new ProvisionalEntitlementRefreshPolicy();

        foreach (var status in Enum.GetValues<SubscriptionStatus>())
        {
            Assert.Equal(TimeSpan.FromHours(24), policy.GetRefreshWindow(status));
            Assert.Null(policy.GetOptionalExpiryUtc(status, T0));
        }
    }

    [Fact]
    public async Task GenerateEntitlementSnapshot_by_organization_and_product_allocates_sequential_versions()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var generate = BuildGenerateUseCase(f);

        var first = await generate.ExecuteAsync(f.OrganizationId, f.ProductCode);
        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value!.SnapshotVersion);
        Assert.Equal(f.SubscriptionId, first.Value.SubscriptionId);

        var second = await generate.ExecuteAsync(f.OrganizationId, f.ProductCode);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, second.Value!.SnapshotVersion);
        Assert.Equal(2, f.Snapshots.AddCount);
    }

    [Fact]
    public async Task GenerateEntitlementSnapshot_by_subscription_id_matches_organization_product_overload()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var generate = BuildGenerateUseCase(f);

        var bySubscription = await generate.ExecuteAsync(f.SubscriptionId);
        Assert.True(bySubscription.IsSuccess);
        Assert.Equal(1, bySubscription.Value!.SnapshotVersion);
        Assert.Equal(f.OrganizationId, bySubscription.Value.OrganizationId);
    }

    [Fact]
    public async Task GenerateEntitlementSnapshot_returns_version_conflict_when_expected_version_is_stale()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var generate = BuildGenerateUseCase(f);

        var first = await generate.ExecuteAsync(f.OrganizationId, f.ProductCode, expectedNextVersion: 1);
        Assert.True(first.IsSuccess);

        var conflict = await generate.ExecuteAsync(f.OrganizationId, f.ProductCode, expectedNextVersion: 1);
        Assert.False(conflict.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SnapshotVersionConflict, conflict.ErrorCode);
        Assert.Equal(1, f.Snapshots.AddCount);
    }

    [Fact]
    public async Task GenerateEntitlementSnapshot_returns_not_found_for_unknown_organization_product()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var generate = BuildGenerateUseCase(f);

        var result = await generate.ExecuteAsync(PlatformOrganizationId.New(), f.ProductCode);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SubscriptionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task ReconcileEntitlementSnapshot_creates_a_new_version_distinct_from_the_original()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var generate = BuildGenerateUseCase(f);
        var reconcile = BuildReconcileUseCase(f);

        var initial = await generate.ExecuteAsync(f.OrganizationId, f.ProductCode);
        Assert.True(initial.IsSuccess);

        var reconciled = await reconcile.ExecuteAsync(f.OrganizationId, f.ProductCode, "manual correction");
        Assert.True(reconciled.IsSuccess);
        Assert.Equal(2, reconciled.Value!.SnapshotVersion);
        Assert.NotEqual(initial.Value!.Id, reconciled.Value.Id);
        Assert.Equal(2, f.Snapshots.AddCount);
    }

    [Fact]
    public async Task ReconcileEntitlementSnapshot_works_without_a_reason()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var reconcile = BuildReconcileUseCase(f);

        var result = await reconcile.ExecuteAsync(f.OrganizationId, f.ProductCode);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.SnapshotVersion);
    }

    [Fact]
    public async Task CreateFeatureOverride_rejects_missing_organization_and_missing_feature()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var createOverride = BuildCreateOverrideUseCase(f);

        var missingOrg = await createOverride.ExecuteAsync(
            PlatformOrganizationId.New(),
            f.ProductCode,
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            enabled: false,
            reason: "Support hold",
            createdByUserId: PlatformUserId.New());
        Assert.Equal(ApplicationErrorCodes.OrganizationNotFound, missingOrg.ErrorCode);

        var missingFeature = await createOverride.ExecuteAsync(
            f.OrganizationId,
            f.ProductCode,
            FeatureCode.Create("does-not-exist"),
            enabled: false,
            reason: "Support hold",
            createdByUserId: PlatformUserId.New());
        Assert.Equal(ApplicationErrorCodes.FeatureNotFound, missingFeature.ErrorCode);
    }

    [Fact]
    public async Task CreateFeatureOverride_rejects_a_second_active_override_for_the_same_feature()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var createOverride = BuildCreateOverrideUseCase(f);
        var featureCode = FeatureCode.Create(FeatureCode.CustomerCreditCreate);

        var first = await createOverride.ExecuteAsync(
            f.OrganizationId, f.ProductCode, featureCode, enabled: false, reason: "Support hold",
            createdByUserId: PlatformUserId.New());
        Assert.True(first.IsSuccess);

        var second = await createOverride.ExecuteAsync(
            f.OrganizationId, f.ProductCode, featureCode, enabled: true, reason: "Trying again",
            createdByUserId: PlatformUserId.New());
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.FeatureOverrideConflict, second.ErrorCode);
        Assert.Equal(1, f.Overrides.AddCount);
    }

    [Fact]
    public async Task RevokeFeatureOverride_is_idempotent_when_called_twice()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var createOverride = BuildCreateOverrideUseCase(f);
        var revoke = BuildRevokeOverrideUseCase(f);

        var created = (await createOverride.ExecuteAsync(
            f.OrganizationId, f.ProductCode, FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            enabled: false, reason: "Support hold", createdByUserId: PlatformUserId.New())).Value!;

        var first = await revoke.ExecuteAsync(created.Id, "First revoke", PlatformUserId.New());
        Assert.True(first.IsSuccess);
        var firstRevokedAt = first.Value!.RevokedAtUtc;

        f.Clock.UtcNow = f.Clock.UtcNow.AddMinutes(5);
        var second = await revoke.ExecuteAsync(created.Id, "Second revoke attempt", PlatformUserId.New());
        Assert.True(second.IsSuccess);
        Assert.Equal(firstRevokedAt, second.Value!.RevokedAtUtc);
        Assert.Equal(first.Value.RevocationReason, second.Value.RevocationReason);
    }

    [Fact]
    public async Task CreateFeatureOverride_allows_a_new_override_after_the_previous_one_is_revoked()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var createOverride = BuildCreateOverrideUseCase(f);
        var revoke = BuildRevokeOverrideUseCase(f);
        var featureCode = FeatureCode.Create(FeatureCode.CustomerCreditCreate);

        var first = (await createOverride.ExecuteAsync(
            f.OrganizationId, f.ProductCode, featureCode, enabled: false, reason: "Support hold",
            createdByUserId: PlatformUserId.New())).Value!;
        await revoke.ExecuteAsync(first.Id, "Resolved", PlatformUserId.New());

        var second = await createOverride.ExecuteAsync(
            f.OrganizationId, f.ProductCode, featureCode, enabled: true, reason: "Re-enable after resolution",
            createdByUserId: PlatformUserId.New());
        Assert.True(second.IsSuccess);
        Assert.Equal(2, f.Overrides.AddCount);
    }

    [Fact]
    public async Task RevokeFeatureOverride_returns_not_found_for_unknown_override()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var revoke = BuildRevokeOverrideUseCase(f);

        var result = await revoke.ExecuteAsync(FeatureOverrideId.New(), "reason", PlatformUserId.New());
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.FeatureOverrideNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task EntitlementQueryService_returns_latest_history_and_by_version_snapshots()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var generate = BuildGenerateUseCase(f);
        var queries = new EntitlementQueryService(f.Snapshots);

        var first = (await generate.ExecuteAsync(f.OrganizationId, f.ProductCode)).Value!;
        var second = (await generate.ExecuteAsync(f.OrganizationId, f.ProductCode)).Value!;

        var byId = await queries.GetSnapshotByIdAsync(first.Id.Value);
        Assert.NotNull(byId);
        Assert.Equal(1, byId!.SnapshotVersion);
        Assert.NotEmpty(byId.Grants);

        var latest = await queries.GetLatestAsync(f.OrganizationId.Value, f.ProductCode.Value);
        Assert.Equal(second.Id.Value, latest!.Id);

        var byVersion = await queries.GetByVersionAsync(f.OrganizationId.Value, f.ProductCode.Value, 1);
        Assert.Equal(first.Id.Value, byVersion!.Id);

        var history = await queries.ListHistoryAsync(f.OrganizationId.Value, f.ProductCode.Value, page: 1, pageSize: 10);
        Assert.Equal(2, history.TotalCount);

        var missing = await queries.GetSnapshotByIdAsync(Guid.NewGuid());
        Assert.Null(missing);
    }

    [Fact]
    public async Task FeatureOverrideQueryService_returns_by_id_and_paginated_list()
    {
        var f = await BuildActiveSubscriptionFixtureAsync();
        var createOverride = BuildCreateOverrideUseCase(f);
        var queries = new FeatureOverrideQueryService(f.Overrides);

        var created = (await createOverride.ExecuteAsync(
            f.OrganizationId,
            f.ProductCode,
            FeatureCode.Create(FeatureCode.CustomerCreditCreate),
            enabled: false,
            reason: "Support hold",
            createdByUserId: PlatformUserId.New())).Value!;

        var byId = await queries.GetByIdAsync(created.Id.Value);
        Assert.NotNull(byId);
        Assert.Equal("Support hold", byId!.Reason);

        var list = await queries.ListByOrganizationProductAsync(
            f.OrganizationId.Value, f.ProductCode.Value, status: null, page: 1, pageSize: 10);
        Assert.Equal(1, list.TotalCount);

        var missing = await queries.GetByIdAsync(Guid.NewGuid());
        Assert.Null(missing);
    }
}
