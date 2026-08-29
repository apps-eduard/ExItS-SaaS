using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

internal static class WasteLossEndpoints
{
    public static IEndpointRouteBuilder MapWasteLossEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/inventory/waste-losses");

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
            WasteLossQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var filter = new WasteLossFilter(
                fromOccurredAtUtc,
                toOccurredAtUtc,
                reason,
                status,
                branchId,
                referenceNumber);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/{wasteLossId:guid}", async (
            HttpRequest request,
            Guid wasteLossId,
            WasteLossQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
            {
                return problem!;
            }

            var wasteLoss = await queries.GetByIdAsync(organizationId, wasteLossId, ct).ConfigureAwait(false);
            return wasteLoss is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.WasteLossNotFound,
                    "Waste/loss was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(wasteLoss);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreateWasteLossRequest body,
            CreateWasteLoss useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            if (body.BranchId is null
                && PosOrganizationScope.TryGetOptionalBranchId(request, out var headerBranch)
                && headerBranch is Guid branchFromHeader)
            {
                body = body with { BranchId = branchFromHeader };
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.WasteLoss,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, body, actorId, ct2),
                    dto => dto,
                    dto => Results.Created($"/api/v1/pos/inventory/waste-losses/{dto.WasteLossId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/{wasteLossId:guid}/void", async (
            HttpRequest request,
            Guid wasteLossId,
            VoidWasteLoss useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, wasteLossId, actorId, ct).ConfigureAwait(false);
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
