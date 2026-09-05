namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Expenses;

internal sealed class ExpenseRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    /// <summary>Null = organization-wide; set = Platform OrganizationBranchId.</summary>
    public Guid? BranchId { get; set; }
    public string ExpenseNumber { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Payee { get; set; }
    public string? GcashReference { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public Guid? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
