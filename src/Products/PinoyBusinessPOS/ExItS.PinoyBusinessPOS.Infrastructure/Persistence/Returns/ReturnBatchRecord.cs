namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

internal sealed class ReturnBatchRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SaleId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ConnectedPurchaseOrderId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? BuyerOrganizationId { get; set; }
    public Guid? SellerOrganizationId { get; set; }
    public Guid? BuyerBranchId { get; set; }
    public Guid? SellerBranchId { get; set; }
    public string? PaymentTiming { get; set; }
    public string? PoNumberSnapshot { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RefundStatus { get; set; } = string.Empty;
    public decimal AcceptedReturnValue { get; set; }
    public decimal RefundDueAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public Guid? SaleReturnId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? SellerReceivedAtUtc { get; set; }
    public Guid? SellerReceivedBy { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
    public Guid? FinalizedBy { get; set; }
    public uint Xmin { get; set; }
}
