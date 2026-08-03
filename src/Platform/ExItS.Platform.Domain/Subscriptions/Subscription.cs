using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
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

        var trialEnd = utcNow.Add(trialDefinition.Duration);
        return new Subscription(
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
            utcNow,
            utcNow,
            version: 1);
    }

    public static Subscription ActivatePaid(
        PlatformOrganizationId organizationId,
        Plan plan,
        PlanVersion planVersion,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
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
