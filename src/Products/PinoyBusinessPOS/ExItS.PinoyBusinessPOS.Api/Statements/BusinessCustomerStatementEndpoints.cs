using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Statements;

namespace ExItS.PinoyBusinessPOS.Api.Statements;

internal static class BusinessCustomerStatementEndpoints
{
    public static IEndpointRouteBuilder MapBusinessCustomerStatementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/pos/connected-suppliers/business-customers/{connectionId:guid}/statement",
            async (
                HttpRequest request,
                Guid connectionId,
                DateOnly periodStart,
                DateOnly periodEnd,
                string? organizationDisplayName,
                string? currencyCode,
                string? culture,
                GetBusinessCustomerStatement useCase,
                IPosCommercialAccessAccessor access,
                CancellationToken ct) =>
            {
                if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
                {
                    return problem!;
                }

                if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewGenerateStatement, out problem))
                {
                    return problem!;
                }

                var result = await useCase
                    .ExecuteAsync(
                        organizationId,
                        connectionId,
                        periodStart,
                        periodEnd,
                        organizationDisplayName,
                        currencyCode ?? "PHP",
                        culture ?? "en-PH",
                        ct)
                    .ConfigureAwait(false);

                return PosApiResults.FromResult(result, Results.Ok);
            });

        return app;
    }
}
