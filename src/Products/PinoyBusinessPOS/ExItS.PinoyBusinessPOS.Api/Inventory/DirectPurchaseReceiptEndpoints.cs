using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

internal static class DirectPurchaseReceiptEndpoints
{
    public static IEndpointRouteBuilder MapDirectPurchaseReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/direct-purchase-receipts");

        group.MapGet("/", async (
            HttpRequest request,
            string? fromPurchaseDate,
            string? toPurchaseDate,
            Guid? supplierId,
            string? sourceSearch,
            string? referenceNumber,
            int? page,
            int? pageSize,
            DirectPurchaseReceiptQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem)
                || !TryParseDate(fromPurchaseDate, "fromPurchaseDate", out var from, out problem)
                || !TryParseDate(toPurchaseDate, "toPurchaseDate", out var to, out problem))
            {
                return problem!;
            }

            var filter = new DirectPurchaseReceiptFilter(from, to, supplierId, sourceSearch, referenceNumber);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/{receiptId:guid}", async (
            HttpRequest request,
            Guid receiptId,
            DirectPurchaseReceiptQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var receipt = await queries.GetByIdAsync(organizationId, receiptId, ct).ConfigureAwait(false);
            return receipt is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.DirectPurchaseReceiptNotFound,
                    "Direct purchase receipt was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(receipt);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreateDirectPurchaseReceiptRequest body,
            CreateDirectPurchaseReceipt useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, body, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/pos/direct-purchase-receipts/{dto.DirectPurchaseReceiptId:D}", dto));
        });

        group.MapPost("/{receiptId:guid}/void", async (
            HttpRequest request,
            Guid receiptId,
            VoidDirectPurchaseReceiptRequest body,
            VoidDirectPurchaseReceipt useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.DirectPurchaseReceiptVoid,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, receiptId, body, actorId, ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
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

    private static bool TryParseDate(string? value, string fieldName, out DateOnly? parsed, out IResult? problem)
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
            $"Invalid date '{value}' for {fieldName}. Use ISO date (yyyy-MM-dd).",
            StatusCodes.Status400BadRequest);
        return false;
    }
}
