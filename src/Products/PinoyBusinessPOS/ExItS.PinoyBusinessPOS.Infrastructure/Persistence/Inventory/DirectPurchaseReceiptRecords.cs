using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class DirectPurchaseReceiptRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly PurchaseDate { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SourceNameSnapshot { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public decimal TotalCost { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? IdempotencyKey { get; set; }
    public string Status { get; set; } = nameof(DirectPurchaseReceiptStatus.Posted);
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public Guid? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
}

internal sealed class DirectPurchaseReceiptLineRecord
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public int LineNumber { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string? SkuSnapshot { get; set; }
    public string UnitOfMeasureSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? LotNumber { get; set; }
    public Guid? InventoryMovementId { get; set; }
}

internal sealed class DirectPurchaseReceiptNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
