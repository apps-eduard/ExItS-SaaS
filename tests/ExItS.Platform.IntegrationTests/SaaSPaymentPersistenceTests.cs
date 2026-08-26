using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class SaaSPaymentPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<(PlatformOrganizationId organizationId, ProductCode productCode)> SeedOrganizationAndProductAsync(
        IServiceProvider provider,
        string prefix)
    {
        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync($"{prefix} Org", Unique(prefix)).ConfigureAwait(false)).Value!;

        var candidate = Unique(prefix);
        var productCode = candidate[..Math.Min(30, candidate.Length)];
        var createProduct = provider.GetRequiredService<CreateProduct>();
        await createProduct.ExecuteAsync(productCode, "POS").ConfigureAwait(false);

        return (org.Id, ProductCode.Create(productCode));
    }

    private static async Task<(PlatformOrganizationId organizationId, PlanId planId, Domain.Catalog.PlanVersionId versionId, TrialDefinitionId trialId, ProductCode productCode)>
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

        var plan = (await createPlan.ExecuteAsync(
            productCode,
            "utang",
            "Utang",
            description: null,
            maxBranches: 1,
            maxActiveStaff: 3,
            maxActivePosDevices: 3,
            maxActiveBusinessTypes: 1,
            customerCreditEnabled: false,
            advancedReportsEnabled: false,
            exportEnabled: false,
            trialAllowed: true,
            defaultTrialDays: 14,
            sortOrder: 100,
            monthlyPrice: 999m,
            annualPrice: 9990m,
            currencyCode: "PHP").ConfigureAwait(false)).Value!;
        await activatePlan.ExecuteAsync(plan.Id).ConfigureAwait(false);

        var grants = new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) };
        var version = (await createVersion
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true, grants, T0)
            .ConfigureAwait(false)).Value!;
        await publish.ExecuteAsync(plan.Id, 1).ConfigureAwait(false);

        var trial = (await createTrial
            .ExecuteAsync(productCode, "Trial", TimeSpan.FromDays(21), grants, Array.Empty<FeatureGrantSpec>())
            .ConfigureAwait(false)).Value!;

        return (org.Id, plan.Id, version.Id, trial.Id, ProductCode.Create(productCode));
    }

    [Fact]
    public async Task CreateManualSaaSPayment_persists_and_reloads_all_metadata()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, productCode) = await SeedOrganizationAndProductAsync(provider, "persist-payment");

        var create = provider.GetRequiredService<CreateManualSaaSPayment>();
        var payments = provider.GetRequiredService<ISaaSPaymentRepository>();

        var created = await create.ExecuteAsync(
            organizationId,
            productCode,
            1500.50m,
            CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.GCash,
            "  gcash-ref-001  ",
            T0.AddMinutes(-5),
            default);

        Assert.True(created.IsSuccess);
        var payment = created.Value!;
        Assert.Equal(SaaSPaymentStatus.PendingConfirmation, payment.Status);
        Assert.Equal("gcash-ref-001", payment.ExternalReference);

        var reloaded = await payments.GetByIdAsync(payment.Id, default);
        Assert.NotNull(reloaded);
        Assert.Equal(organizationId, reloaded!.OrganizationId);
        Assert.Equal(productCode, reloaded.ProductCode);
        Assert.Equal(1500.50m, reloaded.Amount);
        Assert.Equal(CurrencyCode.PHP, reloaded.CurrencyCode.Value);
        Assert.Equal(SaaSPaymentMethod.GCash, reloaded.Method);
        Assert.Equal("gcash-ref-001", reloaded.ExternalReference);
        Assert.Equal(SaaSPaymentStatus.PendingConfirmation, reloaded.Status);
        Assert.Null(reloaded.SubscriptionId);
        Assert.Equal(TimeSpan.Zero, reloaded.CreatedAtUtc.Offset);
    }

    [Fact]
    public async Task CreateManualSaaSPayment_duplicate_reference_for_same_org_and_method_is_rejected()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, productCode) = await SeedOrganizationAndProductAsync(provider, "dup-payment");

        var create = provider.GetRequiredService<CreateManualSaaSPayment>();

        var first = await create.ExecuteAsync(
            organizationId, productCode, 500m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.Cash, "REF-DUP-1", T0, default);
        Assert.True(first.IsSuccess);

        var second = await create.ExecuteAsync(
            organizationId, productCode, 750m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.Cash, "ref-dup-1", T0, default);
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentReferenceConflict, second.ErrorCode);
    }

    [Fact]
    public async Task Duplicate_reference_that_bypasses_the_application_check_is_still_rejected_by_the_database()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, productCode) = await SeedOrganizationAndProductAsync(provider, "dup-db-payment");

        var payments = provider.GetRequiredService<ISaaSPaymentRepository>();
        var unitOfWork = provider.GetRequiredService<IPlatformUnitOfWork>();

        var first = SaaSPayment.CreateManual(
            organizationId, productCode, 100m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.BankTransfer, "DB-DUP-REF", T0, T0);
        await payments.AddAsync(first, default);
        await unitOfWork.SaveChangesAsync();

        var second = SaaSPayment.CreateManual(
            organizationId, productCode, 200m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.BankTransfer, "DB-DUP-REF", T0, T0);
        await payments.AddAsync(second, default);

        var ex = await Assert.ThrowsAsync<PersistenceConflictException>(() => unitOfWork.SaveChangesAsync());
        Assert.Equal(ApplicationErrorCodes.PaymentReferenceConflict, ex.ErrorCode);
    }

    [Fact]
    public async Task Positive_amount_check_constraint_rejects_a_non_positive_amount_at_the_database_level()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, productCode) = await SeedOrganizationAndProductAsync(provider, "amount-check");

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO platform.saas_payments (
                id, organization_id, product_code, subscription_id, amount, currency_code, method, status,
                external_reference, normalized_reference, paid_at_utc, confirmed_at_utc, confirmed_by,
                rejected_at_utc, rejected_by, rejection_reason, voided_at_utc, voided_by, void_reason,
                created_at_utc, updated_at_utc, aggregate_version)
            VALUES (
                @id, @organizationId, @productCode, NULL, 0, 'PHP', 'Cash', 'PendingConfirmation',
                'ZERO-AMOUNT', 'ZERO-AMOUNT', @now, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                @now, @now, 1)
            """,
            connection);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("organizationId", organizationId.Value);
        command.Parameters.AddWithValue("productCode", productCode.Value);
        command.Parameters.AddWithValue("now", T0);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Contains("ck_saas_payments_positive_amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Confirm_reject_and_void_status_transitions_persist_and_reload_correctly()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, productCode) = await SeedOrganizationAndProductAsync(provider, "status-round-trip");

        var create = provider.GetRequiredService<CreateManualSaaSPayment>();
        var confirm = provider.GetRequiredService<ConfirmSaaSPayment>();
        var voidPayment = provider.GetRequiredService<VoidSaaSPayment>();
        var payments = provider.GetRequiredService<ISaaSPaymentRepository>();

        var confirmablePayment = (await create.ExecuteAsync(
            organizationId, productCode, 250m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.Cash, "REF-CONFIRM", T0, default)).Value!;

        var confirmed = await confirm.ExecuteAsync(confirmablePayment.Id, "staff-confirm", default);
        Assert.True(confirmed.IsSuccess);

        var reloadedConfirmed = await payments.GetByIdAsync(confirmablePayment.Id, default);
        Assert.Equal(SaaSPaymentStatus.Confirmed, reloadedConfirmed!.Status);
        Assert.Equal("staff-confirm", reloadedConfirmed.ConfirmedBy);
        Assert.NotNull(reloadedConfirmed.ConfirmedAtUtc);

        var voided = await voidPayment.ExecuteAsync(confirmablePayment.Id, "staff-void", "refund issued", default);
        Assert.True(voided.IsSuccess);

        var reloadedVoided = await payments.GetByIdAsync(confirmablePayment.Id, default);
        Assert.Equal(SaaSPaymentStatus.Voided, reloadedVoided!.Status);
        Assert.Equal("staff-void", reloadedVoided.VoidedBy);
        Assert.Equal("refund issued", reloadedVoided.VoidReason);
        Assert.NotNull(reloadedVoided.VoidedAtUtc);
        // Confirmation history is retained even after the terminal void transition.
        Assert.Equal("staff-confirm", reloadedVoided.ConfirmedBy);
    }

    [Fact]
    public async Task Reject_persists_rejection_metadata_and_leaves_the_payment_in_a_terminal_state()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, productCode) = await SeedOrganizationAndProductAsync(provider, "reject-round-trip");

        var create = provider.GetRequiredService<CreateManualSaaSPayment>();
        var reject = provider.GetRequiredService<RejectSaaSPayment>();
        var confirm = provider.GetRequiredService<ConfirmSaaSPayment>();
        var payments = provider.GetRequiredService<ISaaSPaymentRepository>();

        var payment = (await create.ExecuteAsync(
            organizationId, productCode, 300m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.BankTransfer, "REF-REJECT", T0, default)).Value!;

        var rejected = await reject.ExecuteAsync(payment.Id, "staff-reject", "unverifiable reference", default);
        Assert.True(rejected.IsSuccess);

        var reloaded = await payments.GetByIdAsync(payment.Id, default);
        Assert.Equal(SaaSPaymentStatus.Rejected, reloaded!.Status);
        Assert.Equal("staff-reject", reloaded.RejectedBy);
        Assert.Equal("unverifiable reference", reloaded.RejectionReason);
        Assert.NotNull(reloaded.RejectedAtUtc);

        var confirmTerminal = await confirm.ExecuteAsync(payment.Id, "staff-confirm", default);
        Assert.False(confirmTerminal.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidSaaSPaymentTransition, confirmTerminal.ErrorCode);
    }

    [Fact]
    public async Task ConfirmPaymentAndActivateSubscription_persists_the_link_between_payment_and_subscription()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, planId, versionId, trialId, productCode) =
            await SeedTrialEligibleOrganizationAsync(provider, "activate-payment");

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var subscription = (await startTrial.ExecuteAsync(organizationId, planId, versionId, trialId)).Value!;

        var create = provider.GetRequiredService<CreateManualSaaSPayment>();
        var payment = (await create.ExecuteAsync(
            organizationId, productCode, 999m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.GCash, "REF-ACTIVATE", T0, default)).Value!;

        var activate = provider.GetRequiredService<ConfirmPaymentAndActivateSubscription>();
        var (periodStart, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var result = await activate.ExecuteAsync(
            payment.Id, "staff-activate", subscription.Id, periodStart, periodEnd, BillingCycle.Monthly, default);
        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionStatus.Active, result.Value!.Subscription.Status);

        var payments = provider.GetRequiredService<ISaaSPaymentRepository>();
        var subscriptions = provider.GetRequiredService<ISubscriptionRepository>();

        var reloadedPayment = await payments.GetByIdAsync(payment.Id, default);
        Assert.Equal(SaaSPaymentStatus.Confirmed, reloadedPayment!.Status);
        Assert.Equal(subscription.Id, reloadedPayment.SubscriptionId);

        var reloadedSubscription = await subscriptions.GetByIdAsync(subscription.Id);
        Assert.Equal(SubscriptionStatus.Active, reloadedSubscription!.Status);
    }

    [Fact]
    public async Task A_payment_already_linked_to_a_subscription_cannot_be_used_to_activate_another_one()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, planId, versionId, trialId, productCode) =
            await SeedTrialEligibleOrganizationAsync(provider, "reuse-payment");

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var cancel = provider.GetRequiredService<CancelSubscription>();
        var firstSubscription = (await startTrial.ExecuteAsync(organizationId, planId, versionId, trialId)).Value!;

        var create = provider.GetRequiredService<CreateManualSaaSPayment>();
        var payment = (await create.ExecuteAsync(
            organizationId, productCode, 999m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.GCash, "REF-REUSE", T0, default)).Value!;

        var activate = provider.GetRequiredService<ConfirmPaymentAndActivateSubscription>();
        var (periodStart, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(T0, BillingCycle.Monthly);
        var first = await activate.ExecuteAsync(
            payment.Id, "staff-activate", firstSubscription.Id, periodStart, periodEnd, BillingCycle.Monthly, default);
        Assert.True(first.IsSuccess);

        await cancel.ExecuteAsync(firstSubscription.Id);
        var secondSubscription = (await startTrial.ExecuteAsync(organizationId, planId, versionId, trialId)).Value!;

        var second = await activate.ExecuteAsync(
            payment.Id, "staff-activate", secondSubscription.Id, periodStart, periodEnd, BillingCycle.Monthly, default);
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentAlreadyUsed, second.ErrorCode);
    }

    [Fact]
    public async Task Concurrent_confirmations_of_the_same_payment_surface_a_concurrency_conflict()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (organizationId, productCode) = await SeedOrganizationAndProductAsync(provider, "xmin-payment");

        var create = provider.GetRequiredService<CreateManualSaaSPayment>();
        var payment = (await create.ExecuteAsync(
            organizationId, productCode, 400m, CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.Cash, "REF-XMIN", T0, default)).Value!;

        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();

        var paymentsA = scopeA.ServiceProvider.GetRequiredService<ISaaSPaymentRepository>();
        var paymentsB = scopeB.ServiceProvider.GetRequiredService<ISaaSPaymentRepository>();
        var uowA = scopeA.ServiceProvider.GetRequiredService<IPlatformUnitOfWork>();
        var uowB = scopeB.ServiceProvider.GetRequiredService<IPlatformUnitOfWork>();

        var paymentA = await paymentsA.GetByIdAsync(payment.Id, default);
        var paymentB = await paymentsB.GetByIdAsync(payment.Id, default);
        Assert.NotNull(paymentA);
        Assert.NotNull(paymentB);

        paymentA!.Confirm("staff-a", T0.AddMinutes(1));
        paymentB!.Reject("staff-b", "duplicate submission", T0.AddMinutes(2));

        await paymentsA.UpdateAsync(paymentA, default);
        await paymentsB.UpdateAsync(paymentB, default);

        await uowA.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => uowB.SaveChangesAsync());
    }
}
