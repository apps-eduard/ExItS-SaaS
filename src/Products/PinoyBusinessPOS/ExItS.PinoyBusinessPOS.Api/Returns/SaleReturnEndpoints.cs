using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Api.Returns;

/// <summary>
/// Organization-scoped sale return endpoints. Online-only; refund method always matches the sale.
/// </summary>
internal static class SaleReturnEndpoints
{
    public static IEndpointRouteBuilder MapSaleReturnEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/sale-returns");

        group.MapGet("/", async (
            HttpRequest request,
            Guid? saleId,
            string? returnNumber,
            int? page,
            int? pageSize,
            SaleReturnQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var filter = new SaleReturnFilter(saleId, returnNumber);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/refundable/{saleId:guid}", async (
            HttpRequest request,
            Guid saleId,
            SaleReturnQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await queries.GetRefundableAsync(organizationId, saleId, ct).ConfigureAwait(false);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/{returnId:guid}", async (
            HttpRequest request,
            Guid returnId,
            SaleReturnQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewReturns, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await queries.GetByIdAsync(organizationId, returnId, ct).ConfigureAwait(false);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreateSaleReturnRequest body,
            ProcessSaleReturn useCase,
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
            if (deviceDenied is not null) return deviceDenied;

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
                        body.ReturnId,
                        ct2),
                    SaleReturnQueryService.Map,
                    dto => Results.Created($"/api/v1/pos/sale-returns/{dto.ReturnId:D}", dto),
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
