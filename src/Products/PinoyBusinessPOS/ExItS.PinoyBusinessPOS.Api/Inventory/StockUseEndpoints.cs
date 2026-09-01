using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

internal static class StockUseEndpoints
{
    public static IEndpointRouteBuilder MapStockUseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/inventory/stock-uses");

        group.MapGet("/", async (
            HttpRequest request,
            DateTimeOffset? fromOccurredAtUtc,
            DateTimeOffset? toOccurredAtUtc,
            string? reason,
            string? status,
            Guid? branchId,
            string? referenceNumber,
            int? page,
            int? pageSize,
            StockUseQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var filter = new StockUseFilter(
                fromOccurredAtUtc,
                toOccurredAtUtc,
                reason,
                status,
                branchId,
                referenceNumber);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/{stockUseId:guid}", async (
            HttpRequest request,
            Guid stockUseId,
            StockUseQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var stockUse = await queries.GetByIdAsync(organizationId, stockUseId, ct).ConfigureAwait(false);
            return stockUse is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.StockUseNotFound,
                    "Stock use was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(stockUse);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreateStockUseRequest body,
            CreateStockUse useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var branchResolved = InventoryBranchBodyResolver.ResolveMutationBranch(request, body.BranchId);
            if (!branchResolved.Success)
            {
                return branchResolved.Problem!;
            }

            if (branchResolved.BranchId is Guid branchId)
            {
                body = body with { BranchId = branchId };
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.StockUse,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, body, actorId, ct2),
                    dto => dto,
                    dto => Results.Created($"/api/v1/pos/inventory/stock-uses/{dto.StockUseId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/{stockUseId:guid}/void", async (
            HttpRequest request,
            Guid stockUseId,
            VoidStockUse useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, stockUseId, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
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
