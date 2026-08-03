using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Domain.Payments;

/// <summary>
/// Manually reported SaaS subscription payment (cash, bank transfer, or GCash reference) awaiting
/// staff confirmation before a subscription is activated. Not a POS/retail sale, Utang credit
/// payment, payment gateway transaction, webhook event, QR code, card record, or invoice.
/// </summary>
public sealed class SaaSPayment
{
    public SaaSPaymentId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public ProductCode ProductCode { get; }
    public SubscriptionId? SubscriptionId { get; private set; }
    public decimal Amount { get; }
    public CurrencyCode CurrencyCode { get; }
    public SaaSPaymentMethod Method { get; }
    public string ExternalReference { get; }
    internal string NormalizedReference { get; }
    public SaaSPaymentStatus Status { get; private set; }
    public DateTimeOffset PaidAtUtc { get; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }
    public string? RejectedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public string? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }

    private SaaSPayment(
        SaaSPaymentId id,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        SubscriptionId? subscriptionId,
        decimal amount,
        CurrencyCode currencyCode,
        SaaSPaymentMethod method,
        string externalReference,
        string normalizedReference,
        SaaSPaymentStatus status,
        DateTimeOffset paidAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        Id = id;
        OrganizationId = organizationId;
        ProductCode = productCode;
        SubscriptionId = subscriptionId;
        Amount = amount;
        CurrencyCode = currencyCode;
        Method = method;
        ExternalReference = externalReference;
        NormalizedReference = normalizedReference;
        Status = status;
        PaidAtUtc = paidAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Version = version;
    }

    public static SaaSPayment CreateManual(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        decimal amount,
        CurrencyCode currencyCode,
        SaaSPaymentMethod method,
        string externalReference,
        DateTimeOffset paidAtUtc,
        DateTimeOffset utcNow,
        SaaSPaymentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(currencyCode);
        DomainTime.EnsureUtc(paidAtUtc);
        DomainTime.EnsureUtc(utcNow);

        if (!Enum.IsDefined(method))
        {
            throw new DomainException(DomainErrorCodes.InvalidSaaSPaymentTransition, "Payment method is not defined.");
        }

        if (method == SaaSPaymentMethod.Online)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaaSPaymentTransition,
                "Online payments must be recorded from a successful provider charge.");
        }

        if (amount <= 0)
        {
            throw new DomainException(DomainErrorCodes.PaymentAmountInvalid, "Payment amount must be positive.");
        }

        var normalizedReference = NormalizeReference(externalReference);
        var trimmedReference = externalReference.Trim();

        return new SaaSPayment(
            id ?? SaaSPaymentId.New(),
            organizationId,
            productCode,
            subscriptionId: null,
            amount,
            currencyCode,
            method,
            trimmedReference,
            normalizedReference,
            SaaSPaymentStatus.PendingConfirmation,
            paidAtUtc,
            utcNow,
            utcNow,
            version: 1);
    }

    /// <summary>
    /// Records a successful provider charge as a confirmed SaaS payment already linked to the
    /// subscription it funded — visible in Platform Administration → Payments.
    /// </summary>
    public static SaaSPayment CreateConfirmedLinkedFromProvider(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        SubscriptionId subscriptionId,
        decimal amount,
        CurrencyCode currencyCode,
        string providerReference,
        string confirmedBy,
        DateTimeOffset paidAtUtc,
        DateTimeOffset utcNow,
        SaaSPaymentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(subscriptionId);
        ArgumentNullException.ThrowIfNull(currencyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmedBy);
        DomainTime.EnsureUtc(paidAtUtc);
        DomainTime.EnsureUtc(utcNow);

        if (amount <= 0)
        {
            throw new DomainException(DomainErrorCodes.PaymentAmountInvalid, "Payment amount must be positive.");
        }

        var normalizedReference = NormalizeReference(providerReference);
        var trimmedReference = providerReference.Trim();

        var payment = new SaaSPayment(
            id ?? SaaSPaymentId.New(),
            organizationId,
            productCode,
            subscriptionId,
            amount,
            currencyCode,
            SaaSPaymentMethod.Online,
            trimmedReference,
            normalizedReference,
            SaaSPaymentStatus.Confirmed,
            paidAtUtc,
            utcNow,
            utcNow,
            version: 1);

        payment.ConfirmedAtUtc = utcNow;
        payment.ConfirmedBy = confirmedBy.Trim();
        return payment;
    }

    internal static SaaSPayment Rehydrate(
        SaaSPaymentId id,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        SubscriptionId? subscriptionId,
        decimal amount,
        CurrencyCode currencyCode,
        SaaSPaymentMethod method,
        string externalReference,
        string normalizedReference,
        SaaSPaymentStatus status,
        DateTimeOffset paidAtUtc,
        DateTimeOffset? confirmedAtUtc,
        string? confirmedBy,
        DateTimeOffset? rejectedAtUtc,
        string? rejectedBy,
        string? rejectionReason,
        DateTimeOffset? voidedAtUtc,
        string? voidedBy,
        string? voidReason,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        var payment = new SaaSPayment(
            id,
            organizationId,
            productCode,
            subscriptionId,
            amount,
            currencyCode,
            method,
            externalReference,
            normalizedReference,
            status,
            paidAtUtc,
            createdAtUtc,
            updatedAtUtc,
            version);

        payment.ConfirmedAtUtc = confirmedAtUtc;
        payment.ConfirmedBy = confirmedBy;
        payment.RejectedAtUtc = rejectedAtUtc;
        payment.RejectedBy = rejectedBy;
        payment.RejectionReason = rejectionReason;
        payment.VoidedAtUtc = voidedAtUtc;
        payment.VoidedBy = voidedBy;
        payment.VoidReason = voidReason;
        return payment;
    }

    public void Confirm(string confirmedBy, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);

        if (Status == SaaSPaymentStatus.Confirmed)
        {
            throw new DomainException(DomainErrorCodes.PaymentAlreadyConfirmed, "Payment is already confirmed.");
        }

        if (Status != SaaSPaymentStatus.PendingConfirmation)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaaSPaymentTransition,
                $"Cannot confirm a payment in status {Status}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(confirmedBy);

        Status = SaaSPaymentStatus.Confirmed;
        ConfirmedAtUtc = utcNow;
        ConfirmedBy = confirmedBy.Trim();
        UpdatedAtUtc = utcNow;
        Version++;
    }

    public void Reject(string rejectedBy, string reason, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);

        if (Status != SaaSPaymentStatus.PendingConfirmation)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaaSPaymentTransition,
                $"Cannot reject a payment in status {Status}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rejectedBy);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(DomainErrorCodes.PaymentReasonRequired, "Rejection reason is required.");
        }

        Status = SaaSPaymentStatus.Rejected;
        RejectedAtUtc = utcNow;
        RejectedBy = rejectedBy.Trim();
        RejectionReason = reason.Trim();
        UpdatedAtUtc = utcNow;
        Version++;
    }

    public void Void(string voidedBy, string reason, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);

        if (Status != SaaSPaymentStatus.Confirmed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaaSPaymentTransition,
                $"Cannot void a payment in status {Status}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(voidedBy);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(DomainErrorCodes.PaymentReasonRequired, "Void reason is required.");
        }

        Status = SaaSPaymentStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedBy = voidedBy.Trim();
        VoidReason = reason.Trim();
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>
    /// Links this payment to the subscription it activated. Only permitted once, and only once the
    /// payment has been confirmed — prevents a single manual payment from activating more than one
    /// subscription.
    /// </summary>
    public void LinkSubscription(SubscriptionId subscriptionId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);
        DomainTime.EnsureUtc(utcNow);

        if (Status != SaaSPaymentStatus.Confirmed || SubscriptionId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.PaymentAlreadyUsed,
                "Payment must be confirmed and not already linked to a subscription.");
        }

        SubscriptionId = subscriptionId;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    /// <summary>Trims, uppercases, and collapses internal whitespace for stable reference matching.</summary>
    public static string NormalizeReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException(DomainErrorCodes.PaymentReferenceRequired, "Payment reference cannot be blank.");
        }

        var collapsed = Regex.Replace(reference.Trim(), @"\s+", " ");
        return collapsed.ToUpperInvariant();
    }
}
