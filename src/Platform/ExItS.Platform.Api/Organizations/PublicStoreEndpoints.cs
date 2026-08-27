using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Organizations;
using Microsoft.AspNetCore.RateLimiting;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Anonymous public store landing — Business QR acquisition (EXITS-V1-CLOSURE-01).
/// Minimal public-safe DTO only; no membership/staff/ownership grants.
/// </summary>
internal static class PublicStoreEndpoints
{
    public static IEndpointRouteBuilder MapPublicStoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/public/stores/{publicOrganizationId}", async (
            string publicOrganizationId,
            LookupPublicStoreLanding lookup,
            CancellationToken ct) =>
        {
            var result = await lookup.ExecuteAsync(publicOrganizationId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        })
        .AllowAnonymous()
        .RequireRateLimiting(PlatformSecurityPipeline.PublicIdResolveRateLimitPolicy)
        .AddEndpointFilter<PublicIdResolveRateLimitFilter>();

        return app;
    }
}
