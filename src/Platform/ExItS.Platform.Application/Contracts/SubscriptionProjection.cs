using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Contracts;

/// <summary>HealthCare-facing commercial subscription projection. No payment secrets.</summary>
public sealed class SubscriptionProjection
{
    public PlatformOrganizationId PlatformOrganizationId { get; }
    public ProductCode ProductCode { get; }
    public SubscriptionId SubscriptionId { get; }
    public string PlanCode { get; }
    public int PlanVersionNumber { get; }
    public SubscriptionStatus SubscriptionStatus { get; }
    public DateTimeOffset? TrialStartUtc { get; }
    public DateTimeOffset? TrialEndUtc { get; }
    public DateTimeOffset? PaidPeriodStartUtc { get; }
    public DateTimeOffset? PaidPeriodEndUtc { get; }
    public DateTimeOffset? GracePeriodEndUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; }
    public int SourceAggregateVersion { get; }

    public SubscriptionProjection(
        PlatformOrganizationId platformOrganizationId,
        ProductCode productCode,
        SubscriptionId subscriptionId,
        PlanCode planCode,
        int planVersionNumber,
        SubscriptionStatus subscriptionStatus,
        DateTimeOffset updatedAtUtc,
        int sourceAggregateVersion,
        DateTimeOffset? trialStartUtc = null,
        DateTimeOffset? trialEndUtc = null,
        DateTimeOffset? paidPeriodStartUtc = null,
        DateTimeOffset? paidPeriodEndUtc = null,
        DateTimeOffset? gracePeriodEndUtc = null)
    {
        ArgumentNullException.ThrowIfNull(platformOrganizationId);
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(subscriptionId);
        ArgumentNullException.ThrowIfNull(planCode);

        EnsureUtc(updatedAtUtc);
        EnsureUtcOptional(trialStartUtc);
        EnsureUtcOptional(trialEndUtc);
        EnsureUtcOptional(paidPeriodStartUtc);
        EnsureUtcOptional(paidPeriodEndUtc);
        EnsureUtcOptional(gracePeriodEndUtc);

        if (planVersionNumber < 1)
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Plan version number must be positive.");
        }

        if (sourceAggregateVersion < 1)
        {
            throw new ContractException(ContractErrorCodes.InvalidSourceVersion, "Source aggregate version must be positive.");
        }

        if (!Enum.IsDefined(subscriptionStatus))
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Subscription status is invalid.");
        }

        PlatformOrganizationId = platformOrganizationId;
        ProductCode = productCode;
        SubscriptionId = subscriptionId;
        PlanCode = planCode.Value;
        PlanVersionNumber = planVersionNumber;
        SubscriptionStatus = subscriptionStatus;
        TrialStartUtc = trialStartUtc;
        TrialEndUtc = trialEndUtc;
        PaidPeriodStartUtc = paidPeriodStartUtc;
        PaidPeriodEndUtc = paidPeriodEndUtc;
        GracePeriodEndUtc = gracePeriodEndUtc;
        UpdatedAtUtc = updatedAtUtc;
        SourceAggregateVersion = sourceAggregateVersion;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ContractException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }

    private static void EnsureUtcOptional(DateTimeOffset? value)
    {
        if (value is not null)
        {
            EnsureUtc(value.Value);
        }
    }
}
