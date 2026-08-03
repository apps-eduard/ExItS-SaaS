using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Subscriptions;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class SubscriptionEntityMapper
{
    public static Subscription ToDomain(SubscriptionRecord record) =>
        Subscription.Rehydrate(
            SubscriptionId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            ProductCode.Create(record.ProductCode),
            PlanId.From(record.PlanId),
            PlanVersionId.From(record.PlanVersionId),
            record.TrialDefinitionId is null ? null : TrialDefinitionId.From(record.TrialDefinitionId.Value),
            Enum.Parse<SubscriptionStatus>(record.Status),
            record.TrialStartUtc,
            record.TrialEndUtc,
            record.PaidPeriodStartUtc,
            record.PaidPeriodEndUtc,
            record.GracePeriodEndUtc,
            record.SuspendedAtUtc,
            record.CancelledAtUtc,
            record.PastDueAtUtc,
            record.ExpiredAtUtc,
            ParseBillingCycle(record.BillingCycle),
            record.AgreedPrice,
            record.CurrencyCode,
            record.PriceEffectiveFromUtc,
            record.PendingPlanId is null ? null : PlanId.From(record.PendingPlanId.Value),
            record.PendingPlanEffectiveAtUtc,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.AggregateVersion);

    public static SubscriptionRecord ToRecord(Subscription subscription) =>
        new()
        {
            Id = subscription.Id.Value,
            OrganizationId = subscription.OrganizationId.Value,
            ProductCode = subscription.ProductCode.Value,
            PlanId = subscription.PlanId.Value,
            PlanVersionId = subscription.PlanVersionId.Value,
            TrialDefinitionId = subscription.TrialDefinitionId?.Value,
            Status = subscription.Status.ToString(),
            TrialStartUtc = subscription.TrialStartUtc,
            TrialEndUtc = subscription.TrialEndUtc,
            PaidPeriodStartUtc = subscription.PaidPeriodStartUtc,
            PaidPeriodEndUtc = subscription.PaidPeriodEndUtc,
            GracePeriodEndUtc = subscription.GracePeriodEndUtc,
            SuspendedAtUtc = subscription.SuspendedAtUtc,
            CancelledAtUtc = subscription.CancelledAtUtc,
            PastDueAtUtc = subscription.PastDueAtUtc,
            ExpiredAtUtc = subscription.ExpiredAtUtc,
            BillingCycle = subscription.BillingCycle?.ToString(),
            AgreedPrice = subscription.AgreedPrice,
            CurrencyCode = subscription.CurrencyCode,
            PriceEffectiveFromUtc = subscription.PriceEffectiveFromUtc,
            PendingPlanId = subscription.PendingPlanId?.Value,
            PendingPlanEffectiveAtUtc = subscription.PendingPlanEffectiveAtUtc,
            CreatedAtUtc = subscription.CreatedAtUtc,
            UpdatedAtUtc = subscription.UpdatedAtUtc,
            AggregateVersion = subscription.Version
        };

    public static void ApplyToRecord(Subscription subscription, SubscriptionRecord record)
    {
        record.PlanId = subscription.PlanId.Value;
        record.PlanVersionId = subscription.PlanVersionId.Value;
        record.Status = subscription.Status.ToString();
        record.TrialStartUtc = subscription.TrialStartUtc;
        record.TrialEndUtc = subscription.TrialEndUtc;
        record.PaidPeriodStartUtc = subscription.PaidPeriodStartUtc;
        record.PaidPeriodEndUtc = subscription.PaidPeriodEndUtc;
        record.GracePeriodEndUtc = subscription.GracePeriodEndUtc;
        record.SuspendedAtUtc = subscription.SuspendedAtUtc;
        record.CancelledAtUtc = subscription.CancelledAtUtc;
        record.PastDueAtUtc = subscription.PastDueAtUtc;
        record.ExpiredAtUtc = subscription.ExpiredAtUtc;
        record.BillingCycle = subscription.BillingCycle?.ToString();
        record.AgreedPrice = subscription.AgreedPrice;
        record.CurrencyCode = subscription.CurrencyCode;
        record.PriceEffectiveFromUtc = subscription.PriceEffectiveFromUtc;
        record.PendingPlanId = subscription.PendingPlanId?.Value;
        record.PendingPlanEffectiveAtUtc = subscription.PendingPlanEffectiveAtUtc;
        record.UpdatedAtUtc = subscription.UpdatedAtUtc;
        record.AggregateVersion = subscription.Version;
    }

    private static BillingCycle? ParseBillingCycle(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<BillingCycle>(value);
}
