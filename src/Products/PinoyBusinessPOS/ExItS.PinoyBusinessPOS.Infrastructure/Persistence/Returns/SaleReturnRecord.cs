namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

internal sealed class SaleReturnRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public Guid? CashierShiftId { get; set; }
    public Guid? SourceRegisterId { get; set; }
    public Guid? RefundRegisterId { get; set; }
    public string RefundMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly ReturnDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
