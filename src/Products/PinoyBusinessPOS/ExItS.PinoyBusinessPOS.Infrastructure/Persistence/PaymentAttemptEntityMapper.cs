using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class PaymentAttemptEntityMapper
{
    public static PaymentAttempt ToDomain(PaymentAttemptRecord r) =>
        PaymentAttempt.Rehydrate(
            PaymentAttemptId.From(r.Id),
            PosOrganizationId.From(r.OrganizationId),
            SaleId.From(r.SaleId),
            Enum.Parse<PaymentAttemptMethod>(r.Method),
            Enum.Parse<PaymentProvider>(r.Provider),
            r.ProviderReference,
            r.ExternalReference,
            r.Amount,
            r.Currency,
            Enum.Parse<PaymentAttemptStatus>(r.Status),
            r.CheckoutUrl,
            r.DeepLink,
            r.QrPayload,
            r.CardBrand,
            r.CardLastFour,
            r.FailureCode,
            r.FailureMessage,
            r.IdempotencyKey,
            r.CreatedBy,
            r.VerifiedBy,
            r.VerificationReason,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.ExpiresAtUtc,
            r.CompletedAtUtc,
            r.ProviderEventSequence);

    public static PaymentAttemptRecord ToRecord(PaymentAttempt a) =>
        new()
        {
            Id = a.Id.Value,
            OrganizationId = a.OrganizationId.Value,
            SaleId = a.SaleId.Value,
            Method = a.Method.ToString(),
            Provider = a.Provider.ToString(),
            ProviderReference = a.ProviderReference,
            ExternalReference = a.ExternalReference,
            Amount = a.Amount,
            Currency = a.Currency,
            Status = a.Status.ToString(),
            CheckoutUrl = a.CheckoutUrl,
            DeepLink = a.DeepLink,
            QrPayload = a.QrPayload,
            CardBrand = a.CardBrand,
            CardLastFour = a.CardLastFour,
            FailureCode = a.FailureCode,
            FailureMessage = a.FailureMessage,
            IdempotencyKey = a.IdempotencyKey,
            CreatedBy = a.CreatedBy,
            VerifiedBy = a.VerifiedBy,
            VerificationReason = a.VerificationReason,
            CreatedAtUtc = a.CreatedAtUtc,
            UpdatedAtUtc = a.UpdatedAtUtc,
            ExpiresAtUtc = a.ExpiresAtUtc,
            CompletedAtUtc = a.CompletedAtUtc,
            ProviderEventSequence = a.ProviderEventSequence
        };

    public static void Apply(PaymentAttempt a, PaymentAttemptRecord r)
    {
        r.Provider = a.Provider.ToString();
        r.ProviderReference = a.ProviderReference;
        r.ExternalReference = a.ExternalReference;
        r.Status = a.Status.ToString();
        r.CheckoutUrl = a.CheckoutUrl;
        r.DeepLink = a.DeepLink;
        r.QrPayload = a.QrPayload;
        r.CardBrand = a.CardBrand;
        r.CardLastFour = a.CardLastFour;
        r.FailureCode = a.FailureCode;
        r.FailureMessage = a.FailureMessage;
        r.VerifiedBy = a.VerifiedBy;
        r.VerificationReason = a.VerificationReason;
        r.UpdatedAtUtc = a.UpdatedAtUtc;
        r.ExpiresAtUtc = a.ExpiresAtUtc;
        r.CompletedAtUtc = a.CompletedAtUtc;
        r.ProviderEventSequence = a.ProviderEventSequence;
    }
}
