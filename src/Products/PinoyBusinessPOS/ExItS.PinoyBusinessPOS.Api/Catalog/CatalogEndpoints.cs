using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
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
        MapBrandEndpoints(app.MapGroup("/api/v1/pos/catalog/brands"));
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

    private static void MapBrandEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            string? search,
            int? page,
            int? pageSize,
            ProductBrandQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseBrandStatus(status, out var parsedStatus, out problem))
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
            CreatePosProductBrandRequest body,
            CreateProductBrand useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, body.Name, body.BrandId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                b =>
                {
                    var dto = ProductBrandQueryService.Map(b);
                    return Results.Created($"/api/v1/pos/catalog/brands/{dto.BrandId:D}", dto);
                });
        });

        group.MapGet("/{brandId:guid}", async (
            HttpRequest request,
            Guid brandId,
            ProductBrandQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var brand = await queries.GetByIdAsync(organizationId, brandId, ct).ConfigureAwait(false);
            return brand is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.BrandNotFound,
                    "Brand was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(brand);
        });

        group.MapPut("/{brandId:guid}", async (
            HttpRequest request,
            Guid brandId,
            UpdatePosProductBrandRequest body,
            UpdateProductBrand useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, brandId, body.Name, body.ExpectedUpdatedAtUtc, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, b => Results.Ok(ProductBrandQueryService.Map(b)));
        });

        group.MapPost("/{brandId:guid}/deactivate", async (
            HttpRequest request,
            Guid brandId,
            DeactivateProductBrand useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, brandId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, b => Results.Ok(ProductBrandQueryService.Map(b)));
        });

        group.MapPost("/{brandId:guid}/reactivate", async (
            HttpRequest request,
            Guid brandId,
            ReactivateProductBrand useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, brandId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, b => Results.Ok(ProductBrandQueryService.Map(b)));
        });
    }

    private static void MapProductEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            Guid? categoryId,
            Guid? brandId,
            string? unitOfMeasure,
            string? search,
            bool? canBeSold,
            bool? commerciallyOffered,
            string? scope,
            Guid? originBranchId,
            int? page,
            int? pageSize,
            CatalogProductQueryService queries,
            ICatalogGovernanceActorAccessor actorAccessor,
            ICatalogProductAvailabilityResolver availabilityResolver,
            ICatalogProductRepository products,
            ICatalogProductUnitRepository units,
            IEffectivePriceResolver effectivePrices,
            CatalogBranchStockResolver branchStock,
            BranchInventoryContextResolver branchContext,
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

            CatalogProductScope? parsedScope = null;
            if (!string.IsNullOrWhiteSpace(scope))
            {
                if (!CatalogProductScopes.TryParse(scope, out var scopeValue))
                {
                    return PosApiResults.Problem(
                        DomainErrorCodes.InvalidCatalogProductScope,
                        $"CatalogProductScope must be one of: {string.Join(", ", CatalogProductScopes.Codes)}.",
                        StatusCodes.Status400BadRequest);
                }

                parsedScope = scopeValue;
            }

            if (originBranchId is Guid oid && oid == Guid.Empty)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.ProductBranchInvalid,
                    "Origin branch id must be a non-empty GUID when provided.",
                    StatusCodes.Status400BadRequest);
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

            ProductBrandId? parsedBrand = null;
            if (brandId is not null)
            {
                if (brandId.Value == Guid.Empty)
                {
                    return PosApiResults.Problem(
                        DomainErrorCodes.InvalidProductBrandId,
                        "Brand id must be a non-empty GUID.",
                        StatusCodes.Status400BadRequest);
                }

                parsedBrand = ProductBrandId.From(brandId.Value);
            }

            var commercial = commerciallyOffered == true;
            if (!PosOrganizationScope.TryGetOptionalBranchId(request, out var actingBranchId))
            {
                actingBranchId = null;
            }

            if (commercial && actingBranchId is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.ProductActingBranchRequired,
                    "X-Pos-Branch-Id is required when listing commercially offered products.",
                    StatusCodes.Status400BadRequest);
            }

            var actor = actorAccessor.GetActor();
            var restrictLocal = !actor.IsOrganizationGovernance && !commercial;
            if (restrictLocal && actingBranchId is null)
            {
                // No branch context: non-governance actors must not see any BranchLocal.
                actingBranchId = Guid.Empty;
            }

            var filter = new CatalogProductFilter(
                parsedStatus,
                parsedCategory,
                parsedUnit,
                search,
                BrandId: parsedBrand,
                CanBeSold: canBeSold,
                CommerciallyOfferedAtBranch: commercial,
                ActingBranchId: commercial || restrictLocal ? actingBranchId : null,
                RestrictBranchLocalToActingBranch: restrictLocal,
                Scope: parsedScope,
                OriginBranchId: originBranchId);

            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            if (commercial)
            {
                var marked = result.Items
                    .Select(i => i with { IsOfferedAtBranch = true })
                    .ToList();
                if (actingBranchId is Guid commercialBranch && commercialBranch != Guid.Empty)
                {
                    marked = (await StampBranchCatalogContextAsync(
                            organizationId,
                            commercialBranch,
                            marked,
                            products,
                            units,
                            effectivePrices,
                            branchStock,
                            branchContext,
                            ct)
                        .ConfigureAwait(false)).ToList();
                }

                return Results.Ok(new PagedResult<PosCatalogProductDto>(
                    marked,
                    result.TotalCount,
                    result.Page,
                    result.PageSize));
            }

            // Management list: stamp isOfferedAtBranch for workspace branch without filtering membership.
            if (actingBranchId is Guid managementBranch
                && managementBranch != Guid.Empty
                && result.Items.Count > 0)
            {
                var orgId = Domain.Customers.PosOrganizationId.From(organizationId);
                var productIds = result.Items.Select(i => CatalogProductId.From(i.ProductId)).ToList();
                var loaded = await products.ListByIdsAsync(orgId, productIds, ct).ConfigureAwait(false);
                var offering = await availabilityResolver
                    .ResolveForBranchAsync(
                        orgId,
                        Domain.Inventory.PosBranchId.From(managementBranch),
                        loaded,
                        ct)
                    .ConfigureAwait(false);
                var stamped = result.Items
                    .Select(i =>
                    {
                        if (offering.TryGetValue(i.ProductId, out var offer))
                        {
                            return i with { IsOfferedAtBranch = offer.IsOffered };
                        }

                        return i;
                    })
                    .ToList();
                stamped = (await StampBranchCatalogContextAsync(
                        organizationId,
                        managementBranch,
                        stamped,
                        products,
                        units,
                        effectivePrices,
                        branchStock,
                        branchContext,
                        ct)
                    .ConfigureAwait(false)).ToList();
                return Results.Ok(new PagedResult<PosCatalogProductDto>(
                    stamped,
                    result.TotalCount,
                    result.Page,
                    result.PageSize));
            }

            if (actingBranchId is Guid listBranch
                && listBranch != Guid.Empty
                && result.Items.Count > 0)
            {
                var enriched = await StampBranchCatalogContextAsync(
                        organizationId,
                        listBranch,
                        result.Items,
                        products,
                        units,
                        effectivePrices,
                        branchStock,
                        branchContext,
                        ct)
                    .ConfigureAwait(false);
                return Results.Ok(new PagedResult<PosCatalogProductDto>(
                    enriched,
                    result.TotalCount,
                    result.Page,
                    result.PageSize));
            }

            return Results.Ok(result);
        });

        group.MapGet("/name-conflict", async (
            HttpRequest request,
            string name,
            Guid? excludeProductId,
            QueryCatalogProductNameConflict useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, name, excludeProductId, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreatePosCatalogProductRequest body,
            CreateCatalogProduct useCase,
            CatalogProductQueryService queries,
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
                    body.BrandId,
                    body.ProductId,
                    body.SellingMode,
                    body.TracksExpiration,
                    body.ExpirationWarningDays,
                    body.CanBePurchased,
                    body.CanBeSold,
                    body.CanBeUsedAsIngredient,
                    body.IsProduced,
                    body.UsagePreset,
                    body.Units,
                    body.CanExposeToConnectedBuyers,
                    body.DefaultConnectedPoPrice,
                    body.BusinessUsage,
                    ct,
                    body.Scope)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return PosApiResults.Problem(
                    result.ErrorCode!,
                    result.ErrorMessage!,
                    PosApiResults.MapStatusCode(result.ErrorCode!),
                    result.ErrorDetails);
            }

            var dto = await queries
                .GetByIdAsync(organizationId, result.Value!.Id.Value, ct)
                .ConfigureAwait(false);
            return Results.Created($"/api/v1/pos/catalog/products/{dto!.ProductId:D}", dto);
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

        // Level-1 connected buyer availability — paged query + bulk enable/disable/Default PO pricing.
        group.MapGet("/connected-buyer-availability", async (
            HttpRequest request,
            string? query,
            Guid? categoryId,
            string? availabilityFilter,
            bool? uncategorizedOnly,
            int? page,
            int? pageSize,
            QueryConnectedBuyerAvailability useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(
                        organizationId, query, categoryId, availabilityFilter, page, pageSize,
                        uncategorizedOnly == true, ct)
                    .ConfigureAwait(false),
                Results.Ok);
        });

        group.MapPost("/connected-buyer-availability/bulk", async (
            HttpRequest request,
            BulkConnectedBuyerAvailabilityMutationRequest body,
            BulkMutateConnectedBuyerAvailability useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, body, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapPost("/connected-buyer-availability/pricing/preview", async (
            HttpRequest request,
            BulkDefaultConnectedPoPricingRequest body,
            PreviewDefaultConnectedPoPricing useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, body, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapPost("/connected-buyer-availability/pricing/apply", async (
            HttpRequest request,
            BulkDefaultConnectedPoPricingRequest body,
            ApplyDefaultConnectedPoPricing useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, body, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapGet("/{productId:guid}", async (
            HttpRequest request,
            Guid productId,
            CatalogProductQueryService queries,
            CatalogProductGovernanceAuthority governance,
            ICatalogGovernanceActorAccessor actorAccessor,
            ICatalogProductAvailabilityResolver availabilityResolver,
            ICatalogProductRepository products,
            ICatalogProductUnitRepository units,
            IEffectivePriceResolver effectivePrices,
            CatalogBranchStockResolver branchStock,
            BranchInventoryContextResolver branchContext,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var product = await queries.GetByIdAsync(organizationId, productId, ct).ConfigureAwait(false);
            if (product is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.ProductNotFound,
                    "Product was not found.",
                    StatusCodes.Status404NotFound);
            }

            var actor = actorAccessor.GetActor();
            if (!IsManagementVisible(actor, governance, product.Scope, product.OriginBranchId))
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.ProductNotFound,
                    "Product was not found.",
                    StatusCodes.Status404NotFound);
            }

            if (PosOrganizationScope.TryGetOptionalBranchId(request, out var actingBranchId)
                && actingBranchId is Guid branch
                && branch != Guid.Empty)
            {
                var orgId = Domain.Customers.PosOrganizationId.From(organizationId);
                var entity = await products
                    .GetByIdAsync(orgId, CatalogProductId.From(productId), ct)
                    .ConfigureAwait(false);
                if (entity is not null)
                {
                    var offering = await availabilityResolver
                        .ResolveForBranchAsync(
                            orgId,
                            Domain.Inventory.PosBranchId.From(branch),
                            [entity],
                            ct)
                        .ConfigureAwait(false);
                    if (offering.TryGetValue(productId, out var offer))
                    {
                        var enriched = await StampBranchCatalogContextAsync(
                                organizationId,
                                branch,
                                [product with { IsOfferedAtBranch = offer.IsOffered }],
                                products,
                                units,
                                effectivePrices,
                                branchStock,
                                branchContext,
                                ct)
                            .ConfigureAwait(false);
                        return Results.Ok(enriched[0]);
                    }
                }

                var priced = await StampBranchCatalogContextAsync(
                        organizationId,
                        branch,
                        [product],
                        products,
                        units,
                        effectivePrices,
                        branchStock,
                        branchContext,
                        ct)
                    .ConfigureAwait(false);
                return Results.Ok(priced[0]);
            }

            return Results.Ok(product);
        });

        group.MapGet("/{productId:guid}/branch-pricing", async (
            HttpRequest request,
            Guid productId,
            Guid branchId,
            GetBranchProductPricing useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (branchId == Guid.Empty)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.ProductBranchInvalid,
                    "Branch id must be a non-empty GUID.",
                    StatusCodes.Status400BadRequest);
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, productId, branchId, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapPut("/{productId:guid}/branch-pricing", async (
            HttpRequest request,
            Guid productId,
            SetBranchProductPriceOverrideRequest body,
            SetBranchProductPriceOverride useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, productId, body, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapDelete("/{productId:guid}/branch-pricing", async (
            HttpRequest request,
            Guid productId,
            Guid branchId,
            Guid? unitId,
            RemoveBranchProductPriceOverride useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (branchId == Guid.Empty)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.ProductBranchInvalid,
                    "Branch id must be a non-empty GUID.",
                    StatusCodes.Status400BadRequest);
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, productId, branchId, unitId, ct).ConfigureAwait(false),
                () => Results.NoContent());
        });

        group.MapGet("/{productId:guid}/branch-availability", async (
            HttpRequest request,
            Guid productId,
            QueryProductBranchAvailability useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, productId, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapPut("/{productId:guid}", async (
            HttpRequest request,
            Guid productId,
            UpdatePosCatalogProductRequest body,
            UpdateCatalogProduct useCase,
            CatalogProductQueryService queries,
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
                    body.BrandId,
                    body.ExpectedUpdatedAtUtc,
                    body.SellingMode,
                    body.TracksExpiration,
                    body.ExpirationWarningDays,
                    body.CanBePurchased,
                    body.CanBeSold,
                    body.CanBeUsedAsIngredient,
                    body.IsProduced,
                    body.UsagePreset,
                    body.Units,
                    body.CanExposeToConnectedBuyers,
                    body.DefaultConnectedPoPrice,
                    body.BusinessUsage,
                    ct)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return PosApiResults.Problem(
                    result.ErrorCode!,
                    result.ErrorMessage!,
                    PosApiResults.MapStatusCode(result.ErrorCode!),
                    result.ErrorDetails);
            }

            var dto = await queries
                .GetByIdAsync(organizationId, result.Value!.Id.Value, ct)
                .ConfigureAwait(false);
            return Results.Ok(dto);
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

        group.MapPost("/{productId:guid}/promote", async (
            HttpRequest request,
            Guid productId,
            PromoteCatalogProductToOrganizationStandard useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, productId, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapPut("/{productId:guid}/branches/{branchId:guid}/availability", async (
            HttpRequest request,
            Guid productId,
            Guid branchId,
            SetBranchProductAvailabilityRequest body,
            SetBranchProductAvailability useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase
                    .ExecuteAsync(organizationId, productId, branchId, body.IsOffered, ct)
                    .ConfigureAwait(false),
                Results.Ok);
        });

        group.MapGet("/by-sku/{sku}", async (
            HttpRequest request,
            string sku,
            bool? includeInactive,
            bool? commerciallyOffered,
            CatalogProductQueryService queries,
            CatalogProductGovernanceAuthority governance,
            ICatalogGovernanceActorAccessor actorAccessor,
            ICatalogProductAvailabilityResolver availability,
            ICatalogProductRepository products,
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
            return await FinalizeProductLookupAsync(
                    request,
                    organizationId,
                    result,
                    commerciallyOffered == true,
                    governance,
                    actorAccessor,
                    availability,
                    products,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/by-barcode/{barcode}", async (
            HttpRequest request,
            string barcode,
            bool? includeInactive,
            bool? commerciallyOffered,
            CatalogProductQueryService queries,
            CatalogProductGovernanceAuthority governance,
            ICatalogGovernanceActorAccessor actorAccessor,
            ICatalogProductAvailabilityResolver availability,
            ICatalogProductRepository products,
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
            return await FinalizeProductLookupAsync(
                    request,
                    organizationId,
                    result,
                    commerciallyOffered == true,
                    governance,
                    actorAccessor,
                    availability,
                    products,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPut("/{productId:guid}/image", async (
            HttpRequest request,
            Guid productId,
            SetCatalogProductImage useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var bytes = await ReadImageUploadAsync(request, ct).ConfigureAwait(false);
            if (!bytes.IsSuccess)
            {
                return PosApiResults.Problem(bytes.ErrorCode!, bytes.ErrorMessage!, PosApiResults.MapStatusCode(bytes.ErrorCode!));
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, productId, bytes.Value!, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapDelete("/{productId:guid}/image", async (
            HttpRequest request,
            Guid productId,
            RemoveCatalogProductImage useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, productId, ct).ConfigureAwait(false),
                Results.NoContent);
        });

        group.MapGet("/{productId:guid}/image/{variant}", async (
            HttpRequest request,
            Guid productId,
            string variant,
            GetCatalogProductImage useCase,
            CatalogProductQueryService queries,
            CatalogProductGovernanceAuthority governance,
            ICatalogGovernanceActorAccessor actorAccessor,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var product = await queries.GetByIdAsync(organizationId, productId, ct).ConfigureAwait(false);
            if (product is null
                || !IsManagementVisible(actorAccessor.GetActor(), governance, product.Scope, product.OriginBranchId))
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.ProductNotFound,
                    "Product was not found.",
                    StatusCodes.Status404NotFound);
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, productId, variant, ct).ConfigureAwait(false),
                image => PosApiResults.ImageFile(request.HttpContext.Response, image));
        });

        group.MapGet("/platform-products/{globalProductId:guid}/image/{variant}", async (
            HttpRequest request,
            Guid globalProductId,
            string variant,
            GetPlatformCatalogProductImage useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out _, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(globalProductId, variant, ct).ConfigureAwait(false),
                image => PosApiResults.ImageFile(request.HttpContext.Response, image));
        });
    }

    private static async Task<ApplicationResult<byte[]>> ReadImageUploadAsync(HttpRequest request, CancellationToken ct)
    {
        if (!request.HasFormContentType)
        {
            return ApplicationResult<byte[]>.Failure(
                ApplicationErrorCodes.ProductImageInvalid,
                "Upload a JPEG, PNG, or WebP image file.");
        }

        var form = await request.ReadFormAsync(ct).ConfigureAwait(false);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return ApplicationResult<byte[]>.Failure(
                ApplicationErrorCodes.ProductImageInvalid,
                "An image file is required.");
        }

        if (file.Length > ProductImageUploadLimits.MaxBytes)
        {
            return ApplicationResult<byte[]>.Failure(
                ApplicationErrorCodes.ProductImageTooLarge,
                "Image is too large. Use a file of 10 MB or less.");
        }

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct).ConfigureAwait(false);
        return ApplicationResult<byte[]>.Success(buffer.ToArray());
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

    private static bool IsManagementVisible(
        CatalogGovernanceActor actor,
        CatalogProductGovernanceAuthority governance,
        string? scope,
        Guid? originBranchId)
    {
        if (!string.Equals(scope, nameof(CatalogProductScope.BranchLocal), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var origin = originBranchId is Guid oid
            ? Domain.Inventory.PosBranchId.From(oid)
            : null;
        return governance.CanViewBranchLocalInManagement(actor, origin);
    }

    private static async Task<IReadOnlyList<PosCatalogProductDto>> StampEffectivePricesAsync(
        Guid organizationId,
        Guid branchId,
        IReadOnlyList<PosCatalogProductDto> items,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IEffectivePriceResolver effectivePrices,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return items;
        }

        var orgId = Domain.Customers.PosOrganizationId.From(organizationId);
        var productIds = items.Select(i => CatalogProductId.From(i.ProductId)).ToList();
        var loaded = await products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
        var unitsByProductRaw = await units
            .ListByProductIdsAsync(orgId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var unitsByProduct = unitsByProductRaw.ToDictionary(
            kvp => CatalogProductId.From(kvp.Key),
            kvp => kvp.Value);
        var resolved = await effectivePrices
            .ResolveAsync(
                orgId,
                Domain.Inventory.PosBranchId.From(branchId),
                loaded,
                unitsByProduct,
                cancellationToken)
            .ConfigureAwait(false);
        return CatalogEffectivePriceEnrichment.ApplyMany(items, resolved);
    }

    private static async Task<IReadOnlyList<PosCatalogProductDto>> StampBranchCatalogContextAsync(
        Guid organizationId,
        Guid branchId,
        IReadOnlyList<PosCatalogProductDto> items,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IEffectivePriceResolver effectivePrices,
        CatalogBranchStockResolver branchStock,
        BranchInventoryContextResolver branchContext,
        CancellationToken cancellationToken)
    {
        var priced = await StampEffectivePricesAsync(
                organizationId,
                branchId,
                items,
                products,
                units,
                effectivePrices,
                cancellationToken)
            .ConfigureAwait(false);
        if (priced.Count == 0)
        {
            return priced;
        }

        var contextResult = await branchContext
            .ResolveAsync(organizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        if (!contextResult.IsSuccess || contextResult.Value is null)
        {
            return priced;
        }

        var snapshots = await branchStock
            .ResolveAsync(contextResult.Value, priced, cancellationToken)
            .ConfigureAwait(false);
        return CatalogBranchStockEnrichment.ApplyMany(priced, snapshots);
    }

    private static async Task<IResult> FinalizeProductLookupAsync(
        HttpRequest request,
        Guid organizationId,
        ApplicationResult<PosCatalogProductDto> result,
        bool commerciallyOffered,
        CatalogProductGovernanceAuthority governance,
        ICatalogGovernanceActorAccessor actorAccessor,
        ICatalogProductAvailabilityResolver availability,
        ICatalogProductRepository products,
        CancellationToken ct)
    {
        if (!result.IsSuccess)
        {
            return PosApiResults.FromResult(result, Results.Ok);
        }

        var dto = result.Value!;
        var actor = actorAccessor.GetActor();
        if (!IsManagementVisible(actor, governance, dto.Scope, dto.OriginBranchId))
        {
            return PosApiResults.Problem(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.",
                StatusCodes.Status404NotFound);
        }

        if (!commerciallyOffered)
        {
            return Results.Ok(dto);
        }

        if (!PosOrganizationScope.TryGetOptionalBranchId(request, out var actingBranchId)
            || actingBranchId is null)
        {
            return PosApiResults.Problem(
                ApplicationErrorCodes.ProductActingBranchRequired,
                "X-Pos-Branch-Id is required for commercial product lookup.",
                StatusCodes.Status400BadRequest);
        }

        var orgId = Domain.Customers.PosOrganizationId.From(organizationId);
        var product = await products
            .GetByIdAsync(orgId, CatalogProductId.From(dto.ProductId), ct)
            .ConfigureAwait(false);
        if (product is null)
        {
            return PosApiResults.Problem(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.",
                StatusCodes.Status404NotFound);
        }

        var offering = await availability
            .ResolveForBranchAsync(
                orgId,
                Domain.Inventory.PosBranchId.From(actingBranchId.Value),
                [product],
                ct)
            .ConfigureAwait(false);
        if (!offering.TryGetValue(product.Id.Value, out var offer) || !offer.IsOffered)
        {
            return PosApiResults.Problem(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.",
                StatusCodes.Status404NotFound);
        }

        return Results.Ok(dto with { IsOfferedAtBranch = true });
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

    private static bool TryParseBrandStatus(
        string? status,
        out ProductBrandStatus? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<ProductBrandStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidBrandStatus,
                $"Unrecognized brand status '{status}'.",
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
