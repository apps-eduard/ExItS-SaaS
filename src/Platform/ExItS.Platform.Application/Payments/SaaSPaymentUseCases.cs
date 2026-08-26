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
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

/// <summary>Combined result of confirming a manual payment and activating the subscription it funds.</summary>
public sealed record ConfirmedPaymentActivation(SaaSPayment Payment, Subscription Subscription);

public sealed class CreateManualSaaSPayment
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IProductRepository _products;
    private readonly ISaaSPaymentRepository _payments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateManualSaaSPayment(
        IPlatformOrganizationRepository organizations,
        IProductRepository products,
        ISaaSPaymentRepository payments,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _products = products;
        _payments = payments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaaSPayment>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        decimal amount,
        CurrencyCode currencyCode,
        SaaSPaymentMethod method,
        string externalReference,
        DateTimeOffset paidAtUtc,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<SaaSPayment>.Failure(
                ApplicationErrorCodes.OrganizationNotEligible,
                "Payments can only be recorded for an active Platform Organization.");
        }

        var product = await _products.GetByCodeAsync(productCode, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(ApplicationErrorCodes.ProductNotFound, "Product was not found.");
        }

        try
        {
            var normalizedReference = SaaSPayment.NormalizeReference(externalReference);
            var duplicate = await _payments
                .ExistsByNormalizedReferenceAsync(method, normalizedReference, organizationId, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate)
            {
                return ApplicationResult<SaaSPayment>.Failure(
                    ApplicationErrorCodes.PaymentReferenceConflict,
                    "A payment with this reference already exists for this organization and method.");
            }

            var payment = SaaSPayment.CreateManual(
                organizationId, productCode, amount, currencyCode, method, externalReference, paidAtUtc, _clock.UtcNow);
            await _payments.AddAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SaaSPayment>.Success(payment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ConfirmSaaSPayment
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmSaaSPayment(ISaaSPaymentRepository payments, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaaSPayment>> ExecuteAsync(
        SaaSPaymentId paymentId,
        string confirmedBy,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(ApplicationErrorCodes.PaymentNotFound, "Payment was not found.");
        }

        try
        {
            payment.Confirm(confirmedBy, _clock.UtcNow);
            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SaaSPayment>.Success(payment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RejectSaaSPayment
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RejectSaaSPayment(ISaaSPaymentRepository payments, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaaSPayment>> ExecuteAsync(
        SaaSPaymentId paymentId,
        string rejectedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(ApplicationErrorCodes.PaymentNotFound, "Payment was not found.");
        }

        try
        {
            payment.Reject(rejectedBy, reason, _clock.UtcNow);
            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SaaSPayment>.Success(payment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class VoidSaaSPayment
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public VoidSaaSPayment(ISaaSPaymentRepository payments, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaaSPayment>> ExecuteAsync(
        SaaSPaymentId paymentId,
        string voidedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(ApplicationErrorCodes.PaymentNotFound, "Payment was not found.");
        }

        try
        {
            payment.Void(voidedBy, reason, _clock.UtcNow);
            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SaaSPayment>.Success(payment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Confirms a manually reported payment (if not already confirmed) and atomically activates — or
/// reactivates — the subscription it funds, then links the payment to that subscription so it
/// cannot be reused. Delegates all subscription lifecycle rules to <see cref="Subscription"/>;
/// does not duplicate lifecycle logic here.
/// </summary>
public sealed class ConfirmPaymentAndActivateSubscription
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmPaymentAndActivateSubscription(
        ISaaSPaymentRepository payments,
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        GenerateEntitlementSnapshot generateSnapshot,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _payments = payments;
        _subscriptions = subscriptions;
        _plans = plans;
        _generateSnapshot = generateSnapshot;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ConfirmedPaymentActivation>> ExecuteAsync(
        SaaSPaymentId paymentId,
        string confirmedBy,
        SubscriptionId subscriptionId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        BillingCycle billingCycle,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (payment.OrganizationId != subscription.OrganizationId)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentOrganizationMismatch,
                "Payment and subscription belong to different organizations.");
        }

        if (payment.ProductCode != subscription.ProductCode)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentProductMismatch,
                "Payment and subscription are for different products.");
        }

        if (payment.SubscriptionId is not null)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentAlreadyUsed,
                "Payment has already been used to activate a subscription.");
        }

        if (payment.Status is SaaSPaymentStatus.Rejected or SaaSPaymentStatus.Voided)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                "Payment cannot be confirmed because it is in a terminal state.");
        }

        if (subscription.Status == SubscriptionStatus.Active)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Active subscriptions require the manual paid upgrade flow.");
        }

        var plan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null || !plan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Subscription plan was not found or is not active.");
        }

        var periodValidation = SaaSPaymentFundingValidation.ValidatePaidPeriod(
            billingCycle,
            periodStartUtc,
            periodEndUtc);
        if (!periodValidation.IsSuccess)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                periodValidation.ErrorCode!,
                periodValidation.ErrorMessage!);
        }

        try
        {
            var utcNow = _clock.UtcNow;
            if (payment.Status != SaaSPaymentStatus.Confirmed)
            {
                payment.Confirm(confirmedBy, utcNow);
            }

            var fundingValidation = SaaSPaymentFundingValidation.ValidatePlanFunding(payment, plan, billingCycle);
            if (!fundingValidation.IsSuccess)
            {
                return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                    fundingValidation.ErrorCode!,
                    fundingValidation.ErrorMessage!);
            }

            if (subscription.Status == SubscriptionStatus.Trialing)
            {
                subscription.ActivateFromTrial(periodStartUtc, periodEndUtc, billingCycle, plan, utcNow);
            }
            else
            {
                subscription.Reactivate(utcNow, periodStartUtc, periodEndUtc);
            }

            payment.LinkSubscription(subscription.Id, utcNow);

            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _generateSnapshot
                .ExecuteAsync(subscription.OrganizationId, subscription.ProductCode, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<ConfirmedPaymentActivation>.Success(
                new ConfirmedPaymentActivation(payment, subscription));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Starts a paid subscription only when a confirmed, unused SaaS payment funds it, then links that payment.
/// </summary>
public sealed class ActivatePaidSubscriptionFromConfirmedPayment
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly ActivatePaidSubscription _activatePaid;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ActivatePaidSubscriptionFromConfirmedPayment(
        ISaaSPaymentRepository payments,
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        ActivatePaidSubscription activatePaid,
        GenerateEntitlementSnapshot generateSnapshot,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _payments = payments;
        _subscriptions = subscriptions;
        _plans = plans;
        _activatePaid = activatePaid;
        _generateSnapshot = generateSnapshot;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ConfirmedPaymentActivation>> ExecuteAsync(
        SaaSPaymentId paymentId,
        PlatformOrganizationId organizationId,
        PlanId planId,
        PlanVersionId planVersionId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        BillingCycle billingCycle,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        if (payment.OrganizationId != organizationId)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentOrganizationMismatch,
                "Payment and organization do not match.");
        }

        var fundingCheck = SaaSPaymentFundingValidation.ValidateConfirmedUnused(payment);
        if (!fundingCheck.IsSuccess)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                fundingCheck.ErrorCode!,
                fundingCheck.ErrorMessage!);
        }

        var plan = await _plans.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
        if (plan is null || !plan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found or is not active for new subscriptions.");
        }

        if (payment.ProductCode != plan.ProductCode)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentProductMismatch,
                "Payment and plan are for different products.");
        }

        var planFunding = SaaSPaymentFundingValidation.ValidatePlanFunding(payment, plan, billingCycle);
        if (!planFunding.IsSuccess)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                planFunding.ErrorCode!,
                planFunding.ErrorMessage!);
        }

        var periodValidation = SaaSPaymentFundingValidation.ValidatePaidPeriod(
            billingCycle,
            periodStartUtc,
            periodEndUtc);
        if (!periodValidation.IsSuccess)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                periodValidation.ErrorCode!,
                periodValidation.ErrorMessage!);
        }

        var activated = await _activatePaid
            .ExecuteAsync(organizationId, planId, planVersionId, periodStartUtc, periodEndUtc, billingCycle, cancellationToken)
            .ConfigureAwait(false);
        if (!activated.IsSuccess || activated.Value is null)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                activated.ErrorCode ?? ApplicationErrorCodes.SubscriptionIneligible,
                activated.ErrorMessage ?? "Paid subscription activation failed.");
        }

        try
        {
            payment.LinkSubscription(activated.Value.Id, _clock.UtcNow);
            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _generateSnapshot
                .ExecuteAsync(organizationId, activated.Value.ProductCode, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<ConfirmedPaymentActivation>.Success(
                new ConfirmedPaymentActivation(payment, activated.Value));
        }
        catch (DomainException ex)
        {
            activated.Value.Cancel(_clock.UtcNow);
            await _subscriptions.UpdateAsync(activated.Value, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Applies an immediate paid plan upgrade to an active subscription using a confirmed manual SaaS payment.
/// Does not invoke <see cref="IPaymentProvider"/>.
/// </summary>
public sealed class UpgradeSubscriptionFromConfirmedPayment
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlanRepository _plans;
    private readonly ISaaSPaymentRepository _payments;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpgradeSubscriptionFromConfirmedPayment(
        IPlatformOrganizationRepository organizations,
        IPlanRepository plans,
        ISaaSPaymentRepository payments,
        ISubscriptionRepository subscriptions,
        GenerateEntitlementSnapshot generateSnapshot,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _plans = plans;
        _payments = payments;
        _subscriptions = subscriptions;
        _generateSnapshot = generateSnapshot;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ConfirmedPaymentActivation>> ExecuteAsync(
        SaaSPaymentId paymentId,
        PlatformOrganizationId organizationId,
        SubscriptionId subscriptionId,
        PlanId targetPlanId,
        BillingCycle billingCycle,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null || organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.OrganizationNotEligible,
                "Organization must be active.");
        }

        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        if (payment.OrganizationId != organizationId)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentOrganizationMismatch,
                "Payment and organization do not match.");
        }

        var fundingCheck = SaaSPaymentFundingValidation.ValidateConfirmedUnused(payment);
        if (!fundingCheck.IsSuccess)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                fundingCheck.ErrorCode!,
                fundingCheck.ErrorMessage!);
        }

        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null || subscription.OrganizationId != organizationId)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found for this organization.");
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Manual paid upgrade requires an active subscription.");
        }

        if (subscription.PlanId == targetPlanId)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Subscription is already on the requested plan.");
        }

        var currentPlan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        var targetPlan = await _plans.GetByIdAsync(targetPlanId, cancellationToken).ConfigureAwait(false);
        if (currentPlan is null || targetPlan is null
            || targetPlan.ProductCode != subscription.ProductCode
            || !targetPlan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Target plan was not found or is not active for this product.");
        }

        if (payment.ProductCode != targetPlan.ProductCode)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentProductMismatch,
                "Payment and target plan are for different products.");
        }

        if (targetPlan.PriceForCycle(billingCycle) <= currentPlan.PriceForCycle(billingCycle))
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Manual paid upgrade requires a higher-tier target plan.");
        }

        var planFunding = SaaSPaymentFundingValidation.ValidatePlanFunding(payment, targetPlan, billingCycle);
        if (!planFunding.IsSuccess)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                planFunding.ErrorCode!,
                planFunding.ErrorMessage!);
        }

        var version = await RequirePublishedVersionAsync(targetPlan, cancellationToken).ConfigureAwait(false);

        try
        {
            var utcNow = _clock.UtcNow;
            subscription.ApplyImmediatePlanUpgrade(targetPlan, version, billingCycle, utcNow);
            payment.LinkSubscription(subscription.Id, utcNow);

            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _generateSnapshot
                .ExecuteAsync(organizationId, subscription.ProductCode, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<ConfirmedPaymentActivation>.Success(
                new ConfirmedPaymentActivation(payment, subscription));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(ex.ErrorCode, ex.Message);
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
                "Published plan version is required for paid upgrade.");
        }

        return published;
    }
}
