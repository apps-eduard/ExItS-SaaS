using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

public sealed class PayMongoOptions
{
    public const string SectionName = "PayMongo";

    /// <summary>Backend-only test or live secret. Never return this to clients.</summary>
    public string? SecretKey { get; set; }

    /// <summary>Webhook signing secret from the PayMongo webhook endpoint. Backend-only.</summary>
    public string? WebhookSecret { get; set; }

    /// <summary>Public browser origin used for PayMongo return URLs, such as http://127.0.0.1:5177.</summary>
    public string? PublicAppBaseUrl { get; set; }
}

public sealed record HostedCheckoutCreateRequest(
    Guid PaymentId,
    string ReferenceNumber,
    string PlanName,
    string BillingPeriod,
    decimal Amount,
    string CurrencyCode,
    string SuccessUrl,
    string CancelUrl,
    Guid? OrganizationId);

public sealed record HostedCheckoutSessionResult(
    string SessionId,
    string CheckoutUrl,
    bool IsTest);

public sealed record HostedCheckoutProviderState(
    string SessionId,
    bool IsPaid,
    bool IsFailed,
    bool IsExpired,
    decimal? PaidAmount,
    string? CurrencyCode,
    string? ProviderPaymentId);

public interface ISubscriptionCheckoutGateway
{
    bool IsConfigured { get; }

