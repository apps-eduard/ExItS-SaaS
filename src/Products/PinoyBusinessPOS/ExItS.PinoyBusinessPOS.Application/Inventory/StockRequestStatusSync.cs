using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Recomputes Stock Request parent status from authoritative linked-transfer coverage.
/// Draft quantity never counts as in-transit or fulfillment progress.
/// </summary>
internal static class StockRequestStatusSync
{
    internal static async Task SyncFromLinkedTransfersAsync(
        IStockRequestRepository stockRequests,
        IInventoryTransferRepository transfers,
        StockRequest stockRequest,
        InventoryTransfer? liveTransfer,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (stockRequest.Status is StockRequestStatus.Rejected or StockRequestStatus.Cancelled)
        {
            return;
        }

        var listed = await transfers
            .ListByStockRequestIdAsync(stockRequest.OrganizationId, stockRequest.Id, cancellationToken)
            .ConfigureAwait(false);
        var linked = liveTransfer is null
            ? listed
            : StockRequestDispatchCoverage.WithLiveTransfer(listed, liveTransfer);
        var coverage = StockRequestDispatchCoverage.Compute(stockRequest, linked);
        stockRequest.RecalculateStatusFromFulfillmentCoverage(
            coverage.ReceivedByProduct,
            coverage.WaivedByProduct,
            coverage.OpenInTransitByProduct,
            utcNow);
        await stockRequests.UpdateAsync(stockRequest, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task CancelLinkedDraftsAsync(
        IInventoryTransferRepository transfers,
        StockRequest stockRequest,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var linked = await transfers
            .ListByStockRequestIdAsync(stockRequest.OrganizationId, stockRequest.Id, cancellationToken)
            .ConfigureAwait(false);
        foreach (var transfer in linked)
        {
            if (transfer.Status != InventoryTransferStatus.Draft)
            {
                continue;
            }

            transfer.Cancel(actorId, utcNow);
            await transfers.UpdateAsync(transfer, cancellationToken).ConfigureAwait(false);
        }
    }
}
