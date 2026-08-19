namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;

internal sealed class PaymentAttemptRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SaleId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? ExternalReference { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PHP";
    public string Status { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? DeepLink { get; set; }
    public string? QrPayload { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLastFour { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public Guid? VerifiedBy { get; set; }
    public string? VerificationReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public long ProviderEventSequence { get; set; }
    public bool ProviderFinalizedBySystem { get; set; }
}
