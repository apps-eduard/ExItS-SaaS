using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Api.GlobalCatalog;

/// <summary>
/// Merchant discovery for published business templates under /api/v1/catalog/*.
/// Does not touch commercial SaaS catalog routes under /api/v1/platform/catalog/*.
/// </summary>
internal static class MerchantCatalogDiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapMerchantCatalogDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/api/v1/catalog").RequireAuthorization();

        root.MapGet("/templates", async (
            HttpContext http,
            CatalogTemplateQueryService queries,
            BusinessType? businessType,
            string? search,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = EnsureAuthenticated(http);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries
                .ListPublishedForMerchantsAsync(businessType, search, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        root.MapGet("/templates/{id:guid}", async (
            Guid id,
            HttpContext http,
            CatalogTemplateQueryService queries,
            CancellationToken ct) =>
        {
            var denied = EnsureAuthenticated(http);
            if (denied is not null)
            {
                return denied;
            }

            var template = await queries.GetPublishedByIdAsync(id, ct).ConfigureAwait(false);
            return template is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogTemplateNotPublished,
                    "Published template was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(template);
        });

        root.MapGet("/templates/{id:guid}/products", async (
            Guid id,
            HttpContext http,
            CatalogTemplateQueryService queries,
            CancellationToken ct) =>
        {
            var denied = EnsureAuthenticated(http);
            if (denied is not null)
            {
                return denied;
            }

            var template = await queries.GetPublishedByIdAsync(id, ct).ConfigureAwait(false);
            if (template is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogTemplateNotPublished,
                    "Published template was not found.",
                    StatusCodes.Status404NotFound);
            }

            return Results.Ok(template.Products);
        });

        return app;
    }

    /// <summary>
    /// Authenticated baseline for merchant discovery. Entitlement-aware product-access
    /// filtering is deferred until POS onboarding/import (WP06) wires organization context.
    /// Platform staff may also call these routes when authenticated.
    /// </summary>
    private static IResult? EnsureAuthenticated(HttpContext http)
    {
        if (http.User.Identity?.IsAuthenticated == true)
        {
            return null;
        }

        return PlatformApiResults.Problem(
            ApplicationErrorCodes.SessionInvalid,
            "Authentication is required.",
            StatusCodes.Status401Unauthorized);
    }
}
