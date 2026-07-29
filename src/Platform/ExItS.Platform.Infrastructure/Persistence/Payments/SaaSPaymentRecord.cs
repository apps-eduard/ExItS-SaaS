namespace ExItS.Platform.Infrastructure.Persistence.Payments;

internal sealed class SaaSPaymentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public Guid? SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string NormalizedReference { get; set; } = string.Empty;
    public DateTimeOffset PaidAtUtc { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public string? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int AggregateVersion { get; set; }
    public uint Xmin { get; set; }
}
