using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Domain.Payments;

/// <summary>
/// Authoritative SaaS subscription checkout payment (PaymentTransaction for TASK-50).
/// Distinct from merchant POS tenders and from low-level <see cref="ProviderPayment"/> attempts.
/// Pricing fields are immutable snapshots from <see cref="SubscriptionBillingPricing"/>.
/// </summary>
public sealed class SubscriptionPaymentTransaction
{
    public const int ReferenceMaxLength = 32;
    public const int ProviderReferenceMaxLength = 64;
    public const int CardBrandMaxLength = 32;
    public const int CardLast4MaxLength = 4;
    public const int FailureCodeMaxLength = 64;
    public const int FailureReasonMaxLength = 512;
    public const int PlanKeyMaxLength = 64;

    private readonly List<SubscriptionPaymentActivity> _activities = [];

    public SubscriptionPaymentTransactionId Id { get; }
    public string ReferenceNumber { get; }
    public PlatformUserId InitiatedByUserId { get; }
    public PlatformOrganizationId? OrganizationId { get; private set; }
    public SubscriptionId? SubscriptionId { get; private set; }
    public string PlanKey { get; }
    public BillingCycle BillingCycle { get; }
    public decimal BaseAmount { get; }
    public decimal DiscountAmount { get; }
    public decimal DiscountPercent { get; }
    public decimal FinalAmount { get; }
    public string CurrencyCode { get; }
    public SubscriptionPaymentChannel? Channel { get; private set; }
    public SubscriptionPaymentProvider Provider { get; }
    public SubscriptionPaymentEnvironment Environment { get; }
    public SubscriptionPaymentStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? CardBrand { get; private set; }
    public string? CardLast4 { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ProcessingAtUtc { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public DateTimeOffset? FailedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset? ExpiredAtUtc { get; private set; }
    public DateTimeOffset? PeriodStartUtc { get; private set; }
    public DateTimeOffset? PeriodEndUtc { get; private set; }
    public bool SubscriptionActivated { get; private set; }
    public IReadOnlyList<SubscriptionPaymentActivity> Activities => _activities;

    private SubscriptionPaymentTransaction(
        SubscriptionPaymentTransactionId id,
        string referenceNumber,
        PlatformUserId initiatedByUserId,
        PlatformOrganizationId? organizationId,
        SubscriptionId? subscriptionId,
        string planKey,
        BillingCycle billingCycle,
        decimal baseAmount,
        decimal discountAmount,
        decimal discountPercent,
        decimal finalAmount,
        string currencyCode,
        SubscriptionPaymentChannel? channel,
        SubscriptionPaymentProvider provider,
        SubscriptionPaymentEnvironment environment,
        SubscriptionPaymentStatus status,
        string? providerReference,
        string? cardBrand,
        string? cardLast4,
        string? failureCode,
        string? failureReason,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? processingAtUtc,
        DateTimeOffset? paidAtUtc,
        DateTimeOffset? failedAtUtc,
        DateTimeOffset? cancelledAtUtc,
        DateTimeOffset? expiredAtUtc,
        DateTimeOffset? periodStartUtc,
        DateTimeOffset? periodEndUtc,
        bool subscriptionActivated,
        IEnumerable<SubscriptionPaymentActivity>? activities)
    {
        Id = id;
        ReferenceNumber = referenceNumber;
        InitiatedByUserId = initiatedByUserId;
        OrganizationId = organizationId;
        SubscriptionId = subscriptionId;
        PlanKey = planKey;
        BillingCycle = billingCycle;
        BaseAmount = baseAmount;
        DiscountAmount = discountAmount;
        DiscountPercent = discountPercent;
        FinalAmount = finalAmount;
        CurrencyCode = currencyCode;
        Channel = channel;
        Provider = provider;
        Environment = environment;
        Status = status;
        ProviderReference = providerReference;
        CardBrand = cardBrand;
        CardLast4 = cardLast4;
        FailureCode = failureCode;
        FailureReason = failureReason;
        CreatedAtUtc = createdAtUtc;
        ProcessingAtUtc = processingAtUtc;
        PaidAtUtc = paidAtUtc;
        FailedAtUtc = failedAtUtc;
        CancelledAtUtc = cancelledAtUtc;
        ExpiredAtUtc = expiredAtUtc;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        SubscriptionActivated = subscriptionActivated;
        if (activities is not null)
        {
            _activities.AddRange(activities);
        }
    }

    public static SubscriptionPaymentTransaction CreatePending(
        string referenceNumber,
        PlatformUserId initiatedByUserId,
        string planKey,
        BillingCycle billingCycle,
        SubscriptionPriceQuote quote,
        DateTimeOffset utcNow,
        PlatformOrganizationId? organizationId = null,
        SubscriptionPaymentTransactionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(initiatedByUserId);
        ArgumentNullException.ThrowIfNull(quote);
        DomainTime.EnsureUtc(utcNow);
        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            throw new DomainException(DomainErrorCodes.PaymentReferenceRequired, "Payment reference is required.");
        }

        if (string.IsNullOrWhiteSpace(planKey))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlanCode, "Plan key is required.");
        }

