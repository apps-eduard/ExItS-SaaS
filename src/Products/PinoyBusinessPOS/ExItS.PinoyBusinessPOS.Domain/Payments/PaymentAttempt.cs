using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>
/// Provider-neutral payment attempt for Card / GCash (simulated or future real gateway)
/// and explicit Manual GCash transfer verification. Never stores card numbers, CVV, OTP, PIN,
/// or wallet credentials.
/// </summary>
public sealed class PaymentAttempt
{
    public const int ProviderReferenceMaxLength = 128;
    public const int ExternalReferenceMaxLength = 128;
    public const int IdempotencyKeyMaxLength = 128;
    public const int FailureCodeMaxLength = 64;
    public const int FailureMessageMaxLength = 500;
    public const int UrlMaxLength = 2048;
    public const int QrPayloadMaxLength = 2048;
    public const int CurrencyLength = 3;
    public static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(15);

    public PaymentAttemptId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public SaleId SaleId { get; }
    public PaymentAttemptMethod Method { get; }
    public PaymentProvider Provider { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? ExternalReference { get; private set; }
    public decimal Amount { get; }
    public string Currency { get; }
    public PaymentAttemptStatus Status { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public string? DeepLink { get; private set; }
    public string? QrPayload { get; private set; }
    public string? CardBrand { get; private set; }
    public string? CardLastFour { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public string IdempotencyKey { get; }
    public Guid CreatedBy { get; }
    public Guid? VerifiedBy { get; private set; }
    public string? VerificationReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public long ProviderEventSequence { get; private set; }
    public bool ProviderFinalizedBySystem { get; private set; }

    private PaymentAttempt(
        PaymentAttemptId id,
        PosOrganizationId organizationId,
        SaleId saleId,
        PaymentAttemptMethod method,
        PaymentProvider provider,
        string? providerReference,
        string? externalReference,
        decimal amount,
        string currency,
        PaymentAttemptStatus status,
        string? checkoutUrl,
        string? deepLink,
        string? qrPayload,
        string? cardBrand,
        string? cardLastFour,
        string? failureCode,
        string? failureMessage,
        string idempotencyKey,
        Guid createdBy,
        Guid? verifiedBy,
        string? verificationReason,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset? completedAtUtc,
        long providerEventSequence,
        bool providerFinalizedBySystem)
    {
        Id = id;
        OrganizationId = organizationId;
        SaleId = saleId;
        Method = method;
        Provider = provider;
        ProviderReference = providerReference;
        ExternalReference = externalReference;
        Amount = amount;
        Currency = currency;
        Status = status;
        CheckoutUrl = checkoutUrl;
        DeepLink = deepLink;
        QrPayload = qrPayload;
        CardBrand = cardBrand;
        CardLastFour = cardLastFour;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        IdempotencyKey = idempotencyKey;
        CreatedBy = createdBy;
        VerifiedBy = verifiedBy;
        VerificationReason = verificationReason;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        CompletedAtUtc = completedAtUtc;
        ProviderEventSequence = providerEventSequence;
        ProviderFinalizedBySystem = providerFinalizedBySystem;
    }

    public static PaymentAttempt CreateElectronic(
        PosOrganizationId organizationId,
        SaleId saleId,
        PaymentAttemptMethod method,
        decimal amount,
        string currency,
        string idempotencyKey,
        Guid createdBy,
        DateTimeOffset utcNow,
        PaymentAttemptId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(createdBy);
        if (method is not (PaymentAttemptMethod.Card or PaymentAttemptMethod.GCash))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptMethod,
                "Electronic attempts must use Card or GCash.");
        }

        EnsurePositiveAmount(amount);
        var key = NormalizeIdempotencyKey(idempotencyKey);
        var ccy = NormalizeCurrency(currency);

        return new PaymentAttempt(
            id ?? PaymentAttemptId.New(),
            organizationId,
            saleId,
            method,
            PaymentProvider.Fake,
            providerReference: null,
            externalReference: null,
            amount,
            ccy,
            PaymentAttemptStatus.Created,
            checkoutUrl: null,
            deepLink: null,
            qrPayload: null,
            cardBrand: null,
            cardLastFour: null,
            failureCode: null,
            failureMessage: null,
            key,
            createdBy,
            verifiedBy: null,
            verificationReason: null,
            utcNow,
            utcNow,
            expiresAtUtc: utcNow.Add(DefaultExpiry),
            completedAtUtc: null,
            providerEventSequence: 0,
            providerFinalizedBySystem: false);
    }

    public static PaymentAttempt CreateManualGCashTransfer(
        PosOrganizationId organizationId,
        SaleId saleId,
        decimal amount,
        string currency,
        string externalReference,
        string idempotencyKey,
        Guid createdBy,
        DateTimeOffset utcNow,
        PaymentAttemptId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(createdBy);
        EnsurePositiveAmount(amount);
        var ext = NormalizeExternalReference(externalReference);
        var key = NormalizeIdempotencyKey(idempotencyKey);

        return new PaymentAttempt(
            id ?? PaymentAttemptId.New(),
            organizationId,
            saleId,
            PaymentAttemptMethod.ManualGCashTransfer,
            PaymentProvider.Manual,
            providerReference: null,
            ext,
            amount,
            NormalizeCurrency(currency),
            PaymentAttemptStatus.PendingManualVerification,
            checkoutUrl: null,
            deepLink: null,
            qrPayload: null,
            cardBrand: null,
            cardLastFour: null,
            failureCode: null,
            failureMessage: null,
            key,
            createdBy,
            verifiedBy: null,
            verificationReason: null,
            utcNow,
            utcNow,
            expiresAtUtc: null,
            completedAtUtc: null,
            providerEventSequence: 0,
            providerFinalizedBySystem: false);
    }

    public static PaymentAttempt Rehydrate(
        PaymentAttemptId id,
        PosOrganizationId organizationId,
        SaleId saleId,
        PaymentAttemptMethod method,
        PaymentProvider provider,
        string? providerReference,
        string? externalReference,
        decimal amount,
        string currency,
        PaymentAttemptStatus status,
        string? checkoutUrl,
        string? deepLink,
        string? qrPayload,
        string? cardBrand,
        string? cardLastFour,
        string? failureCode,
        string? failureMessage,
        string idempotencyKey,
        Guid createdBy,
        Guid? verifiedBy,
        string? verificationReason,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset? completedAtUtc,
        long providerEventSequence,
        bool providerFinalizedBySystem) =>
        new(
            id,
            organizationId,
            saleId,
            method,
            provider,
            providerReference,
            externalReference,
            amount,
            currency,
            status,
            checkoutUrl,
            deepLink,
            qrPayload,
            cardBrand,
            cardLastFour,
            failureCode,
            failureMessage,
            idempotencyKey,
            createdBy,
            verifiedBy,
            verificationReason,
            createdAtUtc,
            updatedAtUtc,
            expiresAtUtc,
            completedAtUtc,
            providerEventSequence,
            providerFinalizedBySystem);

    public bool IsTerminal =>
        Status is PaymentAttemptStatus.Paid
            or PaymentAttemptStatus.Failed
            or PaymentAttemptStatus.Cancelled
            or PaymentAttemptStatus.Expired
            or PaymentAttemptStatus.Refunded;

    public bool IsActiveElectronic =>
        Method is PaymentAttemptMethod.Card or PaymentAttemptMethod.GCash
        && Status is PaymentAttemptStatus.Created
            or PaymentAttemptStatus.Pending
            or PaymentAttemptStatus.RequiresCustomerAction
            or PaymentAttemptStatus.Processing;

    public void AttachProviderSession(
        string providerReference,
        string? checkoutUrl,
        string? deepLink,
        string? qrPayload,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureMutable();
        if (Method is PaymentAttemptMethod.ManualGCashTransfer)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptStatusTransition,
                "Manual GCash transfer attempts do not use provider checkout sessions.");
        }

