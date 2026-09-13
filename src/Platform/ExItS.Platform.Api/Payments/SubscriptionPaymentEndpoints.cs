using System.Security.Claims;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Api.Payments;

/// <summary>
/// SaaS subscription checkout payment APIs (simulator for Local Validation / TEST).
/// Distinct from merchant POS tenders and from manual SaaSPayment attestation.
/// </summary>
internal static class SubscriptionPaymentEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        MapOrganizationScoped(app);
        MapAdminScoped(app);
        return app;
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
