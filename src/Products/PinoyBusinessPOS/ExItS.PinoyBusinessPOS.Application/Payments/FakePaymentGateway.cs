using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

/// <summary>
/// Deterministic fake gateway for Development/Testing. Never used for real card or GCash credentials.
/// Signature: HMAC-SHA256 of body with key <c>exits-fake-payment-dev</c>, hex-encoded.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public const string ProviderCodeValue = "Fake";
    public const string DevSigningKey = "exits-fake-payment-dev";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public string ProviderCode => ProviderCodeValue;

    public Task<PaymentGatewaySession> CreateSessionAsync(
        PaymentGatewayCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reference = $"fake_{request.PaymentAttemptId:N}";
        var method = request.Method.Trim().ToUpperInvariant();
        string? checkout = null;
        string? deepLink = null;
        string? qr = null;

        if (method == "CARD")
        {
            checkout = $"https://payments.fake.local/checkout/{reference}";
        }
        else if (method == "GCASH")
        {
            deepLink = $"exits-fake-gcash://pay/{reference}";
            qr = $"EXITS-FAKE-GCASH|{reference}|{request.Amount:0.00}|{request.Currency}";
        }
        else
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptMethod,
                $"Fake gateway does not support method '{request.Method}'.");
        }

        return Task.FromResult(new PaymentGatewaySession(
            reference,
            checkout,
            deepLink,
            qr,
            DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    public bool ValidateWebhookSignature(string? signatureHeader, string rawBody)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrEmpty(rawBody))
        {
            return false;
        }

        var expected = ComputeSignature(rawBody);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signatureHeader.Trim()));
    }

    public PaymentWebhookEvent ParseWebhook(string rawBody)
    {
        var dto = JsonSerializer.Deserialize<FakeWebhookBody>(rawBody, JsonOptions)
            ?? throw new DomainException(
                DomainErrorCodes.PaymentWebhookSignatureInvalid,
                "Webhook body could not be parsed.");

        if (string.IsNullOrWhiteSpace(dto.ProviderReference))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptProviderReference,
                "Webhook providerReference is required.");
        }

        return new PaymentWebhookEvent(
            ProviderCodeValue,
            dto.ProviderReference.Trim(),
            dto.Status?.Trim() ?? "Failed",
            dto.EventSequence,
            dto.FailureCode,
            dto.FailureMessage,
            dto.CardBrand,
            dto.CardLastFour);
    }

    public static string ComputeSignature(string rawBody)
    {
        var key = Encoding.UTF8.GetBytes(DevSigningKey);
        var bytes = Encoding.UTF8.GetBytes(rawBody);
        var hash = HMACSHA256.HashData(key, bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string BuildWebhookBody(
        string providerReference,
        string status,
        long eventSequence,
        string? failureCode = null,
        string? failureMessage = null,
        string? cardBrand = null,
        string? cardLastFour = null) =>
        JsonSerializer.Serialize(
            new FakeWebhookBody(
                providerReference,
                status,
                eventSequence,
                failureCode,
                failureMessage,
                cardBrand,
                cardLastFour),
            JsonOptions);

    private sealed record FakeWebhookBody(
        string ProviderReference,
        string Status,
        long EventSequence,
        string? FailureCode,
        string? FailureMessage,
        string? CardBrand,
        string? CardLastFour);
}
