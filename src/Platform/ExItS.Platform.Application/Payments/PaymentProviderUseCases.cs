using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

public static class SubscriptionBillingPeriods
{
    public static (DateTimeOffset Start, DateTimeOffset End) ComputePaidPeriod(
        DateTimeOffset periodStartUtc,
        BillingCycle cycle) =>
        cycle switch
        {
            BillingCycle.Monthly => (periodStartUtc, periodStartUtc.AddMonths(1)),
            BillingCycle.Annual => (periodStartUtc, periodStartUtc.AddYears(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(cycle))
        };
}

public sealed class ProcessSubscriptionInitialPayment
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly IPaymentProvider _paymentProvider;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ProcessSubscriptionInitialPayment(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        IPaymentProvider paymentProvider,
        GenerateEntitlementSnapshot generateSnapshot,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _paymentProvider = paymentProvider;
        _generateSnapshot = generateSnapshot;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<(Subscription Subscription, PaymentProviderResult Payment)>> ExecuteAsync(
        SubscriptionId subscriptionId,
        BillingCycle billingCycle,
        string idempotencyKey,
        PlanId? targetPlanId = null,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        var plan = targetPlanId is not null
            ? await _plans.GetByIdAsync(targetPlanId, cancellationToken).ConfigureAwait(false)
            : await _plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found.");
        }

        if (!plan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Target plan is not active for new subscriptions.");
        }

        var amount = plan.PriceForCycle(billingCycle);

        if (subscription.Status == SubscriptionStatus.Active
            && subscription.PlanId == plan.Id
            && subscription.BillingCycle == billingCycle)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Success((
                subscription,
                new PaymentProviderResult(
                    PaymentProviderResultStatus.Succeeded,
                    Provider: "idempotent",
                    ProviderReference: idempotencyKey,
                    Amount: subscription.AgreedPrice ?? amount,
                    CurrencyCode: plan.CurrencyCode,
                    IsTest: false,
                    FailureCode: null,
                    FailureMessage: null,
                    IdempotencyKey: idempotencyKey)));
        }

        var isTrialConversion = subscription.Status == SubscriptionStatus.Trialing
            || (subscription.Status == SubscriptionStatus.Expired
                && (subscription.TrialStartUtc is not null || subscription.TrialDefinitionId is not null));

        var chargeRequest = new PaymentChargeRequest(
            subscription.OrganizationId.Value,
            subscription.Id.Value,
            amount,
            plan.CurrencyCode,
            idempotencyKey,
            Purpose: isTrialConversion ? "convert-trial" : "initial");

        PaymentProviderResult paymentResult;
        try
        {
            paymentResult = await _paymentProvider.ChargeAsync(chargeRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (NotSupportedException ex)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.PaymentNotConfigured,
                ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (paymentResult.Status != PaymentProviderResultStatus.Succeeded)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                paymentResult.FailureMessage ?? $"Payment was not successful ({paymentResult.Status}).");
        }

