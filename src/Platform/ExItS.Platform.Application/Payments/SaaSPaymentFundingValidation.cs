using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

/// <summary>
/// Canonical server-side validation that a SaaS payment funds a specific plan/billing cycle.
/// Confirmed status alone does not imply sufficient funding.
/// </summary>
public static class SaaSPaymentFundingValidation
{
    public static ApplicationResult ValidateConfirmedUnused(SaaSPayment payment)
    {
        if (payment.Status is SaaSPaymentStatus.Rejected or SaaSPaymentStatus.Voided)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                "Payment cannot be used because it is in a terminal state.");
        }

        if (payment.Status != SaaSPaymentStatus.Confirmed)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                "Payment must be confirmed before it can fund a subscription.");
        }

        if (payment.SubscriptionId is not null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PaymentAlreadyUsed,
                "Payment has already been used to activate a subscription.");
        }

        return ApplicationResult.Success();
    }

    public static ApplicationResult ValidatePlanFunding(
        SaaSPayment payment,
        Plan plan,
        BillingCycle billingCycle)
    {
        if (payment.ProductCode != plan.ProductCode)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PaymentProductMismatch,
                "Payment and plan are for different products.");
        }

        decimal requiredAmount;
        try
        {
            requiredAmount = plan.PriceForCycle(billingCycle);
        }
        catch (ArgumentOutOfRangeException)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "The selected billing cycle is not supported for this plan.");
        }

        if (requiredAmount <= 0m)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "The selected plan does not have a price for the requested billing cycle.");
        }

        if (payment.Amount != requiredAmount)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PaymentAmountMismatch,
                "The confirmed payment amount does not match the selected plan price.");
        }

        if (!string.Equals(payment.CurrencyCode.Value, plan.CurrencyCode, StringComparison.Ordinal))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PaymentCurrencyMismatch,
                "The confirmed payment currency does not match the selected plan currency.");
        }

        return ApplicationResult.Success();
    }

    public static ApplicationResult ValidatePaidPeriod(
        BillingCycle billingCycle,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc)
    {
        if (periodEndUtc <= periodStartUtc)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Paid period end must be after start.");
        }

        var (_, expectedEndUtc) = SubscriptionBillingPeriods.ComputePaidPeriod(periodStartUtc, billingCycle);
        if (periodEndUtc != expectedEndUtc)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PaymentPeriodMismatch,
                "The paid period does not match the selected billing cycle.");
        }

        return ApplicationResult.Success();
    }
}