        if (quote.FinalAmount <= 0m)
        {
            throw new DomainException(DomainErrorCodes.PaymentAmountInvalid, "Final amount must be positive.");
        }

        var txn = new SubscriptionPaymentTransaction(
            id ?? SubscriptionPaymentTransactionId.New(),
            referenceNumber.Trim(),
            initiatedByUserId,
            organizationId,
            subscriptionId: null,
            planKey.Trim(),
            billingCycle,
            quote.BaseAmount,
            quote.DiscountAmount,
            quote.DiscountPercent,
            quote.FinalAmount,
            quote.CurrencyCode,
            channel: null,
            SubscriptionPaymentProvider.Simulator,
            SubscriptionPaymentEnvironment.Test,
            SubscriptionPaymentStatus.Pending,
            providerReference: null,
            cardBrand: null,
            cardLast4: null,
            failureCode: null,
            failureReason: null,
            utcNow,
            processingAtUtc: null,
            paidAtUtc: null,
            failedAtUtc: null,
            cancelledAtUtc: null,
            expiredAtUtc: null,
            periodStartUtc: null,
            periodEndUtc: null,
            subscriptionActivated: false,
            activities: null);

        txn.AddActivity("PaymentCreated", "Payment created", utcNow);
        return txn;
    }

    public void AttachOrganization(PlatformOrganizationId organizationId)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        OrganizationId ??= organizationId;
    }

    /// <summary>
    /// Records channel choice while remaining Pending (pre-process UX).
    /// Does not begin provider processing.
    /// </summary>
    public void SelectChannel(SubscriptionPaymentChannel channel, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        EnsureMutable();
        if (Status != SubscriptionPaymentStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                $"Cannot select channel from status {Status}.");
        }

        if (Channel == channel)
        {
            return;
        }

        Channel = channel;
        AddActivity("ChannelSelected", $"{channel} selected", utcNow);
    }

    public void BeginProcessing(
        SubscriptionPaymentChannel channel,
        string providerReference,
        DateTimeOffset utcNow,
        string? cardBrand = null,
        string? cardLast4 = null)
    {
        DomainTime.EnsureUtc(utcNow);
        EnsureMutable();
        if (Status is not (SubscriptionPaymentStatus.Pending or SubscriptionPaymentStatus.Processing))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                $"Cannot begin processing from status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(providerReference))
        {
            throw new DomainException(DomainErrorCodes.PaymentReferenceRequired, "Provider reference is required.");
        }

        Channel = channel;
        ProviderReference ??= providerReference.Trim();
        if (!string.IsNullOrWhiteSpace(cardBrand))
        {
            CardBrand = cardBrand.Trim();
        }

        if (!string.IsNullOrWhiteSpace(cardLast4))
        {
            CardLast4 = cardLast4.Trim();
        }

        if (Status == SubscriptionPaymentStatus.Pending)
        {
            Status = SubscriptionPaymentStatus.Processing;
            ProcessingAtUtc = utcNow;
            AddActivity("ProcessingStarted", "Processing started", utcNow);
        }
    }

    public void MarkPaid(DateTimeOffset utcNow, DateTimeOffset periodStartUtc, DateTimeOffset periodEndUtc)
    {
        DomainTime.EnsureUtc(utcNow);
        DomainTime.EnsureUtc(periodStartUtc);
        DomainTime.EnsureUtc(periodEndUtc);
        if (Status == SubscriptionPaymentStatus.Paid)
        {
            return;
        }

        if (Status is not (SubscriptionPaymentStatus.Pending or SubscriptionPaymentStatus.Processing))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                $"Cannot mark Paid from status {Status}.");
        }

        if (Status == SubscriptionPaymentStatus.Pending)
        {
            Status = SubscriptionPaymentStatus.Processing;
            ProcessingAtUtc ??= utcNow;
            AddActivity("ProcessingStarted", "Processing started", utcNow);
        }

        Status = SubscriptionPaymentStatus.Paid;
        PaidAtUtc = utcNow;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        FailureCode = null;
        FailureReason = null;
        AddActivity("PaymentConfirmed", "Payment confirmed", utcNow);
    }

    public void MarkFailed(string failureCode, string failureReason, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == SubscriptionPaymentStatus.Failed)
        {
            return;
        }

        EnsureNotTerminalSuccess();
        if (Status is SubscriptionPaymentStatus.Cancelled or SubscriptionPaymentStatus.Expired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                $"Cannot mark Failed from status {Status}.");
        }

        if (Status == SubscriptionPaymentStatus.Pending)
        {
            Status = SubscriptionPaymentStatus.Processing;
            ProcessingAtUtc ??= utcNow;
            AddActivity("ProcessingStarted", "Processing started", utcNow);
        }

        Status = SubscriptionPaymentStatus.Failed;
        FailedAtUtc = utcNow;
        FailureCode = Truncate(failureCode, FailureCodeMaxLength);
        FailureReason = Truncate(failureReason, FailureReasonMaxLength);
        AddActivity("PaymentFailed", FailureReason ?? "Payment failed", utcNow);
    }

    public void Cancel(DateTimeOffset utcNow, string? reason = null)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == SubscriptionPaymentStatus.Cancelled)
        {
            return;
        }

        if (Status is not SubscriptionPaymentStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                $"Cannot cancel from status {Status}.");
        }

        Status = SubscriptionPaymentStatus.Cancelled;
        CancelledAtUtc = utcNow;
        FailureReason = Truncate(reason, FailureReasonMaxLength);
        AddActivity("PaymentCancelled", FailureReason ?? "Payment cancelled", utcNow);
    }

    public void Expire(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == SubscriptionPaymentStatus.Expired)
        {
            return;
        }

        if (Status is not (SubscriptionPaymentStatus.Pending or SubscriptionPaymentStatus.Processing))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                $"Cannot expire from status {Status}.");
        }

        Status = SubscriptionPaymentStatus.Expired;
        ExpiredAtUtc = utcNow;
        AddActivity("PaymentExpired", "Payment expired", utcNow);
    }

    public void MarkSubscriptionActivated(SubscriptionId subscriptionId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);
        DomainTime.EnsureUtc(utcNow);
        if (Status != SubscriptionPaymentStatus.Paid)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                "Subscription can only be activated after Paid.");
        }

        if (SubscriptionActivated)
        {
            return;
        }

        SubscriptionId = subscriptionId;
        SubscriptionActivated = true;
        AddActivity(
            "SubscriptionActivated",
            $"{PlanKey} · {BillingCycle}",
            utcNow);
    }

    public static SubscriptionPaymentTransaction Rehydrate(
        SubscriptionPaymentTransactionId id,
        string referenceNumber,
        PlatformUserId initiatedByUserId,
        PlatformOrganizationId? organizationId,
        SubscriptionId? subscriptionId,
        string planKey,
        BillingCycle billingCycle,
        decimal baseAmount,
        decimal discountAmount,
        decimal discountPercent,
        decimal finalAmount,
        string currencyCode,
        SubscriptionPaymentChannel? channel,
        SubscriptionPaymentProvider provider,
        SubscriptionPaymentEnvironment environment,
        SubscriptionPaymentStatus status,
        string? providerReference,
        string? cardBrand,
        string? cardLast4,
        string? failureCode,
        string? failureReason,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? processingAtUtc,
        DateTimeOffset? paidAtUtc,
        DateTimeOffset? failedAtUtc,
        DateTimeOffset? cancelledAtUtc,
        DateTimeOffset? expiredAtUtc,
        DateTimeOffset? periodStartUtc,
        DateTimeOffset? periodEndUtc,
        bool subscriptionActivated,
        IEnumerable<SubscriptionPaymentActivity> activities) =>
        new(
            id,
            referenceNumber,
            initiatedByUserId,
            organizationId,
            subscriptionId,
            planKey,
            billingCycle,
            baseAmount,
            discountAmount,
            discountPercent,
            finalAmount,
            currencyCode,
            channel,
            provider,
            environment,
            status,
            providerReference,
            cardBrand,
            cardLast4,
            failureCode,
            failureReason,
            createdAtUtc,
            processingAtUtc,
            paidAtUtc,
            failedAtUtc,
            cancelledAtUtc,
            expiredAtUtc,
            periodStartUtc,
            periodEndUtc,
            subscriptionActivated,
            activities);

    private void EnsureMutable()
    {
        if (Status is SubscriptionPaymentStatus.Paid
            or SubscriptionPaymentStatus.Failed
            or SubscriptionPaymentStatus.Cancelled
            or SubscriptionPaymentStatus.Expired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                $"Payment is terminal ({Status}).");
        }
    }

    private void EnsureNotTerminalSuccess()
    {
        if (Status == SubscriptionPaymentStatus.Paid)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                "Paid payments cannot transition to Failed.");
        }
    }

    private void AddActivity(string eventType, string message, DateTimeOffset utcNow) =>
        _activities.Add(SubscriptionPaymentActivity.Create(eventType, message, utcNow));

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

public sealed class SubscriptionPaymentActivity
{
    public Guid Id { get; }
    public string EventType { get; }
    public string Message { get; }
    public DateTimeOffset OccurredAtUtc { get; }

    private SubscriptionPaymentActivity(Guid id, string eventType, string message, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        EventType = eventType;
        Message = message;
        OccurredAtUtc = occurredAtUtc;
    }

    public static SubscriptionPaymentActivity Create(string eventType, string message, DateTimeOffset utcNow) =>
        new(Guid.NewGuid(), eventType.Trim(), message.Trim(), utcNow);

    public static SubscriptionPaymentActivity Rehydrate(
        Guid id,
        string eventType,
        string message,
        DateTimeOffset occurredAtUtc) =>
        new(id, eventType, message, occurredAtUtc);
}
