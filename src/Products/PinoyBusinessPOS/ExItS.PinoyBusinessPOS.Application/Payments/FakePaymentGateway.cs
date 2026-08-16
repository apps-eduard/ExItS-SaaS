using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public enum FakePaymentGatewayBehavior
{
    Success = 0,
    DefiniteFailure = 1,
    TimeoutBeforeCreate = 2,
    TimeoutAfterCreate = 3
}

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

    private readonly ConcurrentDictionary<string, StoredSession> _byIdempotencyKey = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, StoredSession> _byProviderReference = new(StringComparer.Ordinal);
    private readonly object _behaviorGate = new();
    private FakePaymentGatewayBehavior _behavior = FakePaymentGatewayBehavior.Success;

    public string ProviderCode => ProviderCodeValue;

    public FakePaymentGatewayBehavior Behavior
    {
        get
        {
            lock (_behaviorGate)
            {
                return _behavior;
            }
        }
    }

    public void SetBehavior(FakePaymentGatewayBehavior behavior)
    {
        lock (_behaviorGate)
        {
            _behavior = behavior;
        }
    }

    public void ResetBehavior() => SetBehavior(FakePaymentGatewayBehavior.Success);

    public void ClearSessions()
    {
        _byIdempotencyKey.Clear();
        _byProviderReference.Clear();
    }

    public Task<PaymentGatewaySession> CreateSessionAsync(
        PaymentGatewayCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var behavior = Behavior;
        if (behavior == FakePaymentGatewayBehavior.TimeoutBeforeCreate)
        {
            throw PaymentGatewayException.TimeoutBeforeCreate(
                "Fake gateway timed out before creating a provider session.");
        }

        if (behavior == FakePaymentGatewayBehavior.DefiniteFailure)
        {
            throw PaymentGatewayException.DefiniteFailure(
                "Fake gateway refused to create a provider session.");
        }

        if (_byIdempotencyKey.TryGetValue(request.IdempotencyKey, out var existing))
        {
            EnsureIdempotentPayloadMatches(existing, request);
            return Task.FromResult(existing.Session);
        }

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

        var session = new PaymentGatewaySession(
            reference,
            checkout,
            deepLink,
            qr,
            DateTimeOffset.UtcNow.AddMinutes(15));

        var stored = new StoredSession(
            session,
            request.OrganizationId,
            request.SaleId,
            request.PaymentAttemptId,
            method,
            request.Amount,
            request.Currency.Trim().ToUpperInvariant(),
            request.IdempotencyKey);

        if (!_byIdempotencyKey.TryAdd(request.IdempotencyKey, stored))
        {
            var raced = _byIdempotencyKey[request.IdempotencyKey];
            EnsureIdempotentPayloadMatches(raced, request);
            return Task.FromResult(raced.Session);
        }

        _byProviderReference[reference] = stored;

        if (behavior == FakePaymentGatewayBehavior.TimeoutAfterCreate)
        {
            throw PaymentGatewayException.TimeoutAfterCreate(
                "Fake gateway created a session then timed out before returning it.");
        }

        return Task.FromResult(session);
    }

    public Task<PaymentGatewaySession?> GetSessionAsync(
        string providerReference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(providerReference))
        {
            return Task.FromResult<PaymentGatewaySession?>(null);
        }

        return Task.FromResult(
            _byProviderReference.TryGetValue(providerReference.Trim(), out var stored)
                ? stored.Session
                : null);
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

    private static void EnsureIdempotentPayloadMatches(StoredSession existing, PaymentGatewayCreateRequest request)
    {
        var method = request.Method.Trim().ToUpperInvariant();
        var currency = request.Currency.Trim().ToUpperInvariant();
        if (existing.OrganizationId != request.OrganizationId
            || existing.SaleId != request.SaleId
            || existing.PaymentAttemptId != request.PaymentAttemptId
            || existing.Amount != request.Amount
            || !string.Equals(existing.Method, method, StringComparison.Ordinal)
            || !string.Equals(existing.Currency, currency, StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.PaymentGatewayIdempotencyConflict,
                "Fake gateway idempotency key was reused with a conflicting payload.");
        }
    }

    private sealed record StoredSession(
        PaymentGatewaySession Session,
        Guid OrganizationId,
        Guid SaleId,
        Guid PaymentAttemptId,
        string Method,
        decimal Amount,
        string Currency,
        string IdempotencyKey);

    private sealed record FakeWebhookBody(
        string ProviderReference,
        string Status,
        long EventSequence,
        string? FailureCode,
        string? FailureMessage,
        string? CardBrand,
        string? CardLastFour);
}
