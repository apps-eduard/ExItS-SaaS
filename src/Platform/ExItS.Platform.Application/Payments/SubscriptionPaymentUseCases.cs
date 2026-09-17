using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

public sealed record SubscriptionPaymentActivityDto(
    string EventType,
    string Message,
    DateTimeOffset OccurredAtUtc);

public sealed record SubscriptionPaymentTransactionDto(
    Guid Id,
    string ReferenceNumber,
    Guid? OrganizationId,
    Guid? SubscriptionId,
    string PlanKey,
    string BillingCycle,
    decimal BaseAmount,
    decimal DiscountAmount,
    decimal DiscountPercent,
    decimal FinalAmount,
    string CurrencyCode,
    string? Channel,
    string Provider,
    string Environment,
    string Status,
    string? ProviderReference,
    string? CardBrand,
    string? CardLast4,
    string? FailureCode,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ProcessingAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? FailedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc,
    bool SubscriptionActivated,
    IReadOnlyList<SubscriptionPaymentActivityDto> Activities);

public sealed record ProcessSubscriptionPaymentRequest(
    string Channel,
    string? CardNumber = null,
    string? CardExpiry = null,
    string? CardName = null,
    /// <summary>Never persisted — used only for simulator presence check.</summary>
    string? CardCvv = null,
    string? SimulationOutcome = null);

public static class SubscriptionPaymentMapping
{
    public static SubscriptionPaymentTransactionDto ToDto(SubscriptionPaymentTransaction payment) =>
        new(
            payment.Id.Value,
            payment.ReferenceNumber,
            payment.OrganizationId?.Value,
            payment.SubscriptionId?.Value,
            payment.PlanKey,
            payment.BillingCycle.ToString(),
            payment.BaseAmount,
            payment.DiscountAmount,
            payment.DiscountPercent,
            payment.FinalAmount,
            payment.CurrencyCode,
            payment.Channel?.ToString(),
            payment.Provider.ToString(),
            payment.Environment.ToString(),
            payment.Status.ToString(),
            payment.ProviderReference,
            payment.CardBrand,
            payment.CardLast4,
            payment.FailureCode,
            payment.FailureReason,
            payment.CreatedAtUtc,
            payment.ProcessingAtUtc,
            payment.PaidAtUtc,
            payment.FailedAtUtc,
            payment.CancelledAtUtc,
            payment.ExpiredAtUtc,
            payment.PeriodStartUtc,
            payment.PeriodEndUtc,
            payment.SubscriptionActivated,
            payment.Activities
                .OrderBy(a => a.OccurredAtUtc)
                .Select(a => new SubscriptionPaymentActivityDto(a.EventType, a.Message, a.OccurredAtUtc))
                .ToArray());
}

