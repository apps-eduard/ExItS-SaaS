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
    public uint Xmin { get; set; }
}

internal sealed class PurchaseOrderLineRecord
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public int LineNumber { get; set; }
    public string? NameSnapshot { get; set; }
    public string? UomSnapshot { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal UnitPurchaseCost { get; set; }
    public decimal LineTotal { get; set; }
    public decimal ReceivedQty { get; set; }
    public string? LineNotes { get; set; }
}

internal sealed class GoodsReceiptRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string GrnNumber { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public Guid ReceivedBy { get; set; }
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
}
