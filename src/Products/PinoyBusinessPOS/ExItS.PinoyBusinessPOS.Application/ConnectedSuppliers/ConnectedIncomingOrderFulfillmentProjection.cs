using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Seller-facing fulfillment progress from buyer PO lines + goods receipts.
/// Damaged/rejected come from GRN lines; outstanding from buyer PO.
/// </summary>
public static class ConnectedIncomingOrderFulfillmentProjection
{
    public sealed record LineProgress(
        Guid SupplierProductId,
        decimal OrderedQty,
        decimal GoodReceivedQty,
        decimal DamagedQty,
        decimal MissingQty,
        decimal CancelledRemainingQty,
        decimal OutstandingQty,
        decimal UnitCost,
        decimal RemainingValue);

    public sealed record ReceiptLineSummary(
        Guid ProductId,
        string NameSnapshot,
        string UomSnapshot,
        decimal GoodQty,
        decimal DamagedQty,
        decimal MissingQty,
        decimal CancelledRemainingQty,
        string? DiscrepancyKind,
        string? DiscrepancyNote,
        string? RemainingAction);

    public sealed record ReceiptSummary(
        Guid GoodsReceiptId,
        string GrnNumber,
        DateOnly ReceivedDate,
        DateTimeOffset ReceivedAtUtc,
        string? DeliveryReference,
        string? Notes,
        string Status,
        decimal GoodQtyTotal,
        decimal DamagedQtyTotal,
        decimal MissingQtyTotal,
        decimal CancelledRemainingTotal,
        IReadOnlyList<ReceiptLineSummary> Lines);

    public static IReadOnlyDictionary<Guid, LineProgress> ProjectLineProgress(
        ConnectedPurchaseOrder order,
        PurchaseOrder buyerPo,
        IReadOnlyList<GoodsReceipt> receipts)
    {
        var posted = receipts
            .Where(r => r.Status == GoodsReceiptStatus.Posted)
            .ToList();

        var damagedByBuyerProduct = new Dictionary<Guid, decimal>();
        var missingByBuyerProduct = new Dictionary<Guid, decimal>();
        foreach (var receipt in posted)
        {
            foreach (var line in receipt.Lines)
            {
                Add(damagedByBuyerProduct, line.ProductId.Value, line.DamagedQty);
                Add(missingByBuyerProduct, line.ProductId.Value, line.RejectedQty);
            }
        }

        var bySupplier = new Dictionary<Guid, LineProgress>();
        foreach (var cpoLine in order.Lines)
        {
            var ordered = cpoLine.FulfillmentQty > 0m ? cpoLine.FulfillmentQty : cpoLine.Qty;
            var unitCost = cpoLine.EffectiveConfirmedUnitPrice > 0m
                ? cpoLine.EffectiveConfirmedUnitPrice
                : cpoLine.UnitPriceSnapshot;

            var poLine = FindBuyerLine(buyerPo, cpoLine.ProductId);
            var good = poLine?.ReceivedQty ?? 0m;
            var cancelled = poLine?.ClosedShortQty ?? 0m;
            var outstanding = poLine?.OutstandingQty
                ?? Math.Max(0m, ordered - good - cancelled);
            var buyerProductId = poLine?.ProductId?.Value;
            var damaged = buyerProductId is Guid bp && damagedByBuyerProduct.TryGetValue(bp, out var d)
                ? d
                : 0m;
            var missing = buyerProductId is Guid bp2 && missingByBuyerProduct.TryGetValue(bp2, out var m)
                ? m
                : 0m;

            bySupplier[cpoLine.ProductId.Value] = new LineProgress(
                cpoLine.ProductId.Value,
                ordered,
                good,
                damaged,
                missing,
                cancelled,
                outstanding,
                unitCost,
                SaleMoney.RoundMoney(outstanding * unitCost));
        }

        return bySupplier;
    }

    public static IReadOnlyList<ReceiptSummary> ProjectReceipts(
        IReadOnlyList<GoodsReceipt> receipts,
        decimal remainingOutstandingAfterLatest)
    {
        _ = remainingOutstandingAfterLatest;
        return receipts
            .OrderByDescending(r => r.ReceivedAtUtc)
            .Select(r =>
            {
                var lines = r.Lines
                    .Select(l => new ReceiptLineSummary(
                        l.ProductId.Value,
                        l.NameSnapshot,
                        l.UomSnapshot.ToString(),
                        l.QuantityReceived,
                        l.DamagedQty,
                        l.RejectedQty,
                        l.ShortClosedQty,
                        l.DiscrepancyKind == ConnectedPoReceivingDiscrepancyKind.None
                            ? null
                            : l.DiscrepancyKind.ToString(),
                        l.DiscrepancyNote,
                        PurchaseOrderReceiveDiscrepancy.ResolveRemainingAction(
                            l.ShortClosedQty,
                            l.DamagedQty,
                            l.RejectedQty)))
                    .ToList();
                return new ReceiptSummary(
                    r.Id.Value,
                    r.GrnNumber,
                    r.ReceivedDate,
                    r.ReceivedAtUtc,
                    r.DeliveryReference,
                    r.Notes,
                    GoodsReceiptStatuses.ToCode(r.Status),
                    lines.Sum(l => l.GoodQty),
                    lines.Sum(l => l.DamagedQty),
                    lines.Sum(l => l.MissingQty),
                    lines.Sum(l => l.CancelledRemainingQty),
                    lines);
            })
            .ToList();
    }

    private static PurchaseOrderLine? FindBuyerLine(PurchaseOrder buyerPo, CatalogProductId supplierProductId)
    {
        return buyerPo.Lines.FirstOrDefault(l =>
            l.SupplierProductId == supplierProductId
            || l.ProductId == supplierProductId);
    }

    private static void Add(Dictionary<Guid, decimal> map, Guid key, decimal qty)
    {
        if (qty <= 0m)
        {
            return;
        }

        map[key] = map.TryGetValue(key, out var existing) ? existing + qty : qty;
    }
}
