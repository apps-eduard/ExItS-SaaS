using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>Gross (pre-discount) economics of one checkout line, used as a discount base.</summary>
public sealed record SaleDiscountLineBasis(
    int LineNumber,
    CatalogProductId ProductId,
    decimal GrossLineTotal);

/// <summary>Per-line discount outcome. Net line total is what a sale line persists as LineTotal.</summary>
public sealed record SaleDiscountLineOutcome(
    int LineNumber,
    decimal GrossLineTotal,
    decimal LineDiscountAmount,
    decimal SaleDiscountAllocatedAmount)
{
    public decimal TotalLineDiscount =>
        SaleMoney.RoundMoney(LineDiscountAmount + SaleDiscountAllocatedAmount);

    public decimal NetLineTotal => SaleMoney.RoundMoney(GrossLineTotal - TotalLineDiscount);
}

/// <summary>
/// Snapshot of one applied discount intent: what was requested and what it actually came to in pesos.
/// Materialized into a <see cref="SaleCommercialDiscountAdjustment"/> once the sale exists.
/// </summary>
public sealed record SaleCommercialDiscountAdjustmentDraft(
    SaleDiscountScope Scope,
    SaleDiscountMethod Method,
    decimal RequestedValue,
    decimal CalculatedAmount,
    string Reason,
    int? LineNumber);

/// <summary>Complete server-computed discount outcome for one cart.</summary>
public sealed record SaleCommercialDiscountResult(
    IReadOnlyList<SaleDiscountLineOutcome> Lines,
    IReadOnlyList<SaleCommercialDiscountAdjustmentDraft> Adjustments)
{
    public decimal GrossSubtotal => SaleMoney.RoundMoney(Lines.Sum(l => l.GrossLineTotal));

    public decimal LineDiscountTotal => SaleMoney.RoundMoney(Lines.Sum(l => l.LineDiscountAmount));

    public decimal SaleDiscountTotal =>
        SaleMoney.RoundMoney(Lines.Sum(l => l.SaleDiscountAllocatedAmount));

    public decimal DiscountTotal => SaleMoney.RoundMoney(LineDiscountTotal + SaleDiscountTotal);

    /// <summary>Net pre-tax subtotal. This is the base every tax calculation must use.</summary>
    public decimal NetSubtotal => SaleMoney.RoundMoney(Lines.Sum(l => l.NetLineTotal));
}

/// <summary>
/// Authoritative commercial-discount money engine. The server always recomputes every peso here;
/// client-supplied discount amounts are never trusted, only the requested scope/method/value/reason.
///
/// Order of operations is fixed so the same cart always produces the same numbers:
/// 1. every line starts at its gross line total (UnitPrice × quantity, already rounded);
/// 2. line-scoped intents apply in request order, each against that line's remaining base;
/// 3. sale-scoped intents apply in request order, each against the sum of remaining line bases and
///    allocated by <see cref="SaleCommercialDiscountAllocator"/>.
///
/// A discount only ever reduces money. It never rewrites <see cref="SaleLine.UnitPrice"/> and never
/// touches inventory quantity, so stock movements and weighed quantities are unaffected.
/// </summary>
public static class SaleCommercialDiscountCalculator
{
    public static SaleCommercialDiscountResult Apply(
        IReadOnlyList<SaleDiscountLineBasis> lines,
        IReadOnlyList<CommercialDiscountIntent>? intents)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var ordered = lines.OrderBy(l => l.LineNumber).ToArray();
        var lineDiscounts = new decimal[ordered.Length];
        var saleAllocations = new decimal[ordered.Length];

        if (intents is null || intents.Count == 0)
        {
            return new SaleCommercialDiscountResult(BuildOutcomes(ordered, lineDiscounts, saleAllocations), []);
        }

