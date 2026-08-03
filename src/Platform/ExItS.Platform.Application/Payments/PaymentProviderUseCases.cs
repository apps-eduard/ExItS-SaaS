using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
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
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        var plan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            return ApplicationResult<(Subscription, PaymentProviderResult)>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found.");
        }

        var amount = plan.PriceForCycle(billingCycle);
        var chargeRequest = new PaymentChargeRequest(
            subscription.OrganizationId.Value,
            subscription.Id.Value,
            amount,
            plan.CurrencyCode,
            idempotencyKey,
            Purpose: "initial");

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

            if (subscription.Status == SubscriptionStatus.Trialing)
            {
                subscription.ActivateFromTrial(periodStart, periodEnd, billingCycle, plan, utcNow);
            }
            else if (subscription.Status is SubscriptionStatus.GracePeriod or SubscriptionStatus.PastDue
                     or SubscriptionStatus.Suspended)
            {
                subscription.Reactivate(utcNow, periodStart, periodEnd);
                subscription.ApplyImmediatePlanUpgrade(plan, await RequirePublishedVersionAsync(plan, cancellationToken), billingCycle, utcNow);
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
                .ExecuteAsync(subscriptionId, billingCycle.Value, request.IdempotencyKey, cancellationToken)
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
