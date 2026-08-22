using Microsoft.AspNetCore.Antiforgery;

namespace ExItS.Platform.Api.Common;

internal static class AntiforgeryEndpoints
{
    public static IEndpointRouteBuilder MapAntiforgeryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(PlatformAntiforgeryDefaults.TokenRoute, (HttpContext http, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(http);
            return Results.Ok(new
            {
                headerName = PlatformAntiforgeryDefaults.HeaderName,
                token = tokens.RequestToken,
            });
        })
        .AllowAnonymous()
        .DisableRateLimiting();

        return app;
    }
}
