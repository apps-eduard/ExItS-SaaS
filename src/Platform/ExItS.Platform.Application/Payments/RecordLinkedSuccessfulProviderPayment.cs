using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

/// <summary>
/// Persists a successful provider charge as a confirmed, subscription-linked SaaS payment so it
/// appears in Platform Administration → Payments. Idempotent on organization + Online method + reference.
/// </summary>
public sealed class RecordLinkedSuccessfulProviderPayment
{
    private readonly ISaaSPaymentRepository _payments;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RecordLinkedSuccessfulProviderPayment(
        ISaaSPaymentRepository payments,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _payments = payments;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaaSPayment>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        SubscriptionId subscriptionId,
        PaymentProviderResult providerResult,
        string purpose = "provider-charge",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(subscriptionId);
        ArgumentNullException.ThrowIfNull(providerResult);

        if (providerResult.Status is not PaymentProviderResultStatus.Succeeded
            and not PaymentProviderResultStatus.RenewalSucceeded)
        {
            return ApplicationResult<SaaSPayment>.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                "Only successful provider charges can be recorded as linked SaaS payments.");
        }

        if (string.IsNullOrWhiteSpace(providerResult.ProviderReference))
        {
            return ApplicationResult<SaaSPayment>.Failure(
                ApplicationErrorCodes.PaymentReferenceConflict,
                "Successful provider charge is missing a provider reference.");
        }

        try
        {
            var normalized = SaaSPayment.NormalizeReference(providerResult.ProviderReference);
            var existing = await _payments
                .GetByNormalizedReferenceAsync(
                    SaaSPaymentMethod.Online,
                    normalized,
                    organizationId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                if (existing.Status == SaaSPaymentStatus.Confirmed
                    && existing.SubscriptionId == subscriptionId)
                {
                    return ApplicationResult<SaaSPayment>.Success(existing);
                }

                return ApplicationResult<SaaSPayment>.Failure(
                    ApplicationErrorCodes.PaymentReferenceConflict,
                    "A payment with this provider reference already exists for this organization.");
            }

            var utcNow = _clock.UtcNow;
            var confirmedBy = string.IsNullOrWhiteSpace(providerResult.Provider)
                ? "payment-provider"
                : $"provider:{providerResult.Provider.Trim()}";

            var payment = SaaSPayment.CreateConfirmedLinkedFromProvider(
                organizationId,
                productCode,
                subscriptionId,
                providerResult.Amount,
                CurrencyCode.Create(providerResult.CurrencyCode),
                providerResult.ProviderReference,
                confirmedBy,
                paidAtUtc: utcNow,
                utcNow);

            await _payments.AddAsync(payment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                confirmedBy,
                AuditActorType.System,
                PlatformAuditActions.ProviderPaymentLinked,
                nameof(SaaSPayment),
                payment.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId,
                productCode,
                summary: $"Linked successful {purpose} provider payment {providerResult.ProviderReference} to subscription {subscriptionId.Value:D}.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

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
