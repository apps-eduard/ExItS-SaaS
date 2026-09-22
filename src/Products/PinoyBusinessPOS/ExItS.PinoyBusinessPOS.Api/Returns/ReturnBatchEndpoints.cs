using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Returns;

namespace ExItS.PinoyBusinessPOS.Api.Returns;

internal static class ReturnBatchEndpoints
{
    public static IEndpointRouteBuilder MapReturnBatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/return-batches");

        group.MapPost("/", async (
            HttpRequest request,
            AcceptReturnBatchRequest body,
            AcceptReturnBatch useCase,
            ReturnBatchQueryService queries,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            IPosDeviceTransactionAuthorizer deviceAuthorization,
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

            var deviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, organizationId, ct).ConfigureAwait(false);
            if (deviceDenied is not null)
            {
                return deviceDenied;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.SaleReturnCreate,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        body.SaleId,
                        body.Reason,
                        body.Lines,
                        actorId,
                        body.Notes,
                        body.ReturnBatchId,
                        ct2),
                    ReturnBatchQueryService.Map,
                    dto => Results.Created($"/api/v1/pos/return-batches/{dto.ReturnBatchId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/{returnBatchId:guid}", async (
            HttpRequest request,
            Guid returnBatchId,
            ReturnBatchQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var dto = await queries.GetByIdAsync(organizationId, returnBatchId, ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapGet("/", async (
            HttpRequest request,
            Guid saleId,
            ReturnBatchQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var items = await queries.ListBySaleIdAsync(organizationId, saleId, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        group.MapPut("/{returnBatchId:guid}/lines/{returnBatchLineId:guid}/classification", async (
            HttpRequest request,
            Guid returnBatchId,
            Guid returnBatchLineId,
            ClassifyReturnBatchLine useCase,
            ClassifyReturnBatchLineRequest body,
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
                    returnBatchLineId,
                    body.SellableQuantity,
                    body.DamagedQuantity,
                    actorId,
                    body.InspectionNote,
                    body.ExpectedUpdatedAtUtc,
                    ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, value => Results.Ok(ReturnBatchQueryService.Map(value)));
        });

        group.MapGet("/{returnBatchId:guid}/review", async (
            HttpRequest request,
            Guid returnBatchId,
            ReturnBatchQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var dto = await queries.GetReviewPreviewAsync(organizationId, returnBatchId, ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPost("/{returnBatchId:guid}/finalize", async (
            HttpRequest request,
            Guid returnBatchId,
            FinalizeReturnBatchRequest body,
            FinalizeReturnBatch useCase,
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

        group.MapGet("/{returnBatchId:guid}/timeline", async (
            HttpRequest request,
            Guid returnBatchId,
            ReturnBatchQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var timeline = await queries.GetTimelineAsync(organizationId, returnBatchId, ct).ConfigureAwait(false);
            return timeline is null ? Results.NotFound() : Results.Ok(timeline);
        });

        group.MapPost("/{returnBatchId:guid}/refunds", async (
            HttpRequest request,
            Guid returnBatchId,
            RecordReturnBatchRefundRequest body,
            RecordReturnBatchRefund useCase,
            ReturnBatchQueryService queries,
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
                    body.Amount,
                    body.Method,
                    actorId,
                    body.Reference,
                    body.Note,
                    body.ClientRefundId,
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
