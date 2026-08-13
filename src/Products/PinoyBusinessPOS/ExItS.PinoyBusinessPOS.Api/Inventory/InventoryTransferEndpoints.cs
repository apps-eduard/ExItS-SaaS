using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

internal static class InventoryTransferEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/transfers", ListTransfers);
        group.MapPost("/transfers", CreateTransfer);
        group.MapGet("/transfers/{transferId:guid}", GetTransfer);
        group.MapPost("/transfers/{transferId:guid}/dispatch", DispatchTransfer);
        group.MapPost("/transfers/{transferId:guid}/receive", ReceiveTransfer);
        group.MapPost("/transfers/{transferId:guid}/cancel", CancelTransfer);
    }

    private static async Task<IResult> ListTransfers(
        HttpRequest request,
        string? status,
        string? transferNumber,
        string? direction,
        Guid? sourceBranchId,
        Guid? destinationBranchId,
        int? page,
        int? pageSize,
        InventoryTransferQueryService queries,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        PosOrganizationScope.TryGetOptionalBranchId(request, out var actingBranch);
        var filter = new InventoryTransferFilter(
            status,
            transferNumber,
            sourceBranchId ?? (IsOutgoing(direction) ? actingBranch : null),
            destinationBranchId ?? (IsIncoming(direction) ? actingBranch : null),
            direction,
            actingBranch);
        var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTransfer(
        HttpRequest request,
        Guid transferId,
        InventoryTransferQueryService queries,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var dto = await queries.GetByIdAsync(organizationId, transferId, ct).ConfigureAwait(false);
        return dto is null
            ? PosApiResults.Problem(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Inventory transfer was not found.",
                StatusCodes.Status404NotFound)
            : Results.Ok(dto);
    }

    private static async Task<IResult> CreateTransfer(
        HttpRequest request,
        CreateInventoryTransferRequest body,
        CreateInventoryTransfer useCase,
        InventoryTransferQueryService queries,
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
                OfflineOperationTypes.InventoryTransferCreate,
                idempotency,
                ct2 => ToDtoAsync(
                    useCase.ExecuteAsync(organizationId, body, actorId, branchId, ct2),
                    organizationId,
                    queries,
                    ct2),
                dto => dto,
                dto => Results.Created($"/api/v1/pos/inventory/transfers/{dto.TransferId:D}", dto),
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> DispatchTransfer(
        HttpRequest request,
        Guid transferId,
        DispatchInventoryTransfer useCase,
        InventoryTransferQueryService queries,
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
                OfflineOperationTypes.InventoryTransferDispatch,
                idempotency,
                ct2 => ToDtoAsync(
                    useCase.ExecuteAsync(organizationId, transferId, actorId, branchId, ct2),
                    organizationId,
                    queries,
                    ct2),
                dto => dto,
                Results.Ok,
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> ReceiveTransfer(
        HttpRequest request,
        Guid transferId,
        ReceiveInventoryTransferRequest body,
        ReceiveInventoryTransfer useCase,
        InventoryTransferQueryService queries,
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
                OfflineOperationTypes.InventoryTransferReceive,
                idempotency,
                ct2 => ToDtoAsync(
                    useCase.ExecuteAsync(organizationId, transferId, body, actorId, branchId, ct2),
                    organizationId,
                    queries,
                    ct2),
                dto => dto,
                Results.Ok,
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> CancelTransfer(
        HttpRequest request,
        Guid transferId,
        CancelInventoryTransfer useCase,
        InventoryTransferQueryService queries,
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
                OfflineOperationTypes.InventoryTransferCancel,
                idempotency,
                ct2 => ToDtoAsync(
                    useCase.ExecuteAsync(organizationId, transferId, actorId, branchId, ct2),
                    organizationId,
                    queries,
                    ct2),
                dto => dto,
                Results.Ok,
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<ApplicationResult<InventoryTransferDto>> ToDtoAsync(
        Task<ApplicationResult<InventoryTransfer>> execute,
        Guid organizationId,
        InventoryTransferQueryService queries,
        CancellationToken ct)
    {
        var result = await execute.ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var dto = await queries.GetByIdAsync(organizationId, result.Value!.Id.Value, ct).ConfigureAwait(false);
        return dto is null
            ? ApplicationResult<InventoryTransferDto>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Inventory transfer was not found.")
            : ApplicationResult<InventoryTransferDto>.Success(dto);
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

    private static bool IsOutgoing(string? direction) =>
        string.Equals(direction, "outgoing", StringComparison.OrdinalIgnoreCase);

    private static bool IsIncoming(string? direction) =>
        string.Equals(direction, "incoming", StringComparison.OrdinalIgnoreCase);
}
