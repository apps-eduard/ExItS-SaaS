using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Maps a fulfilled customer order into sale line drafts for Utang settlement posting.
/// </summary>
public static class CustomerOrderUtangSettlementLines
{
    public const string DeliveryFeeLineName = "Delivery fee";

    public static bool IsInventoryCostLine(SaleLineDraft draft) =>
        !string.Equals(draft.NameSnapshot, DeliveryFeeLineName, StringComparison.Ordinal);

    public static IReadOnlyList<SaleLineDraft> FromOrder(CustomerOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var drafts = new List<SaleLineDraft>(order.Lines.Count + 1);
        foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
        {
            var effectiveUnitPrice = line.Quantity > 0m
                ? SaleMoney.RoundMoney(line.LineTotal / line.Quantity)
                : line.UnitPrice;

            drafts.Add(new SaleLineDraft(
                line.ProductId,
                line.NameSnapshot,
                line.SkuSnapshot,
                BarcodeSnapshot: null,
                line.UnitSnapshot,
                effectiveUnitPrice,
                line.Quantity));
        }

        if (order.DeliveryFee > 0m)
        {
            var anchor = order.Lines.OrderBy(l => l.LineNumber).First();
            drafts.Add(new SaleLineDraft(
                anchor.ProductId,
                DeliveryFeeLineName,
                SkuSnapshot: null,
                BarcodeSnapshot: null,
                UnitOfMeasure.Piece,
                order.DeliveryFee,
                1m));
        }

        return drafts;
    }
}
