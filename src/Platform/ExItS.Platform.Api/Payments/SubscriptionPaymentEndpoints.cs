using System.Security.Claims;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Api.Payments;

/// <summary>
/// SaaS subscription checkout payment APIs (simulator for Local Validation / TEST).
/// Distinct from merchant POS tenders and from manual SaaSPayment attestation.
/// </summary>
internal static class SubscriptionPaymentEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        MapPersonalScoped(app);
        MapOrganizationScoped(app);
        MapAdminScoped(app);
        return app;
    }

    private static void MapPersonalScoped(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/personal/subscription-payments");

        // Single MapPost only — registering both "" and "/" causes AmbiguousMatchException (HTTP 500).
        group.MapPost("/", CreatePersonalPaymentAsync);

        async Task<IResult> CreatePersonalPaymentAsync(
            HttpContext http,
            CreatePersonalSubscriptionPaymentRequest body,
            CreatePendingSubscriptionPayment createPayment,
            CancellationToken ct)
        {
            if (!TryGetPersonalUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            if (body is null || string.IsNullOrWhiteSpace(body.PlanKey))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidPlanCode,
                    "Plan key is required.",
                    StatusCodes.Status400BadRequest);
            }

            BillingCycle billingCycle;
            try
            {
                billingCycle = BillingCycleParsing.ParseRequired(body.BillingCycle);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(
                    ex.ErrorCode,
                    ex.Message,
                    StatusCodes.Status400BadRequest);
            }

            var result = await createPayment
                .ExecuteAsync(
                    PlatformUserId.From(userId),
                    body.PlanKey,
                    billingCycle,
                    organizationId: null,
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/personal/subscription-payments/{dto.Id}", dto));
        }

        group.MapGet("/{paymentId:guid}", async (
            HttpContext http,
            Guid paymentId,
            GetSubscriptionPaymentTransaction getPayment,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getPayment
                .ExecuteAsync(paymentId, PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{paymentId:guid}/select-channel", async (
            HttpContext http,
            Guid paymentId,
            ProcessSubscriptionPaymentRequest body,
            SelectSubscriptionPaymentChannel selectChannel,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await selectChannel
                .ExecuteAsync(paymentId, PlatformUserId.From(userId), body.Channel, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{paymentId:guid}/process", async (
            HttpContext http,
            Guid paymentId,
            ProcessSubscriptionPaymentRequest body,
            ProcessSubscriptionPaymentSimulator process,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await process
                .ExecuteAsync(paymentId, PlatformUserId.From(userId), body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{paymentId:guid}/retry", async (
            HttpContext http,
            Guid paymentId,
            RetrySubscriptionPayment retry,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await retry
                .ExecuteAsync(paymentId, PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/personal/subscription-payments/{dto.Id}", dto));
        });
    }

    private static bool TryGetPersonalUserId(HttpContext http, out Guid userId, out IResult? unauthorized)
    {
        userId = Guid.Empty;
        unauthorized = null;
        if (!TryGetUserId(http, out userId, out unauthorized))
        {
            return false;
        }

        var accountClass = http.User.FindFirstValue(PlatformSessionClaimTypes.AccountClass);
        if (!string.Equals(accountClass, "Personal", StringComparison.OrdinalIgnoreCase))
        {
            unauthorized = PlatformApiResults.Problem(
                DomainErrorCodes.AuthorizationDenied,
                "Personal account context is required for pre-organization subscription checkout.",
                StatusCodes.Status403Forbidden);
            return false;
        }

        return true;
    }

    private static void MapOrganizationScoped(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/organizations/{organizationId:guid}/subscription-payments");

        group.MapGet("/{paymentId:guid}", async (
            HttpContext http,
            Guid organizationId,
            Guid paymentId,
            GetSubscriptionPaymentTransaction getPayment,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!SessionMatchesOrganization(http, organizationId))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Organization context does not match the payment organization.",
                    StatusCodes.Status403Forbidden);
            }

            var result = await getPayment
                .ExecuteAsync(paymentId, PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                return PlatformApiResults.FromResult(result, _ => Results.Ok());
            }

            if (result.Value.OrganizationId != organizationId)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Payment does not belong to this organization.",
                    StatusCodes.Status403Forbidden);
            }

            return Results.Ok(result.Value);
        });

        group.MapPost("/{paymentId:guid}/process", async (
            HttpContext http,
            Guid organizationId,
            Guid paymentId,
            ProcessSubscriptionPaymentRequest body,
            GetSubscriptionPaymentTransaction getPayment,
            ProcessSubscriptionPaymentSimulator process,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!SessionMatchesOrganization(http, organizationId))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Organization context does not match the payment organization.",
                    StatusCodes.Status403Forbidden);
            }

            var current = await getPayment
                .ExecuteAsync(paymentId, PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            if (!current.IsSuccess || current.Value is null)
            {
                return PlatformApiResults.FromResult(current, _ => Results.Ok());
            }

            if (current.Value.OrganizationId != organizationId)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Payment does not belong to this organization.",
                    StatusCodes.Status403Forbidden);
            }

            var result = await process
                .ExecuteAsync(paymentId, PlatformUserId.From(userId), body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });
    }

    private static void MapAdminScoped(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/platform/subscription-payments");

        admin.MapGet("/", async (
            int? take,
            ListSubscriptionPaymentTransactions list,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageManualPayments,
                PlatformAuditActions.PlatformAccessChecked,
                "SubscriptionPaymentTransaction",
                "*",
                organizationId: null,
                summary: "List subscription checkout payments (includes TEST simulator).",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var items = await list.ExecuteAsync(take ?? 100, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        admin.MapGet("/{paymentId:guid}", async (
            Guid paymentId,
            GetSubscriptionPaymentTransaction getPayment,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageManualPayments,
                PlatformAuditActions.PlatformAccessChecked,
                "SubscriptionPaymentTransaction",
                paymentId.ToString("D"),
                organizationId: null,
                summary: "View subscription checkout payment detail.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await getPayment.ExecuteAsync(paymentId, requesterUserId: null, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });
    }

    private static bool SessionMatchesOrganization(HttpContext http, Guid organizationId)
    {
        var raw = http.User.FindFirstValue(PlatformSessionClaimTypes.OrganizationId);
        return Guid.TryParse(raw, out var selected) && selected == organizationId;
    }

    private static bool TryGetUserId(HttpContext http, out Guid userId, out IResult? unauthorized)
    {
        userId = Guid.Empty;
        unauthorized = null;
        if (http.User.Identity?.IsAuthenticated != true)
        {
            unauthorized = PlatformApiResults.Problem(
                ApplicationErrorCodes.SessionInvalid,
                "Authentication is required.",
                StatusCodes.Status401Unauthorized);
            return false;
        }

        var raw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out userId) || userId == Guid.Empty)
        {
            unauthorized = PlatformApiResults.Problem(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.",
                StatusCodes.Status401Unauthorized);
            return false;
        }

        return true;
    }
}
