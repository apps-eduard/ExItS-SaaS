using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Applies per-sale unit-price override intents to already-priced draft lines. Order of operations
/// for checkout money is fixed: resolve baseline → override draft UnitPrice → build sale lines →
/// commercial discounts on GrossLineTotal. Catalog SellingPrice is never touched here.
/// </summary>
public static class SalePriceOverrideApplier
{
    /// <param name="maxDeviationRatio">
    /// When null, any positive requested price is allowed (Owner / unlimited). When set (manager
    /// path uses <see cref="SalePriceOverrideRules.ManagerMaxDeviationRatio"/>), deviations above
    /// the ratio are denied — never silently clamped.
    /// </param>
    public static SalePriceOverrideResult Apply(
        IReadOnlyList<SaleLineDraft> drafts,
        IReadOnlyList<SalePriceOverrideIntent>? intents,
        decimal? maxDeviationRatio)
    {
        ArgumentNullException.ThrowIfNull(drafts);

        if (intents is null || intents.Count == 0)
        {
            return new SalePriceOverrideResult(drafts, []);
        }

        if (intents.Count > SalePriceOverrideRules.MaxIntentCount)
        {
            throw new DomainException(
                DomainErrorCodes.SalePriceOverrideTooMany,
                $"A sale may carry at most {SalePriceOverrideRules.MaxIntentCount} price overrides.");
        }

        var working = drafts.ToArray();
        var adjustments = new List<SalePriceOverrideAdjustmentDraft>(intents.Count);
        var overriddenLines = new HashSet<int>();

        foreach (var intent in intents)
        {
            if (intent is null)
            {
                throw new DomainException(
                    DomainErrorCodes.SalePriceOverrideLineUnmatched,
                    "A sale price override entry was empty.");
            }

            var lineIndex = ResolveLineIndex(working, intent);
            var lineNumber = lineIndex + 1;
            if (!overriddenLines.Add(lineNumber))
            {
                throw new DomainException(
                    DomainErrorCodes.SalePriceOverrideLineAmbiguous,
                    $"Line {lineNumber} already has a sale price override.");
            }

            var baseline = working[lineIndex].UnitPrice;
            SalePriceOverrideRules.EnsureBaselineMatches(baseline, intent.ExpectedBaselineUnitPrice);

            var requested = SalePriceOverrideRules.NormalizeRequestedUnitPrice(intent.RequestedUnitPrice);
            var reason = SalePriceOverrideRules.NormalizeReason(intent.Reason);

            if (maxDeviationRatio is decimal limit
                && SalePriceOverrideRules.ExceedsManagerLimit(baseline, requested))
            {
                throw new DomainException(
                    DomainErrorCodes.SalePriceOverrideExceedsManagerLimit,
                    $"The requested unit price exceeds the manager limit of {limit:0.##}× baseline deviation.");
            }

            // No-op relative to baseline still records audit when explicitly requested.
            working[lineIndex] = working[lineIndex] with { UnitPrice = requested };
            adjustments.Add(new SalePriceOverrideAdjustmentDraft(
                lineNumber,
                SaleMoney.RoundMoney(baseline),
                requested,
                reason));
        }

        return new SalePriceOverrideResult(working, adjustments);
    }

    private static int ResolveLineIndex(IReadOnlyList<SaleLineDraft> ordered, SalePriceOverrideIntent intent)
    {
        if (intent.LineNumber is int lineNumber)
        {
            if (lineNumber < 1 || lineNumber > ordered.Count)
            {
                throw new DomainException(
                    DomainErrorCodes.SalePriceOverrideLineUnmatched,
                    $"Sale price override targets line {lineNumber}, which is not in this sale.");
            }

            return lineNumber - 1;
        }

        if (intent.ProductId is null)
        {
            throw new DomainException(
                DomainErrorCodes.SalePriceOverrideLineUnmatched,
                "A sale price override must identify its line by line number or product.");
        }

        var matches = new List<int>();
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].ProductId == intent.ProductId)
            {
                matches.Add(i);
            }
        }

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new DomainException(
                DomainErrorCodes.SalePriceOverrideLineUnmatched,
                "Sale price override targets a product that is not in this sale."),
            _ => throw new DomainException(
                DomainErrorCodes.SalePriceOverrideLineAmbiguous,
                "Sale price override matches more than one line; target it by line number.")
        };
    }
}
