namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.SupplierPayables;

internal sealed class SupplierPayableRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SupplierId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal PaidAtReceiptAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public string? PaymentMethodAtReceipt { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public Guid? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class SupplierPayablePaymentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PayableId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset PaidAtUtc { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}
