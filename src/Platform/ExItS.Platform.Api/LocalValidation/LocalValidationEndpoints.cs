using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.Api.LocalValidation;

/// <summary>
/// Local-validation-only seed discovery for coordinating Platform↔POS bootstrap.
/// Not a user-facing login path — operators sign in through normal /auth/login.
/// Forbidden implicitly when LocalValidation:Enabled is false or environment is Production.
/// </summary>
internal static class LocalValidationEndpoints
{
    public static IEndpointRouteBuilder MapLocalValidationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/platform/local-validation/enabled", (IConfiguration config, IHostEnvironment env) =>
        {
            if (env.IsProduction())
            {
                return Results.Ok(false);
            }

            return Results.Ok(config.GetValue("LocalValidation:Enabled", false));
        })
        .AllowAnonymous()
        .DisableRateLimiting();

        app.MapGet("/api/v1/platform/local-validation/seed-identities", async (
            ListLocalValidationIdentities useCase,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(env.IsProduction(), ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .AllowAnonymous()
        .DisableRateLimiting();

        app.MapGet("/api/v1/platform/local-validation/quick-login-identities", async (
            ListLocalValidationQuickLoginIdentities useCase,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (env.IsProduction())
            {
                return Results.NotFound();
            }

            var result = await useCase.ExecuteAsync(env.IsProduction(), ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .AllowAnonymous()
        .DisableRateLimiting();

        app.MapPost("/api/v1/platform/local-validation/payments/simulate", async (
            SimulateLocalValidationPaymentRequest body,
            SimulateLocalValidationPayment useCase,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (env.IsProduction())
            {
                return Results.NotFound();
            }

            BillingCycle? cycle = string.IsNullOrWhiteSpace(body.BillingCycle)
                ? null
                : Enum.Parse<BillingCycle>(body.BillingCycle, ignoreCase: true);

            var result = await useCase.ExecuteAsync(
                body.Simulation,
                new PaymentChargeRequest(
                    body.OrganizationId,
                    body.SubscriptionId,
                    body.Amount,
                    body.CurrencyCode,
                    body.IdempotencyKey,
                    body.Purpose),
                cycle,
                ct).ConfigureAwait(false);

            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .AllowAnonymous()
        .DisableRateLimiting();

        return app;
    }
}

internal sealed record SimulateLocalValidationPaymentRequest(
    string Simulation,
    Guid OrganizationId,
    Guid SubscriptionId,
    decimal Amount,
    string CurrencyCode,
    string IdempotencyKey,
    string? Purpose = null,
    string? BillingCycle = null);
