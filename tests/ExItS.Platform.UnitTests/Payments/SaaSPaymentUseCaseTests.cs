using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Payments;

public sealed class SaaSPaymentUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private sealed class Fixture
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

        public async Task<PlatformOrganization> CreateOrganizationAsync(string name = "Acme", string slug = "acme") =>
            (await new CreatePlatformOrganization(Organizations, UnitOfWork, Clock).ExecuteAsync(name, slug)).Value!;

        public async Task<ProductCode> CreateProductAsync()
        {
            await new CreateProduct(Products, UnitOfWork, Clock).ExecuteAsync(ProductCode.PinoyBusinessPos, "POS");
            return ProductCode.Create(ProductCode.PinoyBusinessPos);
        }

        private int _planSequence;

        public async Task<Subscription> StartTrialAsync(PlatformOrganizationId orgId, ProductCode productCode)
        {
            var featureCode = FeatureCode.Create(FeatureCode.CustomerCreditView);
            if (await Features.GetByProductAndCodeAsync(productCode, featureCode) is null)
            {
                await Features.AddAsync(FeatureDefinition.Create(
                    productCode, featureCode, "View", FeatureValueType.Boolean, T0));
            }

            var planCode = $"utang-{++_planSequence}";
            var plan = (await new CreatePlan(Products, Plans, UnitOfWork, Clock)
                .ExecuteAsync(productCode.Value, planCode, "Utang")).Value!;
            plan.Activate(T0);

            var version = (await new PublishPlanVersion(Plans, Features, UnitOfWork, Clock)
                    .ExecuteAsync(
                        plan.Id,
                        1,
                        BillingPeriod.Monthly,
                        true,
                        new[] { FeatureGrantSpec.Boolean(featureCode, true) }))
                .Value!;

            var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), plan.Id);
            await Trials.AddAsync(trial);

            return (await new StartTrialSubscription(Organizations, Products, Plans, Trials, Subscriptions, UnitOfWork, Clock)
                .ExecuteAsync(orgId, plan.Id, version.Id, trial.Id)).Value!;
        }
    }

    [Fact]
    public async Task CreateManualSaaSPayment_succeeds_for_active_organization_and_existing_product()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var productCode = await fx.CreateProductAsync();

        var create = new CreateManualSaaSPayment(fx.Organizations, fx.Products, fx.Payments, fx.UnitOfWork, fx.Clock);
        var result = await create.ExecuteAsync(
            org.Id, productCode, 999m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.GCash, "REF-100", T0);

        Assert.True(result.IsSuccess);
        Assert.Equal(SaaSPaymentStatus.PendingConfirmation, result.Value!.Status);
        Assert.Equal(1, fx.Payments.AddCount);
    }

    [Fact]
    public async Task CreateManualSaaSPayment_rejects_duplicate_reference_for_same_method_and_organization()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var productCode = await fx.CreateProductAsync();

        var create = new CreateManualSaaSPayment(fx.Organizations, fx.Products, fx.Payments, fx.UnitOfWork, fx.Clock);
        var first = await create.ExecuteAsync(
            org.Id, productCode, 100m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "REF-DUP", T0);
        Assert.True(first.IsSuccess);

        var duplicate = await create.ExecuteAsync(
            org.Id, productCode, 250m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "ref-dup", T0);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentReferenceConflict, duplicate.ErrorCode);
        Assert.Equal(1, fx.Payments.AddCount);
    }

    [Fact]
    public async Task CreateManualSaaSPayment_allows_same_reference_for_a_different_method()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var productCode = await fx.CreateProductAsync();

        var create = new CreateManualSaaSPayment(fx.Organizations, fx.Products, fx.Payments, fx.UnitOfWork, fx.Clock);
        var first = await create.ExecuteAsync(
            org.Id, productCode, 100m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "REF-1", T0);
        var second = await create.ExecuteAsync(
            org.Id, productCode, 100m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.Cash, "REF-1", T0);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, fx.Payments.AddCount);
    }

    [Fact]
    public async Task CreateManualSaaSPayment_rejects_missing_organization()
    {
        var fx = new Fixture();
        var productCode = await fx.CreateProductAsync();

        var create = new CreateManualSaaSPayment(fx.Organizations, fx.Products, fx.Payments, fx.UnitOfWork, fx.Clock);
        var result = await create.ExecuteAsync(
            PlatformOrganizationId.New(), productCode, 100m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.Cash, "REF-1", T0);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationNotFound, result.ErrorCode);
        Assert.Equal(0, fx.Payments.AddCount);
    }

    [Fact]
    public async Task CreateManualSaaSPayment_rejects_suspended_organization()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        org.Suspend(fx.Clock.UtcNow);
        await fx.Organizations.UpdateAsync(org);
        var productCode = await fx.CreateProductAsync();

        var create = new CreateManualSaaSPayment(fx.Organizations, fx.Products, fx.Payments, fx.UnitOfWork, fx.Clock);
        var result = await create.ExecuteAsync(
            org.Id, productCode, 100m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.Cash, "REF-1", T0);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationNotEligible, result.ErrorCode);
    }

    [Fact]
    public async Task CreateManualSaaSPayment_rejects_missing_product()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();

        var create = new CreateManualSaaSPayment(fx.Organizations, fx.Products, fx.Payments, fx.UnitOfWork, fx.Clock);
        var result = await create.ExecuteAsync(
            org.Id, ProductCode.Create(ProductCode.PinoyBusinessPos), 100m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.Cash, "REF-1", T0);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task ConfirmSaaSPayment_transitions_pending_payment_to_confirmed()
    {
        var fx = new Fixture();
        var payment = SaaSPayment.CreateManual(
            PlatformOrganizationId.New(), ProductCode.Create(ProductCode.PinoyBusinessPos), 500m,
            CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.Cash, "REF-CONFIRM", T0, T0);
        await fx.Payments.AddAsync(payment);

        var confirm = new ConfirmSaaSPayment(fx.Payments, fx.UnitOfWork, fx.Clock);
        var result = await confirm.ExecuteAsync(payment.Id, "staff-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(SaaSPaymentStatus.Confirmed, result.Value!.Status);
        Assert.Equal(1, fx.Payments.UpdateCount);
    }

    [Fact]
    public async Task ConfirmSaaSPayment_returns_not_found_for_missing_payment()
    {
        var fx = new Fixture();
        var confirm = new ConfirmSaaSPayment(fx.Payments, fx.UnitOfWork, fx.Clock);
        var result = await confirm.ExecuteAsync(SaaSPaymentId.New(), "staff-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task RejectSaaSPayment_transitions_pending_payment_to_rejected()
    {
        var fx = new Fixture();
        var payment = SaaSPayment.CreateManual(
            PlatformOrganizationId.New(), ProductCode.Create(ProductCode.PinoyBusinessPos), 500m,
            CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.Cash, "REF-REJECT", T0, T0);
        await fx.Payments.AddAsync(payment);

        var reject = new RejectSaaSPayment(fx.Payments, fx.UnitOfWork, fx.Clock);
        var result = await reject.ExecuteAsync(payment.Id, "staff-1", "Invalid reference");

        Assert.True(result.IsSuccess);
        Assert.Equal(SaaSPaymentStatus.Rejected, result.Value!.Status);
    }

    [Fact]
    public async Task VoidSaaSPayment_transitions_confirmed_payment_to_voided()
    {
        var fx = new Fixture();
        var payment = SaaSPayment.CreateManual(
            PlatformOrganizationId.New(), ProductCode.Create(ProductCode.PinoyBusinessPos), 500m,
            CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.Cash, "REF-VOID", T0, T0);
        payment.Confirm("staff-1", T0.AddMinutes(1));
        await fx.Payments.AddAsync(payment);

        var voidPayment = new VoidSaaSPayment(fx.Payments, fx.UnitOfWork, fx.Clock);
        var result = await voidPayment.ExecuteAsync(payment.Id, "staff-2", "Refunded");

        Assert.True(result.IsSuccess);
        Assert.Equal(SaaSPaymentStatus.Voided, result.Value!.Status);
    }

    [Fact]
    public async Task ConfirmPaymentAndActivateSubscription_confirms_and_activates_trialing_subscription_atomically()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var productCode = await fx.CreateProductAsync();
        var subscription = await fx.StartTrialAsync(org.Id, productCode);

        var payment = SaaSPayment.CreateManual(
            org.Id, productCode, 999m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "REF-ACT-1", T0, T0);
        await fx.Payments.AddAsync(payment);

        var activate = new ConfirmPaymentAndActivateSubscription(fx.Payments, fx.Subscriptions, fx.UnitOfWork, fx.Clock);
        var result = await activate.ExecuteAsync(payment.Id, "staff-1", subscription.Id, T0, T0.AddDays(30));

        Assert.True(result.IsSuccess);
        Assert.Equal(SaaSPaymentStatus.Confirmed, result.Value!.Payment.Status);
        Assert.Equal("staff-1", result.Value.Payment.ConfirmedBy);
        Assert.Equal(subscription.Id, result.Value.Payment.SubscriptionId);
        Assert.Equal(SubscriptionStatus.Active, result.Value.Subscription.Status);
        Assert.Equal(1, fx.Payments.UpdateCount);
        Assert.Equal(1, fx.Subscriptions.UpdateCount);
    }

    [Fact]
    public async Task ConfirmPaymentAndActivateSubscription_reuses_an_already_confirmed_payment_without_reconfirming()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var productCode = await fx.CreateProductAsync();
        var subscription = await fx.StartTrialAsync(org.Id, productCode);

        var payment = SaaSPayment.CreateManual(
            org.Id, productCode, 999m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "REF-ACT-2", T0, T0);
        payment.Confirm("staff-0", T0.AddMinutes(1));
        await fx.Payments.AddAsync(payment);

        var activate = new ConfirmPaymentAndActivateSubscription(fx.Payments, fx.Subscriptions, fx.UnitOfWork, fx.Clock);
        var result = await activate.ExecuteAsync(payment.Id, "staff-1", subscription.Id, T0, T0.AddDays(30));

        Assert.True(result.IsSuccess);
        Assert.Equal("staff-0", result.Value!.Payment.ConfirmedBy);
    }

    [Fact]
    public async Task ConfirmPaymentAndActivateSubscription_rejects_a_terminal_payment_as_not_confirmed()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var productCode = await fx.CreateProductAsync();
        var subscription = await fx.StartTrialAsync(org.Id, productCode);

        var payment = SaaSPayment.CreateManual(
            org.Id, productCode, 999m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "REF-REJECTED", T0, T0);
        payment.Reject("staff-1", "bad ref", T0.AddMinutes(1));
        await fx.Payments.AddAsync(payment);

        var activate = new ConfirmPaymentAndActivateSubscription(fx.Payments, fx.Subscriptions, fx.UnitOfWork, fx.Clock);
        var result = await activate.ExecuteAsync(payment.Id, "staff-2", subscription.Id, T0, T0.AddDays(30));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentNotConfirmed, result.ErrorCode);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
    }

    [Fact]
    public async Task ConfirmPaymentAndActivateSubscription_blocks_reuse_of_a_payment_already_linked_to_a_subscription()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var productCode = await fx.CreateProductAsync();
        var subscription = await fx.StartTrialAsync(org.Id, productCode);

        var payment = SaaSPayment.CreateManual(
            org.Id, productCode, 999m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "REF-REUSE", T0, T0);
        await fx.Payments.AddAsync(payment);

        var activate = new ConfirmPaymentAndActivateSubscription(fx.Payments, fx.Subscriptions, fx.UnitOfWork, fx.Clock);
        var first = await activate.ExecuteAsync(payment.Id, "staff-1", subscription.Id, T0, T0.AddDays(30));
        Assert.True(first.IsSuccess);

        // Free up the org/product slot so a brand-new subscription can be started for the reuse attempt.
        await new CancelSubscription(fx.Subscriptions, fx.UnitOfWork, fx.Clock).ExecuteAsync(subscription.Id);
        var anotherSubscription = await fx.StartTrialAsync(org.Id, productCode);
        var second = await activate.ExecuteAsync(payment.Id, "staff-1", anotherSubscription.Id, T0, T0.AddDays(30));

        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentAlreadyUsed, second.ErrorCode);
    }

    [Fact]
    public async Task ConfirmPaymentAndActivateSubscription_rejects_organization_mismatch()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var otherOrg = await fx.CreateOrganizationAsync("Other Org", "other-org");
        var productCode = await fx.CreateProductAsync();
        var subscription = await fx.StartTrialAsync(org.Id, productCode);

        var payment = SaaSPayment.CreateManual(
            otherOrg.Id, productCode, 999m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "REF-ORG-MISMATCH", T0, T0);
        await fx.Payments.AddAsync(payment);

        var activate = new ConfirmPaymentAndActivateSubscription(fx.Payments, fx.Subscriptions, fx.UnitOfWork, fx.Clock);
        var result = await activate.ExecuteAsync(payment.Id, "staff-1", subscription.Id, T0, T0.AddDays(30));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentOrganizationMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task ConfirmPaymentAndActivateSubscription_rejects_product_mismatch()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var productCode = await fx.CreateProductAsync();
        var subscription = await fx.StartTrialAsync(org.Id, productCode);

        await new CreateProduct(fx.Products, fx.UnitOfWork, fx.Clock).ExecuteAsync("healthcare", "HealthCare");
        var otherProductCode = ProductCode.Create("healthcare");

        var payment = SaaSPayment.CreateManual(
            org.Id, otherProductCode, 999m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "REF-PRODUCT-MISMATCH", T0, T0);
        await fx.Payments.AddAsync(payment);

        var activate = new ConfirmPaymentAndActivateSubscription(fx.Payments, fx.Subscriptions, fx.UnitOfWork, fx.Clock);
        var result = await activate.ExecuteAsync(payment.Id, "staff-1", subscription.Id, T0, T0.AddDays(30));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentProductMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task ConfirmPaymentAndActivateSubscription_returns_not_found_for_missing_payment_or_subscription()
    {
        var fx = new Fixture();
        var org = await fx.CreateOrganizationAsync();
        var productCode = await fx.CreateProductAsync();
        var subscription = await fx.StartTrialAsync(org.Id, productCode);

        var activate = new ConfirmPaymentAndActivateSubscription(fx.Payments, fx.Subscriptions, fx.UnitOfWork, fx.Clock);

        var missingPayment = await activate.ExecuteAsync(SaaSPaymentId.New(), "staff-1", subscription.Id, T0, T0.AddDays(30));
        Assert.Equal(ApplicationErrorCodes.PaymentNotFound, missingPayment.ErrorCode);

        var payment = SaaSPayment.CreateManual(
            org.Id, productCode, 999m, CurrencyCode.Create(CurrencyCode.PHP), SaaSPaymentMethod.GCash, "REF-MISSING-SUB", T0, T0);
        await fx.Payments.AddAsync(payment);

        var missingSubscription = await activate.ExecuteAsync(payment.Id, "staff-1", SubscriptionId.New(), T0, T0.AddDays(30));
        Assert.Equal(ApplicationErrorCodes.SubscriptionNotFound, missingSubscription.ErrorCode);
    }
}
