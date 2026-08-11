using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Api.GlobalCatalog;

/// <summary>
/// Merchant discovery for published business templates under /api/v1/catalog/*.
/// Enforces organization effective Business Type entitlements (WP03). Platform Admin
/// with ViewGlobalCatalog remains unrestricted.
/// </summary>
internal static class MerchantCatalogDiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapMerchantCatalogDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/api/v1/catalog").RequireAuthorization();

        root.MapGet("/business-types", async (
            HttpContext http,
            BusinessTypeQueryService queries,
            MerchantCatalogEntitlementGate gate,
            CancellationToken ct) =>
        {
            var denied = EnsureAuthenticated(http);
            if (denied is not null)
            {
                return denied;
            }

            var scope = await gate.ResolveDiscoveryScopeAsync(cancellationToken: ct).ConfigureAwait(false);
            if (!scope.IsSuccess)
            {
                return PlatformApiResults.Problem(
                    scope.ErrorCode!,
                    scope.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(scope.ErrorCode!));
            }

            var items = await queries
                .ListActiveForMerchantsAsync(
                    ct,
                    scope.Value!.Unrestricted ? null : scope.Value.AllowedBusinessTypeIds)
                .ConfigureAwait(false);
            return Results.Ok(items);
        });

        root.MapGet("/templates", async (
            HttpContext http,
            CatalogTemplateQueryService queries,
            MerchantCatalogEntitlementGate gate,
            Guid? businessTypeId,
            string? businessTypeCode,
            // Legacy aliases (WP01 mismatch) — still entitlement-intersected.
            Guid? primaryBusinessTypeId,
            string? businessType,
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

            var resolvedFilter = await ResolveEntitledFilterAsync(
                    gate,
                    businessTypeId ?? primaryBusinessTypeId,
                    businessTypeCode ?? businessType,
                    ct)
                .ConfigureAwait(false);
            if (resolvedFilter.Denied is not null)
            {
                return resolvedFilter.Denied;
            }

            var result = await queries
                .ListPublishedForMerchantsAsync(
                    resolvedFilter.SingleBusinessTypeId,
                    primaryBusinessTypeCode: null,
                    search,
                    page,
                    pageSize,
                    ct,
                    resolvedFilter.AllowedBusinessTypeIds)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        root.MapGet("/templates/{id:guid}", async (
            Guid id,
            HttpContext http,
            CatalogTemplateQueryService queries,
            MerchantCatalogEntitlementGate gate,
            CancellationToken ct) =>
        {
            var denied = EnsureAuthenticated(http);
            if (denied is not null)
            {
                return denied;
            }

            var scope = await gate.ResolveDiscoveryScopeAsync(cancellationToken: ct).ConfigureAwait(false);
            if (!scope.IsSuccess)
            {
                return PlatformApiResults.Problem(
                    scope.ErrorCode!,
                    scope.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(scope.ErrorCode!));
            }

            var template = await queries.GetPublishedByIdAsync(id, ct).ConfigureAwait(false);
            if (template is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogTemplateNotPublished,
                    "Published template was not found.",
                    StatusCodes.Status404NotFound);
            }

            var entitled = gate.EnsureResourceEntitled(
                scope.Value!,
                BusinessTypeId.From(template.PrimaryBusinessTypeId));
            if (!entitled.IsSuccess)
            {
                return PlatformApiResults.Problem(
                    entitled.ErrorCode!,
                    entitled.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(entitled.ErrorCode!));
            }

            return Results.Ok(template);
        });

        root.MapGet("/templates/{id:guid}/products", async (
            Guid id,
            HttpContext http,
            CatalogTemplateQueryService queries,
            MerchantCatalogEntitlementGate gate,
            CancellationToken ct) =>
        {
            var denied = EnsureAuthenticated(http);
            if (denied is not null)
            {
                return denied;
            }

            var scope = await gate.ResolveDiscoveryScopeAsync(cancellationToken: ct).ConfigureAwait(false);
            if (!scope.IsSuccess)
            {
                return PlatformApiResults.Problem(
                    scope.ErrorCode!,
                    scope.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(scope.ErrorCode!));
            }

            var template = await queries.GetPublishedByIdAsync(id, ct).ConfigureAwait(false);
            if (template is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogTemplateNotPublished,
                    "Published template was not found.",
                    StatusCodes.Status404NotFound);
            }

            var entitled = gate.EnsureResourceEntitled(
                scope.Value!,
                BusinessTypeId.From(template.PrimaryBusinessTypeId));
            if (!entitled.IsSuccess)
            {
                return PlatformApiResults.Problem(
                    entitled.ErrorCode!,
                    entitled.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(entitled.ErrorCode!));
            }

            return Results.Ok(template.Products);
        });

        root.MapGet("/products/search", async (
            HttpContext http,
            GlobalProductQueryService queries,
            MerchantCatalogEntitlementGate gate,
            string? q,
            Guid? businessTypeId,
            string? businessTypeCode,
            string? businessType,
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

            var resolvedFilter = await ResolveEntitledFilterAsync(
                    gate,
                    businessTypeId,
                    businessTypeCode ?? businessType,
                    ct)
                .ConfigureAwait(false);
            if (resolvedFilter.Denied is not null)
            {
                return resolvedFilter.Denied;
            }

            var result = await queries
                .ListAsync(
                    GlobalProductStatus.Active,
                    categoryId,
                    resolvedFilter.SingleBusinessTypeId,
                    businessTypeCode: null,
                    q,
                    barcode,
                    sku,
                    page,
                    pageSize,
                    ct,
                    allowedBusinessTypeIds: resolvedFilter.AllowedBusinessTypeIds)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        root.MapGet("/products/{id:guid}", async (
            Guid id,
            HttpContext http,
            GlobalProductQueryService queries,
            MerchantCatalogEntitlementGate gate,
            CancellationToken ct) =>
        {
            var denied = EnsureAuthenticated(http);
            if (denied is not null)
            {
                return denied;
            }

            var scope = await gate.ResolveDiscoveryScopeAsync(cancellationToken: ct).ConfigureAwait(false);
            if (!scope.IsSuccess)
            {
                return PlatformApiResults.Problem(
                    scope.ErrorCode!,
                    scope.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(scope.ErrorCode!));
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

            var entitled = gate.EnsureResourceEntitled(
                scope.Value!,
                product.BusinessTypeIds.Select(BusinessTypeId.From));
            if (!entitled.IsSuccess)
            {
                return PlatformApiResults.Problem(
                    entitled.ErrorCode!,
                    entitled.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(entitled.ErrorCode!));
            }

            return Results.Ok(product);
        });

        root.MapGet("/categories", async (
            HttpContext http,
            GlobalCategoryQueryService queries,
            MerchantCatalogEntitlementGate gate,
            Guid? businessTypeId,
            string? businessTypeCode,
            string? businessType,
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

            var resolvedFilter = await ResolveEntitledFilterAsync(
                    gate,
                    businessTypeId,
                    businessTypeCode ?? businessType,
                    ct)
                .ConfigureAwait(false);
            if (resolvedFilter.Denied is not null)
            {
                return resolvedFilter.Denied;
            }

            var result = await queries
                .ListAsync(
                    GlobalCategoryStatus.Active,
                    parentId,
                    resolvedFilter.SingleBusinessTypeId,
                    businessTypeCode: null,
                    search,
                    page,
                    pageSize,
                    ct,
                    allowedBusinessTypeIds: resolvedFilter.AllowedBusinessTypeIds)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }

    private static async Task<(
        IResult? Denied,
        Guid? SingleBusinessTypeId,
        IReadOnlyCollection<Guid>? AllowedBusinessTypeIds)> ResolveEntitledFilterAsync(
        MerchantCatalogEntitlementGate gate,
        Guid? businessTypeId,
        string? businessTypeCode,
        CancellationToken ct)
    {
        var scope = await gate.ResolveDiscoveryScopeAsync(cancellationToken: ct).ConfigureAwait(false);
        if (!scope.IsSuccess)
        {
            return (
                PlatformApiResults.Problem(
                    scope.ErrorCode!,
                    scope.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(scope.ErrorCode!)),
                null,
                null);
        }

        var filter = await gate
            .ResolveListFilterAsync(scope.Value!, businessTypeId, businessTypeCode, ct)
            .ConfigureAwait(false);
        if (!filter.IsSuccess)
        {
            return (
                PlatformApiResults.Problem(
                    filter.ErrorCode!,
                    filter.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(filter.ErrorCode!)),
                null,
                null);
        }

        return (null, filter.Value.SingleBusinessTypeId, filter.Value.AllowedBusinessTypeIds);
    }

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
