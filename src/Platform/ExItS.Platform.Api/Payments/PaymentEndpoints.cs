using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Api.Payments;

/// <summary>
/// Manual SaaS payment activation endpoints (P3-WP03-manual-payment-activation). Development-stage
/// only: unauthenticated, no tenant scoping, "confirmedBy"/"rejectedBy"/"voidedBy" accept a plain
/// string actor reference (no auth — a production blocker tracked outside this phase). Cash, bank
/// transfer, and GCash manual reference recording only: no gateway, webhook, QR, or card storage.
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
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<SaaSPaymentMethod>(body.Method, ignoreCase: true, out var method))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidSaaSPaymentTransition,
                    "Payment method is not defined.",
                    StatusCodes.Status400BadRequest);
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
            CancellationToken ct) =>
        {
            var payment = await queries.GetByIdAsync(paymentId, ct).ConfigureAwait(false);
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
            CancellationToken ct) =>
        {
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

                if (status is not null)
                {
                    return Results.Ok(await queries
                        .ListByStatusAsync(status.Value, page, pageSize, ct)
                        .ConfigureAwait(false));
                }

                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.DomainViolation,
                    "Provide a status, productCode, or reference filter to list payments.",
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
            CancellationToken ct) =>
        {
            var result = await useCase
                .ExecuteAsync(SaaSPaymentId.From(paymentId), body.ConfirmedBy, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, p => Results.Ok(MapPayment(p)));
        });

        payments.MapPost("/{paymentId:guid}/reject", async (
            Guid paymentId,
            RejectPaymentRequest body,
            RejectSaaSPayment useCase,
            CancellationToken ct) =>
        {
            var result = await useCase
                .ExecuteAsync(SaaSPaymentId.From(paymentId), body.RejectedBy, body.Reason, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, p => Results.Ok(MapPayment(p)));
        });

        payments.MapPost("/{paymentId:guid}/void", async (
            Guid paymentId,
            VoidPaymentRequest body,
            VoidSaaSPayment useCase,
            CancellationToken ct) =>
        {
            var result = await useCase
                .ExecuteAsync(SaaSPaymentId.From(paymentId), body.VoidedBy, body.Reason, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, p => Results.Ok(MapPayment(p)));
        });

        payments.MapPost("/{paymentId:guid}/activate-subscription", async (
            Guid paymentId,
            ActivateSubscriptionForPaymentRequest body,
            ConfirmPaymentAndActivateSubscription useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(
                SaaSPaymentId.From(paymentId),
                body.ConfirmedBy,
                SubscriptionId.From(body.SubscriptionId),
                body.PeriodStartUtc,
                body.PeriodEndUtc,
                ct).ConfigureAwait(false);

            return PlatformApiResults.FromResult(result, activation => Results.Ok(new
            {
                payment = MapPayment(activation.Payment),
                subscription = MapSubscription(activation.Subscription)
            }));
        });
    }

    private static void MapOrganizationScopedPaymentEndpoints(IEndpointRouteBuilder app)
    {
        var orgPayments = app.MapGroup("/api/v1/platform/organizations/{organizationId:guid}/payments");

        orgPayments.MapGet("/", async (
            Guid organizationId,
            SaaSPaymentStatus? status,
            int? page,
            int? pageSize,
            SaaSPaymentQueryService queries,
            CancellationToken ct) =>
        {
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
        status = subscription.Status.ToString(),
        paidPeriodStartUtc = subscription.PaidPeriodStartUtc,
        paidPeriodEndUtc = subscription.PaidPeriodEndUtc,
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
