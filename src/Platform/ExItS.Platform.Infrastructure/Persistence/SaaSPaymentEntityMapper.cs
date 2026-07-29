using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Payments;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class SaaSPaymentEntityMapper
{
    public static SaaSPayment ToDomain(SaaSPaymentRecord record) =>
        SaaSPayment.Rehydrate(
            SaaSPaymentId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            ProductCode.Create(record.ProductCode),
            record.SubscriptionId is null ? null : SubscriptionId.From(record.SubscriptionId.Value),
            record.Amount,
            CurrencyCode.Create(record.CurrencyCode),
            Enum.Parse<SaaSPaymentMethod>(record.Method),
            record.ExternalReference,
            record.NormalizedReference,
            Enum.Parse<SaaSPaymentStatus>(record.Status),
            record.PaidAtUtc,
            record.ConfirmedAtUtc,
            record.ConfirmedBy,
            record.RejectedAtUtc,
            record.RejectedBy,
            record.RejectionReason,
            record.VoidedAtUtc,
            record.VoidedBy,
            record.VoidReason,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.AggregateVersion);

    public static SaaSPaymentRecord ToRecord(SaaSPayment payment) =>
        new()
        {
            Id = payment.Id.Value,
            OrganizationId = payment.OrganizationId.Value,
            ProductCode = payment.ProductCode.Value,
            SubscriptionId = payment.SubscriptionId?.Value,
            Amount = payment.Amount,
            CurrencyCode = payment.CurrencyCode.Value,
            Method = payment.Method.ToString(),
            Status = payment.Status.ToString(),
            ExternalReference = payment.ExternalReference,
            NormalizedReference = payment.NormalizedReference,
            PaidAtUtc = payment.PaidAtUtc,
            ConfirmedAtUtc = payment.ConfirmedAtUtc,
            ConfirmedBy = payment.ConfirmedBy,
            RejectedAtUtc = payment.RejectedAtUtc,
            RejectedBy = payment.RejectedBy,
            RejectionReason = payment.RejectionReason,
            VoidedAtUtc = payment.VoidedAtUtc,
            VoidedBy = payment.VoidedBy,
            VoidReason = payment.VoidReason,
            CreatedAtUtc = payment.CreatedAtUtc,
            UpdatedAtUtc = payment.UpdatedAtUtc,
            AggregateVersion = payment.Version
        };

    public static void ApplyToRecord(SaaSPayment payment, SaaSPaymentRecord record)
    {
        record.SubscriptionId = payment.SubscriptionId?.Value;
        record.Status = payment.Status.ToString();
        record.ConfirmedAtUtc = payment.ConfirmedAtUtc;
        record.ConfirmedBy = payment.ConfirmedBy;
        record.RejectedAtUtc = payment.RejectedAtUtc;
        record.RejectedBy = payment.RejectedBy;
        record.RejectionReason = payment.RejectionReason;
        record.VoidedAtUtc = payment.VoidedAtUtc;
        record.VoidedBy = payment.VoidedBy;
        record.VoidReason = payment.VoidReason;
        record.UpdatedAtUtc = payment.UpdatedAtUtc;
        record.AggregateVersion = payment.Version;
    }
}
