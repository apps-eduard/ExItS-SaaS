using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Domain.Payments;

/// <summary>
/// Canonical prepaid billing quote. Server-authoritative — clients must not invent discounts.
/// Annual uses the plan's list <see cref="Plan.AnnualPrice"/> when set; other prepaid cycles use
/// configurable percent discounts against months × monthly list price.
/// </summary>
public sealed record SubscriptionPriceQuote(
    string PlanKey,
    BillingCycle BillingCycle,
    int PeriodMonths,
    decimal MonthlyListPrice,
    decimal BaseAmount,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal FinalAmount,
    decimal EquivalentMonthlyAmount,
    string CurrencyCode);

/// <summary>
/// Central subscription billing-cycle pricing. Do not duplicate percent math in UI layers.
/// </summary>
public static class SubscriptionBillingPricing
{
    /// <summary>Initial configurable prepaid discounts (Quarterly / SixMonths). Annual uses list price.</summary>
    public const decimal QuarterlyDiscountPercent = 5m;
    public const decimal SixMonthsDiscountPercent = 10m;
    public const decimal AnnualFallbackDiscountPercent = 15m;

    public static IReadOnlyList<BillingCycle> AllCycles { get; } =
    [
        BillingCycle.Monthly,
        BillingCycle.Quarterly,
        BillingCycle.SixMonths,
        BillingCycle.Annual
    ];

    public static int PeriodMonths(BillingCycle cycle) =>
        cycle switch
        {
            BillingCycle.Monthly => 1,
            BillingCycle.Quarterly => 3,
            BillingCycle.SixMonths => 6,
            BillingCycle.Annual => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(cycle), cycle, "Unsupported billing cycle.")
        };

    public static SubscriptionPriceQuote Quote(Plan plan, BillingCycle cycle)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return Quote(
            plan.PlanKey,
            plan.MonthlyPrice,
            plan.AnnualPrice,
            plan.CurrencyCode,
            cycle);
    }

    public static SubscriptionPriceQuote Quote(
        string planKey,
        decimal monthlyListPrice,
        decimal annualListPrice,
        string currencyCode,
        BillingCycle cycle)
    {
        if (string.IsNullOrWhiteSpace(planKey))
        {
            throw new ArgumentException("Plan key is required.", nameof(planKey));
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency is required.", nameof(currencyCode));
        }

        var months = PeriodMonths(cycle);
        var monthly = RoundMoney(monthlyListPrice);
        var baseAmount = RoundMoney(monthly * months);

        if (cycle == BillingCycle.Monthly)
        {
            return new SubscriptionPriceQuote(
                planKey.Trim(),
                cycle,
                months,
                monthly,
                baseAmount,
                DiscountPercent: 0m,
                DiscountAmount: 0m,
                FinalAmount: monthly,
                EquivalentMonthlyAmount: monthly,
                currencyCode.Trim().ToUpperInvariant());
        }

        decimal finalAmount;
        decimal discountPercent;
        decimal discountAmount;

        if (cycle == BillingCycle.Annual && annualListPrice > 0m)
        {
            // Preserve canonical annual list price (may differ from percent-model fallback).
            finalAmount = RoundMoney(annualListPrice);
            discountAmount = RoundMoney(Math.Max(0m, baseAmount - finalAmount));
            discountPercent = baseAmount > 0m
                ? RoundPercent((discountAmount / baseAmount) * 100m)
                : 0m;
        }
        else
        {
            discountPercent = cycle switch
            {
                BillingCycle.Quarterly => QuarterlyDiscountPercent,
                BillingCycle.SixMonths => SixMonthsDiscountPercent,
                BillingCycle.Annual => AnnualFallbackDiscountPercent,
                _ => 0m
            };
            discountAmount = RoundMoney(baseAmount * (discountPercent / 100m));
            finalAmount = RoundMoney(baseAmount - discountAmount);
        }

        var equivalentMonthly = months > 0
            ? RoundMoney(finalAmount / months)
            : finalAmount;

        return new SubscriptionPriceQuote(
            planKey.Trim(),
            cycle,
            months,
            monthly,
            baseAmount,
            discountPercent,
            discountAmount,
            finalAmount,
            equivalentMonthly,
            currencyCode.Trim().ToUpperInvariant());
    }

    public static decimal RoundMoney(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static decimal RoundPercent(decimal percent) =>
        Math.Round(percent, 2, MidpointRounding.AwayFromZero);
}
