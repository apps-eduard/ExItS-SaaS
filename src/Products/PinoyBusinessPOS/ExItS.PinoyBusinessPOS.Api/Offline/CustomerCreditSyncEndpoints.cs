using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;

namespace ExItS.PinoyBusinessPOS.Api.Offline;

/// <summary>
/// Incremental download endpoints for offline customer/credit reconciliation.
/// </summary>
internal static class CustomerCreditSyncEndpoints
{
    public static IEndpointRouteBuilder MapCustomerCreditSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/sync");

        group.MapGet("/customers", async (
            HttpRequest request,
            DateTimeOffset? sinceUtc,
            int? page,
            int? pageSize,
            POSCustomerQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            var result = await queries
                .ListForSyncAsync(organizationId, sinceUtc, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/credit-entries", async (
            HttpRequest request,
            DateTimeOffset? sinceUtc,
            int? page,
            int? pageSize,
            CreditEntryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            var result = await queries
                .ListForSyncAsync(organizationId, sinceUtc, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }
}
