using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

internal static class ConnectedPoConfirmation
{
    public static async Task AlignBuyerOutstandingAsync(
        PurchaseOrder purchaseOrder,
        ConnectedPurchaseOrder connected,
        IBuyerSupplierProductLinkRepository links,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var list = await links
            .ListAsync(connected.RelationshipId, connected.BuyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var buyerProductBySupplier = list
            .GroupBy(x => x.SupplierProductId.Value)
            .ToDictionary(g => g.Key, g => g.First().BuyerProductId.Value);

        var confirmed = new Dictionary<Guid, decimal>();
        foreach (var line in connected.Lines)
        {
            if (!buyerProductBySupplier.TryGetValue(line.ProductId.Value, out var buyerProductId))
            {
                continue;
            }

            confirmed[buyerProductId] = line.FulfillmentQty;
        }

        if (confirmed.Count == 0)
        {
            return;
        }

        purchaseOrder.AlignOutstandingToConfirmedQuantities(confirmed, utcNow);
    }

    public static async Task<decimal?> RemainingConfirmedQtyAsync(
        PurchaseOrder purchaseOrder,
        ConnectedPurchaseOrder connected,
        CatalogProductId buyerProductId,
        IBuyerSupplierProductLinkRepository links,
        CancellationToken cancellationToken)
    {
        var link = await links
            .FindAsync(connected.RelationshipId, buyerProductId, cancellationToken)
            .ConfigureAwait(false);
        if (link is null)
        {
            return null;
        }

        var cpoLine = connected.Lines.FirstOrDefault(x => x.ProductId == link.SupplierProductId);
        if (cpoLine is null)
        {
            return null;
        }

        var poLine = purchaseOrder.Lines.FirstOrDefault(x => x.ProductId == buyerProductId);
        if (poLine is null)
        {
            return null;
        }

        return Math.Max(0m, cpoLine.FulfillmentQty - poLine.ReceivedQty);
    }
}