public sealed class CreatePendingSubscriptionPayment(
    ISubscriptionPaymentTransactionRepository payments,
    IPlanRepository plans,
    EnsureMvpPosPlans ensureMvpPosPlans,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        PlatformUserId userId,
        string planKey,
        BillingCycle billingCycle,
        PlatformOrganizationId? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        await ensureMvpPosPlans.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        var plan = await plans
            .GetByProductAndCodeAsync(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                PlanCode.Create(planKey),
                cancellationToken)
            .ConfigureAwait(false);

        if (plan is null || !plan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found or is not available.");
        }

        var quote = SubscriptionBillingPricing.Quote(plan, billingCycle);
        var utcNow = clock.UtcNow;
        var sequence = await payments.GetNextSequenceAsync(cancellationToken).ConfigureAwait(false);
        var reference = SubscriptionPaymentReferences.FormatInternalReference(utcNow, sequence);

        try
        {
            var payment = SubscriptionPaymentTransaction.CreatePending(
                reference,
                userId,
                plan.PlanKey,
                billingCycle,
                quote,
                utcNow,
                organizationId);
            await payments.AddAsync(payment, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class GetSubscriptionPaymentTransaction(
    ISubscriptionPaymentTransactionRepository payments)
{
    public async Task<ApplicationResult<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        Guid paymentId,
        PlatformUserId? requesterUserId = null,
        CancellationToken cancellationToken = default)
    {
        var payment = await payments
            .GetByIdAsync(SubscriptionPaymentTransactionId.From(paymentId), cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        if (requesterUserId is not null
            && payment.InitiatedByUserId.Value != requesterUserId.Value)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to the current user.");
        }

        return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
            SubscriptionPaymentMapping.ToDto(payment));
    }
}

public sealed class ProcessSubscriptionPaymentSimulator(
    ISubscriptionPaymentTransactionRepository payments,
    IPlanRepository plans,
    ActivatePaidSubscription activatePaid,
    GenerateEntitlementSnapshot generateSnapshot,
    RecordLinkedSuccessfulProviderPayment recordLinkedPayment,
    GrantProductAccess grantProductAccess,
    IProductLocalRoleGrantRepository roleGrants,
    IAuditWriter auditWriter,
    IPaymentProvider paymentProvider,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        Guid paymentId,
        PlatformUserId requesterUserId,
        ProcessSubscriptionPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!SubscriptionPaymentChannels.TryParse(request.Channel, out var channel))
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.InvalidBillingCycle,
                "Channel must be GCash, Maya, or Card.");
        }

        var payment = await payments
            .GetByIdAsync(SubscriptionPaymentTransactionId.From(paymentId), cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        if (payment.InitiatedByUserId.Value != requesterUserId.Value)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to the current user.");
        }

        if (payment.Status == SubscriptionPaymentStatus.Paid && payment.SubscriptionActivated)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }

        // Pre-org checkout: money can settle with OrganizationId null; activation waits for AttachOrganization.
        var utcNow = clock.UtcNow;
        var providerReference = payment.ProviderReference
            ?? SubscriptionPaymentReferences.FormatProviderReference(channel, utcNow);

        string? cardBrand = null;
        string? cardLast4 = null;
        var leaveProcessing = false;
        string? failureCode = null;
        string? failureReason = null;
        var markPaid = true;

        if (channel == SubscriptionPaymentChannel.Card)
        {
            // CVV is accepted only to prove presence; never read into persistence.
            _ = request.CardCvv;
            cardBrand = SubscriptionCardSimulator.DetectBrand(request.CardNumber);
            cardLast4 = SubscriptionCardSimulator.Last4(request.CardNumber);
            var eval = SubscriptionCardSimulator.Evaluate(request.CardNumber);
            markPaid = eval.Success;
            leaveProcessing = eval.LeaveProcessing;
            failureCode = eval.FailureCode;
            failureReason = eval.FailureReason;
        }
        else
        {
            var outcome = (request.SimulationOutcome ?? "succeed").Trim().ToLowerInvariant();
            if (outcome is "fail" or "failed" or "decline" or "declined")
            {
                markPaid = false;
                failureCode = "simulated_decline";
                failureReason = $"Simulated {channel} decline";
            }
            else if (outcome is "pending" or "processing")
            {
                markPaid = false;
                leaveProcessing = true;
            }
        }

        try
        {
            payment.BeginProcessing(channel, providerReference, utcNow, cardBrand, cardLast4);

            if (leaveProcessing)
            {
                await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                    SubscriptionPaymentMapping.ToDto(payment));
            }

            if (!markPaid)
            {
                payment.MarkFailed(failureCode ?? "failed", failureReason ?? "Payment failed", utcNow);
                await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                    SubscriptionPaymentMapping.ToDto(payment));
            }

            var (periodStart, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(
                utcNow,
                payment.BillingCycle);
            payment.MarkPaid(utcNow, periodStart, periodEnd);

            if (payment.OrganizationId is null)
            {
                // Pre-organization checkout: Paid settles money only; Start Business attaches + activates.
                await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                    SubscriptionPaymentMapping.ToDto(payment));
            }

            if (!payment.SubscriptionActivated)
            {
                var activated = await SubscriptionPaymentActivation
                    .ActivateForAttachedOrganizationAsync(
                        payment,
                        requesterUserId,
                        plans,
                        activatePaid,
                        generateSnapshot,
                        recordLinkedPayment,
                        grantProductAccess,
                        roleGrants,
                        auditWriter,
                        paymentProvider,
                        utcNow,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!activated.IsSuccess)
                {
                    return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                        activated.ErrorCode ?? ApplicationErrorCodes.SubscriptionIneligible,
                        activated.ErrorMessage ?? "Subscription activation failed.");
                }

                await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            // Idempotent read-after-write for concurrent Paid activation only.
            var latest = await payments
                .GetByIdAsync(SubscriptionPaymentTransactionId.From(paymentId), cancellationToken)
                .ConfigureAwait(false);
            if (latest is not null
                && latest.Status == SubscriptionPaymentStatus.Paid
                && latest.SubscriptionActivated)
            {
                return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                    SubscriptionPaymentMapping.ToDto(latest));
            }

            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ex.ErrorCode,
                ex.Message);
        }
    }
}

