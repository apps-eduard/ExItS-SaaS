namespace ExItS.PinoyBusinessPOS.Application.Payments;

/// <summary>Provider-neutral electronic payment gateway. Real providers plug in here later.</summary>
public interface IPaymentGateway
{
    string ProviderCode { get; }

    Task<PaymentGatewaySession> CreateSessionAsync(
        PaymentGatewayCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up an existing provider session by provider reference (recovery after ambiguous timeouts).
    /// </summary>
    Task<PaymentGatewaySession?> GetSessionAsync(
        string providerReference,
        CancellationToken cancellationToken = default);

    bool ValidateWebhookSignature(string? signatureHeader, string rawBody);

    PaymentWebhookEvent ParseWebhook(string rawBody);
}

public sealed record PaymentGatewayCreateRequest(
    Guid OrganizationId,
    Guid SaleId,
    Guid PaymentAttemptId,
    string Method,
    decimal Amount,
    string Currency,
    string IdempotencyKey);

public sealed record PaymentGatewaySession(
    string ProviderReference,
    string? CheckoutUrl,
    string? DeepLink,
    string? QrPayload,
    DateTimeOffset? ExpiresAtUtc);

public sealed record PaymentGatewayResult(
    string ProviderReference,
    string Status,
    long EventSequence,
    string? FailureCode = null,
    string? FailureMessage = null,
    string? CardBrand = null,
    string? CardLastFour = null);

public sealed record PaymentWebhookEvent(
    string Provider,
    string ProviderReference,
    string Status,
    long EventSequence,
    string? FailureCode = null,
    string? FailureMessage = null,
    string? CardBrand = null,
    string? CardLastFour = null);
