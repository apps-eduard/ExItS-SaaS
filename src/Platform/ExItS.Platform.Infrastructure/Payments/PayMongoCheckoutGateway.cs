using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.Platform.Application.Payments;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.Payments;

public sealed class PayMongoCheckoutGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<PayMongoOptions> options) : ISubscriptionCheckoutGateway
{
    public const string HttpClientName = "PayMongo";
    private const string CheckoutPath = "v1/checkout_sessions";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.SecretKey);

    public async Task<HostedCheckoutSessionResult> CreateSessionAsync(
        HostedCheckoutCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var secret = RequireSecret();
        var centavos = ToCentavos(request.Amount);
        var payload = new
        {
            data = new
            {
                attributes = new
                {
                    billing = new { name = "ExItS subscriber" },
                    line_items = new[]
                    {
                        new
                        {
                            currency = request.CurrencyCode,
                            amount = centavos,
                            name = request.PlanName,
                            quantity = 1,
                            description = request.BillingPeriod
                        }
                    },
                    payment_method_types = new[] { "card", "gcash", "paymaya", "grab_pay" },
                    success_url = request.SuccessUrl,
                    cancel_url = request.CancelUrl,
                    description = "ExItS subscription",
                    reference_number = request.ReferenceNumber,
                    metadata = new Dictionary<string, string>
                    {
                        ["exits_payment_id"] = request.PaymentId.ToString("D"),
                        ["organization_id"] = request.OrganizationId?.ToString("D") ?? "",
                        ["plan_key"] = request.PlanName
                    }
                }
            }
        };

        using var document = await SendAsync(HttpMethod.Post, CheckoutPath, secret, payload, cancellationToken)
            .ConfigureAwait(false);
        var sessionId = ReadSessionId(document);
        var checkoutUrl = ReadString(document.RootElement, "data", "attributes", "checkout_url");
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(checkoutUrl))
        {
            throw new InvalidOperationException("PayMongo checkout response was incomplete.");
        }

        var livemode = ReadBool(document.RootElement, "data", "attributes", "livemode");
        return new HostedCheckoutSessionResult(sessionId, checkoutUrl, IsTest: livemode != true);
    }

    public async Task<HostedCheckoutProviderState> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var secret = RequireSecret();
        using var document = await SendAsync(
            HttpMethod.Get,
            $"{CheckoutPath}/{Uri.EscapeDataString(sessionId)}",
            secret,
            payload: null,
            cancellationToken).ConfigureAwait(false);
        return PayMongoCheckoutPayload.ReadProviderState(document.RootElement, sessionId);
    }

    private string RequireSecret()
    {
        var secret = options.Value.SecretKey?.Trim();
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("PayMongo is not configured.");
        }

        return secret;
    }

    private async Task<JsonDocument> SendAsync(
        HttpMethod method,
        string path,
        string secret,
        object? payload,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var message = new HttpRequestMessage(method, path);
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(secret + ":")));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (payload is not null)
        {
            message.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");
        }

        using var response = await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
        {
            throw new HttpRequestException(
                $"PayMongo checkout request failed ({(int)response.StatusCode}).");
        }

        return JsonDocument.Parse(body);
    }

    internal static int ToCentavos(decimal amount)
    {
        var centavos = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        if (centavos <= 0m || centavos != decimal.Truncate(centavos) || centavos > int.MaxValue)
        {
            throw new InvalidOperationException("Subscription amount cannot be sent to the payment provider.");
        }

        return (int)centavos;
    }

    private static string? ReadSessionId(JsonDocument document) =>
        ReadString(document.RootElement, "data", "id");

    private static string? ReadString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static bool? ReadBool(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }
}