/// <summary>
/// Shared Paid → SaaS subscription activation for org-attached payments (idempotent).
/// </summary>
internal static class SubscriptionPaymentActivation
{
    public static async Task<ApplicationResult> ActivateForAttachedOrganizationAsync(
        SubscriptionPaymentTransaction payment,
        PlatformUserId requesterUserId,
        IPlanRepository plans,
        ActivatePaidSubscription activatePaid,
        GenerateEntitlementSnapshot generateSnapshot,
        RecordLinkedSuccessfulProviderPayment recordLinkedPayment,
        GrantProductAccess grantProductAccess,
        IProductLocalRoleGrantRepository roleGrants,
        IAuditWriter auditWriter,
        IPaymentProvider paymentProvider,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (payment.OrganizationId is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.SubscriptionIneligible,
                "Organization must be attached before activating subscription.");
        }

        if (payment.Status != SubscriptionPaymentStatus.Paid)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                "Payment must be Paid before activation.");
        }

        if (payment.SubscriptionActivated)
        {
            return ApplicationResult.Success();
        }

        var plan = await plans
            .GetByProductAndCodeAsync(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                PlanCode.Create(payment.PlanKey),
                cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found for activation.");
        }

        var published = await plans
            .GetLatestPublishedVersionAsync(plan.Id, cancellationToken)
            .ConfigureAwait(false);
        if (published is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                "Published plan version was not found.");
        }

        var periodStart = payment.PeriodStartUtc ?? utcNow;
        var periodEnd = payment.PeriodEndUtc
            ?? SubscriptionBillingPeriods.ComputePaidPeriod(utcNow, payment.BillingCycle).End;

        var activated = await activatePaid
            .ExecuteAsync(
                payment.OrganizationId,
                plan.Id,
                published.Id,
                periodStart,
                periodEnd,
                payment.BillingCycle,
                cancellationToken)
            .ConfigureAwait(false);
        if (!activated.IsSuccess || activated.Value is null)
        {
            return ApplicationResult.Failure(
                activated.ErrorCode ?? ApplicationErrorCodes.SubscriptionIneligible,
                activated.ErrorMessage ?? "Subscription activation failed.");
        }

        payment.MarkSubscriptionActivated(activated.Value.Id, utcNow);

        var idempotencyKey = $"subscription-checkout-{payment.Id.Value:N}";
        try
        {
            var charge = await paymentProvider.ChargeAsync(
                new PaymentChargeRequest(
                    payment.OrganizationId.Value,
                    activated.Value.Id.Value,
                    payment.FinalAmount,
                    payment.CurrencyCode,
                    idempotencyKey,
                    Purpose: "subscription-checkout",
                    PlanKey: payment.PlanKey,
                    BillingCycle: payment.BillingCycle.ToString(),
                    BaseAmount: payment.BaseAmount,
                    DiscountAmount: payment.DiscountAmount,
                    DiscountPercent: payment.DiscountPercent),
                cancellationToken).ConfigureAwait(false);

            if (charge.Status == PaymentProviderResultStatus.Succeeded)
            {
                await recordLinkedPayment
                    .ExecuteAsync(
                        payment.OrganizationId,
                        ProductCode.Create(ProductCode.PinoyBusinessPos),
                        activated.Value.Id,
                        charge,
                        "subscription-checkout",
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (NotSupportedException)
        {
            // LocalValidation may be the only provider; still keep subscription activation.
        }

        await generateSnapshot
            .ExecuteAsync(
                payment.OrganizationId,
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await grantProductAccess
            .ExecuteAsync(
                payment.OrganizationId,
                requesterUserId,
                ProductCode.PinoyBusinessPos,
                $"platform-user:{requesterUserId.Value:D}",
                "Subscription checkout Paid — product access grant.",
                cancellationToken,
                ensureExisting: true)
            .ConfigureAwait(false);

        var existingRole = await roleGrants
            .FindActiveByUserOrganizationProductAsync(
                payment.OrganizationId,
                requesterUserId,
                ProductCode.PinoyBusinessPos,
                cancellationToken)
            .ConfigureAwait(false);
        if (existingRole is null)
        {
            var grant = ProductLocalRoleGrant.Create(
                payment.OrganizationId,
                requesterUserId,
                ProductCode.PinoyBusinessPos,
                ProductLocalRoleGrant.PosOwnerRoleCode,
                requesterUserId,
                utcNow);
            await roleGrants.AddAsync(grant, cancellationToken).ConfigureAwait(false);
            await auditWriter.WriteAsync(
                $"platform-user:{requesterUserId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.ProductLocalRoleGranted,
                nameof(ProductLocalRoleGrant),
                grant.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId: payment.OrganizationId,
                productCode: ProductCode.Create(ProductCode.PinoyBusinessPos),
                summary: "POS Owner granted after subscription checkout Paid.",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult.Success();
    }
}

public sealed class ListSubscriptionPaymentTransactions(
    ISubscriptionPaymentTransactionRepository payments)
{
    public async Task<IReadOnlyList<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var items = await payments.ListRecentAsync(Math.Clamp(take, 1, 500), cancellationToken)
            .ConfigureAwait(false);
        return items.Select(SubscriptionPaymentMapping.ToDto).ToArray();
    }
}

public sealed record CreatePersonalSubscriptionPaymentRequest(
    string PlanKey,
    string BillingCycle);

public sealed class SelectSubscriptionPaymentChannel(
    ISubscriptionPaymentTransactionRepository payments,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        Guid paymentId,
        PlatformUserId requesterUserId,
        string channelRaw,
        CancellationToken cancellationToken = default)
    {
        if (!SubscriptionPaymentChannels.TryParse(channelRaw, out var channel))
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.InvalidBillingCycle,
                "Channel must be GCash, Maya, or Card.");
        }

        var payment = await payments
            .GetByIdAsync(SubscriptionPaymentTransactionId.From(paymentId), cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        if (payment.InitiatedByUserId.Value != requesterUserId.Value)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to the current user.");
        }

        try
        {
            payment.SelectChannel(channel, clock.UtcNow);
            await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Creates a NEW pending payment attempt from a Failed/Cancelled/Expired transaction.
/// Never mutates the prior attempt back to Pending.
/// </summary>
public sealed class RetrySubscriptionPayment(
    ISubscriptionPaymentTransactionRepository payments,
    CreatePendingSubscriptionPayment createPending)
{
    public async Task<ApplicationResult<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        Guid sourcePaymentId,
        PlatformUserId requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var source = await payments
            .GetByIdAsync(SubscriptionPaymentTransactionId.From(sourcePaymentId), cancellationToken)
            .ConfigureAwait(false);
        if (source is null)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        if (source.InitiatedByUserId.Value != requesterUserId.Value)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to the current user.");
        }

        if (source.Status is not (
            SubscriptionPaymentStatus.Failed
            or SubscriptionPaymentStatus.Cancelled
            or SubscriptionPaymentStatus.Expired))
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.InvalidPaymentStatusTransition,
                "Only Failed, Cancelled, or Expired payments can be retried.");
        }

        // New attempt stays pre-org even if the prior row had a provisional OrganizationId.
        return await createPending
            .ExecuteAsync(
                requesterUserId,
                source.PlanKey,
                source.BillingCycle,
                organizationId: null,
                cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// After Start Business creates an organization, attach a Paid pre-org payment and activate once.
/// </summary>
public sealed class AttachAndActivatePaidSubscriptionPayment(
    ISubscriptionPaymentTransactionRepository payments,
    IPlanRepository plans,
    ActivatePaidSubscription activatePaid,
    GenerateEntitlementSnapshot generateSnapshot,
    RecordLinkedSuccessfulProviderPayment recordLinkedPayment,
    GrantProductAccess grantProductAccess,
    IProductLocalRoleGrantRepository roleGrants,
    IAuditWriter auditWriter,
    IPaymentProvider paymentProvider,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        Guid paymentId,
        PlatformUserId requesterUserId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var payment = await payments
            .GetByIdAsync(SubscriptionPaymentTransactionId.From(paymentId), cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        if (payment.InitiatedByUserId.Value != requesterUserId.Value)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to the current user.");
        }

        if (payment.Status != SubscriptionPaymentStatus.Paid)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                "Payment must be Paid before organization attach.");
        }

        if (payment.OrganizationId is not null
            && payment.OrganizationId.Value != organizationId.Value)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment is already attached to a different organization.");
        }

        try
        {
            payment.AttachOrganization(organizationId);
            var utcNow = clock.UtcNow;
            var activated = await SubscriptionPaymentActivation
                .ActivateForAttachedOrganizationAsync(
                    payment,
                    requesterUserId,
                    plans,
                    activatePaid,
                    generateSnapshot,
                    recordLinkedPayment,
                    grantProductAccess,
                    roleGrants,
                    auditWriter,
                    paymentProvider,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!activated.IsSuccess)
            {
                return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                    activated.ErrorCode ?? ApplicationErrorCodes.SubscriptionIneligible,
                    activated.ErrorMessage ?? "Subscription activation failed.");
            }

            await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

