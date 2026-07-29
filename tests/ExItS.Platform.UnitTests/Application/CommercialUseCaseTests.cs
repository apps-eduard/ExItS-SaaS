using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Application;

public sealed class CommercialUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateProduct_and_plan_publish_version_and_start_trial_snapshot()
    {
        var clock = new FixedClock(T0);
        var products = new InMemoryProductRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();
        var overrides = new InMemoryFeatureOverrideRepository();
        var snapshots = new InMemoryEntitlementSnapshotRepository();

        var productResult = await new CreateProduct(products, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "Pinoy Business POS");
        Assert.True(productResult.IsSuccess);

        var productCode = ProductCode.Create(ProductCode.PinoyBusinessPos);
        foreach (var (code, name) in new[]
                 {
                     (FeatureCode.CustomerCreditView, "View Credit"),
                     (FeatureCode.CustomerCreditRepay, "Repay Credit"),
                     (FeatureCode.CustomerCreditCreate, "Create Credit")
                 })
        {
            await features.AddAsync(FeatureDefinition.Create(
                productCode,
                FeatureCode.Create(code),
                name,
                FeatureValueType.Boolean,
                T0));
        }

        var planResult = await new CreatePlan(products, plans, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "utang-trial", "Utang Trial");
        Assert.True(planResult.IsSuccess);
        planResult.Value!.Activate(T0);

        var grants = new[]
        {
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditRepay), true),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditCreate), true)
        };
        var versionResult = await new PublishPlanVersion(plans, features, clock)
            .ExecuteAsync(planResult.Value.Id, 1, BillingPeriod.None, true, grants);
        Assert.True(versionResult.IsSuccess);
        Assert.Equal(1, plans.AddVersionCount);

        var conflictVersion = await new PublishPlanVersion(plans, features, clock)
            .ExecuteAsync(planResult.Value.Id, 1, BillingPeriod.None, true, grants);
        Assert.False(conflictVersion.IsSuccess);

        var trial = TrialDefinition.CreatePinoyBusinessPosUtangTrial(T0, planResult.Value.Id);
        await trials.AddAsync(trial);

        var start = await new StartTrialSubscription(plans, trials, subscriptions, clock)
            .ExecuteAsync(PlatformOrganizationId.New(), planResult.Value.Id, versionResult.Value!.Id, trial.Id);
        Assert.True(start.IsSuccess);
        Assert.Equal(1, subscriptions.AddCount);

        var snapshot = await new GenerateEntitlementSnapshot(
                subscriptions, plans, trials, overrides, snapshots, clock)
            .ExecuteAsync(start.Value!.Id, expectedNextVersion: 1);
        Assert.True(snapshot.IsSuccess);
        Assert.Equal(1, snapshots.AddCount);

        var conflict = await new GenerateEntitlementSnapshot(
                subscriptions, plans, trials, overrides, snapshots, clock)
            .ExecuteAsync(start.Value.Id, expectedNextVersion: 1);
        Assert.Equal(ApplicationErrorCodes.SnapshotVersionConflict, conflict.ErrorCode);
        Assert.Equal(1, snapshots.AddCount);
    }

    [Fact]
    public async Task CreateProduct_duplicate_does_not_persist_second()
    {
        var products = new InMemoryProductRepository();
        var create = new CreateProduct(products, new FixedClock(T0));
        Assert.True((await create.ExecuteAsync("healthcare", "HealthCare")).IsSuccess);
        var dup = await create.ExecuteAsync("HealthCare", "HealthCare Two");
        Assert.Equal(ApplicationErrorCodes.DuplicateProductCode, dup.ErrorCode);
        Assert.Equal(1, products.AddCount);
    }

    [Fact]
    public async Task Suspend_and_cancel_subscription_use_cases()
    {
        var clock = new FixedClock(T0);
        var products = new InMemoryProductRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();

        await new CreateProduct(products, clock).ExecuteAsync(ProductCode.PinoyBusinessPos, "POS");
        var pc = ProductCode.Create(ProductCode.PinoyBusinessPos);
        await features.AddAsync(FeatureDefinition.Create(
            pc, FeatureCode.Create(FeatureCode.CustomerCreditView), "View", FeatureValueType.Boolean, T0));

        var plan = (await new CreatePlan(products, plans, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "utang", "Utang")).Value!;
        plan.Activate(T0);
        var version = (await new PublishPlanVersion(plans, features, clock)
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true,
                new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) }))
            .Value!;
        var trial = TrialDefinition.CreatePinoyBusinessPosUtangTrial(T0, plan.Id);
        await trials.AddAsync(trial);
        var sub = (await new StartTrialSubscription(plans, trials, subscriptions, clock)
            .ExecuteAsync(PlatformOrganizationId.New(), plan.Id, version.Id, trial.Id)).Value!;

        Assert.True((await new SuspendSubscription(subscriptions, clock).ExecuteAsync(sub.Id)).IsSuccess);
        Assert.Equal(SubscriptionStatus.Suspended, sub.Status);
        Assert.True((await new CancelSubscription(subscriptions, clock).ExecuteAsync(sub.Id)).IsSuccess);
        Assert.Equal(SubscriptionStatus.Cancelled, sub.Status);
    }

    [Fact]
    public async Task Create_and_revoke_feature_override()
    {
        var clock = new FixedClock(T0);
        var features = new InMemoryFeatureDefinitionRepository();
        var overrides = new InMemoryFeatureOverrideRepository();
        var pc = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var fc = FeatureCode.Create(FeatureCode.CustomerCreditCreate);
        await features.AddAsync(FeatureDefinition.Create(pc, fc, "Create", FeatureValueType.Boolean, T0));

        var created = await new CreateFeatureOverride(features, overrides, clock).ExecuteAsync(
            PlatformOrganizationId.New(),
            pc,
            fc,
            enabled: false,
            reason: "Support hold",
            createdByUserId: PlatformUserId.New());
        Assert.True(created.IsSuccess);
        Assert.Equal(1, overrides.AddCount);

        var revoked = await new RevokeFeatureOverride(overrides, clock).ExecuteAsync(created.Value!.Id);
        Assert.True(revoked.IsSuccess);
        Assert.Equal(FeatureOverrideStatus.Revoked, revoked.Value!.Status);
    }
}