public static class PayMongoCheckoutPayload
{
    public static bool TryParsePaidEvent(string rawBody, out string eventId, out HostedCheckoutProviderState state)
    {
        eventId = "";
        state = new HostedCheckoutProviderState("", false, false, false, null, null, null);
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            var type = Read(root, "data", "attributes", "type");
            if (!string.Equals(type, "checkout_session.payment.paid", StringComparison.Ordinal))
            {
                return false;
            }

            eventId = Read(root, "data", "id") ?? "";
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            if (!root.TryGetProperty("data", out var data)
                || !data.TryGetProperty("attributes", out var attributes)
                || !attributes.TryGetProperty("data", out var session))
            {
                return false;
            }

            var sessionId = Read(session, "id") ?? "";
            state = ReadProviderState(session, sessionId);
            return state.IsPaid && !string.IsNullOrWhiteSpace(sessionId);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static HostedCheckoutProviderState ReadProviderState(JsonElement sessionOrEnvelope, string fallbackSessionId)
    {
        var session = sessionOrEnvelope;
        if (session.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            session = data;
        }

        var sessionId = Read(session, "id") ?? fallbackSessionId;
        var paidPayment = FindPaidPayment(session);
        if (paidPayment is not null)
        {
            return new HostedCheckoutProviderState(
                sessionId,
                IsPaid: true,
                IsFailed: false,
                IsExpired: false,
                PaidAmount: CentavosToAmount(paidPayment.Value.Amount),
                CurrencyCode: paidPayment.Value.Currency,
                ProviderPaymentId: paidPayment.Value.Id);
        }

        var intentStatus = Read(session, "attributes", "payment_intent", "attributes", "status");
        var intentAmount = ReadInt(session, "attributes", "payment_intent", "attributes", "amount");
        var intentCurrency = Read(session, "attributes", "payment_intent", "attributes", "currency");
        if (string.Equals(intentStatus, "succeeded", StringComparison.OrdinalIgnoreCase) && intentAmount is int cents)
        {
            return new HostedCheckoutProviderState(
                sessionId,
                IsPaid: true,
                IsFailed: false,
                IsExpired: false,
                PaidAmount: CentavosToAmount(cents),
                CurrencyCode: intentCurrency,
                ProviderPaymentId: Read(session, "attributes", "payment_intent", "id"));
        }

        var status = Read(session, "attributes", "status");
        return new HostedCheckoutProviderState(
            sessionId,
            IsPaid: false,
            IsFailed: string.Equals(intentStatus, "failed", StringComparison.OrdinalIgnoreCase),
            IsExpired: string.Equals(status, "expired", StringComparison.OrdinalIgnoreCase),
            PaidAmount: null,
            CurrencyCode: intentCurrency,
            ProviderPaymentId: null);
    }

    private static (string? Id, int Amount, string? Currency)? FindPaidPayment(JsonElement session)
    {
        if (!session.TryGetProperty("attributes", out var attributes)
            || !attributes.TryGetProperty("payments", out var payments)
            || payments.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var payment in payments.EnumerateArray())
        {
            var status = Read(payment, "attributes", "status");
            var amount = ReadInt(payment, "attributes", "amount");
            if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase) && amount is int cents)
            {
                return (Read(payment, "id"), cents, Read(payment, "attributes", "currency"));
            }
        }

        return null;
    }

    private static decimal CentavosToAmount(int centavos) =>
        decimal.Round(centavos / 100m, 2, MidpointRounding.AwayFromZero);

    private static string? Read(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static int? ReadInt(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        if (current.ValueKind == JsonValueKind.Number && current.TryGetInt32(out var value))
        {
            return value;
        }

        return current.ValueKind == JsonValueKind.String
            && int.TryParse(current.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}

public static class PayMongoWebhookSignature
{
    public static bool Matches(string rawBody, string? signatureHeader, string? secret)
    {
        if (string.IsNullOrWhiteSpace(rawBody) || string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        string? timestamp = null;
        string? testSignature = null;
        string? liveSignature = null;
        foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();
            switch (key)
            {
                case "t":
                    timestamp = value;
                    break;
                case "te":
                    testSignature = value;
                    break;
                case "li":
                    liveSignature = value;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(timestamp))
        {
            return false;
        }

        var expected = Compute(secret, timestamp, rawBody);
        return FixedEquals(expected, testSignature) || FixedEquals(expected, liveSignature);
    }

    public static string Compute(string secret, string timestamp, string rawBody)
    {
        var payload = Encoding.UTF8.GetBytes(timestamp + "." + rawBody);
        var key = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(key, payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedEquals(string expected, string? actual)
    {
        if (string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        var left = Encoding.UTF8.GetBytes(expected);
        var right = Encoding.UTF8.GetBytes(actual.Trim());
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
