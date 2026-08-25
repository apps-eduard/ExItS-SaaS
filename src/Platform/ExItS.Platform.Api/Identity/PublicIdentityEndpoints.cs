using System.Security.Claims;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Api.Identity;

internal static class PublicIdentityEndpoints
{
    public static IEndpointRouteBuilder MapPublicIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/me/public-identity", async (
            HttpContext http,
            GetOrAssignPublicIdentity getIdentity,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getIdentity.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireAuthorization();

        app.MapPost("/api/v1/users/resolve-public-id", async (
            HttpContext http,
            ResolvePublicUserIdRequest body,
            ResolvePublicUserId resolve,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(http, out var userId, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await resolve.ExecuteAsync(
                PlatformUserId.From(userId),
                body,
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireAuthorization()
        .AddEndpointFilter<PublicIdResolveRateLimitFilter>();

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
