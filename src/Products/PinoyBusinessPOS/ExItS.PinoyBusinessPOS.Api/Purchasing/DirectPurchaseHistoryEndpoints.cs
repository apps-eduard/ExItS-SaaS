using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Purchasing;

namespace ExItS.PinoyBusinessPOS.Api.Purchasing;

internal static class DirectPurchaseHistoryEndpoints
{
    public static IEndpointRouteBuilder MapDirectPurchaseHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/purchasing/direct-purchases");

        group.MapGet("/", async (
            HttpRequest request,
            string? sourceType,
            string? fromDate,
            string? toDate,
            string? search,
            string? status,
            int? page,
            int? pageSize,
            DirectPurchaseHistoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, out var organizationId, out var problem)
                || !TryParseDate(fromDate, "fromDate", out var from, out problem)
                || !TryParseDate(toDate, "toDate", out var to, out problem))
            {
                return problem!;
            }

            var filter = new DirectPurchaseHistoryFilter(sourceType, from, to, search, status);
            var result = await queries
                .ListAsync(organizationId, filter, page, pageSize, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapGet("/b2b/{saleId:guid}", async (
            HttpRequest request,
            Guid saleId,
            DirectPurchaseHistoryQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await queries
                .GetB2bDetailAsync(organizationId, saleId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, UtangCapability.ViewInventory, out problem);
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
