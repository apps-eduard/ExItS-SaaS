using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;

internal sealed class RepaymentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Remarks { get; set; }
    public string PaymentMethod { get; set; } = nameof(UtangPaymentMethod.Cash);
    public string? CheckNumber { get; set; }
    public string? BankName { get; set; }
    public DateOnly? CheckDate { get; set; }
    public string? AccountName { get; set; }
    public string? Reference { get; set; }
    public string CheckClearingStatus { get; set; } = nameof(UtangCheckClearingStatus.None);
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTimeOffset? ReversedAtUtc { get; set; }
    public string? ReversalReason { get; set; }
    public Guid? ReversedBy { get; set; }
    public DateTimeOffset? ClearedAtUtc { get; set; }
    public Guid? ClearedBy { get; set; }
    public DateTimeOffset? BouncedAtUtc { get; set; }
    public Guid? BouncedBy { get; set; }
    public string? BounceReason { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancelReason { get; set; }
    public uint Xmin { get; set; }
}
