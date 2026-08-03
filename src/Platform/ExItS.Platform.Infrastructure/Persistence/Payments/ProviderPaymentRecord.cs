namespace ExItS.Platform.Infrastructure.Persistence.Payments;

internal sealed class ProviderPaymentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ProviderReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsTest { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
