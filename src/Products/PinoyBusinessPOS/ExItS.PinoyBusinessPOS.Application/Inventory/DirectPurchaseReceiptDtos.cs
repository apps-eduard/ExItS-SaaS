using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record DirectPurchaseReceiptLineDto(
    Guid LineId,
    Guid ProductId,
    int LineNumber,
    string ProductNameSnapshot,
    string? SkuSnapshot,
    string UnitOfMeasure,
    decimal Quantity,
    decimal UnitCost,
    decimal LineTotal,
    DateOnly? ExpiryDate,
    string? LotNumber,
    Guid? InventoryMovementId);

public sealed record DirectPurchaseReceiptDto(
    Guid DirectPurchaseReceiptId,
    Guid OrganizationId,
    string ReceiptNumber,
    DateOnly PurchaseDate,
    Guid? SupplierId,
    string? SourceNameSnapshot,
    string? ReferenceNumber,
    string? Notes,
    decimal TotalCost,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<DirectPurchaseReceiptLineDto> Lines,
    string Status = "Posted",
    DateTimeOffset? VoidedAtUtc = null,
    Guid? VoidedByUserId = null,
    string? VoidReason = null);

public sealed record VoidDirectPurchaseReceiptRequest(string Reason, string? Notes = null);

public sealed record DirectPurchaseReceiptListItemDto(
    Guid DirectPurchaseReceiptId,
    string ReceiptNumber,
    DateOnly PurchaseDate,
    Guid? SupplierId,
    string? SourceNameSnapshot,
    string? ReferenceNumber,
    decimal TotalCost,
    int LineCount,
    DateTimeOffset CreatedAtUtc,
    string Status = "Posted");

public sealed record CreateDirectPurchaseReceiptLineRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitCost,
    DateOnly? ExpiryDate = null,
    string? LotNumber = null);

public sealed record CreateDirectPurchaseReceiptRequest(
    DateOnly PurchaseDate,
    IReadOnlyList<CreateDirectPurchaseReceiptLineRequest> Lines,
    Guid? SupplierId = null,
    string? SourceName = null,
    string? ReferenceNumber = null,
    string? Notes = null,
    string? IdempotencyKey = null,
    decimal? PaidNow = null,
    DateOnly? DueDate = null,
    string? PaymentMethodAtReceipt = null);

public static class DirectPurchaseReceiptMapper
{
    public static DirectPurchaseReceiptDto Map(DirectPurchaseReceipt receipt) =>
        new(
            receipt.Id.Value,
            receipt.OrganizationId.Value,
            receipt.ReceiptNumber,
            receipt.PurchaseDate,
            receipt.SupplierId?.Value,
            receipt.SourceNameSnapshot,
            receipt.ReferenceNumber,
            receipt.Notes,
            receipt.TotalCost,
            receipt.CreatedByUserId,
            receipt.CreatedAtUtc,
            receipt.Lines.Select(MapLine).ToList(),
            DirectPurchaseReceiptStatuses.ToCode(receipt.Status),
            receipt.VoidedAtUtc,
            receipt.VoidedByUserId,
            receipt.VoidReason);

    public static DirectPurchaseReceiptListItemDto MapListItem(DirectPurchaseReceipt receipt) =>
        new(
            receipt.Id.Value,
            receipt.ReceiptNumber,
            receipt.PurchaseDate,
            receipt.SupplierId?.Value,
            receipt.SourceNameSnapshot,
            receipt.ReferenceNumber,
            receipt.TotalCost,
            receipt.Lines.Count,
            receipt.CreatedAtUtc,
            DirectPurchaseReceiptStatuses.ToCode(receipt.Status));

    public static DirectPurchaseReceiptLineDto MapLine(DirectPurchaseReceiptLine line) =>
        new(
            line.Id.Value,
            line.ProductId.Value,
            line.LineNumber,
            line.ProductNameSnapshot,
            line.SkuSnapshot,
            line.UnitOfMeasureSnapshot.ToString(),
            line.Quantity,
            line.UnitCost,
            line.LineTotal,
            line.ExpiryDate,
            line.LotNumber,
            line.InventoryMovementId);
}
