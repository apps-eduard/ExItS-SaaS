namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Combined quote outcome: price overrides applied to drafts, then commercial discounts on the
/// resulting gross line totals. <see cref="PricedDrafts"/> carry the overridden UnitPrice values.
/// </summary>
public sealed record SaleQuoteMoneyResult(
    SalePriceOverrideResult PriceOverrides,
    SaleCommercialDiscountResult Discounts,
    IReadOnlyList<SaleLineDraft> PricedDrafts);
