namespace ExItS.Platform.Domain.Subscriptions;

public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    GracePeriod = 3,
    PastDue = 4,
    Suspended = 5,
    Cancelled = 6,
    Expired = 7
}
