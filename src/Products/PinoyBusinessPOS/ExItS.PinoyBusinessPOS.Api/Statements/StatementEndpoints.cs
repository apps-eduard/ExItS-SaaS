using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Api.Statements;

internal static class StatementEndpoints
{
    public static IEndpointRouteBuilder MapStatementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pos/customers/{customerId:guid}/statement", async (
            HttpRequest request,
            Guid customerId,
            DateOnly periodStart,
            DateOnly periodEnd,
            string? organizationDisplayName,
            string? currencyCode,
            string? culture,
            ICustomerStatementService statements,
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

            var result = await statements
                .GenerateAsync(
                    PosOrganizationId.From(organizationId),
                    customerId,
                    periodStart,
                    periodEnd,
                    organizationDisplayName,
                    currencyCode ?? "PHP",
                    culture ?? "en-PH",
                    ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(result, Results.Ok);
        });

        app.MapGet("/api/v1/pos/repayments/{repaymentId:guid}/receipt", async (
            HttpRequest request,
            Guid repaymentId,
            string? organizationDisplayName,
            string? currencyCode,
            string? culture,
            IRepaymentReceiptService receipts,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewGenerateReceipt, out problem))
            {
                return problem!;
            }

            var result = await receipts
                .GetAsync(
                    PosOrganizationId.From(organizationId),
                    repaymentId,
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
