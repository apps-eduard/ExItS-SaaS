using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Family fulfillment coverage helpers shared by transfer queries and prepare-remaining.
/// </summary>
internal static class InventoryTransferCoverageMath
{
    /// <summary>
    /// Outstanding left open by a historical ExpectedLater ("Wait for remaining delivery") receive.
    /// New receives close ExpectedLater immediately; this covers rows received before that change.
    /// Those qty count toward RemainingToDispatch (source Fulfill), not OpenInTransit.
    /// </summary>
    internal static decimal ExpectedLaterOpenQty(InventoryTransfer transfer, Guid productId)
    {
        if (transfer.Status is not (
            InventoryTransferStatus.InTransit or InventoryTransferStatus.PartiallyReceived))
        {
            return 0m;
        }

        var outstanding = transfer.Lines
            .Where(l => l.ProductId.Value == productId)
            .Sum(l => Math.Max(0m, l.OutstandingQty));
        if (outstanding <= 0m)
        {
            return 0m;
        }

        var expectedLaterMissing = transfer.Receipts
            .SelectMany(r => r.Lines)
            .Where(l =>
                l.ProductId.Value == productId
                && l.MissingDisposition == InventoryTransferMissingDisposition.ExpectedLater)
            .Sum(l => l.QuantityMissing);

        return Math.Min(outstanding, Math.Max(0m, expectedLaterMissing));
    }

    internal static decimal ExpectedLaterOpenQty(InventoryTransfer transfer) =>
        transfer.Lines
            .Select(l => l.ProductId.Value)
            .Distinct()
            .Sum(productId => ExpectedLaterOpenQty(transfer, productId));

    /// <summary>
    /// True open-in-transit qty for a product: outstanding on InTransit/PartiallyReceived
    /// excluding historical ExpectedLater open (those are Needs fulfillment).
    /// </summary>
    internal static decimal OpenInTransitQtyForProduct(
        IEnumerable<InventoryTransfer> family,
        Guid productId)
    {
        var open = 0m;
        foreach (var transfer in family)
        {
            if (transfer.Status is not (
                InventoryTransferStatus.InTransit or InventoryTransferStatus.PartiallyReceived))
            {
                continue;
            }

            var outstanding = transfer.Lines
                .Where(l => l.ProductId.Value == productId)
                .Sum(l => Math.Max(0m, l.OutstandingQty));
            open += Math.Max(0m, outstanding - ExpectedLaterOpenQty(transfer, productId));
        }

        return open;
    }

    internal static decimal OpenInTransitQty(IEnumerable<InventoryTransfer> family)
    {
        var list = family as IReadOnlyList<InventoryTransfer> ?? family.ToList();
        var productIds = list
            .SelectMany(t => t.Lines.Select(l => l.ProductId.Value))
            .Distinct();
        return productIds.Sum(id => OpenInTransitQtyForProduct(list, id));
    }

    /// <summary>
    /// Closes historical ExpectedLater outstanding so prepare-remaining cannot double-cover
    /// with a later destination receive on the same transfer.
    /// </summary>
    internal static bool TryCloseExpectedLaterOpen(
        InventoryTransfer transfer,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        if (transfer.Status != InventoryTransferStatus.PartiallyReceived)
        {
            return false;
        }

        if (ExpectedLaterOpenQty(transfer) <= 0m)
        {
            return false;
        }

        if (transfer.TotalOutstandingQty <= 0m)
        {
            return false;
        }

        transfer.CloseRemainder(
            actorId,
            utcNow,
            closeLines: null,
            transferLevelReason: InventoryTransferDiscrepancyReason.ShortShipment,
            transferLevelNote: "Closed for remaining fulfillment (wait for remaining delivery)");
        return true;
    }
}
