using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Api.Payments;

/// <summary>
/// Manual SaaS payment activation endpoints (P3-WP03-manual-payment-activation). Development-stage
/// only: actor identity is unauthenticated, "confirmedBy"/"rejectedBy"/"voidedBy" still accept a plain
/// string actor reference (no auth — a production blocker tracked outside this phase), but mutations
/// enforce <see cref="PlatformPermission.ManageManualPayments"/> (scoped to the owning organization
/// when known) and record audit trail entries. Cash, bank transfer, and GCash manual reference
/// recording only: no gateway, webhook, QR, or card storage.
/// </summary>
internal static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        MapTopLevelPaymentEndpoints(app);
        MapOrganizationScopedPaymentEndpoints(app);
        return app;
    }

    private static void MapTopLevelPaymentEndpoints(IEndpointRouteBuilder app)
    {
        var payments = app.MapGroup("/api/v1/platform/payments");

        payments.MapPost("/manual", async (
            CreateManualPaymentRequest body,
            CreateManualSaaSPayment useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SaaSPaymentMethod>(body.Method, ignoreCase: true, out var method))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidSaaSPaymentTransition,
                    "Payment method is not defined.",
                    StatusCodes.Status400BadRequest);
            }

            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageManualPayments,
                PlatformAuditActions.ManualPaymentCreated,
                "SaaSPayment",
                body.OrganizationId.ToString("D"),
                body.OrganizationId,
                body.ProductCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(body.OrganizationId),
                    ProductCode.Create(body.ProductCode),
                    body.Amount,
                    CurrencyCode.Create(body.CurrencyCode),
                    method,
                    body.ExternalReference,
                    body.PaidAtUtc,
                    ct).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.ManualPaymentCreated,
                        "SaaSPayment",
                        result.Value!.Id.Value.ToString("D"),
                        body.OrganizationId,
                        body.ProductCode,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, p => Results.Created(
                    $"/api/v1/platform/payments/{p.Id.Value}",
                    MapPayment(p)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        payments.MapGet("/{paymentId:guid}", async (
            Guid paymentId,
            SaaSPaymentQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var payment = await queries.GetByIdAsync(paymentId, ct).ConfigureAwait(false);
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageManualPayments,
                PlatformAuditActions.PlatformAccessChecked,
                "SaaSPayment",
                paymentId.ToString("D"),
                payment?.OrganizationId,
                payment?.ProductCode,
                summary: "Get manual payment.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            return payment is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.PaymentNotFound,
                    "Payment was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(payment);
        });

        payments.MapGet("/", async (
            SaaSPaymentStatus? status,
            string? productCode,
            string? reference,
            Guid? organizationId,
            SaaSPaymentMethod? method,
            int? page,
            int? pageSize,
            SaaSPaymentQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageManualPayments,
                PlatformAuditActions.PlatformAccessChecked,
                "SaaSPayment",
                "list",
                organizationId,
                productCode,
                summary: "List manual payments.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(reference))
                {
                    if (organizationId is null || method is null)
                    {
                        return PlatformApiResults.Problem(
                            ApplicationErrorCodes.DomainViolation,
                            "Searching by reference requires organizationId and method.",
                            StatusCodes.Status400BadRequest);
                    }

                    var found = await queries
                        .SearchByReferenceAsync(method.Value, reference, organizationId.Value, ct)
                        .ConfigureAwait(false);
                    return found is null
                        ? PlatformApiResults.Problem(
                            ApplicationErrorCodes.PaymentNotFound,
                            "Payment was not found.",
                            StatusCodes.Status404NotFound)
                        : Results.Ok(found);
                }

                if (!string.IsNullOrWhiteSpace(productCode))
                {
                    return Results.Ok(await queries
                        .ListByProductAsync(productCode, status, page, pageSize, ct)
                        .ConfigureAwait(false));
                }

                if (organizationId is not null)
                {
                    return Results.Ok(await queries
                        .ListByOrganizationAsync(organizationId.Value, status, page, pageSize, ct)
                        .ConfigureAwait(false));
                }

                if (status is not null)
                {
                    return Results.Ok(await queries
                        .ListByStatusAsync(status.Value, page, pageSize, ct)
                        .ConfigureAwait(false));
                }

                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.DomainViolation,
                    "Provide a status, productCode, organizationId, or reference filter to list payments.",
                    StatusCodes.Status400BadRequest);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        payments.MapPost("/{paymentId:guid}/confirm", async (
            Guid paymentId,
            ConfirmPaymentRequest body,
            ConfirmSaaSPayment useCase,
            SaaSPaymentQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsurePaymentMutationAsync(
                authz, queries, paymentId, PlatformAuditActions.ManualPaymentConfirmed, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(SaaSPaymentId.From(paymentId), body.ConfirmedBy, ct)
                .ConfigureAwait(false);
            await AuditPaymentSuccessAsync(authz, result, PlatformAuditActions.ManualPaymentConfirmed, paymentId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, p => Results.Ok(MapPayment(p)));
        });

        payments.MapPost("/{paymentId:guid}/reject", async (
            Guid paymentId,
            RejectPaymentRequest body,
            RejectSaaSPayment useCase,
            SaaSPaymentQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsurePaymentMutationAsync(
                authz, queries, paymentId, PlatformAuditActions.ManualPaymentRejected, ct, body.Reason).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(SaaSPaymentId.From(paymentId), body.RejectedBy, body.Reason, ct)
                .ConfigureAwait(false);
            await AuditPaymentSuccessAsync(
                authz, result, PlatformAuditActions.ManualPaymentRejected, paymentId, ct, body.Reason).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, p => Results.Ok(MapPayment(p)));
        });

        payments.MapPost("/{paymentId:guid}/void", async (
            Guid paymentId,
            VoidPaymentRequest body,
            VoidSaaSPayment useCase,
            SaaSPaymentQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsurePaymentMutationAsync(
                authz, queries, paymentId, PlatformAuditActions.ManualPaymentVoided, ct, body.Reason).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(SaaSPaymentId.From(paymentId), body.VoidedBy, body.Reason, ct)
                .ConfigureAwait(false);
            await AuditPaymentSuccessAsync(
                authz, result, PlatformAuditActions.ManualPaymentVoided, paymentId, ct, body.Reason).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, p => Results.Ok(MapPayment(p)));
        });

        payments.MapPost("/{paymentId:guid}/activate-subscription", async (
            Guid paymentId,
            ActivateSubscriptionForPaymentRequest body,
            ConfirmPaymentAndActivateSubscription useCase,
            SaaSPaymentQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsurePaymentMutationAsync(
                authz, queries, paymentId, PlatformAuditActions.SubscriptionActivated, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(
                SaaSPaymentId.From(paymentId),
                body.ConfirmedBy,
                SubscriptionId.From(body.SubscriptionId),
                body.PeriodStartUtc,
                body.PeriodEndUtc,
                ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.SubscriptionActivated,
                    "SaaSPayment",
                    paymentId.ToString("D"),
                    result.Value!.Payment.OrganizationId.Value,
                    result.Value.Payment.ProductCode.Value,
                    summary: $"Confirmed payment and activated subscription {result.Value.Subscription.Id.Value:D}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, activation => Results.Ok(new
            {
                payment = MapPayment(activation.Payment),
                subscription = MapSubscription(activation.Subscription)
            }));
        });
    }

    private static async Task<IResult?> EnsurePaymentMutationAsync(
        PlatformAuthz authz,
        SaaSPaymentQueryService queries,
        Guid paymentId,
        string actionCode,
        CancellationToken ct,
        string? reason = null)
    {
        var existing = await queries.GetByIdAsync(paymentId, ct).ConfigureAwait(false);
        return await authz.EnsureAsync(
            PlatformPermission.ManageManualPayments,
            actionCode,
            "SaaSPayment",
            paymentId.ToString("D"),
            existing?.OrganizationId,
            existing?.ProductCode,
            reason: reason,
            cancellationToken: ct).ConfigureAwait(false);
    }

    private static Task AuditPaymentSuccessAsync(
        PlatformAuthz authz,
        ApplicationResult<SaaSPayment> result,
        string actionCode,
        Guid paymentId,
        CancellationToken ct,
        string? reason = null) =>
        result.IsSuccess
            ? authz.AuditSucceededAsync(
                actionCode,
                "SaaSPayment",
                paymentId.ToString("D"),
                result.Value!.OrganizationId.Value,
                result.Value.ProductCode.Value,
                reason: reason,
                cancellationToken: ct)
            : Task.CompletedTask;

    private static void MapOrganizationScopedPaymentEndpoints(IEndpointRouteBuilder app)
    {
        var orgPayments = app.MapGroup("/api/v1/platform/organizations/{organizationId:guid}/payments");

        orgPayments.MapGet("/", async (
            Guid organizationId,
            SaaSPaymentStatus? status,
            int? page,
            int? pageSize,
            SaaSPaymentQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageManualPayments,
                PlatformAuditActions.PlatformAccessChecked,
                "SaaSPayment",
                organizationId.ToString("D"),
                organizationId,
                summary: "List organization manual payments.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries
                .ListByOrganizationAsync(organizationId, status, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });
    }

    private static object MapPayment(SaaSPayment payment) => new
    {
        id = payment.Id.Value,
        organizationId = payment.OrganizationId.Value,
        productCode = payment.ProductCode.Value,
        subscriptionId = payment.SubscriptionId?.Value,
        amount = payment.Amount,
        currencyCode = payment.CurrencyCode.Value,
        method = payment.Method.ToString(),
        externalReference = payment.ExternalReference,
        status = payment.Status.ToString(),
        paidAtUtc = payment.PaidAtUtc,
        confirmedAtUtc = payment.ConfirmedAtUtc,
        confirmedBy = payment.ConfirmedBy,
        rejectedAtUtc = payment.RejectedAtUtc,
        rejectedBy = payment.RejectedBy,
        rejectionReason = payment.RejectionReason,
        voidedAtUtc = payment.VoidedAtUtc,
        voidedBy = payment.VoidedBy,
        voidReason = payment.VoidReason,
        createdAtUtc = payment.CreatedAtUtc,
        updatedAtUtc = payment.UpdatedAtUtc,
        version = payment.Version
    };

    private static object MapSubscription(Subscription subscription) => new
    {
        id = subscription.Id.Value,
        organizationId = subscription.OrganizationId.Value,
        productCode = subscription.ProductCode.Value,
        planId = subscription.PlanId.Value,
        planVersionId = subscription.PlanVersionId.Value,
        trialDefinitionId = subscription.TrialDefinitionId?.Value,
        status = subscription.Status.ToString(),
        trialStartUtc = subscription.TrialStartUtc,
        trialEndUtc = subscription.TrialEndUtc,
        paidPeriodStartUtc = subscription.PaidPeriodStartUtc,
        paidPeriodEndUtc = subscription.PaidPeriodEndUtc,
        gracePeriodEndUtc = subscription.GracePeriodEndUtc,
        suspendedAtUtc = subscription.SuspendedAtUtc,
        pastDueAtUtc = subscription.PastDueAtUtc,
        cancelledAtUtc = subscription.CancelledAtUtc,
        expiredAtUtc = subscription.ExpiredAtUtc,
        createdAtUtc = subscription.CreatedAtUtc,
        updatedAtUtc = subscription.UpdatedAtUtc,
        version = subscription.Version
    };
}

internal sealed record CreateManualPaymentRequest(
    Guid OrganizationId,
    string ProductCode,
    decimal Amount,
    string CurrencyCode,
    string Method,
    string ExternalReference,
    DateTimeOffset PaidAtUtc);

internal sealed record ConfirmPaymentRequest(string ConfirmedBy);

internal sealed record RejectPaymentRequest(string RejectedBy, string Reason);

internal sealed record VoidPaymentRequest(string VoidedBy, string Reason);

internal sealed record ActivateSubscriptionForPaymentRequest(
    string ConfirmedBy,
    Guid SubscriptionId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc);
