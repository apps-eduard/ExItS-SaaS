using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Commercial;

public sealed record StartOrganizationCommercialRequest(
    string? ProductCode = null,
    string? PlanKey = null,
    Guid? PlanId = null,
    string? BillingCycle = null,
    bool StartAsTrial = false,
    bool PayNow = false,
    string? IdempotencyKey = null);

/// <summary>
/// Org-owner commercial self-service: start trial or paid subscription from catalog PlanKey.
/// Reuses <see cref="StartTrialSubscription"/> and <see cref="ActivatePaidSubscription"/>.
/// </summary>
public sealed class StartOrganizationCommercialSubscription
{
    private readonly EnsureMvpPosPlans _ensureMvpPosPlans;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly StartTrialSubscription _startTrial;
    private readonly ActivatePaidSubscription _activatePaid;
    private readonly IPaymentProvider _paymentProvider;
    private readonly RecordLinkedSuccessfulProviderPayment _recordLinkedPayment;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public StartOrganizationCommercialSubscription(
        EnsureMvpPosPlans ensureMvpPosPlans,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        ISubscriptionRepository subscriptions,
        StartTrialSubscription startTrial,
        ActivatePaidSubscription activatePaid,
        IPaymentProvider paymentProvider,
        RecordLinkedSuccessfulProviderPayment recordLinkedPayment,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _ensureMvpPosPlans = ensureMvpPosPlans;
        _plans = plans;
        _trials = trials;
        _subscriptions = subscriptions;
        _startTrial = startTrial;
        _activatePaid = activatePaid;
        _paymentProvider = paymentProvider;
        _recordLinkedPayment = recordLinkedPayment;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Subscription>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        StartOrganizationCommercialRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.StartAsTrial && !request.PayNow)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Specify StartAsTrial or PayNow.");
        }

        if (request.StartAsTrial && request.PayNow)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "StartAsTrial and PayNow cannot both be true.");
        }

        var productCode = string.IsNullOrWhiteSpace(request.ProductCode)
            ? ProductCode.PinoyBusinessPos
            : request.ProductCode.Trim().ToLowerInvariant();

        BillingCycle billingCycle;
        try
        {
            billingCycle = ParseBillingCycle(request.BillingCycle);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Subscription>.Failure(ex.ErrorCode, ex.Message);
        }

        await _ensureMvpPosPlans.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        var catalog = await ResolveCatalogAsync(productCode, request.PlanKey, request.PlanId, cancellationToken)
            .ConfigureAwait(false);
        if (!catalog.IsSuccess || catalog.Value is null)
        {
            return ApplicationResult<Subscription>.Failure(
                catalog.ErrorCode ?? ApplicationErrorCodes.PlanNotFound,
                catalog.ErrorMessage ?? "Plan catalog could not be resolved.");
        }

        var existing = await _subscriptions
            .GetCurrentForOrganizationProductAsync(
                organizationId,
                ProductCode.Create(productCode),
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && Subscription.IsActiveLike(existing.Status))
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "An active subscription already exists. Use upgrade, convert trial, or subscribe from the current plan.");
        }

        if (request.StartAsTrial)
        {
            var plan = await _plans.GetByIdAsync(catalog.Value.PlanId, cancellationToken).ConfigureAwait(false);
            if (plan is null)
            {
                return ApplicationResult<Subscription>.Failure(
                    ApplicationErrorCodes.PlanNotFound,
                    "Plan was not found.");
            }

            if (!plan.TrialAllowed)
            {
                return ApplicationResult<Subscription>.Failure(
                    ApplicationErrorCodes.TrialNotAllowed,
                    "This plan does not allow trials. Subscribe now instead.");
            }

            return await _startTrial
                .ExecuteAsync(
                    organizationId,
                    catalog.Value.PlanId,
                    catalog.Value.PlanVersionId,
                    catalog.Value.TrialDefinitionId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var paidPlan = await _plans.GetByIdAsync(catalog.Value.PlanId, cancellationToken).ConfigureAwait(false);
        if (paidPlan is null)
        {
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found.");
        }

        // provider_payments.subscription_id is required — activate, then charge the real id.
        var utcNow = _clock.UtcNow;
        var (periodStart, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(utcNow, billingCycle);
        var paid = await _activatePaid
            .ExecuteAsync(
                organizationId,
                catalog.Value.PlanId,
                catalog.Value.PlanVersionId,
                periodStart,
                periodEnd,
                billingCycle,
                cancellationToken)
            .ConfigureAwait(false);
        if (!paid.IsSuccess || paid.Value is null)
        {
            return paid;
        }

        var activated = paid.Value;
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"org-commercial-{organizationId.Value:N}-{activated.Id.Value:N}"
            : request.IdempotencyKey.Trim();

        PaymentProviderResult paymentResult;
        try
        {
            paymentResult = await _paymentProvider
                .ChargeAsync(
                    new PaymentChargeRequest(
                        organizationId.Value,
                        activated.Id.Value,
                        paidPlan.PriceForCycle(billingCycle),
                        paidPlan.CurrencyCode,
                        idempotencyKey,
                        Purpose: "org-commercial-subscribe"),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (NotSupportedException ex)
        {
            activated.Cancel(_clock.UtcNow);
            await _subscriptions.UpdateAsync(activated, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PaymentNotConfigured,
                ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (paymentResult.Status != PaymentProviderResultStatus.Succeeded)
        {
            activated.Cancel(_clock.UtcNow);
            await _subscriptions.UpdateAsync(activated, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                paymentResult.FailureMessage ?? "Initial payment was not successful.");
        }

        var linked = await _recordLinkedPayment
            .ExecuteAsync(
                organizationId,
                ProductCode.Create(productCode),
                activated.Id,
                paymentResult,
                "org-commercial-subscribe",
                cancellationToken)
            .ConfigureAwait(false);
        if (!linked.IsSuccess)
        {
            activated.Cancel(_clock.UtcNow);
            await _subscriptions.UpdateAsync(activated, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Subscription>.Failure(
                linked.ErrorCode ?? ApplicationErrorCodes.PaymentNotConfirmed,
                linked.ErrorMessage ?? "Successful payment could not be linked for administration.");
        }

        return ApplicationResult<Subscription>.Success(activated);
    }

    private sealed record CatalogSelection(PlanId PlanId, PlanVersionId PlanVersionId, TrialDefinitionId TrialDefinitionId);

    private async Task<ApplicationResult<CatalogSelection>> ResolveCatalogAsync(
        string productCode,
        string? planKey,
        Guid? planId,
        CancellationToken cancellationToken)
    {
        Plan? plan = null;
        if (planId is Guid id)
        {
            plan = await _plans.GetByIdAsync(PlanId.From(id), cancellationToken).ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(planKey))
        {
            plan = await _plans
                .GetByProductAndCodeAsync(
                    ProductCode.Create(productCode),
                    PlanCode.Create(planKey.Trim()),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (plan is null || plan.Status != PlanStatus.Active)
        {
            return ApplicationResult<CatalogSelection>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Active plan was not found.");
        }

        var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var version = versions.FirstOrDefault(v => v.Status == PlanVersionStatus.Published);
        if (version is null)
        {
            return ApplicationResult<CatalogSelection>.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Published plan version was not found.");
        }

        var trials = await _trials
            .ListByProductAsync(plan.ProductCode, cancellationToken)
            .ConfigureAwait(false);
        var activeTrials = trials.Where(t => t.Status == TrialDefinitionStatus.Active).ToList();
        var expectedDuration = TimeSpan.FromDays(Math.Max(plan.DefaultTrialDays, 1));
        var trial = activeTrials.FirstOrDefault(t => t.PlanId == plan.Id)
            ?? activeTrials.FirstOrDefault(t => t.Duration == expectedDuration)
            ?? activeTrials.FirstOrDefault();

        if (trial is null)
        {
            return ApplicationResult<CatalogSelection>.Failure(
                ApplicationErrorCodes.TrialNotFound,
                "No active trial definition is available for this product.");
        }

        return ApplicationResult<CatalogSelection>.Success(
            new CatalogSelection(plan.Id, version.Id, trial.Id));
    }

    private static BillingCycle ParseBillingCycle(string? billingCycle)
    {
        if (string.IsNullOrWhiteSpace(billingCycle))
        {
            return BillingCycle.Monthly;
        }

        if (Enum.TryParse<BillingCycle>(billingCycle, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new DomainException(
            ApplicationErrorCodes.InvalidBillingCycle,
            $"Unrecognized billing cycle '{billingCycle}'.");
    }
}