        ProviderReference = NormalizeProviderReference(providerReference);
        CheckoutUrl = NormalizeOptionalUrl(checkoutUrl);
        DeepLink = NormalizeOptionalUrl(deepLink);
        QrPayload = NormalizeOptionalText(qrPayload, QrPayloadMaxLength, DomainErrorCodes.InvalidPaymentAttemptQr);
        Status = PaymentAttemptStatus.RequiresCustomerAction;
        UpdatedAtUtc = utcNow;
    }

    public void MarkProcessing(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is PaymentAttemptStatus.Paid or PaymentAttemptStatus.Failed
            or PaymentAttemptStatus.Cancelled or PaymentAttemptStatus.Expired
            or PaymentAttemptStatus.Refunded)
        {
            return;
        }

        Status = PaymentAttemptStatus.Processing;
        UpdatedAtUtc = utcNow;
    }

    public void MarkPaidFromProvider(
        long eventSequence,
        DateTimeOffset utcNow,
        string? cardBrand = null,
        string? cardLastFour = null)
    {
        EnsureUtc(utcNow);
        if (Status == PaymentAttemptStatus.Paid)
        {
            // Idempotent duplicate.
            if (eventSequence <= ProviderEventSequence)
            {
                return;
            }

            ProviderEventSequence = eventSequence;
            UpdatedAtUtc = utcNow;
            return;
        }

        if (IsTerminal)
        {
            // Authoritative provider Paid may override Failed/Cancelled/Expired when sequence
            // is equal or newer (concurrent webhooks often share the same millisecond stamp).
            if (Status is not (PaymentAttemptStatus.Failed
                    or PaymentAttemptStatus.Cancelled
                    or PaymentAttemptStatus.Expired)
                || eventSequence < ProviderEventSequence)
            {
                return;
            }
        }
        else if (eventSequence < ProviderEventSequence)
        {
            return; // out-of-order older event
        }

        Status = PaymentAttemptStatus.Paid;
        CardBrand = NormalizeOptionalText(cardBrand, 32, DomainErrorCodes.InvalidPaymentAttemptMetadata);
        CardLastFour = NormalizeLastFour(cardLastFour);
        FailureCode = null;
        FailureMessage = null;
        CompletedAtUtc = utcNow;
        ProviderEventSequence = eventSequence;
        ProviderFinalizedBySystem = true;
        UpdatedAtUtc = utcNow;
    }

    public void MarkFailedFromProvider(long eventSequence, string? failureCode, string? failureMessage, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == PaymentAttemptStatus.Paid)
        {
            return; // never downgrade Paid
        }

        if (Status == PaymentAttemptStatus.Failed && eventSequence <= ProviderEventSequence)
        {
            return;
        }

        if (eventSequence < ProviderEventSequence)
        {
            return;
        }

        Status = PaymentAttemptStatus.Failed;
        FailureCode = NormalizeOptionalText(failureCode, FailureCodeMaxLength, DomainErrorCodes.InvalidPaymentAttemptFailure);
        FailureMessage = NormalizeOptionalText(failureMessage, FailureMessageMaxLength, DomainErrorCodes.InvalidPaymentAttemptFailure);
        CompletedAtUtc = utcNow;
        ProviderEventSequence = eventSequence;
        ProviderFinalizedBySystem = true;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Marks a locally Created attempt as Failed without a provider event sequence
    /// (definite gateway failure before/without a durable provider session).
    /// </summary>
    public void MarkFailedLocally(string code, string message, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == PaymentAttemptStatus.Paid)
        {
            return;
        }

        if (Status == PaymentAttemptStatus.Failed)
        {
            return;
        }

        if (Status != PaymentAttemptStatus.Created)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptStatusTransition,
                $"Cannot mark locally failed from status {Status}.");
        }

        Status = PaymentAttemptStatus.Failed;
        FailureCode = NormalizeOptionalText(code, FailureCodeMaxLength, DomainErrorCodes.InvalidPaymentAttemptFailure);
        FailureMessage = NormalizeOptionalText(message, FailureMessageMaxLength, DomainErrorCodes.InvalidPaymentAttemptFailure);
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(DateTimeOffset utcNow, string? reason = null)
    {
        EnsureUtc(utcNow);
        if (Status == PaymentAttemptStatus.Paid)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptStatusTransition,
                "A paid attempt cannot be cancelled.");
        }

        if (Status is PaymentAttemptStatus.Cancelled or PaymentAttemptStatus.Expired)
        {
            return;
        }

        Status = PaymentAttemptStatus.Cancelled;
        FailureMessage = NormalizeOptionalText(reason, FailureMessageMaxLength, DomainErrorCodes.InvalidPaymentAttemptFailure);
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void ExpireIfDue(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (IsTerminal || ExpiresAtUtc is null || utcNow < ExpiresAtUtc)
        {
            return;
        }

        Status = PaymentAttemptStatus.Expired;
        FailureCode = "expired";
        FailureMessage = "Payment attempt expired before customer completion.";
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkExpiredFromProvider(long eventSequence, string? failureMessage, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == PaymentAttemptStatus.Paid)
        {
            return;
        }

        if (Status == PaymentAttemptStatus.Expired && eventSequence <= ProviderEventSequence)
        {
            return;
        }

        if (eventSequence < ProviderEventSequence)
        {
            return;
        }

        Status = PaymentAttemptStatus.Expired;
        FailureCode = "expired";
        FailureMessage = NormalizeOptionalText(
            failureMessage ?? "Payment attempt expired.",
            FailureMessageMaxLength,
            DomainErrorCodes.InvalidPaymentAttemptFailure);
        CompletedAtUtc = utcNow;
        ProviderEventSequence = eventSequence;
        ProviderFinalizedBySystem = true;
        UpdatedAtUtc = utcNow;
    }

    public void VerifyManualTransfer(Guid verifierId, string reason, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(verifierId);
        if (Method != PaymentAttemptMethod.ManualGCashTransfer)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptMethod,
                "Only Manual GCash transfer attempts can be manually verified.");
        }

        if (Status == PaymentAttemptStatus.Paid)
        {
            return;
        }

        if (Status != PaymentAttemptStatus.PendingManualVerification)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptStatusTransition,
                $"Cannot verify from status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptVerification,
                "Verification reason is required.");
        }

        VerifiedBy = verifierId;
        VerificationReason = reason.Trim();
        if (VerificationReason.Length > FailureMessageMaxLength)
        {
            VerificationReason = VerificationReason[..FailureMessageMaxLength];
        }

        Status = PaymentAttemptStatus.Paid;
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureMutable()
    {
        if (IsTerminal)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptStatusTransition,
                $"Payment attempt is terminal ({Status}).");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidPaymentAttemptTime, "Timestamps must be UTC.");
        }
    }

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPaymentAttemptActor, "Actor is required.");
        }
    }

    private static void EnsurePositiveAmount(decimal amount)
    {
        if (amount <= 0m || !HasAtMostTwoDecimals(amount))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptAmount,
                "Amount must be a positive monetary value with at most 2 decimals.");
        }
    }

    private static bool HasAtMostTwoDecimals(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero) == value;

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != CurrencyLength)
        {
            throw new DomainException(DomainErrorCodes.InvalidPaymentAttemptCurrency, "Currency must be a 3-letter code.");
        }

        return currency.Trim().ToUpperInvariant();
    }

    private static string NormalizeIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException(DomainErrorCodes.InvalidPaymentAttemptIdempotencyKey, "Idempotency key is required.");
        }

        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptIdempotencyKey,
                $"Idempotency key exceeds {IdempotencyKeyMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeProviderReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidPaymentAttemptProviderReference, "Provider reference is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > ProviderReferenceMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptProviderReference,
                $"Provider reference exceeds {ProviderReferenceMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeExternalReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptExternalReference,
                "External transaction reference is required for Manual GCash transfer.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > ExternalReferenceMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptExternalReference,
                $"External reference exceeds {ExternalReferenceMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalUrl(string? value) =>
        NormalizeOptionalText(value, UrlMaxLength, DomainErrorCodes.InvalidPaymentAttemptUrl);

    private static string? NormalizeLastFour(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Trim().Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? digits[^4..] : digits;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"Value exceeds maximum length of {maxLength}.");
        }

        return trimmed;
    }
}
