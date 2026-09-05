using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

internal static class StockRequestEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/supply-routes", ListSupplyRoutes);
        group.MapGet("/supply-routes/by-destination/{destinationLocationId:guid}", ListSupplyRoutesByDestination);
        group.MapPut("/supply-routes/by-destination/{destinationLocationId:guid}", UpsertSupplyRoutesForDestination);
        group.MapPost("/supply-routes/by-destination/{destinationLocationId:guid}/preferred", SetPreferredRoute);
        group.MapPost("/supply-routes/{routeId:guid}/disable", DisableRoute);

        group.MapGet("/stock-requests/outgoing", ListOutgoing);
        group.MapGet("/stock-requests/incoming", ListIncoming);
        group.MapGet("/stock-requests/{stockRequestId:guid}", GetStockRequest);
        group.MapPost("/stock-requests", CreateStockRequest);
        group.MapPost("/stock-requests/{stockRequestId:guid}/reject", RejectStockRequest);
        group.MapPost("/stock-requests/{stockRequestId:guid}/cancel", CancelStockRequest);
        group.MapPost("/stock-requests/{stockRequestId:guid}/fulfill-transfer", FulfillViaTransfer);
    }

    private static async Task<IResult> ListSupplyRoutes(
        HttpRequest request,
        SupplyRouteQueryService queries,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var items = await queries.ListAllAsync(organizationId, ct).ConfigureAwait(false);
        return Results.Ok(items);
    }

    private static async Task<IResult> ListSupplyRoutesByDestination(
        HttpRequest request,
        Guid destinationLocationId,
        SupplyRouteQueryService queries,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var items = await queries.ListByDestinationAsync(organizationId, destinationLocationId, ct).ConfigureAwait(false);
        return Results.Ok(items);
    }

    private static async Task<IResult> UpsertSupplyRoutesForDestination(
        HttpRequest request,
        Guid destinationLocationId,
        UpsertSupplyRoutesRequest body,
        UpsertSupplyRoutes useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var payload = body with { DestinationLocationId = destinationLocationId };
        var result = await useCase.ExecuteAsync(organizationId, payload, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> SetPreferredRoute(
        HttpRequest request,
        Guid destinationLocationId,
        SetPreferredSupplyRouteRequest body,
        SetPreferredSupplyRoute useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var result = await useCase.ExecuteAsync(organizationId, destinationLocationId, body, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> DisableRoute(
        HttpRequest request,
        Guid routeId,
        DisableSupplyRoute useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var result = await useCase.ExecuteAsync(organizationId, routeId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> ListOutgoing(
        HttpRequest request,
        int? page,
        int? pageSize,
        StockRequestQueryService queries,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetBranchId(request, out var branchId, out problem))
        {
            return problem!;
        }

        var result = await queries.ListOutgoingAsync(organizationId, branchId, page, pageSize, ct).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> ListIncoming(
        HttpRequest request,
        int? page,
        int? pageSize,
        StockRequestQueryService queries,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetBranchId(request, out var branchId, out problem))
        {
            return problem!;
        }

        var result = await queries.ListIncomingAsync(organizationId, branchId, page, pageSize, ct).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetStockRequest(
        HttpRequest request,
        Guid stockRequestId,
        StockRequestQueryService queries,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var dto = await queries.GetByIdAsync(organizationId, stockRequestId, ct).ConfigureAwait(false);
        return dto is null
            ? PosApiResults.Problem(
                "pos.inventory.stock_request.not_found",
                "Stock request was not found.",
                StatusCodes.Status404NotFound)
            : Results.Ok(dto);
    }

    private static async Task<IResult> CreateStockRequest(
        HttpRequest request,
        CreateStockRequestRequest body,
        CreateStockRequest useCase,
        IPosIdempotencyService idempotency,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem)
            || !PosOrganizationScope.TryGetBranchId(request, out var branchId, out problem))
        {
            return problem!;
        }

        return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                request,
                organizationId,
                OfflineOperationTypes.StockRequestCreate,
                idempotency,
                ct2 => useCase.ExecuteAsync(organizationId, body, actorId, branchId, ct2),
                dto => dto,
                dto => Results.Created($"/api/v1/pos/inventory/stock-requests/{dto.StockRequestId:D}", dto),
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> RejectStockRequest(
        HttpRequest request,
        Guid stockRequestId,
        RejectStockRequestRequest body,
        RejectStockRequest useCase,
        IPosIdempotencyService idempotency,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem)
            || !PosOrganizationScope.TryGetBranchId(request, out var branchId, out problem))
        {
            return problem!;
        }

        return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                request,
                organizationId,
                OfflineOperationTypes.StockRequestReject,
                idempotency,
                ct2 => useCase.ExecuteAsync(organizationId, stockRequestId, body, actorId, branchId, ct2),
                dto => dto,
                Results.Ok,
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> CancelStockRequest(
        HttpRequest request,
        Guid stockRequestId,
        CancelStockRequest useCase,
        IPosIdempotencyService idempotency,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem)
            || !PosOrganizationScope.TryGetBranchId(request, out var branchId, out problem))
        {
            return problem!;
        }

        return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                request,
                organizationId,
                OfflineOperationTypes.StockRequestCancel,
                idempotency,
                ct2 => useCase.ExecuteAsync(organizationId, stockRequestId, actorId, branchId, ct2),
                dto => dto,
                Results.Ok,
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> FulfillViaTransfer(
        HttpRequest request,
        Guid stockRequestId,
        FulfillStockRequestViaTransferRequest body,
        FulfillStockRequestViaTransfer useCase,
        InventoryTransferQueryService transferQueries,
        IPosIdempotencyService idempotency,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem)
            || !PosOrganizationScope.TryGetBranchId(request, out var branchId, out problem))
        {
            return problem!;
        }

        return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                request,
                organizationId,
                OfflineOperationTypes.StockRequestFulfillTransfer,
                idempotency,
                ct2 => useCase.ExecuteAsync(organizationId, stockRequestId, body, actorId, branchId, transferQueries, ct2),
                dto => dto,
                dto => Results.Created($"/api/v1/pos/inventory/transfers/{dto.TransferId:D}", dto),
                ct)
            .ConfigureAwait(false);
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