        if (intents.Count > SaleCommercialDiscountRules.MaxIntentCount)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountTooMany,
                $"A sale may carry at most {SaleCommercialDiscountRules.MaxIntentCount} commercial discounts.");
        }

        // Validate every intent before touching any amount so a rejected request changes nothing.
        var normalized = intents.Select(Normalize).ToArray();
        var adjustments = new List<SaleCommercialDiscountAdjustmentDraft>(normalized.Length);

        foreach (var intent in normalized.Where(i => i.Intent.Scope == SaleDiscountScope.Line))
        {
            var index = ResolveLineIndex(ordered, intent.Intent);
            var eligible = SaleMoney.RoundMoney(ordered[index].GrossLineTotal - lineDiscounts[index]);
            var amount = ComputeAmount(intent.Intent, intent.Value, eligible);

            lineDiscounts[index] = SaleMoney.RoundMoney(lineDiscounts[index] + amount);
            adjustments.Add(new SaleCommercialDiscountAdjustmentDraft(
                SaleDiscountScope.Line,
                intent.Intent.Method,
                intent.Value,
                amount,
                intent.Reason,
                ordered[index].LineNumber));
        }

        foreach (var intent in normalized.Where(i => i.Intent.Scope == SaleDiscountScope.Sale))
        {
            var bases = new decimal[ordered.Length];
            for (var i = 0; i < ordered.Length; i++)
            {
                bases[i] = SaleMoney.RoundMoney(
                    ordered[i].GrossLineTotal - lineDiscounts[i] - saleAllocations[i]);
            }

            var eligible = SaleMoney.RoundMoney(bases.Where(b => b > 0m).Sum());
            var amount = ComputeAmount(intent.Intent, intent.Value, eligible);
            var allocations = SaleCommercialDiscountAllocator.Allocate(bases, amount);

            for (var i = 0; i < ordered.Length; i++)
            {
                saleAllocations[i] = SaleMoney.RoundMoney(saleAllocations[i] + allocations[i]);
            }

            adjustments.Add(new SaleCommercialDiscountAdjustmentDraft(
                SaleDiscountScope.Sale,
                intent.Intent.Method,
                intent.Value,
                SaleMoney.RoundMoney(allocations.Sum()),
                intent.Reason,
                null));
        }

        return new SaleCommercialDiscountResult(
            BuildOutcomes(ordered, lineDiscounts, saleAllocations),
            adjustments);
    }

    private static IReadOnlyList<SaleDiscountLineOutcome> BuildOutcomes(
        IReadOnlyList<SaleDiscountLineBasis> ordered,
        decimal[] lineDiscounts,
        decimal[] saleAllocations)
    {
        var outcomes = new List<SaleDiscountLineOutcome>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            outcomes.Add(new SaleDiscountLineOutcome(
                ordered[i].LineNumber,
                ordered[i].GrossLineTotal,
                lineDiscounts[i],
                saleAllocations[i]));
        }

        return outcomes;
    }

    private static (CommercialDiscountIntent Intent, decimal Value, string Reason) Normalize(
        CommercialDiscountIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var reason = SaleCommercialDiscountRules.NormalizeReason(intent.Reason);
        var value = intent.Method switch
        {
            SaleDiscountMethod.Percentage => SaleCommercialDiscountRules.NormalizePercentage(intent.Value),
            SaleDiscountMethod.FixedAmount => SaleCommercialDiscountRules.NormalizeFixedAmount(intent.Value),
            _ => throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidMethod,
                "Unrecognized commercial discount method.")
        };

        if (intent.Scope is not (SaleDiscountScope.Line or SaleDiscountScope.Sale))
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidScope,
                "Unrecognized commercial discount scope.");
        }

        return (intent, value, reason);
    }

    private static decimal ComputeAmount(CommercialDiscountIntent intent, decimal value, decimal eligibleBase)
    {
        if (eligibleBase <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountExceedsEligible,
                "There is no remaining discountable amount for this commercial discount.");
        }

        var amount = intent.Method == SaleDiscountMethod.Percentage
            ? SaleMoney.RoundMoney(eligibleBase * value / 100m)
            : value;

        if (amount > eligibleBase)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountExceedsEligible,
                $"A commercial discount of {amount} exceeds the eligible base of {eligibleBase}.");
        }

        return amount;
    }

    private static int ResolveLineIndex(IReadOnlyList<SaleDiscountLineBasis> ordered, CommercialDiscountIntent intent)
    {
        if (intent.LineNumber is int lineNumber)
        {
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].LineNumber == lineNumber)
                {
                    return i;
                }
            }

            throw new DomainException(
                DomainErrorCodes.SaleDiscountLineUnmatched,
                $"Line-scoped commercial discount targets line {lineNumber}, which is not in this sale.");
        }

        if (intent.ProductId is null)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountLineUnmatched,
                "A line-scoped commercial discount must identify its line by line number or product.");
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
                DomainErrorCodes.SaleDiscountLineUnmatched,
                "Line-scoped commercial discount targets a product that is not in this sale."),
            _ => throw new DomainException(
                DomainErrorCodes.SaleDiscountLineAmbiguous,
                "Line-scoped commercial discount matches more than one line; target it by line number.")
        };
    }
}
