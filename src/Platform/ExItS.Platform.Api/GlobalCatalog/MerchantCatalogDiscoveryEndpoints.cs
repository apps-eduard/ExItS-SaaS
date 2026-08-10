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

        root.MapGet("/business-types", async (
            HttpContext http,
            BusinessTypeQueryService queries,
            CancellationToken ct) =>
        {
            var denied = EnsureAuthenticated(http);
            if (denied is not null)
            {
                return denied;
            }

            var items = await queries.ListActiveForMerchantsAsync(ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        root.MapGet("/templates", async (
            HttpContext http,
            CatalogTemplateQueryService queries,
            Guid? businessTypeId,
            string? businessTypeCode,
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
                .ListPublishedForMerchantsAsync(businessTypeId, businessTypeCode, search, page, pageSize, ct)
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

        root.MapGet("/products/search", async (
            HttpContext http,
            GlobalProductQueryService queries,
            string? q,
            Guid? businessTypeId,
            string? businessTypeCode,
            Guid? categoryId,
            string? barcode,
            string? sku,
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
                .ListAsync(
                    GlobalProductStatus.Active,
                    categoryId,
                    businessTypeId,
                    businessTypeCode,
                    q,
                    barcode,
                    sku,
                    page,
                    pageSize,
                    ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        root.MapGet("/products/{id:guid}", async (
            Guid id,
            HttpContext http,
            GlobalProductQueryService queries,
            CancellationToken ct) =>
        {
            var denied = EnsureAuthenticated(http);
            if (denied is not null)
            {
                return denied;
            }

            var product = await queries.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (product is null
                || !string.Equals(product.Status, nameof(GlobalProductStatus.Active), StringComparison.OrdinalIgnoreCase))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.GlobalProductNotFound,
                    "Active global product was not found.",
                    StatusCodes.Status404NotFound);
            }

            return Results.Ok(product);
        });

        root.MapGet("/categories", async (
            HttpContext http,
            GlobalCategoryQueryService queries,
            Guid? businessTypeId,
            string? businessTypeCode,
            Guid? parentId,
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
                .ListAsync(
                    GlobalCategoryStatus.Active,
                    parentId,
                    businessTypeId,
                    businessTypeCode,
                    search,
                    page,
                    pageSize,
                    ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
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
