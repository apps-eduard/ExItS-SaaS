using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;

internal sealed class RepaymentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Remarks { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTimeOffset? ReversedAtUtc { get; set; }
    public string? ReversalReason { get; set; }
    public Guid? ReversedBy { get; set; }
    public uint Xmin { get; set; }
}
