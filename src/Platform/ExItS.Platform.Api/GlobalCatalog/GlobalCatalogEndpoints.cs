using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Api.GlobalCatalog;

internal static class GlobalCatalogEndpoints
{
    public static IEndpointRouteBuilder MapGlobalCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/api/v1/platform/global-catalog");
        MapCategoryEndpoints(root);
        MapProductEndpoints(root);
        return app;
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder root)
    {
        var categories = root.MapGroup("/categories");

        categories.MapGet("/", async (
            GlobalCategoryQueryService queries,
            PlatformAuthz authz,
            GlobalCategoryStatus? status,
            Guid? parentId,
            BusinessType? businessType,
            string? search,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(GlobalCategory),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries
                .ListAsync(status, parentId, businessType, search, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        categories.MapGet("/{id:guid}", async (
            Guid id,
            GlobalCategoryQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(GlobalCategory),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var category = await queries.GetByIdAsync(id, ct).ConfigureAwait(false);
            return category is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.GlobalCategoryNotFound,
                    "Category was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(category);
        });

        categories.MapPost("/", async (
            CreateGlobalCategoryRequest body,
            CreateGlobalCategory useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalCategories,
                PlatformAuditActions.GlobalCategoryCreated,
                nameof(GlobalCategory),
                body.Name,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalCategoryCreated,
                    nameof(GlobalCategory),
                    result.Value!.Id.ToString("D"),
                    summary: $"Created global category {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                c => Results.Created($"/api/v1/platform/global-catalog/categories/{c.Id}", c));
        });

        categories.MapPut("/{id:guid}", async (
            Guid id,
            UpdateGlobalCategoryRequest body,
            UpdateGlobalCategory useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalCategories,
                PlatformAuditActions.GlobalCategoryUpdated,
                nameof(GlobalCategory),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalCategoryUpdated,
                    nameof(GlobalCategory),
                    result.Value!.Id.ToString("D"),
                    summary: $"Updated global category {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        categories.MapPatch("/{id:guid}/status", async (
            Guid id,
            SetGlobalCategoryStatusRequest body,
            SetGlobalCategoryStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalCategories,
                PlatformAuditActions.GlobalCategoryStatusChanged,
                nameof(GlobalCategory),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalCategoryStatusChanged,
                    nameof(GlobalCategory),
                    result.Value!.Id.ToString("D"),
                    summary: $"Changed global category status to {result.Value.Status}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
    }

    private static void MapProductEndpoints(RouteGroupBuilder root)
    {
        var products = root.MapGroup("/products");

        products.MapGet("/", async (
            GlobalProductQueryService queries,
            PlatformAuthz authz,
            GlobalProductStatus? status,
            Guid? categoryId,
            BusinessType? businessType,
            string? search,
            string? barcode,
            string? sku,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(GlobalProduct),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries
                .ListAsync(status, categoryId, businessType, search, barcode, sku, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        products.MapGet("/{id:guid}", async (
            Guid id,
            GlobalProductQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(GlobalProduct),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var product = await queries.GetByIdAsync(id, ct).ConfigureAwait(false);
            return product is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.GlobalProductNotFound,
                    "Product was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(product);
        });

        products.MapPost("/", async (
            CreateGlobalProductRequest body,
            CreateGlobalProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalProducts,
                PlatformAuditActions.GlobalProductCreated,
                nameof(GlobalProduct),
                body.Name,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalProductCreated,
                    nameof(GlobalProduct),
                    result.Value!.Id.ToString("D"),
                    summary: $"Created global product {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                p => Results.Created($"/api/v1/platform/global-catalog/products/{p.Id}", p));
        });

        products.MapPut("/{id:guid}", async (
            Guid id,
            UpdateGlobalProductRequest body,
            UpdateGlobalProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalProducts,
                PlatformAuditActions.GlobalProductUpdated,
                nameof(GlobalProduct),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalProductUpdated,
                    nameof(GlobalProduct),
                    result.Value!.Id.ToString("D"),
                    summary: $"Updated global product {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        products.MapPatch("/{id:guid}/status", async (
            Guid id,
            SetGlobalProductStatusRequest body,
            SetGlobalProductStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalProducts,
                PlatformAuditActions.GlobalProductStatusChanged,
                nameof(GlobalProduct),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalProductStatusChanged,
                    nameof(GlobalProduct),
                    result.Value!.Id.ToString("D"),
                    summary: $"Changed global product status to {result.Value.Status}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
    }
}
