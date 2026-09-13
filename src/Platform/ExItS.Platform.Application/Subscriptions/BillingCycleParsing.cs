using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Subscriptions;

public static class BillingCycleParsing
{
    public static BillingCycle ParseOrDefault(string? billingCycle, BillingCycle fallback = BillingCycle.Monthly)
    {
        if (string.IsNullOrWhiteSpace(billingCycle))
        {
            return fallback;
        }

        return ParseRequired(billingCycle);
    }

    public static BillingCycle ParseRequired(string billingCycle)
    {
        var normalized = billingCycle.Trim();
        if (Enum.TryParse<BillingCycle>(normalized, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        // Friendly aliases used in query strings / UI.
        parsed = normalized.ToLowerInvariant() switch
        {
            "3months" or "3-months" or "quarter" => BillingCycle.Quarterly,
            "6months" or "6-months" or "semiannual" or "semi-annual" => BillingCycle.SixMonths,
            "year" or "yearly" => BillingCycle.Annual,
            "month" => BillingCycle.Monthly,
            _ => (BillingCycle)(-1)
        };

        if (parsed is BillingCycle.Monthly or BillingCycle.Quarterly or BillingCycle.SixMonths or BillingCycle.Annual)
        {
            return parsed;
        }

        throw new DomainException(
            ApplicationErrorCodes.InvalidBillingCycle,
            "BillingCycle must be Monthly, Quarterly, SixMonths, or Annual.");
    }
}
