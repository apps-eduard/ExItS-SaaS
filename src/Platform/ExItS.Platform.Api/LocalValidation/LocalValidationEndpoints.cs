using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.LocalValidation;

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

        return app;
    }
}
