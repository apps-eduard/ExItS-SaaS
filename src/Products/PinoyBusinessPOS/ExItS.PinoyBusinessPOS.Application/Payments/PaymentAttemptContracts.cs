using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public interface IPaymentAttemptRepository
{
    Task<PaymentAttempt?> GetByIdAsync(
        PosOrganizationId organizationId,
        PaymentAttemptId id,
        CancellationToken cancellationToken = default);

    Task<PaymentAttempt?> GetByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PaymentAttempt?> GetByProviderReferenceAsync(
        string provider,
        string providerReference,
        CancellationToken cancellationToken = default);

    Task<PaymentAttempt?> FindActiveForSaleAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsExternalReferenceAsync(
        PosOrganizationId organizationId,
        string externalReference,
        PaymentAttemptId? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(PaymentAttempt attempt, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentAttempt attempt, CancellationToken cancellationToken = default);
}

public sealed record PaymentAttemptDto(
    Guid Id,
    Guid SaleId,
    Guid OrganizationId,
    string Method,
    string Provider,
    string? ProviderReference,
    string? ExternalReference,
    decimal Amount,
    string Currency,
    string Status,
    string? CheckoutUrl,
    string? DeepLink,
    string? QrPayload,
    string? CardBrand,
    string? CardLastFour,
    string? FailureCode,
    string? FailureMessage,
    string IdempotencyKey,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? CompletedAtUtc,
    Guid? VerifiedBy,
    string? VerificationReason,
    bool ProviderFinalizedBySystem);

public sealed record CreatePaymentAttemptRequest(
    string Method,
    string IdempotencyKey,
    string? ExternalReference = null,
    bool ManualGCashTransfer = false);

public sealed record SimulatePaymentRequest(string Outcome);

public static class PaymentAttemptMaps
{
    public static PaymentAttemptDto Map(PaymentAttempt a) =>
        new(
            a.Id.Value,
            a.SaleId.Value,
            a.OrganizationId.Value,
            a.Method.ToString(),
            a.Provider.ToString(),
            a.ProviderReference,
            a.ExternalReference,
            a.Amount,
            a.Currency,
            a.Status.ToString(),
            a.CheckoutUrl,
            a.DeepLink,
            a.QrPayload,
            a.CardBrand,
            a.CardLastFour,
            a.FailureCode,
            a.FailureMessage,
            a.IdempotencyKey,
            a.CreatedAtUtc,
            a.UpdatedAtUtc,
            a.ExpiresAtUtc,
            a.CompletedAtUtc,
            a.VerifiedBy,
            a.VerificationReason,
            a.ProviderFinalizedBySystem);
}
