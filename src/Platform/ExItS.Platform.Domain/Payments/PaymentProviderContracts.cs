namespace ExItS.Platform.Domain.Payments;

public enum PaymentProviderResultStatus
{
    Succeeded = 0,
    Declined = 1,
    Pending = 2,
    Failed = 3,
    Refunded = 4,
    RenewalSucceeded = 5,
    RenewalFailed = 6
}

public sealed record PaymentChargeRequest(
    Guid OrganizationId,
    Guid SubscriptionId,
    decimal Amount,
    string CurrencyCode,
    string IdempotencyKey,
    string? Purpose);

public sealed record PaymentProviderResult(
    PaymentProviderResultStatus Status,
    string Provider,
    string ProviderReference,
    decimal Amount,
    string CurrencyCode,
    bool IsTest,
    string? FailureCode,
    string? FailureMessage,
    string IdempotencyKey);

public interface IPaymentProvider
{
    string ProviderName { get; }
    bool IsTestProvider { get; }
    Task<PaymentProviderResult> ChargeAsync(PaymentChargeRequest request, CancellationToken ct = default);
    Task<PaymentProviderResult> SimulateAsync(string simulation, PaymentChargeRequest request, CancellationToken ct = default);
}
