using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

/// <summary>Combined result of confirming a manual payment and activating the subscription it funds.</summary>
public sealed record ConfirmedPaymentActivation(SaaSPayment Payment, Subscription Subscription);

public sealed class CreateManualSaaSPayment
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IProductRepository _products;
    private readonly ISaaSPaymentRepository _payments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateManualSaaSPayment(
        IPlatformOrganizationRepository organizations,
        IProductRepository products,
        ISaaSPaymentRepository payments,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _products = products;
        _payments = payments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaaSPayment>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        decimal amount,
        CurrencyCode currencyCode,
        SaaSPaymentMethod method,
        string externalReference,
        DateTimeOffset paidAtUtc,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<SaaSPayment>.Failure(
                ApplicationErrorCodes.OrganizationNotEligible,
                "Payments can only be recorded for an active Platform Organization.");
        }

        var product = await _products.GetByCodeAsync(productCode, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(ApplicationErrorCodes.ProductNotFound, "Product was not found.");
        }

        try
        {
            var normalizedReference = SaaSPayment.NormalizeReference(externalReference);
            var duplicate = await _payments
                .ExistsByNormalizedReferenceAsync(method, normalizedReference, organizationId, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate)
            {
                return ApplicationResult<SaaSPayment>.Failure(
                    ApplicationErrorCodes.PaymentReferenceConflict,
                    "A payment with this reference already exists for this organization and method.");
            }

            var payment = SaaSPayment.CreateManual(
                organizationId, productCode, amount, currencyCode, method, externalReference, paidAtUtc, _clock.UtcNow);
            await _payments.AddAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SaaSPayment>.Success(payment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ConfirmSaaSPayment
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmSaaSPayment(ISaaSPaymentRepository payments, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaaSPayment>> ExecuteAsync(
        SaaSPaymentId paymentId,
        string confirmedBy,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(ApplicationErrorCodes.PaymentNotFound, "Payment was not found.");
        }

        try
        {
            payment.Confirm(confirmedBy, _clock.UtcNow);
            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SaaSPayment>.Success(payment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RejectSaaSPayment
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RejectSaaSPayment(ISaaSPaymentRepository payments, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaaSPayment>> ExecuteAsync(
        SaaSPaymentId paymentId,
        string rejectedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(ApplicationErrorCodes.PaymentNotFound, "Payment was not found.");
        }

        try
        {
            payment.Reject(rejectedBy, reason, _clock.UtcNow);
            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SaaSPayment>.Success(payment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class VoidSaaSPayment
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public VoidSaaSPayment(ISaaSPaymentRepository payments, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaaSPayment>> ExecuteAsync(
        SaaSPaymentId paymentId,
        string voidedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SaaSPayment>.Failure(ApplicationErrorCodes.PaymentNotFound, "Payment was not found.");
        }

        try
        {
            payment.Void(voidedBy, reason, _clock.UtcNow);
            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SaaSPayment>.Success(payment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SaaSPayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Confirms a manually reported payment (if not already confirmed) and atomically activates — or
/// reactivates — the subscription it funds, then links the payment to that subscription so it
/// cannot be reused. Delegates all subscription lifecycle rules to <see cref="Subscription"/>;
/// does not duplicate lifecycle logic here.
/// </summary>
public sealed class ConfirmPaymentAndActivateSubscription
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmPaymentAndActivateSubscription(
        ISaaSPaymentRepository payments,
        ISubscriptionRepository subscriptions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _payments = payments;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ConfirmedPaymentActivation>> ExecuteAsync(
        SaaSPaymentId paymentId,
        string confirmedBy,
        SubscriptionId subscriptionId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        if (payment.OrganizationId != subscription.OrganizationId)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentOrganizationMismatch,
                "Payment and subscription belong to different organizations.");
        }

        if (payment.ProductCode != subscription.ProductCode)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentProductMismatch,
                "Payment and subscription are for different products.");
        }

        if (payment.Status is SaaSPaymentStatus.Rejected or SaaSPaymentStatus.Voided)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                "Payment cannot be confirmed because it is in a terminal state.");
        }

        if (payment.SubscriptionId is not null)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(
                ApplicationErrorCodes.PaymentAlreadyUsed,
                "Payment has already been used to activate a subscription.");
        }

        try
        {
            var utcNow = _clock.UtcNow;
            if (payment.Status != SaaSPaymentStatus.Confirmed)
            {
                payment.Confirm(confirmedBy, utcNow);
            }

            if (subscription.Status == SubscriptionStatus.Trialing)
            {
                subscription.ActivateFromTrial(periodStartUtc, periodEndUtc, utcNow);
            }
            else
            {
                subscription.Reactivate(utcNow, periodStartUtc, periodEndUtc);
            }

            payment.LinkSubscription(subscription.Id, utcNow);

            await _payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await _subscriptions.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ApplicationResult<ConfirmedPaymentActivation>.Success(
                new ConfirmedPaymentActivation(payment, subscription));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ConfirmedPaymentActivation>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