    Task<HostedCheckoutSessionResult> CreateSessionAsync(
        HostedCheckoutCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<HostedCheckoutProviderState> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

public sealed record HostedSubscriptionCheckoutDto(
    Guid PaymentId,
    string CheckoutUrl,
    string Status,
    decimal Amount,
    string CurrencyCode,
    string PlanKey,
    string BillingCycle,
    Guid? OrganizationId);

public interface IHostedCheckoutSubscriptionActivator
{
    Task<ApplicationResult> ActivateAsync(
        SubscriptionPaymentTransaction payment,
        CancellationToken cancellationToken = default);
}

public static class SubscriptionCheckoutReturnUrls
{
    public static bool TryResolveBase(string? configured, string? origin, bool allowLoopbackOrigin, out string baseUrl)
    {
        if (TryNormalize(configured, allowLoopback: true, requireHttps: !allowLoopbackOrigin, out baseUrl))
        {
            return true;
        }

        if (allowLoopbackOrigin && TryNormalize(origin, allowLoopback: true, requireHttps: false, out baseUrl) && IsLoopback(baseUrl))
        {
            return true;
        }

        baseUrl = "";
        return false;
    }

    public static string Success(string baseUrl, Guid paymentId, Guid? organizationId) =>
        WithPayment(baseUrl, "/billing/payment/success", paymentId, organizationId);

    public static string Cancelled(string baseUrl, Guid paymentId, Guid? organizationId) =>
        WithPayment(baseUrl, "/billing/payment/cancelled", paymentId, organizationId);

    private static string WithPayment(string baseUrl, string path, Guid paymentId, Guid? organizationId)
    {
        var url = $"{baseUrl.TrimEnd('/')}{path}?paymentId={paymentId:D}";
        return organizationId is Guid org && org != Guid.Empty
            ? $"{url}&organizationId={org:D}"
            : url;
    }

    private static bool TryNormalize(string? value, bool allowLoopback, bool requireHttps, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && !(allowLoopback && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp))
        {
            return false;
        }

        if (requireHttps && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return true;
    }

    private static bool IsLoopback(string baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;
}

internal static class SubscriptionCheckoutAccess
{
    public static bool OrganizationContextMatches(
        SubscriptionPaymentTransaction payment,
        Guid? expectedOrganizationId)
    {
        if (expectedOrganizationId is Guid orgId)
        {
            return payment.OrganizationId?.Value == orgId;
        }

        return payment.OrganizationId is null;
    }
}

public sealed class StartHostedSubscriptionCheckout(
    ISubscriptionPaymentTransactionRepository payments,
    IPlanRepository plans,
    ISubscriptionCheckoutGateway gateway,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    OrganizationProductCheckoutGuard? checkoutAffiliation = null)
{
    public async Task<ApplicationResult<HostedSubscriptionCheckoutDto>> ExecuteForPaymentAsync(
        Guid paymentId,
        PlatformUserId userId,
        Guid? expectedOrganizationId,
        string returnBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var existing = await payments
            .GetByIdAsync(SubscriptionPaymentTransactionId.From(paymentId), cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        return await ExecuteUnderOpenCheckoutLockAsync(
            existing.InitiatedByUserId.Value,
            existing.OrganizationId?.Value ?? Guid.Empty,
            ct => ExecuteExistingAsync(paymentId, userId, expectedOrganizationId, returnBaseUrl, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApplicationResult<HostedSubscriptionCheckoutDto>> ExecuteForPlanAsync(
        PlatformUserId userId,
        string planId,
        BillingCycle billingCycle,
        PlatformOrganizationId? organizationId,
        string returnBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (userId.Value == Guid.Empty)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Authentication is required.");
        }

        Plan? plan;
        try
        {
            plan = await ResolvePlanAsync(planId, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(ex.ErrorCode, ex.Message);
        }

        if (plan is null || !plan.AcceptsNewSubscriptions)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                "Plan was not found or is not available.");
        }

        SubscriptionPriceQuote quote;
        try
        {
            quote = SubscriptionBillingPricing.Quote(plan, billingCycle);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(ex.ErrorCode, ex.Message);
        }

        return await ExecuteUnderOpenCheckoutLockAsync(
            userId.Value,
            organizationId?.Value ?? Guid.Empty,
            ct => StartOpenCheckoutAsync(userId, plan, billingCycle, quote, organizationId, returnBaseUrl, ct),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes find-or-create and the provider call for one subscriber and organization.
    /// The PostgreSQL advisory lock is held until the authoritative checkout URL is stored.
    /// </summary>
    private async Task<ApplicationResult<HostedSubscriptionCheckoutDto>> ExecuteUnderOpenCheckoutLockAsync(
        Guid lockKeyA,
        Guid lockKeyB,
        Func<CancellationToken, Task<ApplicationResult<HostedSubscriptionCheckoutDto>>> action,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                ApplicationResult<HostedSubscriptionCheckoutDto>? outcome = null;
                await unitOfWork.ExecuteWithAdvisoryLockAsync(
                    lockKeyA,
                    lockKeyB,
                    async ct =>
                    {
                        outcome = await action(ct).ConfigureAwait(false);
                    },
                    cancellationToken).ConfigureAwait(false);
                return outcome ?? ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                    ApplicationErrorCodes.ConcurrencyConflict,
                    "Checkout could not be started. Try again.");
            }
            catch (PersistenceConflictException) when (attempt == 0)
            {
            }
        }

        return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
            ApplicationErrorCodes.ConcurrencyConflict,
            "An open subscription checkout already exists for this plan.");
    }

    private async Task<ApplicationResult<HostedSubscriptionCheckoutDto>> StartOpenCheckoutAsync(
        PlatformUserId userId,
        Plan plan,
        BillingCycle billingCycle,
        SubscriptionPriceQuote quote,
        PlatformOrganizationId? organizationId,
        string returnBaseUrl,
        CancellationToken cancellationToken)
    {
        var checkoutOrganizationId = organizationId;
        if (checkoutAffiliation is not null)
        {
            var decision = await checkoutAffiliation
                .ResolveAsync(userId, plan.ProductCode, organizationId, cancellationToken)
                .ConfigureAwait(false);
            if (decision.IsBlocked)
            {
                return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                    decision.ErrorCode!,
                    decision.ErrorMessage!);
            }

            checkoutOrganizationId = decision.OrganizationId ?? organizationId;
        }

        var open = await payments
            .FindLatestOpenAsync(userId, checkoutOrganizationId, plan.PlanKey, billingCycle, cancellationToken)
            .ConfigureAwait(false);
        if (open is not null)
        {
            return await ExecuteExistingAsync(
                open.Id.Value,
                userId,
                checkoutOrganizationId?.Value,
                returnBaseUrl,
                cancellationToken).ConfigureAwait(false);
        }

        var utcNow = clock.UtcNow;
        var sequence = await payments.GetNextSequenceAsync(cancellationToken).ConfigureAwait(false);
        var reference = SubscriptionPaymentReferences.FormatInternalReference(utcNow, sequence);
        SubscriptionPaymentTransaction created;
        try
        {
            created = SubscriptionPaymentTransaction.CreatePending(
                reference,
                userId,
                plan.PlanKey,
                billingCycle,
                quote,
                utcNow,
                checkoutOrganizationId);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await payments.AddAsync(created, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteExistingAsync(
            created.Id.Value,
            userId,
            checkoutOrganizationId?.Value,
            returnBaseUrl,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApplicationResult<HostedSubscriptionCheckoutDto>> ExecuteExistingAsync(
        Guid paymentId,
        PlatformUserId userId,
        Guid? expectedOrganizationId,
        string returnBaseUrl,
        CancellationToken cancellationToken)
    {
        if (userId.Value == Guid.Empty)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Authentication is required.");
        }

        if (string.IsNullOrWhiteSpace(returnBaseUrl))
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.PaymentNotConfigured,
                "Subscription return address is not configured.");
        }

        var payment = await payments
            .GetByIdAsync(SubscriptionPaymentTransactionId.From(paymentId), cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        if (payment.InitiatedByUserId.Value != userId.Value)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to the current user.");
        }

        if (expectedOrganizationId is Guid orgId)
        {
            if (payment.OrganizationId?.Value != orgId)
            {
                return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                    DomainErrorCodes.AuthorizationDenied,
                    "Payment does not belong to this organization.");
            }
        }
        else if (payment.OrganizationId is not null)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Organization context is required for this payment.");
        }

        if (payment.Status == SubscriptionPaymentStatus.Paid)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.PaymentAlreadyConfirmed,
                "This subscription payment is already paid.");
        }

