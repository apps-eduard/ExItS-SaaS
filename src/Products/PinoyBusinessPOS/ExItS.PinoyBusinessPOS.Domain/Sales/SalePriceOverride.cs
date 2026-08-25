using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// One requested per-sale unit-price override. This is an intent, not a catalog change: the live
/// product <c>SellingPrice</c> / Today's Price is never rewritten. The server always resolves the
/// baseline from the already-priced draft line and applies <see cref="RequestedUnitPrice"/> to that
/// line's <see cref="SaleLine.UnitPrice"/> only.
///
/// Free (₱0) prices are not overrides — use a commercial discount (RMAP-B03). Authority limits
/// (Cashier deny / Manager ≤100% deviation / Owner unlimited) are enforced by the caller via
/// <see cref="SalePriceOverrideRules.ManagerMaxDeviationRatio"/>; this type only carries intent.
/// </summary>
public sealed record SalePriceOverrideIntent(
    decimal RequestedUnitPrice,
    string Reason,
    CatalogProductId? ProductId = null,
    int? LineNumber = null,
    decimal? ExpectedBaselineUnitPrice = null);

/// <summary>Shared validation limits and normalization for sale-line unit-price overrides.</summary>
public static class SalePriceOverrideRules
{
    /// <summary>
    /// Inclusive manager ceiling: <c>abs(requested − baseline) / baseline ≤ 1.00</c>.
    /// Exact 100% deviation is allowed; anything above requires unlimited authority.
    /// </summary>
    public const decimal ManagerMaxDeviationRatio = 1.00m;

    public const int ReasonMaxLength = SaleCommercialDiscountRules.ReasonMaxLength;
    public const int MaxIntentCount = 50;

    /// <summary>Trims a required operator reason. Whitespace-only reasons are rejected (same rules as B03).</summary>
    public static string NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.SalePriceOverrideReasonRequired,
                "A sale price override requires a reason.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.SalePriceOverrideReasonRequired,
                $"A sale price override reason must be at most {ReasonMaxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Validates a positive unit price with at most two decimal places. Zero/negative is denied.</summary>
    public static decimal NormalizeRequestedUnitPrice(decimal requestedUnitPrice)
    {
        if (requestedUnitPrice <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.SalePriceOverrideInvalidAmount,
                "A sale price override must be a positive unit price. Use a commercial discount for free items.");
        }

        if (requestedUnitPrice > SaleLine.MaxUnitPrice)
        {
            throw new DomainException(
                DomainErrorCodes.SalePriceOverrideInvalidAmount,
                "The sale price override unit price is too large.");
        }

        if (!SaleMoney.HasAtMostDecimals(requestedUnitPrice, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.SalePriceOverrideInvalidAmount,
                "A sale price override unit price must have at most 2 decimal places.");
        }

        return requestedUnitPrice;
    }

    /// <summary>
    /// True when the requested price exceeds the manager inclusive 100% deviation ceiling relative
    /// to the resolved baseline. A zero or negative baseline cannot host a manager override.
    /// </summary>
    public static bool ExceedsManagerLimit(decimal baselineUnitPrice, decimal requestedUnitPrice)
    {
        if (baselineUnitPrice <= 0m)
        {
            return true;
        }

        var deviation = Math.Abs(requestedUnitPrice - baselineUnitPrice) / baselineUnitPrice;
        return deviation > ManagerMaxDeviationRatio;
    }

    /// <summary>
    /// Ensures the optional client-expected baseline still matches the live resolved baseline.
    /// Mismatches fail closed as a conflict — never silent clamp.
    /// </summary>
    public static void EnsureBaselineMatches(decimal resolvedBaseline, decimal? expectedBaseline)
    {
        if (expectedBaseline is null)
        {
            return;
        }

        if (SaleMoney.RoundMoney(expectedBaseline.Value) != SaleMoney.RoundMoney(resolvedBaseline))
        {
            throw new DomainException(
                DomainErrorCodes.SalePriceOverrideStaleBaseline,
                "The catalog unit price changed since this override was prepared. Re-price the cart and retry.");
        }
    }
}

/// <summary>
/// Snapshot of one applied price-override intent before the sale exists: baseline vs applied unit
/// price and the operator reason. Materialized into <see cref="SalePriceOverrideAdjustment"/>.
/// </summary>
public sealed record SalePriceOverrideAdjustmentDraft(
    int LineNumber,
    decimal BaselineUnitPrice,
    decimal AppliedUnitPrice,
    string Reason);

/// <summary>Outcome of applying price overrides to priced draft lines.</summary>
public sealed record SalePriceOverrideResult(
    IReadOnlyList<SaleLineDraft> Drafts,
    IReadOnlyList<SalePriceOverrideAdjustmentDraft> Adjustments);
