using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Options;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.Catalog;

/// <summary>
/// Platform → POS support read APIs. Authenticated by shared support API key only.
/// GET only — no merchant mutations.
/// </summary>
internal static class PlatformSupportCatalogEndpoints
{
    public static IEndpointRouteBuilder MapPlatformSupportCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/platform-support");

        group.MapGet("/organizations/{organizationId:guid}/catalog", async (
            Guid organizationId,
            HttpRequest request,
            int? page,
            int? pageSize,
            string? search,
            GetOrganizationCatalogForPlatformSupport useCase,
            IOptions<PlatformSupportOptions> options,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSupportKey(request, options.Value, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, page, pageSize, search, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }

    internal static bool TryAuthorizeSupportKey(
        HttpRequest request,
        PlatformSupportOptions options,
        out IResult? problem)
    {
        problem = null;
        var provided = request.Headers[PlatformSupportApiKeyGuard.HeaderName].FirstOrDefault();
        if (PlatformSupportApiKeyGuard.IsAuthorized(options.ApiKey, provided))
        {
            return true;
        }

        problem = PosApiResults.Problem(
            "pos.platform_support.unauthorized",
            "Platform support API key is missing or invalid.",
            StatusCodes.Status401Unauthorized);
        return false;
    }
}