        if (payment.Status is SubscriptionPaymentStatus.Failed
            or SubscriptionPaymentStatus.Cancelled
            or SubscriptionPaymentStatus.Expired)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.PaymentInvalidTransition,
                "This payment can no longer start checkout.");
        }

        if (!string.IsNullOrWhiteSpace(payment.CheckoutUrl))
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Success(ToDto(payment));
        }

        if (!gateway.IsConfigured)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.PaymentNotConfigured,
                "Subscription payments are not configured.");
        }

        HostedCheckoutSessionResult session;
        try
        {
            session = await gateway.CreateSessionAsync(
                new HostedCheckoutCreateRequest(
                    payment.Id.Value,
                    payment.ReferenceNumber,
                    payment.PlanKey,
                    payment.BillingCycle.ToString(),
                    payment.FinalAmount,
                    payment.CurrencyCode,
                    SubscriptionCheckoutReturnUrls.Success(returnBaseUrl, payment.Id.Value, payment.OrganizationId?.Value),
                    SubscriptionCheckoutReturnUrls.Cancelled(returnBaseUrl, payment.Id.Value, payment.OrganizationId?.Value),
                    payment.OrganizationId?.Value),
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.PaymentNotConfigured,
                "Subscription payments are not configured.");
        }
        catch (HttpRequestException)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(
                ApplicationErrorCodes.PaymentNotConfirmed,
                "The payment provider is unavailable. Try again shortly.");
        }

        try
        {
            payment.AttachHostedCheckout(session.SessionId, session.CheckoutUrl, clock.UtcNow);
            await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Success(ToDto(payment));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<HostedSubscriptionCheckoutDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<Plan?> ResolvePlanAsync(string planId, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(planId, out var id) && id != Guid.Empty)
        {
            var byId = await plans.GetByIdAsync(PlanId.From(id), cancellationToken).ConfigureAwait(false);
            if (byId is not null)
            {
                return byId;
            }
        }

        if (string.IsNullOrWhiteSpace(planId))
        {
            return null;
        }

        return await plans
            .GetByProductAndCodeAsync(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                PlanCode.Create(planId.Trim()),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static HostedSubscriptionCheckoutDto ToDto(SubscriptionPaymentTransaction payment) =>
        new(
            payment.Id.Value,
            payment.CheckoutUrl ?? "",
            payment.Status.ToString(),
            payment.FinalAmount,
            payment.CurrencyCode,
            payment.PlanKey,
            payment.BillingCycle.ToString(),
            payment.OrganizationId?.Value);
}

public sealed class ApplyTrustedHostedCheckoutPayment(
    ISubscriptionPaymentTransactionRepository payments,
    IHostedCheckoutSubscriptionActivator activator,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        string checkoutSessionId,
        decimal paidAmount,
        string currencyCode,
        string providerEventId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(checkoutSessionId))
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Checkout session was not found.");
        }

        var payment = await payments
            .GetByProviderReferenceAsync(checkoutSessionId.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Checkout session was not found.");
        }

        if (payment.HasCompletedProviderEvent(providerEventId)
            || (payment.Status == SubscriptionPaymentStatus.Paid
                && (payment.OrganizationId is null || payment.SubscriptionActivated)))
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }

        if (!string.Equals(currencyCode?.Trim(), payment.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentCurrencyMismatch,
                "Payment currency does not match the subscription quote.");
        }

        if (decimal.Round(paidAmount, 2, MidpointRounding.AwayFromZero)
            != decimal.Round(payment.FinalAmount, 2, MidpointRounding.AwayFromZero))
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                ApplicationErrorCodes.PaymentAmountMismatch,
                "Payment amount does not match the subscription quote.");
        }

        var utcNow = clock.UtcNow;
        try
        {
            var (periodStart, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(utcNow, payment.BillingCycle);
            payment.MarkPaid(utcNow, periodStart, periodEnd);
            if (payment.OrganizationId is not null && !payment.SubscriptionActivated)
            {
                var activated = await activator.ActivateAsync(payment, cancellationToken).ConfigureAwait(false);
                if (!activated.IsSuccess)
                {
                    return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                        activated.ErrorCode ?? ApplicationErrorCodes.SubscriptionIneligible,
                        activated.ErrorMessage ?? "Subscription activation failed.");
                }
            }

            if (!string.IsNullOrWhiteSpace(providerEventId))
            {
                payment.RememberProviderEvent(providerEventId);
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

public sealed class SyncHostedSubscriptionCheckout(
    ISubscriptionPaymentTransactionRepository payments,
    ISubscriptionCheckoutGateway gateway,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        Guid paymentId,
        PlatformUserId userId,
        Guid? expectedOrganizationId,
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

        if (userId.Value == Guid.Empty || payment.InitiatedByUserId.Value != userId.Value)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to the current user.");
        }

        if (!SubscriptionCheckoutAccess.OrganizationContextMatches(payment, expectedOrganizationId))
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to this organization.");
        }

        if (payment.Status == SubscriptionPaymentStatus.Paid
            || string.IsNullOrWhiteSpace(payment.ProviderReference)
            || !gateway.IsConfigured)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }

        HostedCheckoutProviderState state;
        try
        {
            state = await gateway.GetSessionAsync(payment.ProviderReference, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }

        if (state.IsPaid)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }

        var utcNow = clock.UtcNow;
        try
        {
            if (state.IsFailed && payment.Status is SubscriptionPaymentStatus.Pending or SubscriptionPaymentStatus.Processing)
            {
                payment.MarkFailed("payment_failed", "The payment provider reported a failed payment.", utcNow);
            }
            else if (state.IsExpired && payment.Status is SubscriptionPaymentStatus.Pending or SubscriptionPaymentStatus.Processing)
            {
                payment.Expire(utcNow);
            }
            else
            {
                return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                    SubscriptionPaymentMapping.ToDto(payment));
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

public sealed class CancelHostedSubscriptionCheckout(
    ISubscriptionPaymentTransactionRepository payments,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<SubscriptionPaymentTransactionDto>> ExecuteAsync(
        Guid paymentId,
        PlatformUserId userId,
        Guid? expectedOrganizationId,
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

        if (userId.Value == Guid.Empty || payment.InitiatedByUserId.Value != userId.Value)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to the current user.");
        }

        if (!SubscriptionCheckoutAccess.OrganizationContextMatches(payment, expectedOrganizationId))
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Failure(
                DomainErrorCodes.AuthorizationDenied,
                "Payment does not belong to this organization.");
        }

        if (payment.Status == SubscriptionPaymentStatus.Paid)
        {
            return ApplicationResult<SubscriptionPaymentTransactionDto>.Success(
                SubscriptionPaymentMapping.ToDto(payment));
        }

        try
        {
            payment.CancelOpenCheckout(clock.UtcNow, "Checkout cancelled before payment.");
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
