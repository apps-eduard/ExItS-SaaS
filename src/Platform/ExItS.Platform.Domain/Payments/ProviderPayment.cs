using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Domain.Payments;

/// <summary>
/// Persisted provider payment attempt (gateway or local-validation simulation).
/// Distinct from manual <see cref="SaaSPayment"/> staff-recorded references.
/// Pricing snapshot columns preserve historical charged amounts if catalog prices change later.
/// </summary>
public sealed class ProviderPayment
{
    public ProviderPaymentId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public SubscriptionId SubscriptionId { get; }
    public decimal Amount { get; }
    public string CurrencyCode { get; }
    public string Provider { get; }
    public string ProviderReference { get; }
    public PaymentProviderResultStatus Status { get; }
    public bool IsTest { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public string IdempotencyKey { get; }
    public string? Purpose { get; }
    public string? PlanKey { get; }
    public string? BillingCycle { get; }
    public decimal? BaseAmount { get; }
    public decimal? DiscountAmount { get; }
    public decimal? DiscountPercent { get; }
    public decimal? FinalAmount { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private ProviderPayment(
        ProviderPaymentId id,
        PlatformOrganizationId organizationId,
        SubscriptionId subscriptionId,
        decimal amount,
        string currencyCode,
        string provider,
        string providerReference,
        PaymentProviderResultStatus status,
        bool isTest,
        string? failureCode,
        string? failureMessage,
        string idempotencyKey,
        string? purpose,
        string? planKey,
        string? billingCycle,
        decimal? baseAmount,
        decimal? discountAmount,
        decimal? discountPercent,
        decimal? finalAmount,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        SubscriptionId = subscriptionId;
        Amount = amount;
        CurrencyCode = currencyCode;
        Provider = provider;
        ProviderReference = providerReference;
        Status = status;
        IsTest = isTest;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        IdempotencyKey = idempotencyKey;
        Purpose = purpose;
        PlanKey = planKey;
        BillingCycle = billingCycle;
        BaseAmount = baseAmount;
        DiscountAmount = discountAmount;
        DiscountPercent = discountPercent;
        FinalAmount = finalAmount;
        CreatedAtUtc = createdAtUtc;
    }

    public static ProviderPayment FromResult(
        PlatformOrganizationId organizationId,
        SubscriptionId subscriptionId,
        PaymentProviderResult result,
        string? purpose,
        DateTimeOffset utcNow,
        ProviderPaymentId? id = null,
        PaymentChargeRequest? charge = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(subscriptionId);
        ArgumentNullException.ThrowIfNull(result);
        DomainTime.EnsureUtc(utcNow);

        if (string.IsNullOrWhiteSpace(result.IdempotencyKey))
        {
            throw new DomainException(DomainErrorCodes.PaymentReferenceRequired, "IdempotencyKey is required.");
        }

        return new ProviderPayment(
            id ?? ProviderPaymentId.New(),
            organizationId,
            subscriptionId,
            result.Amount,
            result.CurrencyCode,
            result.Provider,
            result.ProviderReference,
            result.Status,
            result.IsTest,
            result.FailureCode,
            result.FailureMessage,
            result.IdempotencyKey.Trim(),
            purpose?.Trim(),
            charge?.PlanKey?.Trim(),
            charge?.BillingCycle?.Trim(),
            charge?.BaseAmount,
            charge?.DiscountAmount,
            charge?.DiscountPercent,
            charge is null ? result.Amount : charge.Amount,
            utcNow);
    }

    internal static ProviderPayment Rehydrate(
        ProviderPaymentId id,
        PlatformOrganizationId organizationId,
        SubscriptionId subscriptionId,
        decimal amount,
        string currencyCode,
        string provider,
        string providerReference,
        PaymentProviderResultStatus status,
        bool isTest,
        string? failureCode,
        string? failureMessage,
        string idempotencyKey,
        string? purpose,
        DateTimeOffset createdAtUtc,
        string? planKey = null,
        string? billingCycle = null,
        decimal? baseAmount = null,
        decimal? discountAmount = null,
        decimal? discountPercent = null,
        decimal? finalAmount = null) =>
        new(
            id,
            organizationId,
            subscriptionId,
            amount,
            currencyCode,
            provider,
            providerReference,
            status,
            isTest,
            failureCode,
            failureMessage,
            idempotencyKey,
            purpose,
            planKey,
            billingCycle,
            baseAmount,
            discountAmount,
            discountPercent,
            finalAmount,
            createdAtUtc);
}
