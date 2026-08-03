using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Payments;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class ProviderPaymentEntityMapper
{
    public static ProviderPayment ToDomain(ProviderPaymentRecord record) =>
        ProviderPayment.Rehydrate(
            ProviderPaymentId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            SubscriptionId.From(record.SubscriptionId),
            record.Amount,
            record.CurrencyCode,
            record.Provider,
            record.ProviderReference,
            Enum.Parse<PaymentProviderResultStatus>(record.Status),
            record.IsTest,
            record.FailureCode,
            record.FailureMessage,
            record.IdempotencyKey,
            record.Purpose,
            record.CreatedAtUtc);

    public static ProviderPaymentRecord ToRecord(ProviderPayment payment) =>
        new()
        {
            Id = payment.Id.Value,
            OrganizationId = payment.OrganizationId.Value,
            SubscriptionId = payment.SubscriptionId.Value,
            Amount = payment.Amount,
            CurrencyCode = payment.CurrencyCode,
            Provider = payment.Provider,
            ProviderReference = payment.ProviderReference,
            Status = payment.Status.ToString(),
            IsTest = payment.IsTest,
            FailureCode = payment.FailureCode,
            FailureMessage = payment.FailureMessage,
            IdempotencyKey = payment.IdempotencyKey,
            Purpose = payment.Purpose,
            CreatedAtUtc = payment.CreatedAtUtc
        };
}
