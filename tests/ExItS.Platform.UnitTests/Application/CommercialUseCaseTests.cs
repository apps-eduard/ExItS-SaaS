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

using ExItS.Platform.UnitTests.TestSupport;
namespace ExItS.Platform.UnitTests.Application;

public sealed class CommercialUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StartTrial_rejects_inactive_plan()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var products = new InMemoryProductRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Group", "acme-inactive-plan")).Value!;
        var product = (await new CreateProduct(products, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "Pinoy Business POS")).Value!;
        var plan = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(product.Code.Value, MvpPosPlanCodes.Growth, "Growth")).Value!;
        plan.Activate(T0);
        plan.Deactivate(T0.AddMinutes(1));
        await plans.UpdateAsync(plan);

        var version = PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.Monthly, true, [], T0);
        version.Publish(T0);
        await plans.AddVersionAsync(version);

        var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(7), plan.Id);
        await trials.AddAsync(trial);

        var start = await new StartTrialSubscription(orgs, products, plans, trials, subscriptions, uow, clock)
            .ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id);
        Assert.False(start.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SubscriptionIneligible, start.ErrorCode);
        Assert.Contains("active plan", start.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ActivatePaidSubscription_rejects_retired_and_inactive_plans()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var products = new InMemoryProductRepository();
        var plans = new InMemoryPlanRepository();
        var subscriptions = new InMemorySubscriptionRepository();

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Group", "acme-paid-plan")).Value!;
        await new CreateProduct(products, uow, clock).ExecuteAsync(ProductCode.PinoyBusinessPos, "POS");

        var inactivePlan = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "inactive-paid", "Inactive Paid")).Value!;
        inactivePlan.Activate(T0);
        inactivePlan.Deactivate(T0.AddMinutes(1));
        await plans.UpdateAsync(inactivePlan);
        var inactiveVersion = PlanVersion.CreateDraft(inactivePlan, 1, T0, BillingPeriod.Monthly, false, [], T0);
        inactiveVersion.Publish(T0);
        await plans.AddVersionAsync(inactiveVersion);

        var inactivePaid = await new ActivatePaidSubscription(orgs, products, plans, subscriptions, uow, clock)
            .ExecuteAsync(org.Id, inactivePlan.Id, inactiveVersion.Id, T0, T0.AddDays(30), BillingCycle.Monthly);
        Assert.False(inactivePaid.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SubscriptionIneligible, inactivePaid.ErrorCode);

        var retiredPlan = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "retired-paid", "Retired Paid")).Value!;
        retiredPlan.Activate(T0);
        retiredPlan.Retire(T0.AddMinutes(2));
        await plans.UpdateAsync(retiredPlan);
        var retiredVersion = PlanVersion.CreateDraft(retiredPlan, 1, T0, BillingPeriod.Monthly, false, [], T0);
        retiredVersion.Publish(T0);
        await plans.AddVersionAsync(retiredVersion);

        var retiredPaid = await new ActivatePaidSubscription(orgs, products, plans, subscriptions, uow, clock)
            .ExecuteAsync(org.Id, retiredPlan.Id, retiredVersion.Id, T0, T0.AddDays(30), BillingCycle.Monthly);
        Assert.False(retiredPaid.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SubscriptionIneligible, retiredPaid.ErrorCode);
        Assert.Contains("Retired", retiredPaid.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartTrial_rejects_retired_plan()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var products = new InMemoryProductRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Group", "acme-retired-plan")).Value!;
        var product = (await new CreateProduct(products, uow, clock)
            .ExecuteAsync("retired-plan-prod", "Retired Plan Product")).Value!;
        var plan = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(product.Code.Value, "retired-plan", "Retired Plan")).Value!;
        plan.Activate(T0);
        plan.Retire(T0.AddMinutes(1));
        await plans.UpdateAsync(plan);

        var version = PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.Monthly, true, [], T0);
        version.Publish(T0);
        await plans.AddVersionAsync(version);

        var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(7), plan.Id);
        await trials.AddAsync(trial);

        var start = await new StartTrialSubscription(orgs, products, plans, trials, subscriptions, uow, clock)
            .ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id);
        Assert.False(start.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SubscriptionIneligible, start.ErrorCode);
    }

    [Fact]
    public async Task CreateProduct_and_plan_publish_version_and_start_trial_snapshot()
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

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Group", "acme-group")).Value!;

        var productResult = await new CreateProduct(products, uow, clock)
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

        var planResult = await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "utang-trial", "Utang Trial");
        Assert.True(planResult.IsSuccess);
        planResult.Value!.Activate(T0);

        var grants = new[]
        {
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditRepay), true),
            FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditCreate), true)
        };
        var versionResult = await new PublishPlanVersion(plans, features, uow, clock)
            .ExecuteAsync(planResult.Value.Id, 1, BillingPeriod.None, true, grants);
        Assert.True(versionResult.IsSuccess);
        Assert.Equal(1, plans.AddVersionCount);

        var conflictVersion = await new PublishPlanVersion(plans, features, uow, clock)
            .ExecuteAsync(planResult.Value.Id, 1, BillingPeriod.None, true, grants);
        Assert.False(conflictVersion.IsSuccess);

        var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), planResult.Value.Id);
        await trials.AddAsync(trial);

        var start = await new StartTrialSubscription(orgs, products, plans, trials, subscriptions, uow, clock)
            .ExecuteAsync(org.Id, planResult.Value.Id, versionResult.Value!.Id, trial.Id);
        Assert.True(start.IsSuccess);
        Assert.Equal(T0.AddDays(14), start.Value!.TrialEndUtc);
        Assert.Equal(1, subscriptions.AddCount);

        var snapshot = await new GenerateEntitlementSnapshot(
                subscriptions, plans, trials, overrides, snapshots, refreshPolicy, uow, clock)
            .ExecuteAsync(start.Value!.Id, expectedNextVersion: 1);
        Assert.True(snapshot.IsSuccess);
        Assert.Equal(1, snapshots.AddCount);

        var conflict = await new GenerateEntitlementSnapshot(
                subscriptions, plans, trials, overrides, snapshots, refreshPolicy, uow, clock)
            .ExecuteAsync(start.Value.Id, expectedNextVersion: 1);
        Assert.Equal(ApplicationErrorCodes.SnapshotVersionConflict, conflict.ErrorCode);
        Assert.Equal(1, snapshots.AddCount);
    }

    [Fact]
    public async Task CreateProduct_duplicate_does_not_persist_second()
    {
        var products = new InMemoryProductRepository();
        var create = new CreateProduct(products, new NoOpUnitOfWork(), new FixedClock(T0));
        Assert.True((await create.ExecuteAsync("other-product", "Other Product")).IsSuccess);
        var dup = await create.ExecuteAsync("Other-Product", "Other Product Two");
        Assert.Equal(ApplicationErrorCodes.DuplicateProductCode, dup.ErrorCode);
        Assert.Equal(1, products.AddCount);
    }

    [Fact]
    public async Task Suspend_and_cancel_subscription_use_cases()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var products = new InMemoryProductRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Group", "acme-group")).Value!;

        await new CreateProduct(products, uow, clock).ExecuteAsync(ProductCode.PinoyBusinessPos, "POS");
        var pc = ProductCode.Create(ProductCode.PinoyBusinessPos);
        await features.AddAsync(FeatureDefinition.Create(
            pc, FeatureCode.Create(FeatureCode.CustomerCreditView), "View", FeatureValueType.Boolean, T0));

        var plan = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "utang", "Utang")).Value!;
        plan.Activate(T0);
        var version = (await new PublishPlanVersion(plans, features, uow, clock)
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true,
                new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) }))
            .Value!;
        var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), plan.Id);
        await trials.AddAsync(trial);
        var sub = (await new StartTrialSubscription(orgs, products, plans, trials, subscriptions, uow, clock)
            .ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id)).Value!;

        Assert.True((await new SuspendSubscription(subscriptions, uow, clock).ExecuteAsync(sub.Id)).IsSuccess);
        Assert.Equal(SubscriptionStatus.Suspended, sub.Status);
        Assert.True((await new CancelSubscription(subscriptions, uow, clock).ExecuteAsync(sub.Id)).IsSuccess);
        Assert.Equal(SubscriptionStatus.Cancelled, sub.Status);
    }

    [Fact]
    public async Task StartTrialSubscription_rejects_missing_or_ineligible_organization()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var products = new InMemoryProductRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();

        await new CreateProduct(products, uow, clock).ExecuteAsync(ProductCode.PinoyBusinessPos, "POS");
        var pc = ProductCode.Create(ProductCode.PinoyBusinessPos);
        await features.AddAsync(FeatureDefinition.Create(
            pc, FeatureCode.Create(FeatureCode.CustomerCreditView), "View", FeatureValueType.Boolean, T0));
        var plan = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "utang", "Utang")).Value!;
        plan.Activate(T0);
        var version = (await new PublishPlanVersion(plans, features, uow, clock)
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true,
                new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) }))
            .Value!;
        var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), plan.Id);
        await trials.AddAsync(trial);

        var startTrial = new StartTrialSubscription(orgs, products, plans, trials, subscriptions, uow, clock);

        var missingOrg = await startTrial.ExecuteAsync(PlatformOrganizationId.New(), plan.Id, version.Id, trial.Id);
        Assert.Equal(ApplicationErrorCodes.OrganizationNotFound, missingOrg.ErrorCode);

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Group", "acme-group")).Value!;
        org.Suspend(clock.UtcNow);
        await orgs.UpdateAsync(org);

        var suspendedOrg = await startTrial.ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id);
        Assert.Equal(ApplicationErrorCodes.OrganizationNotEligible, suspendedOrg.ErrorCode);
        Assert.Equal(0, subscriptions.AddCount);
    }

    [Fact]
    public async Task StartTrialSubscription_rejects_second_active_like_subscription_for_same_product()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var products = new InMemoryProductRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Group", "acme-group")).Value!;
        await new CreateProduct(products, uow, clock).ExecuteAsync(ProductCode.PinoyBusinessPos, "POS");
        var pc = ProductCode.Create(ProductCode.PinoyBusinessPos);
        await features.AddAsync(FeatureDefinition.Create(
            pc, FeatureCode.Create(FeatureCode.CustomerCreditView), "View", FeatureValueType.Boolean, T0));
        var plan = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "utang", "Utang")).Value!;
        plan.Activate(T0);
        var version = (await new PublishPlanVersion(plans, features, uow, clock)
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true,
                new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) }))
            .Value!;
        var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), plan.Id);
        await trials.AddAsync(trial);

        var startTrial = new StartTrialSubscription(orgs, products, plans, trials, subscriptions, uow, clock);
        var first = await startTrial.ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id);
        Assert.True(first.IsSuccess);

        var second = await startTrial.ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id);
        Assert.True(
            second.ErrorCode is ApplicationErrorCodes.ActiveSubscriptionConflict
                or ApplicationErrorCodes.TrialAlreadyConsumed,
            $"Expected active-conflict or trial-consumed, got {second.ErrorCode}");
        Assert.Equal(1, subscriptions.AddCount);
    }

    [Fact]
    public async Task StartTrial_rejects_trial_definition_for_a_different_plan()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var products = new InMemoryProductRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Group", "acme-cross-plan")).Value!;
        await new CreateProduct(products, uow, clock).ExecuteAsync(ProductCode.PinoyBusinessPos, "POS");
        var pc = ProductCode.Create(ProductCode.PinoyBusinessPos);
        await features.AddAsync(FeatureDefinition.Create(
            pc, FeatureCode.Create(FeatureCode.CustomerCreditView), "View", FeatureValueType.Boolean, T0));

        var planA = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "starter-a", "Starter A")).Value!;
        planA.Activate(T0);
        var planB = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "growth-b", "Growth B")).Value!;
        planB.Activate(T0);

        var versionA = (await new PublishPlanVersion(plans, features, uow, clock)
            .ExecuteAsync(planA.Id, 1, BillingPeriod.Monthly, true,
                new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) }))
            .Value!;
        var trialB = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), planB.Id);
        await trials.AddAsync(trialB);

        var start = await new StartTrialSubscription(orgs, products, plans, trials, subscriptions, uow, clock)
            .ExecuteAsync(org.Id, planA.Id, versionA.Id, trialB.Id);
        Assert.False(start.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SubscriptionIneligible, start.ErrorCode);
        Assert.Equal(0, subscriptions.AddCount);
    }

    [Fact]
    public async Task StartTrial_accepts_matching_plan_trial_and_product_wide_trial()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var products = new InMemoryProductRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var plans = new InMemoryPlanRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();

        var orgA = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Match", "acme-match")).Value!;
        var orgB = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Wide", "acme-wide")).Value!;
        await new CreateProduct(products, uow, clock).ExecuteAsync(ProductCode.PinoyBusinessPos, "POS");
        var pc = ProductCode.Create(ProductCode.PinoyBusinessPos);
        await features.AddAsync(FeatureDefinition.Create(
            pc, FeatureCode.Create(FeatureCode.CustomerCreditView), "View", FeatureValueType.Boolean, T0));

        var plan = (await new CreatePlan(products, plans, uow, clock)
            .ExecuteAsync(ProductCode.PinoyBusinessPos, "growth-bind", "Growth Bind")).Value!;
        plan.Activate(T0);
        var version = (await new PublishPlanVersion(plans, features, uow, clock)
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true,
                new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) }))
            .Value!;
        var matching = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), plan.Id);
        var productWide = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), planId: null);
        await trials.AddAsync(matching);
        await trials.AddAsync(productWide);

        var startTrial = new StartTrialSubscription(orgs, products, plans, trials, subscriptions, uow, clock);
        var matched = await startTrial.ExecuteAsync(orgA.Id, plan.Id, version.Id, matching.Id);
        Assert.True(matched.IsSuccess);
        Assert.Equal(matching.Id, matched.Value!.TrialDefinitionId);

        var wide = await startTrial.ExecuteAsync(orgB.Id, plan.Id, version.Id, productWide.Id);
        Assert.True(wide.IsSuccess);
        Assert.Equal(productWide.Id, wide.Value!.TrialDefinitionId);
        Assert.Equal(2, subscriptions.AddCount);

        var duplicate = await startTrial.ExecuteAsync(orgA.Id, plan.Id, version.Id, matching.Id);
        Assert.True(
            duplicate.ErrorCode is ApplicationErrorCodes.ActiveSubscriptionConflict
                or ApplicationErrorCodes.TrialAlreadyConsumed,
            $"Expected active-conflict or trial-consumed, got {duplicate.ErrorCode}");
        Assert.Equal(2, subscriptions.AddCount);
    }

    [Fact]
    public async Task Create_and_revoke_feature_override()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var overrides = new InMemoryFeatureOverrideRepository();
        var pc = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var fc = FeatureCode.Create(FeatureCode.CustomerCreditCreate);
        await features.AddAsync(FeatureDefinition.Create(pc, fc, "Create", FeatureValueType.Boolean, T0));

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Acme Group", "acme-group")).Value!;

        var revokedBy = PlatformUserId.New();
        var created = await new CreateFeatureOverride(orgs, features, overrides, uow, clock).ExecuteAsync(
            org.Id,
            pc,
            fc,
            enabled: false,
            reason: "Support hold",
            createdByUserId: PlatformUserId.New());
        Assert.True(created.IsSuccess);
        Assert.Equal(1, overrides.AddCount);

        var revoked = await new RevokeFeatureOverride(overrides, uow, clock)
            .ExecuteAsync(created.Value!.Id, "No longer needed", revokedBy);
        Assert.True(revoked.IsSuccess);
        Assert.Equal(FeatureOverrideStatus.Revoked, revoked.Value!.Status);
    }
}
