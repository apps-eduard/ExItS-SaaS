using ExItS.Platform.Api.Common;
using ExItS.Platform.Api.Identity;
using ExItS.Platform.Application.LivePreview;
using ExItS.Platform.Application.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Api.LivePreview;

internal static class LivePreviewEndpoints
{
    public static IEndpointRouteBuilder MapLivePreviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/platform/live-preview/identities", async (
            ListLivePreviewIdentities useCase,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(env.IsProduction(), ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/live-preview/sessions", async (
            LivePreviewLoginRequest body,
            HttpContext http,
            LoginLivePreviewIdentity useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(
                body.IdentityKey,
                env.IsProduction(),
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString(),
                ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return PlatformApiResults.FromResult(result, _ => Results.Ok());
            }

            AuthEndpoints.AppendSessionCookie(
                http,
                result.Value.SessionToken,
                result.Value.ExpiresAtUtc,
                sessionOptions.Value,
                env);
            return Results.Ok(result.Value);
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthLoginRateLimitPolicy)
        .AllowAnonymous();

        return app;
    }
}

internal sealed record LivePreviewLoginRequest(string? IdentityKey);
