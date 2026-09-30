using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Stock-request-linked draft transfers may only ship approved SR products,
/// at quantities not exceeding remaining needs.
/// Callers must omit zero-qty lines before invoking (those mean "not in this shipment").
/// Draft transfers do not reduce <see cref="StockRequestDispatchCoverage"/> remaining
/// (only InTransit/PartiallyReceived do); other drafts' qty are reserved explicitly.
/// </summary>
internal static class StockRequestLinkedTransferGuard
{
    /// <summary>
    /// Drops non-positive quantities. Empty result means the shipment has no lines to keep.
    /// </summary>
    internal static IReadOnlyList<InventoryTransferLineRequest> OmitZeroQuantityLines(
        IReadOnlyList<InventoryTransferLineRequest>? lines) =>
        (lines ?? Array.Empty<InventoryTransferLineRequest>())
            .Where(l => l.Quantity > 0m)
            .ToList();

    internal static ApplicationResult<InventoryTransfer>? ValidateProposedLines(
        StockRequest stockRequest,
        IReadOnlyList<InventoryTransfer> linkedTransfers,
        InventoryTransfer? editingDraft,
        IReadOnlyList<(Guid ProductId, decimal Quantity, string NameSnapshot)> proposedLines)
    {
        if (proposedLines.Count == 0)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                DomainErrorCodes.InvalidStockRequestQuantity,
                "At least one shipment line with quantity greater than zero is required.");
        }

        var allowedProducts = stockRequest.Lines
            .Select(l => l.ProductId.Value)
            .ToHashSet();

        var proposedByProduct = proposedLines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => (Qty: g.Sum(x => x.Quantity), Name: g.First().NameSnapshot));

        foreach (var (productId, row) in proposedByProduct)
        {
            if (row.Qty <= 0m)
            {
                return ApplicationResult<InventoryTransfer>.Failure(
                    DomainErrorCodes.InvalidStockRequestQuantity,
                    $"Shipment quantity for '{row.Name}' must be greater than zero.");
            }

            if (!allowedProducts.Contains(productId))
            {
                return ApplicationResult<InventoryTransfer>.Failure(
                    DomainErrorCodes.InvalidStockRequestLine,
                    $"'{row.Name}' is not on stock request {stockRequest.RequestNumber ?? stockRequest.Id.Value.ToString("D")} and cannot be added to this shipment.");
            }
        }

        var coverage = StockRequestDispatchCoverage.Compute(stockRequest, linkedTransfers);
        var otherDraftQtyByProduct = linkedTransfers
            .Where(t =>
                t.Status == InventoryTransferStatus.Draft
                && (editingDraft is null || t.Id != editingDraft.Id))
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SentQty));

        foreach (var (productId, row) in proposedByProduct)
        {
            var remaining = coverage.RemainingToDispatchByProduct.GetValueOrDefault(productId);
            var reservedByOtherDrafts = otherDraftQtyByProduct.GetValueOrDefault(productId);
            var maxAllowed = Math.Max(0m, remaining - reservedByOtherDrafts);
            if (row.Qty > maxAllowed)
            {
                return ApplicationResult<InventoryTransfer>.Failure(
                    DomainErrorCodes.InvalidStockRequestQuantity,
                    $"'{row.Name}' shipment qty {row.Qty} exceeds remaining stock-request need {maxAllowed}.");
            }
        }

        return null;
    }
}
