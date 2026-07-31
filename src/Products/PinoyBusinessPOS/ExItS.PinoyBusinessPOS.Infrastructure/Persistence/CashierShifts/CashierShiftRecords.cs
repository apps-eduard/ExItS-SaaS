namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CashierShifts;

internal sealed class CashierShiftRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ShiftNumber { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public Guid? RegisterId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly BusinessDate { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public DateTimeOffset OpenedAtUtc { get; set; }
    public Guid OpenedBy { get; set; }
    public decimal? ClosingCashAmount { get; set; }
    public decimal? ExpectedCashAmountSnapshot { get; set; }
    public decimal? CashVarianceAmount { get; set; }
    public string? ClosingNotes { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid? ClosedBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class CashierShiftMovementRecord
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid OrganizationId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid RecordedBy { get; set; }
}

internal sealed class CashierShiftNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
