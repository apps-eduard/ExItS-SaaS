using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

/// <summary>
/// Organization-scoped basic inventory endpoints (P8-WP04). Development-stage only: organization
/// scope from <c>X-Pos-Organization-Id</c>, actor from <c>X-Dev-Platform-User-Id</c>. Online-only —
/// no offline inventory queue, suppliers, warehouses, or costing surface.
/// </summary>
internal static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/inventory");

        group.MapGet("/", async (
            HttpRequest request,
            string? search,
            bool? tracked,
            bool? lowStock,
            string? productStatus,
            int? page,
            int? pageSize,
            InventoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var filter = new InventoryAccountFilter(search, tracked, lowStock, productStatus);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/low-stock", async (
            HttpRequest request,
            string? search,
            int? page,
            int? pageSize,
            InventoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await queries
                .ListLowStockAsync(organizationId, search, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/{productId:guid}", async (
            HttpRequest request,
            Guid productId,
            InventoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var dto = await queries.GetByProductIdAsync(organizationId, productId, ct).ConfigureAwait(false);
            return dto is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.InventoryProductNotFound,
                    "Product was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(dto);
        });

        group.MapPost("/{productId:guid}/enable", async (
            HttpRequest request,
            Guid productId,
            EnableInventoryTrackingRequest? body,
            EnableInventoryTracking useCase,
            InventoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            body ??= new EnableInventoryTrackingRequest();
            var result = await useCase
                .ExecuteAsync(organizationId, productId, actorId, body.OpeningQuantity, body.ReorderLevel, ct)
                .ConfigureAwait(false);
            return await FromAccountResultAsync(organizationId, productId, result, queries, ct).ConfigureAwait(false);
        });

        group.MapPost("/{productId:guid}/disable", async (
            HttpRequest request,
            Guid productId,
            DisableInventoryTracking useCase,
            InventoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, productId, ct).ConfigureAwait(false);
            return await FromAccountResultAsync(organizationId, productId, result, queries, ct).ConfigureAwait(false);
        });

        group.MapPost("/{productId:guid}/adjustments", async (
            HttpRequest request,
            Guid productId,
            AdjustInventoryRequest body,
            AdjustInventoryStock useCase,
            InventoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(
                    organizationId,
                    productId,
                    body.Direction,
                    body.Quantity,
                    body.Reason,
                    actorId,
                    body.ReorderLevel,
                    ct)
                .ConfigureAwait(false);
            return await FromAccountResultAsync(organizationId, productId, result, queries, ct).ConfigureAwait(false);
        });

        group.MapGet("/{productId:guid}/movements", async (
            HttpRequest request,
            Guid productId,
            int? page,
            int? pageSize,
            InventoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var product = await queries.GetByProductIdAsync(organizationId, productId, ct).ConfigureAwait(false);
            if (product is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.InventoryProductNotFound,
                    "Product was not found.",
                    StatusCodes.Status404NotFound);
            }

            var result = await queries
                .ListMovementsAsync(organizationId, productId, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }

    private static async Task<IResult> FromAccountResultAsync(
        Guid organizationId,
        Guid productId,
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

        var dto = await queries.GetByProductIdAsync(organizationId, productId, ct).ConfigureAwait(false);
        return dto is null
            ? PosApiResults.Problem(
                ApplicationErrorCodes.InventoryAccountNotFound,
                "Inventory account was not found.",
                StatusCodes.Status404NotFound)
            : Results.Ok(dto);
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
