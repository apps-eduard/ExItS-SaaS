using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Payments;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class SubscriptionPaymentTransactionMapper
{
    public static SubscriptionPaymentTransaction ToDomain(SubscriptionPaymentTransactionRecord record)
    {
        var activities = record.Activities
            .OrderBy(a => a.OccurredAtUtc)
            .Select(a => SubscriptionPaymentActivity.Rehydrate(a.Id, a.EventType, a.Message, a.OccurredAtUtc))
            .ToArray();

        return SubscriptionPaymentTransaction.Rehydrate(
            SubscriptionPaymentTransactionId.From(record.Id),
            record.ReferenceNumber,
            PlatformUserId.From(record.InitiatedByUserId),
            record.OrganizationId is Guid oid ? PlatformOrganizationId.From(oid) : null,
            record.SubscriptionId is Guid sid ? SubscriptionId.From(sid) : null,
            record.PlanKey,
            Enum.Parse<BillingCycle>(record.BillingCycle),
            record.BaseAmount,
            record.DiscountAmount,
            record.DiscountPercent,
            record.FinalAmount,
            record.CurrencyCode,
            string.IsNullOrWhiteSpace(record.Channel)
                ? null
                : Enum.Parse<SubscriptionPaymentChannel>(record.Channel),
            Enum.Parse<SubscriptionPaymentProvider>(record.Provider),
            Enum.Parse<SubscriptionPaymentEnvironment>(record.Environment),
            Enum.Parse<SubscriptionPaymentStatus>(record.Status),
            record.ProviderReference,
            record.CardBrand,
            record.CardLast4,
            record.FailureCode,
            record.FailureReason,
            record.CreatedAtUtc,
            record.ProcessingAtUtc,
            record.PaidAtUtc,
            record.FailedAtUtc,
            record.CancelledAtUtc,
            record.ExpiredAtUtc,
            record.PeriodStartUtc,
            record.PeriodEndUtc,
            record.SubscriptionActivated,
            activities);
    }

    public static void ApplyToRecord(
        SubscriptionPaymentTransaction payment,
        SubscriptionPaymentTransactionRecord record,
        bool assignIdentity = false)
    {
        if (assignIdentity || record.Id == Guid.Empty)
        {
            record.Id = payment.Id.Value;
        }

        record.ReferenceNumber = payment.ReferenceNumber;
        record.InitiatedByUserId = payment.InitiatedByUserId.Value;
        record.OrganizationId = payment.OrganizationId?.Value;
        record.SubscriptionId = payment.SubscriptionId?.Value;
        record.PlanKey = payment.PlanKey;
        record.BillingCycle = payment.BillingCycle.ToString();
        record.BaseAmount = payment.BaseAmount;
        record.DiscountAmount = payment.DiscountAmount;
        record.DiscountPercent = payment.DiscountPercent;
        record.FinalAmount = payment.FinalAmount;
        record.CurrencyCode = payment.CurrencyCode;
        record.Channel = payment.Channel?.ToString();
        record.Provider = payment.Provider.ToString();
        record.Environment = payment.Environment.ToString();
        record.Status = payment.Status.ToString();
        record.ProviderReference = payment.ProviderReference;
        record.CardBrand = payment.CardBrand;
        record.CardLast4 = payment.CardLast4;
        record.FailureCode = payment.FailureCode;
        record.FailureReason = payment.FailureReason;
        if (assignIdentity || record.CreatedAtUtc == default)
        {
            record.CreatedAtUtc = payment.CreatedAtUtc;
        }
        record.ProcessingAtUtc = payment.ProcessingAtUtc;
        record.PaidAtUtc = payment.PaidAtUtc;
        record.FailedAtUtc = payment.FailedAtUtc;
        record.CancelledAtUtc = payment.CancelledAtUtc;
        record.ExpiredAtUtc = payment.ExpiredAtUtc;
        record.PeriodStartUtc = payment.PeriodStartUtc;
        record.PeriodEndUtc = payment.PeriodEndUtc;
        record.SubscriptionActivated = payment.SubscriptionActivated;

        var existing = record.Activities.ToDictionary(a => a.Id);
        foreach (var activity in payment.Activities)
        {
            if (existing.TryGetValue(activity.Id, out var row))
            {
                row.EventType = activity.EventType;
                row.Message = activity.Message;
                row.OccurredAtUtc = activity.OccurredAtUtc;
                row.PaymentId = payment.Id.Value;
            }
            else
            {
                record.Activities.Add(new SubscriptionPaymentActivityRecord
                {
                    Id = activity.Id,
                    PaymentId = payment.Id.Value,
                    EventType = activity.EventType,
                    Message = activity.Message,
                    OccurredAtUtc = activity.OccurredAtUtc
                });
            }
        }
    }

    public static SubscriptionPaymentTransactionRecord ToNewRecord(SubscriptionPaymentTransaction payment)
    {
        var record = new SubscriptionPaymentTransactionRecord();
        ApplyToRecord(payment, record, assignIdentity: true);
        return record;
    }
}