        try
        {
            var utcNow = _clock.UtcNow;
            var (periodStart, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(utcNow, billingCycle);

            if (isTrialConversion)
            {
                var version = await RequirePublishedVersionAsync(plan, cancellationToken).ConfigureAwait(false);
                subscription.ConvertTrialToPaid(plan, version, billingCycle, periodStart, periodEnd, utcNow);
            }
            else if (subscription.Status == SubscriptionStatus.Trialing)
            {
                subscription.ActivateFromTrial(periodStart, periodEnd, billingCycle, plan, utcNow);
            }
            else if (subscription.Status is SubscriptionStatus.GracePeriod or SubscriptionStatus.PastDue
                     or SubscriptionStatus.Suspended)
            {
                subscription.Reactivate(utcNow, periodStart, periodEnd);
                subscription.ApplyImmediatePlanUpgrade(plan, await RequirePublishedVersionAsync(plan, cancellationToken), billingCycle, utcNow);
            }
            else
            {
                return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                    ApplicationErrorCodes.SubscriptionIneligible,
                    "Subscription is not eligible for initial payment activation.");
            }

            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _generateSnapshot
                .ExecuteAsync(subscription.OrganizationId, subscription.ProductCode, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<(Subscription, PaymentProviderResult)>.Success((subscription, paymentResult));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<PlanVersion> RequirePublishedVersionAsync(Plan plan, CancellationToken cancellationToken)
    {
        var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var published = versions.FirstOrDefault(v => v.Status == PlanVersionStatus.Published);
        if (published is null)
        {
            throw new DomainException(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Published plan version is required for payment activation.");
        }

        return published;
    }
}

public sealed class ProcessSubscriptionRenewal
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly IPaymentProvider _paymentProvider;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ProcessSubscriptionRenewal(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        IPaymentProvider paymentProvider,
        GenerateEntitlementSnapshot generateSnapshot,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _paymentProvider = paymentProvider;
        _generateSnapshot = generateSnapshot;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<(Subscription Subscription, PaymentProviderResult Payment)>> ExecuteAsync(
        SubscriptionId subscriptionId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (subscription.BillingCycle is null)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Renewal requires an active billing cycle on the subscription.");
        }

        var plan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found.");
        }

        var cycle = subscription.BillingCycle.Value;
        var amount = subscription.AgreedPrice ?? plan.PriceForCycle(cycle);
        var currency = subscription.CurrencyCode ?? plan.CurrencyCode;

        var chargeRequest = new PaymentChargeRequest(
            subscription.OrganizationId.Value,
            subscription.Id.Value,
            amount,
            currency,
            idempotencyKey,
            Purpose: "renewal");

        PaymentProviderResult paymentResult;
        try
        {
            paymentResult = await _paymentProvider.ChargeAsync(chargeRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (NotSupportedException ex)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.PaymentNotConfigured,
                ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var utcNow = _clock.UtcNow;
            if (paymentResult.Status == PaymentProviderResultStatus.RenewalSucceeded
                || paymentResult.Status == PaymentProviderResultStatus.Succeeded)
            {
                var periodStart = subscription.PaidPeriodEndUtc ?? utcNow;
                var (_, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(periodStart, cycle);
                subscription.Reactivate(utcNow, periodStart, periodEnd);
            }
            else if (paymentResult.Status == PaymentProviderResultStatus.RenewalFailed)
            {
                subscription.MarkPastDue(utcNow);
            }
            else
            {
                return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                    ApplicationErrorCodes.PaymentNotConfirmed,
                    paymentResult.FailureMessage ?? $"Renewal payment failed ({paymentResult.Status}).");
            }

            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _generateSnapshot
                .ExecuteAsync(subscription.OrganizationId, subscription.ProductCode, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<(Subscription, PaymentProviderResult)>.Success((subscription, paymentResult));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class SimulateLocalValidationPayment
{
    private readonly IPaymentProvider _paymentProvider;
    private readonly ProcessSubscriptionInitialPayment _initialPayment;
    private readonly ProcessSubscriptionRenewal _renewal;
    private readonly ApplyDuePendingPlanChanges _applyPending;
    private readonly IPlatformUnitOfWork _unitOfWork;

    public SimulateLocalValidationPayment(
        IPaymentProvider paymentProvider,
        ProcessSubscriptionInitialPayment initialPayment,
        ProcessSubscriptionRenewal renewal,
        ApplyDuePendingPlanChanges applyPending,
        IPlatformUnitOfWork unitOfWork)
    {
        _paymentProvider = paymentProvider;
        _initialPayment = initialPayment;
        _renewal = renewal;
        _applyPending = applyPending;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult<PaymentProviderResult>> ExecuteAsync(
        string simulation,
        PaymentChargeRequest request,
        BillingCycle? billingCycle,
        CancellationToken cancellationToken = default)
    {
        if (!_paymentProvider.IsTestProvider)
        {
            return ApplicationResult<PaymentProviderResult>.Failure(
                ApplicationErrorCodes.PaymentNotConfigured,
                "Local validation payment simulation is only available with LocalValidation provider.");
        }

        PaymentProviderResult result;
        try
        {
            result = await _paymentProvider.SimulateAsync(simulation, request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (NotSupportedException ex)
        {
            return ApplicationResult<PaymentProviderResult>.Failure(ApplicationErrorCodes.PaymentNotConfigured, ex.Message);
        }

        var subscriptionId = SubscriptionId.From(request.SubscriptionId);
        var normalized = simulation.Trim().ToLowerInvariant();

        if (result.Status == PaymentProviderResultStatus.Succeeded && billingCycle is not null)
        {
            var activation = await _initialPayment
                .ExecuteAsync(subscriptionId, billingCycle.Value, request.IdempotencyKey, targetPlanId: null, cancellationToken)
                .ConfigureAwait(false);
            if (!activation.IsSuccess)
            {
                return ApplicationResult<PaymentProviderResult>.Failure(
                    activation.ErrorCode ?? ApplicationErrorCodes.PaymentNotConfirmed,
                    activation.ErrorMessage ?? "Subscription activation after simulated payment failed.");
            }

            return ApplicationResult<PaymentProviderResult>.Success(activation.Value.Payment);
        }

        if (normalized.Contains("renewal", StringComparison.Ordinal))
        {
            var renewal = await _renewal
                .ExecuteAsync(subscriptionId, request.IdempotencyKey, cancellationToken)
                .ConfigureAwait(false);
            if (!renewal.IsSuccess)
            {
                return ApplicationResult<PaymentProviderResult>.Failure(
                    renewal.ErrorCode ?? ApplicationErrorCodes.PaymentNotConfirmed,
                    renewal.ErrorMessage ?? "Simulated renewal processing failed.");
            }

            return ApplicationResult<PaymentProviderResult>.Success(renewal.Value.Payment);
        }

        if (normalized.Contains("pending-plan", StringComparison.Ordinal) || normalized == "apply-pending")
        {
            await _applyPending.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult<PaymentProviderResult>.Success(result);
    }
}

public sealed class ConvertTrialSubscription
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlanRepository _plans;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly ProcessSubscriptionInitialPayment _initialPayment;

    public ConvertTrialSubscription(
        IPlatformOrganizationRepository organizations,
        IPlanRepository plans,
        ISubscriptionRepository subscriptions,
        ProcessSubscriptionInitialPayment initialPayment)
    {
        _organizations = organizations;
        _plans = plans;
        _subscriptions = subscriptions;
        _initialPayment = initialPayment;
    }

    public Task<ApplicationResult<(Subscription Subscription, PaymentProviderResult Payment)>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        SubscriptionId subscriptionId,
        PlanId targetPlanId,
        BillingCycle billingCycle,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(organizationId, subscriptionId, targetPlanId, billingCycle, idempotencyKey, expectedVersion: null, cancellationToken);

    public async Task<ApplicationResult<(Subscription Subscription, PaymentProviderResult Payment)>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        SubscriptionId subscriptionId,
        PlanId targetPlanId,
        BillingCycle billingCycle,
        string idempotencyKey,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null || organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.OrganizationNotEligible,
                "Organization must be active.");
        }

        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null || subscription.OrganizationId != organizationId)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found for this organization.");
        }

        if (SubscriptionConcurrency.IsMismatch(subscription.Version, expectedVersion))
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The subscription was modified by another request. Refresh and try again.");
        }

        var targetPlan = await _plans.GetByIdAsync(targetPlanId, cancellationToken).ConfigureAwait(false);
        if (targetPlan is null
            || targetPlan.ProductCode != subscription.ProductCode
            || !targetPlan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Target plan was not found or is not active for this product.");
        }

        var isTrialEligible = subscription.Status == SubscriptionStatus.Trialing
            || (subscription.Status == SubscriptionStatus.Expired
                && (subscription.TrialStartUtc is not null || subscription.TrialDefinitionId is not null));

        if (!isTrialEligible && subscription.Status != SubscriptionStatus.Active)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Only trialing or expired trial subscriptions can be converted.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.PaymentReferenceConflict,
                "IdempotencyKey is required for trial conversion.");
        }

        return await _initialPayment
            .ExecuteAsync(subscriptionId, billingCycle, idempotencyKey, targetPlanId, cancellationToken)
            .ConfigureAwait(false);
    }
}
