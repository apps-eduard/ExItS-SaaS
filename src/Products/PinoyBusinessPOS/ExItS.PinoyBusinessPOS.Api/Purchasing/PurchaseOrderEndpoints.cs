using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Api.Purchasing;

/// <summary>
/// Organization-scoped purchasing endpoints (P10-WP02). Online-only — no offline purchasing queue.
/// </summary>
internal static class PurchaseOrderEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        MapPurchaseOrders(app.MapGroup("/api/v1/pos/purchase-orders"));
        MapGoodsReceipts(app.MapGroup("/api/v1/pos/goods-receipts"));
        return app;
    }

    private static void MapPurchaseOrders(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            Guid? supplierId,
            string? poNumber,
            string? fromOrderDate,
            string? toOrderDate,
            int? page,
            int? pageSize,
            PurchaseOrderQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPurchasing, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out var parsedStatus, out problem)
                || !TryParseDate(fromOrderDate, "fromOrderDate", out var parsedFrom, out problem)
                || !TryParseDate(toOrderDate, "toOrderDate", out var parsedTo, out problem))
            {
                return problem!;
            }

            var filter = new PurchaseOrderFilter(parsedStatus, supplierId, poNumber, parsedFrom, parsedTo);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreatePurchaseOrderRequest body,
            CreatePurchaseOrder useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManagePurchasing, out var organizationId, out var problem)
                || !DenyInventoryStaffPoMutation(out problem))
            {
                return problem!;
            }

            PosOrganizationScope.TryGetActorId(request, out var actorId, out _);
            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.PurchaseOrderCreate,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, body, ct2, actorId),
                    dto => dto,
                    dto => Results.Created($"/api/v1/pos/purchase-orders/{dto.PurchaseOrderId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/{purchaseOrderId:guid}", async (
            HttpRequest request,
            Guid purchaseOrderId,
            PurchaseOrderQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPurchasing, out var organizationId, out var problem))
            {
                return problem!;
            }

            var po = await queries.GetByIdAsync(organizationId, purchaseOrderId, ct).ConfigureAwait(false);
            return po is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.PurchaseOrderNotFound,
                    "Purchase order was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(po);
        });

        group.MapPut("/{purchaseOrderId:guid}", async (
            HttpRequest request,
            Guid purchaseOrderId,
            UpdatePurchaseOrderRequest body,
            UpdatePurchaseOrder useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManagePurchasing, out var organizationId, out var problem)
                || !DenyInventoryStaffPoMutation(out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, purchaseOrderId, body, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{purchaseOrderId:guid}/submit", async (
            HttpRequest request,
            Guid purchaseOrderId,
            SubmitPurchaseOrder useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManagePurchasing, out var organizationId, out var problem)
                || !DenyInventoryStaffPoMutation(out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.PurchaseOrderSubmit,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, purchaseOrderId, actorId, ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/{purchaseOrderId:guid}/cancel", async (
            HttpRequest request,
            Guid purchaseOrderId,
            CancelPurchaseOrder useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManagePurchasing, out var organizationId, out var problem)
                || !DenyInventoryStaffPoMutation(out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, purchaseOrderId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{purchaseOrderId:guid}/accept-changes", async (
            HttpRequest request,
            Guid purchaseOrderId,
            AcceptConnectedPoChanges useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManagePurchasing, out var organizationId, out var problem)
                || !DenyInventoryStaffPoMutation(out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, purchaseOrderId, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{purchaseOrderId:guid}/receive", async (
            HttpRequest request,
            Guid purchaseOrderId,
            ReceivePurchaseOrderRequest body,
            ReceivePurchaseOrder useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManagePurchasing, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.PurchaseOrderReceive,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, purchaseOrderId, body, actorId, ct2),
                    dto => dto,
                    dto => Results.Created($"/api/v1/pos/goods-receipts/{dto.GoodsReceiptId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });
    }

    private static void MapGoodsReceipts(RouteGroupBuilder group)
    {
        group.MapGet("/{goodsReceiptId:guid}", async (
            HttpRequest request,
            Guid goodsReceiptId,
            GoodsReceiptQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPurchasing, out var organizationId, out var problem))
            {
                return problem!;
            }

            var receipt = await queries.GetByIdAsync(organizationId, goodsReceiptId, ct).ConfigureAwait(false);
            return receipt is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.GoodsReceiptNotFound,
                    "Goods receipt was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(receipt);
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

    private static bool DenyInventoryStaffPoMutation(out IResult? problem)
    {
        problem = null;
        if (PosRoleRequestContext.CurrentRole == PosRole.InventoryStaff)
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.PosRoleDenied,
                "InventoryStaff may receive purchase orders but cannot create, edit, submit, or cancel them.",
                StatusCodes.Status403Forbidden);
            return false;
        }

        return true;
    }

    private static bool TryParseStatus(
        string? status,
        out PurchaseOrderStatus? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<PurchaseOrderStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidPurchaseOrderStatus,
                $"Unrecognized purchase order status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParseDate(
        string? value,
        string paramName,
        out DateOnly? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateOnly.TryParse(value, out var date))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.DomainViolation,
                $"Invalid {paramName} '{value}'. Use ISO date (yyyy-MM-dd).",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = date;
        return true;
    }
}
