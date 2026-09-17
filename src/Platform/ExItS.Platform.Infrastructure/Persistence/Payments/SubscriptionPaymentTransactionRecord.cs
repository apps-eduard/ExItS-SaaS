namespace ExItS.Platform.Infrastructure.Persistence.Payments;

internal sealed class SubscriptionPaymentTransactionRecord
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public Guid InitiatedByUserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string PlanKey { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal FinalAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLast4 { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ProcessingAtUtc { get; set; }
    public DateTimeOffset? PaidAtUtc { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public DateTimeOffset? ExpiredAtUtc { get; set; }
    public DateTimeOffset? PeriodStartUtc { get; set; }
    public DateTimeOffset? PeriodEndUtc { get; set; }
    public bool SubscriptionActivated { get; set; }
    public List<SubscriptionPaymentActivityRecord> Activities { get; set; } = [];
}

internal sealed class SubscriptionPaymentActivityRecord
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
