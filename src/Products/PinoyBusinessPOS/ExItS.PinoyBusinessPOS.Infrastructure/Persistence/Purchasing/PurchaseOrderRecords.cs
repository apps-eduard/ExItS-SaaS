using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Purchasing;

internal sealed class PurchaseOrderNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}

internal sealed class GoodsReceiptNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}

internal sealed class PurchaseOrderRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string? PoNumber { get; set; }
    public Guid SupplierId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public string? SupplierReference { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? OrderedAtUtc { get; set; }
    public Guid? OrderedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int PaymentTerm { get; set; }
    public Guid? SupplierBranchId { get; set; }
    public string? SupplierBranchNameSnapshot { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class PurchaseOrderLineRecord
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SupplierProductId { get; set; }
    public int LineNumber { get; set; }
    public string? NameSnapshot { get; set; }
    public string? UomSnapshot { get; set; }
    public string? SkuSnapshot { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal UnitPurchaseCost { get; set; }
    public decimal LineTotal { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal ClosedShortQty { get; set; }
    public string? LineNotes { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public string? PurchaseUnitNameSnapshot { get; set; }
    public decimal MultiplierToBaseSnapshot { get; set; } = 1m;
}

internal sealed class GoodsReceiptRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid SupplierId { get; set; }
    public string GrnNumber { get; set; } = string.Empty;
    public DateOnly ReceivedDate { get; set; }
    public string? DeliveryReference { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public Guid ReceivedBy { get; set; }
    public Guid? ReceivingBranchId { get; set; }
    public string Status { get; set; } = nameof(GoodsReceiptStatus.Posted);
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public Guid? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
}

internal sealed class GoodsReceiptLineRecord
{
    public Guid Id { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public int LineNumber { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public decimal ReceivedQty { get; set; }
    public decimal DamagedQty { get; set; }
    public decimal RejectedQty { get; set; }
    public decimal ShortClosedQty { get; set; }
    public string DiscrepancyKind { get; set; } = "None";
    public string? DiscrepancyNote { get; set; }
    public decimal UnitPurchaseCostSnapshot { get; set; }
    public decimal LineTotalSnapshot { get; set; }
    public Guid? InventoryMovementId { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public string? PurchaseUnitNameSnapshot { get; set; }
    public decimal MultiplierToBaseSnapshot { get; set; } = 1m;
    public DateOnly? ExpiryDate { get; set; }
    public string? LotNumber { get; set; }
}
