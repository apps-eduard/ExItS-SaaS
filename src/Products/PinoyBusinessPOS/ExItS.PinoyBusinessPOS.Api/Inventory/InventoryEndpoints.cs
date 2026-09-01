using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

/// <summary>
/// Organization-scoped inventory endpoints (P8-WP04 basic + P10-WP03 advanced). Online-only.
/// </summary>
internal static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/inventory");

        group.MapGet("/", ListInventory);
        group.MapGet("/low-stock", ListLowStock);
        group.MapGet("/reorder-suggestions", ListReorderSuggestions);
        group.MapGet("/lots", ListExpiringLots);
        group.MapGet("/movements/{movementId:guid}", GetMovementById);
        MapStockCounts(group);
        InventoryTransferEndpoints.Map(group);

        group.MapGet("/{productId:guid}", GetByProduct);
        group.MapPut("/{productId:guid}/reorder", SetReorder);
        group.MapGet("/{productId:guid}/reconciliation", GetReconciliation);
        group.MapGet("/{productId:guid}/organization-summary", GetOrganizationSummary);
        group.MapGet("/physical-audit", GetPhysicalAudit);
        group.MapPost("/{productId:guid}/enable", Enable);
        group.MapPost("/{productId:guid}/opening-stock", AddOpeningStock);
        group.MapPost("/{productId:guid}/disable", Disable);
        group.MapPost("/{productId:guid}/adjustments", Adjust);
        group.MapPost("/products/{productId:guid}/expiration-tracking/enable", EnableExpirationTracking);
        group.MapGet("/{productId:guid}/lots", ListLots);
        group.MapGet("/{productId:guid}/movements", ListMovements);

        return app;
    }

    private static void MapStockCounts(RouteGroupBuilder group)
    {
        group.MapGet("/stock-counts", async (
            HttpRequest request,
            string? status,
            string? countNumber,
            int? page,
            int? pageSize,
            StockCountQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var filter = new StockCountFilter(status, countNumber);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/stock-counts", async (
            HttpRequest request,
            CreateStockCountRequest body,
            CreateStockCount useCase,
            StockCountQueryService queries,
            BranchInventoryContextResolver branchResolver,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
            if (!branchResolved.Success)
            {
                return branchResolved.Problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, body, actorId, branchResolved.Context!.BranchId, ct)
                .ConfigureAwait(false);
            return await FromStockCountResultAsync(organizationId, result, queries, ct).ConfigureAwait(false);
        });

        group.MapGet("/stock-counts/{stockCountId:guid}", async (
            HttpRequest request,
            Guid stockCountId,
            StockCountQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var dto = await queries.GetByIdAsync(organizationId, stockCountId, ct).ConfigureAwait(false);
            return dto is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.StockCountNotFound,
                    "Stock count was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(dto);
        });

        group.MapPut("/stock-counts/{stockCountId:guid}", async (
            HttpRequest request,
            Guid stockCountId,
            UpdateStockCountRequest body,
            UpdateStockCountDraft draftUseCase,
            UpdateStockCountInProgress inProgressUseCase,
            StockCountQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var existing = await queries.GetByIdAsync(organizationId, stockCountId, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.StockCountNotFound,
                    "Stock count was not found.",
                    StatusCodes.Status404NotFound);
            }

            ApplicationResult<StockCount> result = string.Equals(existing.Status, nameof(StockCountStatus.InProgress), StringComparison.OrdinalIgnoreCase)
                ? await inProgressUseCase.ExecuteAsync(organizationId, stockCountId, body, ct).ConfigureAwait(false)
                : await draftUseCase.ExecuteAsync(organizationId, stockCountId, body, ct).ConfigureAwait(false);
            return await FromStockCountResultAsync(organizationId, result, queries, ct).ConfigureAwait(false);
        });

        group.MapPost("/stock-counts/{stockCountId:guid}/start", async (
            HttpRequest request,
            Guid stockCountId,
            StartStockCount useCase,
            StockCountQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, stockCountId, actorId, ct).ConfigureAwait(false);
            return await FromStockCountResultAsync(organizationId, result, queries, ct).ConfigureAwait(false);
        });

        group.MapPost("/stock-counts/{stockCountId:guid}/complete", async (
            HttpRequest request,
            Guid stockCountId,
            CompleteStockCount useCase,
            StockCountQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, stockCountId, actorId, ct).ConfigureAwait(false);
            return await FromStockCountResultAsync(organizationId, result, queries, ct).ConfigureAwait(false);
        });

        group.MapPost("/stock-counts/{stockCountId:guid}/cancel", async (
            HttpRequest request,
            Guid stockCountId,
            CancelStockCount useCase,
            StockCountQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, stockCountId, actorId, ct).ConfigureAwait(false);
            return await FromStockCountResultAsync(organizationId, result, queries, ct).ConfigureAwait(false);
        });
    }

    private static async Task<IResult> ListInventory(
        HttpRequest request,
        string? search,
        bool? tracked,
        bool? lowStock,
        string? productStatus,
        int? page,
        int? pageSize,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var filter = new InventoryAccountFilter(search, tracked, lowStock, ProductStatus: productStatus);
        var result = await queries.ListAsync(branchResolved.Context!, filter, page, pageSize, ct).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> ListLowStock(
        HttpRequest request,
        string? search,
        int? page,
        int? pageSize,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await queries.ListLowStockAsync(branchResolved.Context!, search, page, pageSize, ct).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> ListReorderSuggestions(
        HttpRequest request,
        string? search,
        int? page,
        int? pageSize,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await queries.ListReorderSuggestionsAsync(branchResolved.Context!, search, page, pageSize, ct).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetByProduct(
        HttpRequest request,
        Guid productId,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var dto = await queries.GetByProductIdAsync(organizationId, productId, branchResolved.Context!, ct).ConfigureAwait(false);
        return dto is null
            ? PosApiResults.Problem(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.",
                StatusCodes.Status404NotFound)
            : Results.Ok(dto);
    }

    private static async Task<IResult> ListExpiringLots(
        HttpRequest request,
        string? window,
        string? fromDate,
        string? toDate,
        string? search,
        int? page,
        int? pageSize,
        InventoryLotQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        if (!TryParseDate(fromDate, out var from, out problem) || !TryParseDate(toDate, out var to, out problem))
        {
            return problem!;
        }

        var result = await queries
            .ListExpiringAsync(organizationId, branchResolved.Context!.BranchId, window, from, to, search, page, pageSize, ct)
            .ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> ListLots(
        HttpRequest request,
        Guid productId,
        bool? includeDepleted,
        int? page,
        int? pageSize,
        InventoryLotQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await queries
            .ListAsync(organizationId, productId, includeDepleted ?? false, page, pageSize, branchResolved.Context!.BranchId, ct)
            .ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> SetReorder(
        HttpRequest request,
        Guid productId,
        SetInventoryReorderRequest body,
        SetInventoryReorderConfiguration useCase,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await useCase
            .ExecuteAsync(
                organizationId,
                branchResolved.Context!.BranchId,
                productId,
                body.ReorderLevel,
                body.ReorderQuantity,
                body.Reason,
                actorId,
                ct)
            .ConfigureAwait(false);
        return await FromAccountResultAsync(organizationId, productId, branchResolved.Context!, result, queries, ct).ConfigureAwait(false);
    }

    private static async Task<IResult> GetReconciliation(
        HttpRequest request,
        Guid productId,
        InventoryReconciliationQuery query,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var result = await query.GetAsync(organizationId, productId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetOrganizationSummary(
        HttpRequest request,
        Guid productId,
        OrganizationInventoryQuery query,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var result = await query.GetProductAsync(organizationId, productId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetPhysicalAudit(
        HttpRequest request,
        IInventoryPhysicalAudit audit,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var result = await audit.AuditAsync(organizationId, ct).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> Enable(
        HttpRequest request,
        Guid productId,
        EnableInventoryTrackingRequest? body,
        EnableInventoryTracking useCase,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        body ??= new EnableInventoryTrackingRequest();
        var result = await useCase
            .ExecuteAsync(
                organizationId,
                productId,
                actorId,
                branchResolved.Context!.BranchId,
                body.OpeningQuantity,
                body.ReorderLevel,
                body.ExpirationDate,
                body.LotNumber,
                body.UnitCost,
                ct)
            .ConfigureAwait(false);
        return await FromAccountResultAsync(organizationId, productId, branchResolved.Context!, result, queries, ct).ConfigureAwait(false);
    }

    private static async Task<IResult> AddOpeningStock(
        HttpRequest request,
        Guid productId,
        AddOpeningStockRequest body,
        AddOpeningStock useCase,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await useCase
            .ExecuteAsync(
                organizationId,
                productId,
                actorId,
                branchResolved.Context!.BranchId,
                body.OpeningQuantity,
                body.UnitCost,
                body.ExpirationDate,
                body.LotNumber,
                ct)
            .ConfigureAwait(false);
        return await FromAccountResultAsync(organizationId, productId, branchResolved.Context!, result, queries, ct).ConfigureAwait(false);
    }

    private static async Task<IResult> Disable(
        HttpRequest request,
        Guid productId,
        DisableInventoryTracking useCase,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await useCase.ExecuteAsync(organizationId, productId, ct).ConfigureAwait(false);
        return await FromAccountResultAsync(organizationId, productId, branchResolved.Context!, result, queries, ct).ConfigureAwait(false);
    }

    private static async Task<IResult> Adjust(
        HttpRequest request,
        Guid productId,
        AdjustInventoryRequest body,
        AdjustInventoryStock useCase,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosIdempotencyService idempotency,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var context = branchResolved.Context!;
        return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                request,
                organizationId,
                OfflineOperationTypes.InventoryAdjustment,
                idempotency,
                ct2 => ToAccountDtoAsync(
                    useCase.ExecuteAsync(
                        organizationId,
                        productId,
                        body.Direction,
                        body.Quantity,
                        body.Reason,
                        actorId,
                        body.ReorderLevel,
                        context.BranchId,
                        body.ExpirationDate,
                        body.LotNumber,
                        body.LotId,
                        body.ProductUnitId,
                        body.MovementId,
                        ct2),
                    organizationId,
                    productId,
                    context,
                    queries,
                    ct2),
                dto => dto,
                Results.Ok,
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> EnableExpirationTracking(
        HttpRequest request,
        Guid productId,
        EnableExpirationTrackingRequest? body,
        EnableExpirationTracking useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
        {
            return problem!;
        }

        body ??= new EnableExpirationTrackingRequest();
        PosOrganizationScope.TryGetOptionalBranchId(request, out var branchId);
        var result = await useCase
            .ExecuteAsync(
                organizationId,
                productId,
                actorId,
                body.ExpirationWarningDays,
                body.ExistingStockLots,
                body.ExpectedOnHandQuantity,
                branchId,
                ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetMovementById(
        HttpRequest request,
        Guid movementId,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var movement = await queries.GetMovementByIdAsync(organizationId, movementId, branchResolved.Context!, ct).ConfigureAwait(false);
        return movement is null
            ? PosApiResults.Problem(
                ApplicationErrorCodes.InventoryMovementNotFound,
                "Stock movement was not found.",
                StatusCodes.Status404NotFound)
            : Results.Ok(movement);
    }

    private static async Task<IResult> ListMovements(
        HttpRequest request,
        Guid productId,
        string? movementType,
        string? sourceType,
        string? fromDateUtc,
        string? toDateUtc,
        int? page,
        int? pageSize,
        InventoryQueryService queries,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveInventoryBranchAsync(request, organizationId, branchResolver, ct).ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        if (!TryParseDate(fromDateUtc, out var fromDate, out problem)
            || !TryParseDate(toDateUtc, out var toDate, out problem))
        {
            return problem!;
        }

        var product = await queries.GetByProductIdAsync(organizationId, productId, branchResolved.Context!, ct).ConfigureAwait(false);
        if (product is null)
        {
            return PosApiResults.Problem(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.",
                StatusCodes.Status404NotFound);
        }

        var filter = new StockMovementFilter(movementType, sourceType, fromDate, toDate);
        var result = await queries
            .ListMovementsAsync(organizationId, productId, branchResolved.Context!, filter, page, pageSize, ct)
            .ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> FromAccountResultAsync(
        Guid organizationId,
        Guid productId,
        BranchInventoryContext context,
        ApplicationResult<InventoryAccount> result,
        InventoryQueryService queries,
        CancellationToken ct)
    {
        if (!result.IsSuccess)
        {
            return PosApiResults.Problem(
                result.ErrorCode!,
                result.ErrorMessage!,
                PosApiResults.MapStatusCode(result.ErrorCode!));
        }

        var dto = await queries.GetByProductIdAsync(organizationId, productId, context, ct).ConfigureAwait(false);
        return dto is null
            ? PosApiResults.Problem(
                ApplicationErrorCodes.InventoryAccountNotFound,
                "Inventory account was not found.",
                StatusCodes.Status404NotFound)
            : Results.Ok(dto);
    }

    private static async Task<ApplicationResult<PosInventoryAccountDto>> ToAccountDtoAsync(
        Task<ApplicationResult<InventoryAccount>> execute,
        Guid organizationId,
        Guid productId,
        BranchInventoryContext context,
        InventoryQueryService queries,
        CancellationToken ct)
    {
        var result = await execute.ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ApplicationResult<PosInventoryAccountDto>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var dto = await queries.GetByProductIdAsync(organizationId, productId, context, ct).ConfigureAwait(false);
        return dto is null
            ? ApplicationResult<PosInventoryAccountDto>.Failure(
                ApplicationErrorCodes.InventoryAccountNotFound,
                "Inventory account was not found.")
            : ApplicationResult<PosInventoryAccountDto>.Success(dto);
    }

    private static async Task<(bool Success, BranchInventoryContext? Context, IResult? Problem)> ResolveInventoryBranchAsync(
        HttpRequest request,
        Guid organizationId,
        BranchInventoryContextResolver resolver,
        CancellationToken ct)
    {
        if (!PosOrganizationScope.TryGetOptionalBranchId(request, out var branchId) || branchId is null)
        {
            return (false, null, PosApiResults.Problem(
                ApplicationErrorCodes.InventoryBranchRequired,
                "Header 'X-Pos-Branch-Id' is required for branch inventory.",
                StatusCodes.Status400BadRequest));
        }

        var resolved = await resolver.ResolveAsync(organizationId, branchId.Value, ct).ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return (false, null, PosApiResults.Problem(
                resolved.ErrorCode!,
                resolved.ErrorMessage!,
                PosApiResults.MapStatusCode(resolved.ErrorCode!)));
        }

        return (true, resolved.Value, null);
    }

    private static async Task<IResult> FromStockCountResultAsync(
        Guid organizationId,
        ApplicationResult<StockCount> result,
        StockCountQueryService queries,
        CancellationToken ct)
    {
        if (!result.IsSuccess)
        {
            return PosApiResults.Problem(
                result.ErrorCode!,
                result.ErrorMessage!,
                PosApiResults.MapStatusCode(result.ErrorCode!));
        }

        var dto = await queries.GetByIdAsync(organizationId, result.Value!.Id.Value, ct).ConfigureAwait(false);
        return dto is null
            ? PosApiResults.Problem(
                ApplicationErrorCodes.StockCountNotFound,
                "Stock count was not found.",
                StatusCodes.Status404NotFound)
            : Results.Ok(dto);
    }

    private static bool TryParseDate(string? value, out DateOnly? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (DateOnly.TryParse(value, out var date))
        {
            parsed = date;
            return true;
        }

        problem = PosApiResults.Problem(
            ApplicationErrorCodes.DomainViolation,
            $"Invalid date '{value}'. Use ISO date (yyyy-MM-dd).",
            StatusCodes.Status400BadRequest);
        return false;
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
}
