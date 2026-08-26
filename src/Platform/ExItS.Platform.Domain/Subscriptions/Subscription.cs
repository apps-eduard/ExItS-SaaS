using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Domain.Subscriptions;

/// <summary>
/// Authoritative Platform subscription. Does not grant product operational permissions directly.
/// </summary>
public sealed class Subscription
{
    public SubscriptionId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public ProductCode ProductCode { get; }
    public PlanId PlanId { get; private set; }
    public PlanVersionId PlanVersionId { get; private set; }
    public TrialDefinitionId? TrialDefinitionId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset? TrialStartUtc { get; private set; }
    public DateTimeOffset? TrialEndUtc { get; private set; }
    public DateTimeOffset? PaidPeriodStartUtc { get; private set; }
    public DateTimeOffset? PaidPeriodEndUtc { get; private set; }
    public DateTimeOffset? GracePeriodEndUtc { get; private set; }
    public DateTimeOffset? SuspendedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset? PastDueAtUtc { get; private set; }
    public DateTimeOffset? ExpiredAtUtc { get; private set; }
    public BillingCycle? BillingCycle { get; private set; }
    public decimal? AgreedPrice { get; private set; }
    public string? CurrencyCode { get; private set; }
    public DateTimeOffset? PriceEffectiveFromUtc { get; private set; }
    public PlanId? PendingPlanId { get; private set; }
    public DateTimeOffset? PendingPlanEffectiveAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }

    private Subscription(
        SubscriptionId id,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        PlanId planId,
        PlanVersionId planVersionId,
        TrialDefinitionId? trialDefinitionId,
        SubscriptionStatus status,
        DateTimeOffset? trialStartUtc,
        DateTimeOffset? trialEndUtc,
        DateTimeOffset? paidPeriodStartUtc,
        DateTimeOffset? paidPeriodEndUtc,
        BillingCycle? billingCycle,
        decimal? agreedPrice,
        string? currencyCode,
        DateTimeOffset? priceEffectiveFromUtc,
        PlanId? pendingPlanId,
        DateTimeOffset? pendingPlanEffectiveAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        Id = id;
        OrganizationId = organizationId;
        ProductCode = productCode;
        PlanId = planId;
        PlanVersionId = planVersionId;
        TrialDefinitionId = trialDefinitionId;
        Status = status;
        TrialStartUtc = trialStartUtc;
        TrialEndUtc = trialEndUtc;
        PaidPeriodStartUtc = paidPeriodStartUtc;
        PaidPeriodEndUtc = paidPeriodEndUtc;
        BillingCycle = billingCycle;
        AgreedPrice = agreedPrice;
        CurrencyCode = currencyCode;
        PriceEffectiveFromUtc = priceEffectiveFromUtc;
        PendingPlanId = pendingPlanId;
        PendingPlanEffectiveAtUtc = pendingPlanEffectiveAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Version = version;
    }

    public static Subscription StartTrial(
        PlatformOrganizationId organizationId,
        Plan plan,
        PlanVersion planVersion,
        TrialDefinition trialDefinition,
        DateTimeOffset utcNow,
        SubscriptionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(planVersion);
        ArgumentNullException.ThrowIfNull(trialDefinition);
        DomainTime.EnsureUtc(utcNow);

        EnsureSameProduct(plan.ProductCode, planVersion.ProductCode, trialDefinition.ProductCode);
        if (planVersion.PlanId != plan.Id)
        {
            throw new DomainException(DomainErrorCodes.ProductMismatch, "Plan version does not belong to the plan.");
        }

        if (planVersion.Status != PlanVersionStatus.Published)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSubscriptionTransition,
                "Trial requires a published plan version.");
        }

        if (trialDefinition.Status != TrialDefinitionStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSubscriptionTransition,
                "Trial definition must be active.");
        }

        if (trialDefinition.PlanId is not null && trialDefinition.PlanId != plan.Id)
        {
            throw new DomainException(
                DomainErrorCodes.ProductMismatch,
                "Trial definition does not belong to the plan.");
        }

        var trialEnd = utcNow.Add(trialDefinition.Duration);
        var subscription = new Subscription(
            id ?? SubscriptionId.New(),
            organizationId,
            plan.ProductCode,
            plan.Id,
            planVersion.Id,
            trialDefinition.Id,
            SubscriptionStatus.Trialing,
            utcNow,
            trialEnd,
            paidPeriodStartUtc: null,
            paidPeriodEndUtc: null,
            billingCycle: Subscriptions.BillingCycle.Monthly,
            agreedPrice: plan.MonthlyPrice,
            currencyCode: plan.CurrencyCode,
            priceEffectiveFromUtc: utcNow,
            pendingPlanId: null,
            pendingPlanEffectiveAtUtc: null,
            utcNow,
            utcNow,
            version: 1);
        return subscription;
    }

    public static Subscription ActivatePaid(
        PlatformOrganizationId organizationId,
        Plan plan,
        PlanVersion planVersion,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        BillingCycle billingCycle,
        DateTimeOffset utcNow,
        SubscriptionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(planVersion);
        DomainTime.EnsureUtc(utcNow);
        DomainTime.EnsureUtc(periodStartUtc);
        DomainTime.EnsureUtc(periodEndUtc);
        EnsureSameProduct(plan.ProductCode, planVersion.ProductCode);

        if (periodEndUtc <= periodStartUtc)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEffectiveRange,
                "Paid period end must be after start.");
        }

        if (planVersion.PlanId != plan.Id || planVersion.Status != PlanVersionStatus.Published)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSubscriptionTransition,
                "Paid activation requires a published plan version for the plan.");
        }

        return new Subscription(
            id ?? SubscriptionId.New(),
            organizationId,
            plan.ProductCode,
            plan.Id,
            planVersion.Id,
            trialDefinitionId: null,
            SubscriptionStatus.Active,
            trialStartUtc: null,
            trialEndUtc: null,
            periodStartUtc,
            periodEndUtc,
            billingCycle,
            plan.PriceForCycle(billingCycle),
            plan.CurrencyCode,
            utcNow,
            pendingPlanId: null,
            pendingPlanEffectiveAtUtc: null,
            utcNow,
            utcNow,
            version: 1);
    }

    internal static Subscription Rehydrate(
        SubscriptionId id,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        PlanId planId,
        PlanVersionId planVersionId,
        TrialDefinitionId? trialDefinitionId,
        SubscriptionStatus status,
        DateTimeOffset? trialStartUtc,
        DateTimeOffset? trialEndUtc,
        DateTimeOffset? paidPeriodStartUtc,
        DateTimeOffset? paidPeriodEndUtc,
        DateTimeOffset? gracePeriodEndUtc,
        DateTimeOffset? suspendedAtUtc,
        DateTimeOffset? cancelledAtUtc,
        DateTimeOffset? pastDueAtUtc,
        DateTimeOffset? expiredAtUtc,
        BillingCycle? billingCycle,
        decimal? agreedPrice,
        string? currencyCode,
        DateTimeOffset? priceEffectiveFromUtc,
        PlanId? pendingPlanId,
        DateTimeOffset? pendingPlanEffectiveAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        var subscription = new Subscription(
            id,
            organizationId,
            productCode,
            planId,
            planVersionId,
            trialDefinitionId,
            status,
            trialStartUtc,
            trialEndUtc,
            paidPeriodStartUtc,
            paidPeriodEndUtc,
            billingCycle,
            agreedPrice,
            currencyCode,
            priceEffectiveFromUtc,
            pendingPlanId,
            pendingPlanEffectiveAtUtc,
            createdAtUtc,
            updatedAtUtc,
            version);

        subscription.GracePeriodEndUtc = gracePeriodEndUtc;
        subscription.SuspendedAtUtc = suspendedAtUtc;
        subscription.CancelledAtUtc = cancelledAtUtc;
        subscription.PastDueAtUtc = pastDueAtUtc;
        subscription.ExpiredAtUtc = expiredAtUtc;
        return subscription;
    }

    /// <summary>Statuses representing a subscription that still occupies the one-active-like-per-organization-product slot.</summary>
    public static bool IsActiveLike(SubscriptionStatus status) =>
        status is SubscriptionStatus.Trialing
            or SubscriptionStatus.Active
            or SubscriptionStatus.GracePeriod
            or SubscriptionStatus.PastDue
            or SubscriptionStatus.Suspended;

    public void ActivateFromTrial(
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        BillingCycle billingCycle,
        Plan plan,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(plan);
        DomainTime.EnsureUtc(utcNow);
        DomainTime.EnsureUtc(periodStartUtc);
        DomainTime.EnsureUtc(periodEndUtc);
        if (periodEndUtc <= periodStartUtc)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEffectiveRange,
                "Paid period end must be after start.");
        }

        TransitionTo(SubscriptionStatus.Active, utcNow);
        PaidPeriodStartUtc = periodStartUtc;
        PaidPeriodEndUtc = periodEndUtc;
        BillingCycle = billingCycle;
        AgreedPrice = plan.PriceForCycle(billingCycle);
        CurrencyCode = plan.CurrencyCode;
        PriceEffectiveFromUtc = utcNow;
    }

    public void ActivateFromTrial(DateTimeOffset periodStartUtc, DateTimeOffset periodEndUtc, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        DomainTime.EnsureUtc(periodStartUtc);
        DomainTime.EnsureUtc(periodEndUtc);
        if (periodEndUtc <= periodStartUtc)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEffectiveRange,
                "Paid period end must be after start.");
        }

        TransitionTo(SubscriptionStatus.Active, utcNow);
        PaidPeriodStartUtc = periodStartUtc;
        PaidPeriodEndUtc = periodEndUtc;
    }

    /// <summary>
    /// Converts a Trialing or Expired trial subscription to a paid subscription, optionally on a different plan.
    /// </summary>
    public void ConvertTrialToPaid(
        Plan targetPlan,
        PlanVersion version,
        BillingCycle billingCycle,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(targetPlan);
        ArgumentNullException.ThrowIfNull(version);
        DomainTime.EnsureUtc(utcNow);
        DomainTime.EnsureUtc(periodStartUtc);
        DomainTime.EnsureUtc(periodEndUtc);

        if (TrialStartUtc is null && TrialDefinitionId is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSubscriptionTransition,
                "Trial conversion requires a subscription that used a trial.");
        }

        if (Status is not SubscriptionStatus.Trialing and not SubscriptionStatus.Expired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSubscriptionTransition,
                "Trial conversion requires Trialing or Expired status.");
        }

        if (periodEndUtc <= periodStartUtc)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEffectiveRange,
                "Paid period end must be after start.");
        }

        EnsureSameProduct(ProductCode, targetPlan.ProductCode, version.ProductCode);
        if (version.PlanId != targetPlan.Id || version.Status != PlanVersionStatus.Published)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanVersionTransition,
                "Trial conversion requires a published plan version for the target plan.");
        }

        PlanId = targetPlan.Id;
        PlanVersionId = version.Id;
        BillingCycle = billingCycle;
        AgreedPrice = targetPlan.PriceForCycle(billingCycle);
        CurrencyCode = targetPlan.CurrencyCode;
        PriceEffectiveFromUtc = utcNow;
        PaidPeriodStartUtc = periodStartUtc;
        PaidPeriodEndUtc = periodEndUtc;
        PendingPlanId = null;
        PendingPlanEffectiveAtUtc = null;

        if (Status == SubscriptionStatus.Expired)
        {
            Status = SubscriptionStatus.Active;
            ExpiredAtUtc = null;
            UpdatedAtUtc = utcNow;
            Version++;
        }
        else
        {
            TransitionTo(SubscriptionStatus.Active, utcNow);
        }
    }

    public void EnterGracePeriod(DateTimeOffset gracePeriodEndUtc, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        DomainTime.EnsureUtc(gracePeriodEndUtc);

        if (PaidPeriodEndUtc is not null && gracePeriodEndUtc < PaidPeriodEndUtc.Value)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEffectiveRange,
                "Grace period end cannot precede the current paid period end.");
        }

        TransitionTo(SubscriptionStatus.GracePeriod, utcNow);
        GracePeriodEndUtc = gracePeriodEndUtc;
    }

    public void MarkPastDue(DateTimeOffset utcNow)
    {
        TransitionTo(SubscriptionStatus.PastDue, utcNow);
        PastDueAtUtc = utcNow;
    }

    public void Suspend(DateTimeOffset utcNow)
    {
        TransitionTo(SubscriptionStatus.Suspended, utcNow);
        SuspendedAtUtc = utcNow;
    }

    /// <summary>
    /// Reactivates a subscription. Reactivating from GracePeriod or PastDue requires a new, valid
    /// paid period range (the prior period has lapsed). Reactivating from Suspended may optionally
    /// supply a paid period range; if omitted, the previously recorded paid period is retained.
    /// </summary>
    public void Reactivate(
        DateTimeOffset utcNow,
        DateTimeOffset? periodStartUtc = null,
        DateTimeOffset? periodEndUtc = null)
    {
        if (Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSubscriptionTransition,
                "Cancelled or expired subscriptions cannot be reactivated; create a new subscription.");
        }

        var requiresPeriod = Status is SubscriptionStatus.GracePeriod or SubscriptionStatus.PastDue;
        if (requiresPeriod && (periodStartUtc is null || periodEndUtc is null))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEffectiveRange,
                "Reactivating from grace period or past due requires a new paid period range.");
        }

        if (periodStartUtc is not null && periodEndUtc is not null)
        {
            DomainTime.EnsureUtc(periodStartUtc.Value);
            DomainTime.EnsureUtc(periodEndUtc.Value);
            if (periodEndUtc.Value <= periodStartUtc.Value)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidEffectiveRange,
                    "Paid period end must be after start.");
            }
        }

        TransitionTo(SubscriptionStatus.Active, utcNow);

        if (periodStartUtc is not null && periodEndUtc is not null)
        {
            PaidPeriodStartUtc = periodStartUtc;
            PaidPeriodEndUtc = periodEndUtc;
        }

        GracePeriodEndUtc = null;
        PastDueAtUtc = null;
        SuspendedAtUtc = null;
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        TransitionTo(SubscriptionStatus.Cancelled, utcNow);
        CancelledAtUtc = utcNow;
    }

    public void Expire(DateTimeOffset utcNow)
    {
        TransitionTo(SubscriptionStatus.Expired, utcNow);
        ExpiredAtUtc = utcNow;
    }

    private void TransitionTo(SubscriptionStatus target, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            SubscriptionStatus.Trialing => target is SubscriptionStatus.Active or SubscriptionStatus.GracePeriod
                or SubscriptionStatus.PastDue or SubscriptionStatus.Suspended or SubscriptionStatus.Cancelled
                or SubscriptionStatus.Expired,
            SubscriptionStatus.Active => target is SubscriptionStatus.GracePeriod or SubscriptionStatus.PastDue
                or SubscriptionStatus.Suspended or SubscriptionStatus.Cancelled or SubscriptionStatus.Expired,
            SubscriptionStatus.GracePeriod => target is SubscriptionStatus.Active or SubscriptionStatus.PastDue
                or SubscriptionStatus.Suspended or SubscriptionStatus.Cancelled or SubscriptionStatus.Expired,
            SubscriptionStatus.PastDue => target is SubscriptionStatus.Active or SubscriptionStatus.GracePeriod
                or SubscriptionStatus.Suspended or SubscriptionStatus.Cancelled or SubscriptionStatus.Expired,
            SubscriptionStatus.Suspended => target is SubscriptionStatus.Active or SubscriptionStatus.Cancelled
                or SubscriptionStatus.Expired,
            SubscriptionStatus.Cancelled => false,
            SubscriptionStatus.Expired => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSubscriptionTransition,
                $"Cannot transition Subscription from {Status} to {target}.");
        }

        Status = target;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Platform catalog migration only: rebinds Plan/PlanVersion without changing lifecycle status.
    /// Does not assign Product roles or change Account Class.
    /// </summary>
    public void RebindCommercialPackage(Plan plan, PlanVersion planVersion, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(planVersion);
        DomainTime.EnsureUtc(utcNow);
        EnsureSameProduct(ProductCode, plan.ProductCode, planVersion.ProductCode);
        if (planVersion.PlanId != plan.Id)
        {
            throw new DomainException(
                DomainErrorCodes.ProductMismatch,
                "Plan version does not belong to the target Plan.");
        }

        if (planVersion.Status != PlanVersionStatus.Published)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanVersionTransition,
                "Only a published Plan version may be bound to a Subscription.");
        }

        PlanId = plan.Id;
        PlanVersionId = planVersion.Id;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    public void ApplyImmediatePlanUpgrade(
        Plan newPlan,
        PlanVersion version,
        BillingCycle cycle,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(newPlan);
        ArgumentNullException.ThrowIfNull(version);
        DomainTime.EnsureUtc(utcNow);
        EnsureSameProduct(ProductCode, newPlan.ProductCode, version.ProductCode);
        if (version.PlanId != newPlan.Id || version.Status != PlanVersionStatus.Published)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanVersionTransition,
                "Plan upgrade requires a published plan version.");
        }

        PlanId = newPlan.Id;
        PlanVersionId = version.Id;
        BillingCycle = cycle;
        AgreedPrice = newPlan.PriceForCycle(cycle);
        CurrencyCode = newPlan.CurrencyCode;
        PriceEffectiveFromUtc = utcNow;
        PendingPlanId = null;
        PendingPlanEffectiveAtUtc = null;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    public void SchedulePlanDowngrade(PlanId pendingPlanId, DateTimeOffset effectiveAt, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(pendingPlanId);
        DomainTime.EnsureUtc(utcNow);
        DomainTime.EnsureUtc(effectiveAt);

        PendingPlanId = pendingPlanId;
        PendingPlanEffectiveAtUtc = effectiveAt;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    public void ApplyPendingPlanChange(
        Plan newPlan,
        PlanVersion version,
        BillingCycle cycle,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(newPlan);
        ArgumentNullException.ThrowIfNull(version);
        DomainTime.EnsureUtc(utcNow);
        EnsureSameProduct(ProductCode, newPlan.ProductCode, version.ProductCode);
        if (version.PlanId != newPlan.Id || version.Status != PlanVersionStatus.Published)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanVersionTransition,
                "Pending plan change requires a published plan version.");
        }

        if (PendingPlanId is null || PendingPlanId != newPlan.Id)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSubscriptionTransition,
                "No matching pending plan change for the requested plan.");
        }

        PlanId = newPlan.Id;
        PlanVersionId = version.Id;
        BillingCycle = cycle;
        AgreedPrice = newPlan.PriceForCycle(cycle);
        CurrencyCode = newPlan.CurrencyCode;
        PriceEffectiveFromUtc = utcNow;
        PendingPlanId = null;
        PendingPlanEffectiveAtUtc = null;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    private static void EnsureSameProduct(params ProductCode[] codes)
    {
        var first = codes[0];
        foreach (var code in codes)
        {
            if (code != first)
            {
                throw new DomainException(
                    DomainErrorCodes.ProductMismatch,
                    "Plan, plan version, and trial must share the same ProductCode.");
            }
        }
    }
}
