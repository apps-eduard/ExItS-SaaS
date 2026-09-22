using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Returns;

namespace ExItS.PinoyBusinessPOS.Api.Returns;

internal static class ConnectedPoReturnEndpoints
{
    public static IEndpointRouteBuilder MapConnectedPoReturnEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/connected-po-returns");

        group.MapGet("/eligibility/{purchaseOrderId:guid}", async (
            HttpRequest request,
            Guid purchaseOrderId,
            ConnectedPoReturnQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var dto = await queries.GetEligibilityAsync(organizationId, purchaseOrderId, ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapGet("/by-purchase-order/{purchaseOrderId:guid}", async (
            HttpRequest request,
            Guid purchaseOrderId,
            ConnectedPoReturnQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var items = await queries.ListByPurchaseOrderAsync(organizationId, purchaseOrderId, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        group.MapGet("/seller-inbox", async (
            HttpRequest request,
            ConnectedPoReturnQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var items = await queries.ListSellerInboxAsync(organizationId, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        group.MapPost("/", async (
            HttpRequest request,
            RequestConnectedPoReturnBatchRequest body,
            RequestConnectedPoReturnBatch useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ProcessReturn, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(
                    organizationId,
                    body.PurchaseOrderId,
                    body.Reason,
                    body.Lines,
                    actorId,
                    body.Notes,
                    body.ReturnBatchId,
                    ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                value =>
                {
                    var dto = ReturnBatchQueryService.Map(value);
                    return Results.Created($"/api/v1/pos/return-batches/{dto.ReturnBatchId:D}", dto);
                });
        });

        group.MapPost("/{returnBatchId:guid}/receive", async (
            HttpRequest request,
            Guid returnBatchId,
            ReceiveConnectedPoReturnBatchRequest? body,
            ReceiveConnectedPoReturnBatch useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ProcessReturn, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(
                    organizationId,
                    returnBatchId,
                    actorId,
                    body?.ExpectedUpdatedAtUtc,
                    ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, value => Results.Ok(ReturnBatchQueryService.Map(value)));
        });

        group.MapPost("/{returnBatchId:guid}/finalize", async (
            HttpRequest request,
            Guid returnBatchId,
            FinalizeReturnBatchRequest body,
            FinalizeConnectedPoReturnBatch useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ProcessReturn, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(
                    organizationId,
                    returnBatchId,
                    body.ExpectedUpdatedAtUtc,
                    actorId,
                    ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, value => Results.Ok(ReturnBatchQueryService.Map(value)));
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
        problem = null;
        organizationId = Guid.Empty;

        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        if (!PosCommercialScope.TryAuthorize(access, capability, out problem))
        {
            return false;
        }

        return true;
    }
}
