using System.Security.Claims;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Qr;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.AspNetCore.RateLimiting;

namespace ExItS.Platform.Api.Identity;

internal static class ScopedQrEndpoints
{
    public static IEndpointRouteBuilder MapScopedQrEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/organizations/{organizationId:guid}/public-identity", async (
            HttpContext http,
            Guid organizationId,
            GetOrganizationPublicIdentity getIdentity,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getIdentity
                .ExecuteAsync(PlatformUserId.From(userId), PlatformOrganizationId.From(organizationId), ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        })
        .RequireAuthorization();

        app.MapPost("/api/v1/organizations/resolve-public-id", async (
            HttpContext http,
            ResolvePublicOrganizationIdRequest body,
            ResolvePublicOrganizationId resolve,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await resolve
                .ExecuteAsync(PlatformUserId.From(userId), body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        })
        .RequireAuthorization()
        .RequireRateLimiting(PlatformSecurityPipeline.PublicIdResolveRateLimitPolicy);

        app.MapPost("/api/v1/qr/resolve", async (
            HttpContext http,
            ResolveExItsQrRequest body,
            ResolveExItsQr resolve,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await resolve
                .ExecuteAsync(PlatformUserId.From(userId), body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        })
        .RequireAuthorization()
        .RequireRateLimiting(PlatformSecurityPipeline.PublicIdResolveRateLimitPolicy);

        return app;
    }

    private static bool TryGetUserId(HttpContext http, out Guid userId, out IResult? unauthorized)
    {
        userId = default;
        unauthorized = null;
        var raw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out userId))
        {
            unauthorized = Results.Unauthorized();
            return false;
        }

        return true;
    }
}
