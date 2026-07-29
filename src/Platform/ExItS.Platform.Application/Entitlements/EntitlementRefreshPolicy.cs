using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Entitlements;

/// <summary>
/// Determines how often an entitlement snapshot must be refreshed, and whether it should carry
/// an optional expiry, based on the funding subscription's status. Kept outside the Domain
/// composer so the policy can evolve (e.g. per-plan SLAs) without touching composition rules.
/// </summary>
public interface IEntitlementRefreshPolicy
{
    TimeSpan GetRefreshWindow(SubscriptionStatus status);

    DateTimeOffset? GetOptionalExpiryUtc(SubscriptionStatus status, DateTimeOffset utcNow);
}

/// <summary>
/// Provisional entitlement refresh policy for this development stage: every subscription status
/// gets a uniform 24-hour refresh window and no forced snapshot expiry. This is intentionally
/// simple pending a fuller SLA-driven policy (tracked as R-022, currently open) that may vary the
/// refresh window per status (e.g. tighter windows for GracePeriod/PastDue).
/// </summary>
public sealed class ProvisionalEntitlementRefreshPolicy : IEntitlementRefreshPolicy
{
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromHours(24);

    public TimeSpan GetRefreshWindow(SubscriptionStatus status) => RefreshWindow;

    public DateTimeOffset? GetOptionalExpiryUtc(SubscriptionStatus status, DateTimeOffset utcNow) => null;
}
