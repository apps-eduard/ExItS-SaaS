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
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.Payments;

public sealed class SaaSPaymentFundingIntegrityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private sealed class Harness
    {
        public FixedClock Clock { get; } = new(T0);
        public NoOpUnitOfWork UnitOfWork { get; } = new();
        public InMemoryPlatformOrganizationRepository Organizations { get; } = new();
        public InMemoryProductRepository Products { get; } = new();
        public InMemoryFeatureDefinitionRepository Features { get; } = new();
        public InMemoryPlanRepository Plans { get; } = new();
        public InMemoryTrialDefinitionRepository Trials { get; } = new();
        public InMemorySubscriptionRepository Subscriptions { get; } = new();
        public InMemorySaaSPaymentRepository Payments { get; } = new();
        public InMemoryFeatureOverrideRepository Overrides { get; } = new();
        public InMemoryEntitlementSnapshotRepository Snapshots { get; } = new();
        public GenerateEntitlementSnapshot GenerateSnapshot { get; }
        public PlatformOrganization Organization { get; private set; } = null!;
        public Plan GrowthPlan { get; private set; } = null!;
        public Plan ProPlan { get; private set; } = null!;
        public PlanVersion GrowthVersion { get; private set; } = null!;
        public TrialDefinition Trial { get; private set; } = null!;
        public ProductCode ProductCode { get; private set; } = null!;

        public Harness()
        {
            GenerateSnapshot = new GenerateEntitlementSnapshot(
                Subscriptions,
                Plans,
                Trials,
                Overrides,
                Snapshots,
                new ProvisionalEntitlementRefreshPolicy(),
                UnitOfWork,
                Clock);
        }

        public static async Task<Harness> CreateAsync()
        {
            var ctx = new Harness();
            ctx.Organization = (await new CreatePlatformOrganization(
                    ctx.Organizations,
                    new FakePublicOrganizationIdGenerator(),
                    ctx.UnitOfWork,
                    ctx.Clock)
                .ExecuteAsync("Acme Store", "acme-store")).Value!;
            await new CreateProduct(ctx.Products, ctx.UnitOfWork, ctx.Clock)
                .ExecuteAsync(ProductCode.PinoyBusinessPos, "Pinoy Business POS");
            ctx.ProductCode = ProductCode.Create(ProductCode.PinoyBusinessPos);
            await ctx.Features.AddAsync(FeatureDefinition.Create(
                ctx.ProductCode,
                FeatureCode.Create(FeatureCode.CustomerCreditView),
                "View",
                FeatureValueType.Boolean,
                T0));

            ctx.GrowthPlan = await ctx.CreatePricedPlanAsync(MvpPosPlanCodes.Growth, "Growth", 999m, 9990m, 3);
            ctx.ProPlan = await ctx.CreatePricedPlanAsync(MvpPosPlanCodes.Pro, "Pro", 1999m, 19990m, 10);
            ctx.GrowthVersion = await ctx.PublishVersionAsync(ctx.GrowthPlan);
            await ctx.PublishVersionAsync(ctx.ProPlan);
            ctx.Trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), ctx.GrowthPlan.Id);
            await ctx.Trials.AddAsync(ctx.Trial);
            return ctx;
        }

        private async Task<Plan> CreatePricedPlanAsync(
            string code,
            string name,
            decimal monthly,
            decimal annual,
            int maxDevices)
        {
            var plan = (await new CreatePlan(Products, Plans, UnitOfWork, Clock)
                .ExecuteAsync(
                    ProductCode.PinoyBusinessPos,
                    code,
                    name,
                    description: null,
                    maxBranches: 1,
                    maxActiveStaff: 3,
                    maxActivePosDevices: maxDevices,
                    maxActiveBusinessTypes: 3,
                    customerCreditEnabled: false,
                    advancedReportsEnabled: false,
                    exportEnabled: false,
                    trialAllowed: true,
                    defaultTrialDays: 14,
                    sortOrder: 20,
                    monthlyPrice: monthly,
                    annualPrice: annual,
                    currencyCode: "PHP")).Value!;
            plan.Activate(Clock.UtcNow);
            await Plans.UpdateAsync(plan);
            return plan;
        }

        private async Task<PlanVersion> PublishVersionAsync(Plan plan)
        {
            return (await new PublishPlanVersion(Plans, Features, UnitOfWork, Clock)
                .ExecuteAsync(
                    plan.Id,
                    1,
                    BillingPeriod.Monthly,
                    true,
                    new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) }))
                .Value!;
        }

        public async Task<Subscription> StartTrialingAsync()
        {
            return (await new StartTrialSubscription(
                    Organizations,
                    Products,
                    Plans,
                    Trials,
                    Subscriptions,
                    UnitOfWork,
                    Clock)
                .ExecuteAsync(Organization.Id, GrowthPlan.Id, GrowthVersion.Id, Trial.Id)).Value!;
        }

        public async Task<Subscription> StartActiveGrowthAsync(BillingCycle cycle = BillingCycle.Monthly)
        {
            var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, cycle);
            return (await new ActivatePaidSubscription(
                    Organizations,
                    Products,
                    Plans,
                    Subscriptions,
                    UnitOfWork,
                    Clock)
                .ExecuteAsync(Organization.Id, GrowthPlan.Id, GrowthVersion.Id, start, end, cycle)).Value!;
        }

        public ConfirmPaymentAndActivateSubscription CreateActivateFromPayment() =>
            new(Payments, Subscriptions, Plans, GenerateSnapshot, UnitOfWork, Clock);

        public ActivatePaidSubscriptionFromConfirmedPayment CreateActivatePaidFromPayment() =>
            new(
                Payments,
                Subscriptions,
                Plans,
                new ActivatePaidSubscription(Organizations, Products, Plans, Subscriptions, UnitOfWork, Clock),
                GenerateSnapshot,
                UnitOfWork,
                Clock);

        public UpgradeSubscriptionFromConfirmedPayment CreateUpgradeFromPayment() =>
            new(Organizations, Plans, Payments, Subscriptions, GenerateSnapshot, UnitOfWork, Clock);

        public async Task<SaaSPayment> AddPaymentAsync(
            decimal amount,
            string currency = CurrencyCode.PHP,
            SaaSPaymentStatus status = SaaSPaymentStatus.PendingConfirmation,
            ProductCode? productCode = null,
            PlatformOrganizationId? organizationId = null,
            SubscriptionId? linkedSubscriptionId = null)
        {
            var payment = SaaSPayment.CreateManual(
                organizationId ?? Organization.Id,
                productCode ?? ProductCode,
                amount,
                CurrencyCode.Create(currency),
                SaaSPaymentMethod.GCash,
                $"REF-{Guid.NewGuid():N}"[..16],
                T0,
                T0);
            if (status == SaaSPaymentStatus.Confirmed)
            {
                payment.Confirm("staff-confirm", T0.AddMinutes(1));
            }
            else if (status == SaaSPaymentStatus.Rejected)
            {
                payment.Reject("staff-reject", "bad ref", T0.AddMinutes(1));
            }
            else if (status == SaaSPaymentStatus.Voided)
            {
                payment.Confirm("staff-confirm", T0.AddMinutes(1));
                payment.Void("staff-void", "voided", T0.AddMinutes(2));
            }

            await Payments.AddAsync(payment);
            if (linkedSubscriptionId is not null)
            {
                payment.LinkSubscription(linkedSubscriptionId, T0.AddMinutes(3));
                await Payments.UpdateAsync(payment);
            }

            return payment;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(998)]
    public async Task Underpayment_cannot_activate_trialing_subscription(decimal amount)
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(amount, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentAmountMismatch, result.ErrorCode);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.Null(payment.SubscriptionId);
    }

    [Fact]
    public async Task Wrong_currency_cannot_activate_trialing_subscription()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(999m, "USD", SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentCurrencyMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Exact_monthly_price_activates_trialing_subscription()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(999m, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly);
        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionStatus.Active, result.Value!.Subscription.Status);
        Assert.Equal(subscription.Id, result.Value.Payment.SubscriptionId);
    }

    [Fact]
    public async Task Exact_annual_price_activates_trialing_subscription()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(9990m, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Annual);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Annual);
        Assert.True(result.IsSuccess);
        Assert.Equal(BillingCycle.Annual, result.Value!.Subscription.BillingCycle);
    }

    [Fact]
    public async Task Monthly_amount_cannot_activate_annual_subscription_when_prices_differ()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(999m, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Annual);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Annual);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentAmountMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Payment_for_another_product_is_rejected()
    {
        var ctx = await Harness.CreateAsync();
        await new CreateProduct(ctx.Products, ctx.UnitOfWork, ctx.Clock).ExecuteAsync("other-product", "Other");
        var otherProduct = ProductCode.Create("other-product");
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(999m, productCode: otherProduct, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentProductMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Cross_organization_payment_is_rejected()
    {
        var ctx = await Harness.CreateAsync();
        var otherOrg = (await new CreatePlatformOrganization(
                ctx.Organizations,
                new FakePublicOrganizationIdGenerator(),
                ctx.UnitOfWork,
                ctx.Clock)
            .ExecuteAsync("Other Org", "other-org")).Value!;
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(
            999m,
            organizationId: otherOrg.Id,
            status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentOrganizationMismatch, result.ErrorCode);
    }

    [Theory]
    [InlineData(SaaSPaymentStatus.Rejected)]
    [InlineData(SaaSPaymentStatus.Voided)]
    public async Task Terminal_payment_cannot_activate(SaaSPaymentStatus status)
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(999m, status: status);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentNotConfirmed, result.ErrorCode);
    }

    [Fact]
    public async Task Pending_payment_cannot_create_paid_subscription_without_prior_confirmation()
    {
        var ctx = await Harness.CreateAsync();
        var payment = await ctx.AddPaymentAsync(999m);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivatePaidFromPayment().ExecuteAsync(
            payment.Id,
            ctx.Organization.Id,
            ctx.GrowthPlan.Id,
            ctx.GrowthVersion.Id,
            start,
            end,
            BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentNotConfirmed, result.ErrorCode);
    }

    [Fact]
    public async Task Already_used_payment_is_rejected()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(999m, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var activate = ctx.CreateActivateFromPayment();
        Assert.True((await activate.ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly)).IsSuccess);
        var reuse = await activate.ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly);
        Assert.False(reuse.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentAlreadyUsed, reuse.ErrorCode);
    }

    [Fact]
    public async Task Exact_confirmed_payment_creates_paid_subscription()
    {
        var ctx = await Harness.CreateAsync();
        var payment = await ctx.AddPaymentAsync(999m, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivatePaidFromPayment().ExecuteAsync(
            payment.Id,
            ctx.Organization.Id,
            ctx.GrowthPlan.Id,
            ctx.GrowthVersion.Id,
            start,
            end,
            BillingCycle.Monthly);
        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionStatus.Active, result.Value!.Subscription.Status);
        Assert.Equal(payment.Id, result.Value.Payment.Id);
        Assert.Equal(result.Value.Subscription.Id, result.Value.Payment.SubscriptionId);
    }

    [Fact]
    public async Task Wrong_amount_does_not_create_paid_subscription()
    {
        var ctx = await Harness.CreateAsync();
        var payment = await ctx.AddPaymentAsync(1m, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivatePaidFromPayment().ExecuteAsync(
            payment.Id,
            ctx.Organization.Id,
            ctx.GrowthPlan.Id,
            ctx.GrowthVersion.Id,
            start,
            end,
            BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentAmountMismatch, result.ErrorCode);
        var (subscriptions, _) = await ctx.Subscriptions.ListByOrganizationAsync(ctx.Organization.Id, null, 0, 100);
        Assert.Empty(subscriptions);
        Assert.Null(payment.SubscriptionId);
    }

    [Fact]
    public async Task Entitlement_snapshot_is_generated_after_valid_activation()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(999m, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly);
        Assert.True(result.IsSuccess);
        var snapshot = await ctx.GenerateSnapshot.ExecuteAsync(ctx.Organization.Id, ctx.ProductCode);
        Assert.True(snapshot.IsSuccess);
        Assert.Equal(SubscriptionStatus.Active, snapshot.Value!.SubscriptionStatus);
    }

    [Fact]
    public async Task Active_growth_upgrades_to_pro_with_exact_pro_payment()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartActiveGrowthAsync();
        var growthId = subscription.Id;
        var payment = await ctx.AddPaymentAsync(1999m, status: SaaSPaymentStatus.Confirmed);
        var result = await ctx.CreateUpgradeFromPayment().ExecuteAsync(
            payment.Id,
            ctx.Organization.Id,
            subscription.Id,
            ctx.ProPlan.Id,
            BillingCycle.Monthly);
        Assert.True(result.IsSuccess);
        Assert.Equal(growthId, result.Value!.Subscription.Id);
        Assert.Equal(ctx.ProPlan.Id, result.Value.Subscription.PlanId);
        Assert.Equal(subscription.Id, result.Value.Payment.SubscriptionId);
    }

    [Fact]
    public async Task Active_growth_rejects_underpayment_for_pro_upgrade()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartActiveGrowthAsync();
        var payment = await ctx.AddPaymentAsync(1m, status: SaaSPaymentStatus.Confirmed);
        var result = await ctx.CreateUpgradeFromPayment().ExecuteAsync(
            payment.Id,
            ctx.Organization.Id,
            subscription.Id,
            ctx.ProPlan.Id,
            BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentAmountMismatch, result.ErrorCode);
        Assert.Equal(ctx.GrowthPlan.Id, subscription.PlanId);
    }

    [Fact]
    public async Task Growth_priced_payment_cannot_fund_pro_upgrade()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartActiveGrowthAsync();
        var payment = await ctx.AddPaymentAsync(999m, status: SaaSPaymentStatus.Confirmed);
        var result = await ctx.CreateUpgradeFromPayment().ExecuteAsync(
            payment.Id,
            ctx.Organization.Id,
            subscription.Id,
            ctx.ProPlan.Id,
            BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentAmountMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Active_upgrade_regenerates_pro_entitlement_snapshot()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartActiveGrowthAsync();
        await ctx.GenerateSnapshot.ExecuteAsync(subscription.Id);
        var payment = await ctx.AddPaymentAsync(1999m, status: SaaSPaymentStatus.Confirmed);
        var upgrade = await ctx.CreateUpgradeFromPayment().ExecuteAsync(
            payment.Id,
            ctx.Organization.Id,
            subscription.Id,
            ctx.ProPlan.Id,
            BillingCycle.Monthly);
        Assert.True(upgrade.IsSuccess);
        var snapshot = await ctx.GenerateSnapshot.ExecuteAsync(subscription.Id);
        Assert.True(snapshot.IsSuccess);
        Assert.Equal(ctx.ProPlan.Id, (await ctx.Subscriptions.GetByIdAsync(subscription.Id))!.PlanId);
    }

    [Fact]
    public async Task Invalid_paid_period_is_rejected()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartTrialingAsync();
        var payment = await ctx.AddPaymentAsync(999m, status: SaaSPaymentStatus.Confirmed);
        var result = await ctx.CreateActivateFromPayment().ExecuteAsync(
            payment.Id,
            "staff-1",
            subscription.Id,
            T0,
            T0.AddYears(20),
            BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentPeriodMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Active_subscription_cannot_use_activate_from_payment()
    {
        var ctx = await Harness.CreateAsync();
        var subscription = await ctx.StartActiveGrowthAsync();
        var payment = await ctx.AddPaymentAsync(999m, status: SaaSPaymentStatus.Confirmed);
        var (start, end) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await ctx.CreateActivateFromPayment()
            .ExecuteAsync(payment.Id, "staff-1", subscription.Id, start, end, BillingCycle.Monthly);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SubscriptionIneligible, result.ErrorCode);
    }
}
