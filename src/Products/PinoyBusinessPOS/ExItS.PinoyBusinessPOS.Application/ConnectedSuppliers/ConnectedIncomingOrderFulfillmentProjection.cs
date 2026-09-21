using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Seller-facing fulfillment progress from buyer PO lines + goods receipts.
/// Damaged/rejected come from GRN lines; outstanding from buyer PO.
/// Buyer catalog product ids differ from supplier product ids — match via
/// <see cref="SupplierProductId"/> on the buyer line and/or active product links.
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

    /// <summary>
    /// Active buyer↔supplier catalog links for matching CPO lines to buyer PO / GRN lines.
    /// </summary>
    public sealed record ProductLinkMaps(
        IReadOnlyDictionary<Guid, Guid> SupplierToBuyerProductId,
        IReadOnlyDictionary<Guid, Guid> BuyerToSupplierProductId)
    {
        public static ProductLinkMaps Empty { get; } = new(
            new Dictionary<Guid, Guid>(),
            new Dictionary<Guid, Guid>());

        public static ProductLinkMaps FromLinks(IEnumerable<BuyerSupplierProductLink> links)
        {
            var active = links.Where(x => x.IsActive).ToList();
            var supplierToBuyer = active
                .GroupBy(x => x.SupplierProductId.Value)
                .ToDictionary(g => g.Key, g => g.First().BuyerProductId.Value);
            var buyerToSupplier = active
                .GroupBy(x => x.BuyerProductId.Value)
                .ToDictionary(g => g.Key, g => g.First().SupplierProductId.Value);
            return new ProductLinkMaps(supplierToBuyer, buyerToSupplier);
        }
    }

    public static IReadOnlyDictionary<Guid, LineProgress> ProjectLineProgress(
        ConnectedPurchaseOrder order,
        PurchaseOrder buyerPo,
        IReadOnlyList<GoodsReceipt> receipts,
        ProductLinkMaps? productLinks = null)
    {
        var links = productLinks ?? ProductLinkMaps.Empty;
        var posted = receipts
            .Where(r => r.Status == GoodsReceiptStatus.Posted)
            .ToList();

        // Prefer PO-line id (stable across buyer/supplier catalog ids).
        var damagedByPoLineId = new Dictionary<Guid, decimal>();
        var missingByPoLineId = new Dictionary<Guid, decimal>();
        var damagedByBuyerProduct = new Dictionary<Guid, decimal>();
        var missingByBuyerProduct = new Dictionary<Guid, decimal>();
        foreach (var receipt in posted)
        {
            foreach (var line in receipt.Lines)
            {
                Add(damagedByPoLineId, line.PurchaseOrderLineId.Value, line.DamagedQty);
                Add(missingByPoLineId, line.PurchaseOrderLineId.Value, line.RejectedQty);
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

            var poLine = FindBuyerLine(buyerPo, cpoLine.ProductId, links);
            var good = poLine?.ReceivedQty ?? 0m;
            var cancelled = poLine?.ClosedShortQty ?? 0m;
            var outstanding = poLine?.OutstandingQty
                ?? Math.Max(0m, ordered - good - cancelled);

            var damaged = 0m;
            var missing = 0m;
            if (poLine is not null)
            {
                if (damagedByPoLineId.TryGetValue(poLine.Id.Value, out var dByLine))
                {
                    damaged = dByLine;
                }
                else if (poLine.ProductId is { } buyerProduct
                         && damagedByBuyerProduct.TryGetValue(buyerProduct.Value, out var dByProduct))
                {
                    damaged = dByProduct;
                }

                if (missingByPoLineId.TryGetValue(poLine.Id.Value, out var mByLine))
                {
                    missing = mByLine;
                }
                else if (poLine.ProductId is { } buyerProduct2
                         && missingByBuyerProduct.TryGetValue(buyerProduct2.Value, out var mByProduct))
                {
                    missing = mByProduct;
                }
            }

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

    internal static PurchaseOrderLine? FindBuyerLine(
        PurchaseOrder buyerPo,
        CatalogProductId supplierProductId,
        ProductLinkMaps links)
    {
        Guid? linkedBuyerProductId = null;
        if (links.SupplierToBuyerProductId.TryGetValue(supplierProductId.Value, out var mappedBuyer))
        {
            linkedBuyerProductId = mappedBuyer;
        }

        return buyerPo.Lines.FirstOrDefault(l =>
            l.SupplierProductId == supplierProductId
            || l.ProductId == supplierProductId
            || (linkedBuyerProductId is Guid buyerId && l.ProductId?.Value == buyerId)
            || (l.ProductId is { } bp
                && links.BuyerToSupplierProductId.TryGetValue(bp.Value, out var mappedSupplier)
                && mappedSupplier == supplierProductId.Value));
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
