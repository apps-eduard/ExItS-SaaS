using System.Text;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using Microsoft.AspNetCore.Hosting;

namespace ExItS.PinoyBusinessPOS.Api.Payments;

/// <summary>
/// Provider-ready POS payment-attempt endpoints. Final Paid state is accepted only from signed
/// webhooks (or Development simulation that posts through the same webhook handler). Clients never
/// set Paid directly.
/// </summary>
internal static class PaymentAttemptEndpoints
{
    public static IEndpointRouteBuilder MapPaymentAttemptEndpoints(this IEndpointRouteBuilder app)
    {
        var sales = app.MapGroup("/api/v1/pos/sales/{saleId:guid}");
        sales.MapPost("/payment-attempts", async (
            HttpRequest request,
            Guid saleId,
            CreatePaymentAttemptRequest body,
            CreatePaymentAttempt useCase,
            IPosCommercialAccessAccessor access,
            IPosDeviceTransactionAuthorizer deviceAuthorization,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeCashier(request, access, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var deviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, organizationId, ct).ConfigureAwait(false);
            if (deviceDenied is not null) return deviceDenied;

            var result = await useCase
                .ExecuteAsync(organizationId, saleId, body, actorId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/pos/payment-attempts/{dto.Id:D}", dto));
        });

        var attempts = app.MapGroup("/api/v1/pos/payment-attempts");
        attempts.MapGet("/{id:guid}", async (
            HttpRequest request,
            Guid id,
            GetPaymentAttempt useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeCashier(request, access, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, id, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        attempts.MapPost("/{id:guid}/cancel", async (
            HttpRequest request,
            Guid id,
            CancelPaymentAttempt useCase,
            IPosCommercialAccessAccessor access,
            IPosDeviceTransactionAuthorizer deviceAuthorization,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeCashier(request, access, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var deviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, organizationId, ct).ConfigureAwait(false);
            if (deviceDenied is not null) return deviceDenied;

            var result = await useCase.ExecuteAsync(organizationId, id, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        attempts.MapPost("/{id:guid}/verify-manual-gcash", async (
            HttpRequest request,
            Guid id,
            VerifyManualGCashRequest body,
            VerifyManualGCashTransfer useCase,
            IPosCommercialAccessAccessor access,
            IPosDeviceTransactionAuthorizer deviceAuthorization,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeCashier(request, access, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var deviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, organizationId, ct).ConfigureAwait(false);
            if (deviceDenied is not null) return deviceDenied;

            if (!CanVerifyManualTransfer(out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, id, actorId, body.Reason ?? string.Empty, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        attempts.MapPost("/{id:guid}/simulate", async (
            HttpRequest request,
            Guid id,
            SimulatePaymentRequest body,
            SimulatePaymentOutcome useCase,
            IWebHostEnvironment env,
            IPosCommercialAccessAccessor access,
            IPosDeviceTransactionAuthorizer deviceAuthorization,
            CancellationToken ct) =>
        {
            if (env.IsProduction() || string.Equals(env.EnvironmentName, "Release", StringComparison.OrdinalIgnoreCase))
            {
                return PosApiResults.Problem(
                    DomainErrorCodes.PaymentSimulatorDisabled,
                    "Payment simulation endpoints are disabled in Release/Production.",
                    StatusCodes.Status404NotFound);
            }

            if (!TryAuthorizeCashier(request, access, out var organizationId, out var problem))
            {
                return problem!;
            }

            var deviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, organizationId, ct).ConfigureAwait(false);
            if (deviceDenied is not null) return deviceDenied;

            var result = await useCase
                .ExecuteAsync(organizationId, id, body.Outcome, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost("/api/v1/pos/payment-webhooks/{provider}", async (
            HttpRequest request,
            string provider,
            ProcessPaymentWebhook useCase,
            CancellationToken ct) =>
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
            var signature = request.Headers["X-ExItS-Payment-Signature"].FirstOrDefault()
                ?? request.Headers["X-Fake-Payment-Signature"].FirstOrDefault();

            var result = await useCase
                .ExecuteAsync(provider, signature, rawBody, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, _ => Results.Ok(new { received = true }));
        });

        return app;
    }

    private static bool TryAuthorizeCashier(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, UtangCapability.CreateSale, out problem);
    }

    private static bool CanVerifyManualTransfer(out IResult? problem)
    {
        problem = null;
        if (PosRoleRequestContext.BypassRoleEnforcement || !PosRoleRequestContext.HasActorHeader)
        {
            return true;
        }

        var role = PosRoleRequestContext.CurrentRole;
        if (role is PosRole.Owner or PosRole.Admin or PosRole.StoreManager)
        {
            return true;
        }

        problem = PosApiResults.Problem(
            DomainErrorCodes.PosRoleDenied,
            "Manual GCash transfer verification requires Owner, Admin, or Store Manager.",
            StatusCodes.Status403Forbidden);
        return false;
    }
}

internal sealed record VerifyManualGCashRequest(string? Reason);
