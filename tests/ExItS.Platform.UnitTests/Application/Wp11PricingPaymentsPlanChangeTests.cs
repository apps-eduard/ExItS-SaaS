using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Application;

public sealed class Wp11PricingPaymentsPlanChangeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 3, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Plan_commercial_pricing_and_currency_persist()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var update = new UpdatePlanCommercialPackage(ctx.Plans, ctx.UnitOfWork, ctx.Clock);
        var result = await update.ExecuteAsync(
            ctx.StarterPlan.Id,
            "Starter Plus",
            description: null,
            maxBranches: 1,
            maxActiveStaff: 3,
            customerCreditEnabled: false,
            advancedReportsEnabled: false,
            exportEnabled: false,
            trialAllowed: true,
            defaultTrialDays: 14,
            sortOrder: 10,
            monthlyPrice: 499m,
            annualPrice: 4990m,
            currencyCode: "PHP");
        Assert.True(result.IsSuccess);
        Assert.Equal(499m, result.Value!.MonthlyPrice);
        Assert.Equal(4990m, result.Value.AnnualPrice);
        Assert.Equal("PHP", result.Value.CurrencyCode);
    }

    [Fact]
    public async Task Plan_display_order_is_independent_of_price()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var update = new UpdatePlanCommercialPackage(ctx.Plans, ctx.UnitOfWork, ctx.Clock);
        await update.ExecuteAsync(
            ctx.StarterPlan.Id,
            ctx.StarterPlan.DisplayName,
            null,
            ctx.StarterPlan.MaxBranches,
            ctx.StarterPlan.MaxActiveStaff,
            ctx.StarterPlan.CustomerCreditEnabled,
            ctx.StarterPlan.AdvancedReportsEnabled,
            ctx.StarterPlan.ExportEnabled,
            ctx.StarterPlan.TrialAllowed,
            ctx.StarterPlan.DefaultTrialDays,
            sortOrder: 99,
            monthlyPrice: 100m,
            annualPrice: 1000m,
            currencyCode: "PHP");
        var plan = (await ctx.Plans.GetByIdAsync(ctx.StarterPlan.Id))!;
        Assert.Equal(99, plan.SortOrder);
        Assert.Equal(100m, plan.MonthlyPrice);
    }

    [Fact]
    public async Task Subscription_retains_agreed_price_when_plan_catalog_price_changes()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartTrialingSubscriptionAsync(ctx.StarterPlan, ctx.StarterVersion);
        var originalPrice = sub.AgreedPrice;

        var update = new UpdatePlanCommercialPackage(ctx.Plans, ctx.UnitOfWork, ctx.Clock);
        await update.ExecuteAsync(
            ctx.StarterPlan.Id,
            ctx.StarterPlan.DisplayName,
            null,
            ctx.StarterPlan.MaxBranches,
            ctx.StarterPlan.MaxActiveStaff,
            ctx.StarterPlan.CustomerCreditEnabled,
            ctx.StarterPlan.AdvancedReportsEnabled,
            ctx.StarterPlan.ExportEnabled,
            ctx.StarterPlan.TrialAllowed,
            ctx.StarterPlan.DefaultTrialDays,
            sortOrder: ctx.StarterPlan.SortOrder,
            monthlyPrice: 9999m,
            annualPrice: 99999m,
            currencyCode: "PHP");

        var reloaded = (await ctx.Subscriptions.GetByIdAsync(sub.Id))!;
        Assert.Equal(originalPrice, reloaded.AgreedPrice);
    }

    [Fact]
    public async Task Paid_subscription_selects_monthly_catalog_price()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        await SetStarterPricingAsync(ctx, 499m, 4990m);
        var result = await ActivatePaidAsync(ctx, BillingCycle.Monthly);
        Assert.True(result.IsSuccess);
        Assert.Equal(499m, result.Value!.AgreedPrice);
    }

    [Fact]
    public async Task Paid_subscription_selects_annual_catalog_price()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        await SetStarterPricingAsync(ctx, 499m, 4990m);
        var result = await ActivatePaidAsync(ctx, BillingCycle.Annual);
        Assert.True(result.IsSuccess);
        Assert.Equal(4990m, result.Value!.AgreedPrice);
    }

    private static async Task SetStarterPricingAsync(Wp11CommercialHarness ctx, decimal monthly, decimal annual)
    {
        var update = new UpdatePlanCommercialPackage(ctx.Plans, ctx.UnitOfWork, ctx.Clock);
        await update.ExecuteAsync(
            ctx.StarterPlan.Id,
            ctx.StarterPlan.DisplayName,
            null,
            1, 3, false, false, false, true, 14, 10, monthly, annual, "PHP");
        ctx.StarterPlan.Activate(T0);
        await ctx.Plans.UpdateAsync(ctx.StarterPlan);
    }

    private static async Task<ApplicationResult<Subscription>> ActivatePaidAsync(
        Wp11CommercialHarness ctx,
        BillingCycle cycle)
    {
        var activate = new ActivatePaidSubscription(
            ctx.Organizations, ctx.Products, ctx.Plans, ctx.Subscriptions, ctx.UnitOfWork, ctx.Clock);
        return await activate.ExecuteAsync(
            ctx.Organization.Id,
            ctx.StarterPlan.Id,
            ctx.StarterVersion.Id,
            T0,
            cycle == BillingCycle.Monthly ? T0.AddMonths(1) : T0.AddYears(1),
            cycle);
    }

    [Fact]
    public async Task Simulated_payment_success_activates_trialing_subscription()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartTrialingSubscriptionAsync(ctx.StarterPlan, ctx.StarterVersion);
        var initial = new ProcessSubscriptionInitialPayment(
            ctx.Subscriptions, ctx.Plans, ctx.PaymentProvider, ctx.GenerateSnapshot, ctx.UnitOfWork, ctx.Clock);
        var key = Guid.NewGuid().ToString("N");
        var charge = new PaymentChargeRequest(
            ctx.Organization.Id.Value, sub.Id.Value, 499m, "PHP", key, "initial");
        await ctx.PaymentProvider.SimulateAsync("Succeeded", charge);
        var result = await initial.ExecuteAsync(sub.Id, BillingCycle.Monthly, key);
        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionStatus.Active, result.Value.Subscription.Status);
    }

    [Fact]
    public async Task Simulated_decline_does_not_activate_paid_subscription()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartTrialingSubscriptionAsync(ctx.StarterPlan, ctx.StarterVersion);
        var initial = new ProcessSubscriptionInitialPayment(
            ctx.Subscriptions, ctx.Plans, ctx.PaymentProvider, ctx.GenerateSnapshot, ctx.UnitOfWork, ctx.Clock);
        var key = Guid.NewGuid().ToString("N");
        var charge = new PaymentChargeRequest(
            ctx.Organization.Id.Value, sub.Id.Value, 499m, "PHP", key, "initial");
        await ctx.PaymentProvider.SimulateAsync("Declined", charge);
        var result = await initial.ExecuteAsync(sub.Id, BillingCycle.Monthly, key);
        Assert.False(result.IsSuccess);
        var reloaded = (await ctx.Subscriptions.GetByIdAsync(sub.Id))!;
        Assert.Equal(SubscriptionStatus.Trialing, reloaded.Status);
    }

    [Theory]
    [InlineData("Pending", PaymentProviderResultStatus.Pending)]
    [InlineData("Failed", PaymentProviderResultStatus.Failed)]
    public async Task Simulated_pending_and_failed_statuses_are_represented(string simulation, PaymentProviderResultStatus expected)
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartTrialingSubscriptionAsync(ctx.StarterPlan, ctx.StarterVersion);
        var key = Guid.NewGuid().ToString("N");
        var charge = new PaymentChargeRequest(
            ctx.Organization.Id.Value, sub.Id.Value, 499m, "PHP", key, "test");
        var result = await ctx.PaymentProvider.SimulateAsync(simulation, charge);
        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public async Task Duplicate_payment_event_is_idempotent()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartTrialingSubscriptionAsync(ctx.StarterPlan, ctx.StarterVersion);
        var key = Guid.NewGuid().ToString("N");
        var charge = new PaymentChargeRequest(
            ctx.Organization.Id.Value, sub.Id.Value, 499m, "PHP", key, "test");
        var first = await ctx.PaymentProvider.SimulateAsync("Succeeded", charge);
        var second = await ctx.PaymentProvider.SimulateAsync("Succeeded", charge);
        Assert.Equal(first.ProviderReference, second.ProviderReference);
        Assert.Equal(first.Status, second.Status);
    }

    [Fact]
    public async Task Renewal_success_extends_paid_period()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartActivePaidSubscriptionAsync(ctx.BusinessPlan, ctx.BusinessVersion, BillingCycle.Monthly);
        var periodEnd = sub.PaidPeriodEndUtc!.Value;
        var renewal = new ProcessSubscriptionRenewal(
            ctx.Subscriptions, ctx.Plans, ctx.PaymentProvider, ctx.GenerateSnapshot, ctx.UnitOfWork, ctx.Clock);
        var key = Guid.NewGuid().ToString("N");
        await ctx.PaymentProvider.SimulateAsync("RenewalSucceeded", new PaymentChargeRequest(
            ctx.Organization.Id.Value, sub.Id.Value, sub.AgreedPrice!.Value, "PHP", key, "renewal"));
        var result = await renewal.ExecuteAsync(sub.Id, key);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Subscription.PaidPeriodEndUtc > periodEnd);
    }

    [Fact]
    public async Task Renewal_failure_marks_subscription_past_due()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartActivePaidSubscriptionAsync(ctx.BusinessPlan, ctx.BusinessVersion, BillingCycle.Monthly);
        var renewal = new ProcessSubscriptionRenewal(
            ctx.Subscriptions, ctx.Plans, ctx.PaymentProvider, ctx.GenerateSnapshot, ctx.UnitOfWork, ctx.Clock);
        var key = Guid.NewGuid().ToString("N");
        await ctx.PaymentProvider.SimulateAsync("RenewalFailed", new PaymentChargeRequest(
            ctx.Organization.Id.Value, sub.Id.Value, sub.AgreedPrice!.Value, "PHP", key, "renewal"));
        var result = await renewal.ExecuteAsync(sub.Id, key);
        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionStatus.PastDue, result.Value.Subscription.Status);
    }

    [Fact]
    public async Task Trialing_subscription_generates_entitlement_snapshot()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartTrialingSubscriptionAsync(ctx.StarterPlan, ctx.StarterVersion);
        var snapshot = await ctx.GenerateSnapshot.ExecuteAsync(sub.Id, expectedNextVersion: 1);
        Assert.True(snapshot.IsSuccess);
        Assert.Equal(SubscriptionStatus.Trialing, snapshot.Value!.SubscriptionStatus);
    }

    [Fact]
    public async Task Expired_trial_subscription_is_not_active_like()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartTrialingSubscriptionAsync(ctx.StarterPlan, ctx.StarterVersion);
        sub.Expire(T0.AddDays(15));
        await ctx.Subscriptions.UpdateAsync(sub);
        Assert.False(Subscription.IsActiveLike(sub.Status));
        Assert.Equal(SubscriptionStatus.Expired, sub.Status);
    }

    [Fact]
    public async Task Upgrade_applies_plan_price_and_entitlement_revision()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartTrialingSubscriptionAsync(ctx.StarterPlan, ctx.StarterVersion);
        await ctx.GenerateSnapshot.ExecuteAsync(sub.Id, expectedNextVersion: 1);
        var upgrade = new UpgradeOrganizationSubscription(
            ctx.Organizations, ctx.Products, ctx.Plans, ctx.Subscriptions,
            ctx.PaymentProvider, ctx.GenerateSnapshot, ctx.UnitOfWork, ctx.Clock);
        var result = await upgrade.ExecuteAsync(
            ctx.Organization.Id,
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            ctx.BusinessPlan.Id,
            BillingCycle.Monthly,
            idempotencyKey: null,
            skipPaymentWhenTrialing: true);
        Assert.True(result.IsSuccess);
        Assert.Equal(ctx.BusinessPlan.Id, result.Value!.PlanId);
        Assert.Equal(ctx.BusinessPlan.MonthlyPrice, result.Value.AgreedPrice);
    }

    [Fact]
    public async Task Downgrade_schedules_pending_plan_without_deleting_subscription()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        var sub = await ctx.StartTrialingSubscriptionAsync(ctx.BusinessPlan, ctx.BusinessVersion);
        var downgrade = new ScheduleOrganizationSubscriptionDowngrade(
            ctx.Subscriptions, ctx.Plans, ctx.UnitOfWork, ctx.Clock);
        var effective = T0.AddMonths(1);
        var result = await downgrade.ExecuteAsync(
            ctx.Organization.Id,
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            ctx.StarterPlan.Id,
            effective);
        Assert.True(result.IsSuccess);
        Assert.Equal(ctx.StarterPlan.Id, result.Value!.PendingPlanId);
        Assert.Equal(effective, result.Value.PendingPlanEffectiveAtUtc);
        Assert.Equal(ctx.BusinessPlan.Id, result.Value.PlanId);
    }

    [Fact]
    public void Downgrade_preview_shows_usage_conflicts_when_limits_exceeded()
    {
        var utc = T0;
        var current = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("biz-preview"),
            "Business Preview",
            utc,
            description: "d",
            maxBranches: 3,
            maxActiveStaff: 15,
            customerCreditEnabled: true,
            advancedReportsEnabled: true,
            exportEnabled: true,
            trialAllowed: true,
            defaultTrialDays: 14,
            sortOrder: 20,
            monthlyPrice: 699m,
            annualPrice: 6990m,
            currencyCode: "PHP");
        var target = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("starter-preview"),
            "Starter Preview",
            utc,
            description: "d",
            maxBranches: 1,
            maxActiveStaff: 3,
            customerCreditEnabled: false,
            advancedReportsEnabled: false,
            exportEnabled: false,
            trialAllowed: true,
            defaultTrialDays: 14,
            sortOrder: 10,
            monthlyPrice: 299m,
            annualPrice: 2990m,
            currencyCode: "PHP");
        var preview = PlanChangeImpact.Evaluate(current, target, activeBranchCount: 2, activeStaffCount: 8);
        Assert.True(preview.HasBlockingUsageConflicts);
        Assert.Contains(preview.UsageConflicts, c => c.Resource == "Branches");
        Assert.Contains(preview.UsageConflicts, c => c.Resource == "ActiveStaff");
        Assert.Contains(preview.LostFeatures, f => f.Contains("Customer credit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Plan_list_sorts_by_display_order_before_pagination()
    {
        var ctx = await Wp11CommercialHarness.CreateAsync(T0);
        ctx.ProPlan.Activate(T0);
        await ctx.Plans.UpdateAsync(ctx.ProPlan);
        var (items, total) = await ctx.Plans.ListAsync(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanStatus.Active,
            search: null,
            CatalogListSortBy.SortOrder,
            sortDescending: false,
            skip: 0,
            take: 2);
        Assert.Equal(3, total);
        Assert.Equal(2, items.Count);
        Assert.True(items[0].SortOrder <= items[1].SortOrder);
    }

    [Fact]
    public void LocalValidation_payment_provider_is_forbidden_in_production()
    {
        var root = FindRepoRoot();
        var source = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Infrastructure", "Payments", "PaymentProviderServiceCollectionExtensions.cs"));
        Assert.Contains("forbidden in Production", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IsProduction()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_admin_ui_has_no_personal_or_organization_account_creation_buttons()
    {
        var root = FindRepoRoot();
        var users = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Users.razor"));
        var orgs = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Organizations.razor"));
        Assert.Contains("CanCreatePlatformStaff", users, StringComparison.Ordinal);
        Assert.DoesNotContain("Create Personal", users, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Create Organization account", users, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CanManageLifecycle", orgs, StringComparison.Ordinal);
        Assert.DoesNotContain("assign Plan to Personal", File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "PersonalStartBusiness.razor")), StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class Wp11CommercialHarness
    {
        public FixedClock Clock { get; }
        public NoOpUnitOfWork UnitOfWork { get; } = new();
        public InMemoryPlatformOrganizationRepository Organizations { get; } = new();
        public InMemoryProductRepository Products { get; } = new();
        public InMemoryFeatureDefinitionRepository Features { get; } = new();
        public InMemoryPlanRepository Plans { get; } = new();
        public InMemoryTrialDefinitionRepository Trials { get; } = new();
        public InMemorySubscriptionRepository Subscriptions { get; } = new();
        public InMemoryFeatureOverrideRepository Overrides { get; } = new();
        public InMemoryEntitlementSnapshotRepository Snapshots { get; } = new();
        public InMemoryProviderPaymentRepository ProviderPayments { get; } = new();
        public FakeLocalValidationPaymentProvider PaymentProvider { get; }
        public GenerateEntitlementSnapshot GenerateSnapshot { get; }
        public PlatformOrganization Organization { get; private set; } = null!;
        public Plan StarterPlan { get; private set; } = null!;
        public Plan BusinessPlan { get; private set; } = null!;
        public Plan ProPlan { get; private set; } = null!;
        public PlanVersion StarterVersion { get; private set; } = null!;
        public PlanVersion BusinessVersion { get; private set; } = null!;
        public TrialDefinition Trial { get; private set; } = null!;

        private Wp11CommercialHarness(DateTimeOffset utcNow)
        {
            Clock = new FixedClock(utcNow);
            PaymentProvider = new FakeLocalValidationPaymentProvider(ProviderPayments, Clock);
            GenerateSnapshot = new GenerateEntitlementSnapshot(
                Subscriptions, Plans, Trials, Overrides, Snapshots,
                new ProvisionalEntitlementRefreshPolicy(), UnitOfWork, Clock);
        }

        public static async Task<Wp11CommercialHarness> CreateAsync(DateTimeOffset utcNow)
        {
            var ctx = new Wp11CommercialHarness(utcNow);
            ctx.Organization = (await new CreatePlatformOrganization(ctx.Organizations, ctx.UnitOfWork, ctx.Clock)
                .ExecuteAsync("ABC Store", "abc-store")).Value!;
            await new CreateProduct(ctx.Products, ctx.UnitOfWork, ctx.Clock)
                .ExecuteAsync(ProductCode.PinoyBusinessPos, "Pinoy Business POS");
            var pc = ProductCode.Create(ProductCode.PinoyBusinessPos);
            await ctx.Features.AddAsync(FeatureDefinition.Create(
                pc, FeatureCode.Create(FeatureCode.CustomerCreditView), "View", FeatureValueType.Boolean, utcNow));

            ctx.StarterPlan = await ctx.CreatePricedPlanAsync(MvpPosPlanCodes.Starter, "Starter", 10, 499m, 4990m);
            ctx.BusinessPlan = await ctx.CreatePricedPlanAsync(MvpPosPlanCodes.Business, "Business", 20, 999m, 9990m);
            ctx.ProPlan = await ctx.CreatePricedPlanAsync(MvpPosPlanCodes.Pro, "Pro", 30, 1999m, 19990m);
            ctx.StarterVersion = await ctx.PublishVersionAsync(ctx.StarterPlan);
            ctx.BusinessVersion = await ctx.PublishVersionAsync(ctx.BusinessPlan);
            await ctx.PublishVersionAsync(ctx.ProPlan);
            ctx.Trial = UtangTrialTestFactory.CreateConfigured(utcNow, TimeSpan.FromDays(14), ctx.StarterPlan.Id);
            await ctx.Trials.AddAsync(ctx.Trial);
            return ctx;
        }

        private async Task<Plan> CreatePricedPlanAsync(string code, string name, int sortOrder, decimal monthly, decimal annual)
        {
            var plan = (await new CreatePlan(Products, Plans, UnitOfWork, Clock)
                .ExecuteAsync(
                    ProductCode.PinoyBusinessPos,
                    code,
                    name,
                    description: null,
                    maxBranches: 1,
                    maxActiveStaff: 3,
                    customerCreditEnabled: false,
                    advancedReportsEnabled: false,
                    exportEnabled: false,
                    trialAllowed: true,
                    defaultTrialDays: 14,
                    sortOrder: sortOrder,
                    monthlyPrice: monthly,
                    annualPrice: annual,
                    currencyCode: "PHP")).Value!;
            plan.Activate(Clock.UtcNow);
            await Plans.UpdateAsync(plan);
            return plan;
        }

        private async Task<PlanVersion> PublishVersionAsync(Plan plan)
        {
            var version = (await new PublishPlanVersion(Plans, Features, UnitOfWork, Clock)
                .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true,
                    new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) })).Value!;
            return version;
        }

        public async Task<Subscription> StartTrialingSubscriptionAsync(Plan plan, PlanVersion version)
        {
            var start = new StartTrialSubscription(
                Organizations, Products, Plans, Trials, Subscriptions, UnitOfWork, Clock);
            return (await start.ExecuteAsync(Organization.Id, plan.Id, version.Id, Trial.Id)).Value!;
        }

        public async Task<Subscription> StartActivePaidSubscriptionAsync(Plan plan, PlanVersion version, BillingCycle cycle)
        {
            var end = cycle == BillingCycle.Monthly ? T0.AddMonths(1) : T0.AddYears(1);
            return (await new ActivatePaidSubscription(
                Organizations, Products, Plans, Subscriptions, UnitOfWork, Clock)
                .ExecuteAsync(Organization.Id, plan.Id, version.Id, T0, end, cycle)).Value!;
        }
    }
}
