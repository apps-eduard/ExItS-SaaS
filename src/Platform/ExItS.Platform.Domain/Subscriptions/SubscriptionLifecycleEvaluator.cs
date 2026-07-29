using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Subscriptions;

/// <summary>Deterministic outcome suggested by evaluating a subscription's lifecycle against the current time.</summary>
public enum SubscriptionLifecycleAction
{
    None = 0,
    ExpireTrial = 1,
    ExpirePaid = 2,
    ExpireGrace = 3,
    SuggestPastDue = 4,
    SuggestSuspend = 5
}

/// <summary>
/// Pure, deterministic lifecycle evaluation. No database access, no clock access — the caller
/// (an application-layer scheduled/manual use case) supplies <paramref name="utcNow"/> via IClock.
/// </summary>
public static class SubscriptionLifecycleEvaluator
{
    public static SubscriptionLifecycleAction Evaluate(Subscription subscription, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        DomainTime.EnsureUtc(utcNow);

        return subscription.Status switch
        {
            SubscriptionStatus.Trialing when subscription.TrialEndUtc is not null
                && subscription.TrialEndUtc.Value < utcNow => SubscriptionLifecycleAction.ExpireTrial,

            // Active period lapsing is not auto-expired here; entering grace period is an explicit
            // command issued by the billing/commercial workflow, not inferred by this evaluator.
            SubscriptionStatus.Active => SubscriptionLifecycleAction.None,

            SubscriptionStatus.GracePeriod when subscription.GracePeriodEndUtc is not null
                && subscription.GracePeriodEndUtc.Value < utcNow => SubscriptionLifecycleAction.SuggestPastDue,

            _ => SubscriptionLifecycleAction.None
        };
    }
}
