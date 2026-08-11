using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Api.Catalog;

/// <summary>
/// Organization-scoped POS catalog endpoints (P8-WP01). Development-stage only: organization scope
/// comes from <c>X-Pos-Organization-Id</c> and cross-organization access returns 404 (fail closed).
/// Catalog identification and lifecycle only — no sales, stock, tax, or discount surface.
/// </summary>
internal static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        MapCategoryEndpoints(app.MapGroup("/api/v1/pos/catalog/categories"));
        MapProductEndpoints(app.MapGroup("/api/v1/pos/catalog/products"));
        return app;
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            string? search,
            int? page,
            int? pageSize,
            ProductCategoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseCategoryStatus(status, out var parsedStatus, out problem))
            {
                return problem!;
            }

            var result = await queries
                .ListAsync(organizationId, parsedStatus, search, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreatePosProductCategoryRequest body,
            CreateProductCategory useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, body.Name, body.CategoryId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                c =>
                {
                    var dto = ProductCategoryQueryService.Map(c);
                    return Results.Created($"/api/v1/pos/catalog/categories/{dto.CategoryId:D}", dto);
                });
        });

        group.MapGet("/{categoryId:guid}", async (
            HttpRequest request,
            Guid categoryId,
            ProductCategoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var category = await queries.GetByIdAsync(organizationId, categoryId, ct).ConfigureAwait(false);
            return category is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.CategoryNotFound,
                    "Category was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(category);
        });

        group.MapPut("/{categoryId:guid}", async (
            HttpRequest request,
            Guid categoryId,
            UpdatePosProductCategoryRequest body,
            UpdateProductCategory useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, categoryId, body.Name, body.ExpectedUpdatedAtUtc, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(ProductCategoryQueryService.Map(c)));
        });

        group.MapPost("/{categoryId:guid}/deactivate", async (
            HttpRequest request,
            Guid categoryId,
            DeactivateProductCategory useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, categoryId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(ProductCategoryQueryService.Map(c)));
        });

        group.MapPost("/{categoryId:guid}/reactivate", async (
            HttpRequest request,
            Guid categoryId,
            ReactivateProductCategory useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, categoryId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(ProductCategoryQueryService.Map(c)));
        });
    }

    private static void MapProductEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            Guid? categoryId,
            string? unitOfMeasure,
            string? search,
            int? page,
            int? pageSize,
            CatalogProductQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseProductStatus(status, out var parsedStatus, out problem))
            {
                return problem!;
            }

            UnitOfMeasure? parsedUnit = null;
            if (!string.IsNullOrWhiteSpace(unitOfMeasure))
            {
                if (!UnitOfMeasures.TryParse(unitOfMeasure, out var unit))
                {
                    return PosApiResults.Problem(
                        DomainErrorCodes.InvalidUnitOfMeasure,
                        $"Unrecognized unit of measure '{unitOfMeasure}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedUnit = unit;
            }

            ProductCategoryId? parsedCategory = null;
            if (categoryId is not null)
            {
                if (categoryId.Value == Guid.Empty)
                {
                    return PosApiResults.Problem(
                        DomainErrorCodes.InvalidProductCategoryId,
                        "Category id must be a non-empty GUID.",
                        StatusCodes.Status400BadRequest);
                }

                parsedCategory = ProductCategoryId.From(categoryId.Value);
            }

            var filter = new CatalogProductFilter(parsedStatus, parsedCategory, parsedUnit, search);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreatePosCatalogProductRequest body,
            CreateCatalogProduct useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(
                    organizationId,
                    body.Name,
                    body.UnitOfMeasure,
                    body.SellingPrice,
                    body.Description,
                    body.Sku,
                    body.Barcode,
                    body.CategoryId,
                    body.ProductId,
                    body.SellingMode,
                    ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(
                result,
                p =>
                {
                    var dto = CatalogProductQueryService.Map(p);
                    return Results.Created($"/api/v1/pos/catalog/products/{dto.ProductId:D}", dto);
                });
        });

        // Today's Prices — narrow bulk current-price update (ManageCatalog; partial success).
        group.MapPost("/prices", async (
            HttpRequest request,
            UpdatePosCatalogProductPricesRequest body,
            UpdateCatalogProductPrices useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, body.Items ?? Array.Empty<UpdatePosCatalogProductPriceItem>(), ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapGet("/{productId:guid}", async (
            HttpRequest request,
            Guid productId,
            CatalogProductQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var product = await queries.GetByIdAsync(organizationId, productId, ct).ConfigureAwait(false);
            return product is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.ProductNotFound,
                    "Product was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(product);
        });

        group.MapPut("/{productId:guid}", async (
            HttpRequest request,
            Guid productId,
            UpdatePosCatalogProductRequest body,
            UpdateCatalogProduct useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(
                    organizationId,
                    productId,
                    body.Name,
                    body.UnitOfMeasure,
                    body.SellingPrice,
                    body.Description,
                    body.Sku,
                    body.Barcode,
                    body.CategoryId,
                    body.ExpectedUpdatedAtUtc,
                    body.SellingMode,
                    ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(result, p => Results.Ok(CatalogProductQueryService.Map(p)));
        });

        group.MapPost("/{productId:guid}/deactivate", async (
            HttpRequest request,
            Guid productId,
            DeactivateCatalogProduct useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, productId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, p => Results.Ok(CatalogProductQueryService.Map(p)));
        });

        group.MapPost("/{productId:guid}/reactivate", async (
            HttpRequest request,
            Guid productId,
            ReactivateCatalogProduct useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, productId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, p => Results.Ok(CatalogProductQueryService.Map(p)));
        });

        group.MapGet("/by-sku/{sku}", async (
            HttpRequest request,
            string sku,
            bool? includeInactive,
            CatalogProductQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await queries
                .LookupBySkuAsync(organizationId, sku, includeInactive ?? false, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapGet("/by-barcode/{barcode}", async (
            HttpRequest request,
            string barcode,
            bool? includeInactive,
            CatalogProductQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await queries
                .LookupByBarcodeAsync(organizationId, barcode, includeInactive ?? false, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }

    private static bool TryParseCategoryStatus(
        string? status,
        out ProductCategoryStatus? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<ProductCategoryStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidCategoryStatus,
                $"Unrecognized category status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParseProductStatus(
        string? status,
        out CatalogProductStatus? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<CatalogProductStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidProductStatus,
                $"Unrecognized product status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }
}
