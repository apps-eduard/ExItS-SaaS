namespace ExItS.Platform.Domain.Subscriptions;

/// <summary>
/// SaaS subscription billing period. Default for new selections is <see cref="Monthly"/>.
/// Prepaid cycles use calendar month/year arithmetic via <c>SubscriptionBillingPeriods</c>.
/// </summary>
public enum BillingCycle
{
    Monthly = 0,
    Annual = 1,
    Quarterly = 2,
    SixMonths = 3
}
