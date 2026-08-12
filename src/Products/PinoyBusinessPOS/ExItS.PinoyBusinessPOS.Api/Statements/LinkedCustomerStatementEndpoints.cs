using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Statements;

namespace ExItS.PinoyBusinessPOS.Api.Statements;

/// <summary>
/// Personal linked-customer Business Utang projection (WP04) + lazy sale receipt detail (WP05).
/// Authorization is WP03 only — not staff UtangCapability / org-header alone.
/// </summary>
internal static class LinkedCustomerStatementEndpoints
{
    public static IEndpointRouteBuilder MapLinkedCustomerStatementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:guid}/statement", async (
            Guid platformBusinessCustomerId,
            Guid? organizationId,
            string? currency,
            GetLinkedCustomerStatementSummary summary,
            CancellationToken ct) =>
        {
            if (!TryGetOrganizationId(organizationId, out var orgId, out var problem))
            {
                return problem!;
            }

            var result = await summary
                .ExecuteAsync(orgId, platformBusinessCustomerId, currency ?? "PHP", ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        app.MapGet("/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:guid}/activity", async (
            Guid platformBusinessCustomerId,
            Guid? organizationId,
            int? page,
            int? pageSize,
            ListLinkedCustomerRecentActivity activity,
            CancellationToken ct) =>
        {
            if (!TryGetOrganizationId(organizationId, out var orgId, out var problem))
            {
                return problem!;
            }

            var result = await activity
                .ExecuteAsync(orgId, platformBusinessCustomerId, page, pageSize, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        // WP05: one receipt only, after explicit open — never nested under activity.
        app.MapGet("/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:guid}/receipts/{saleId:guid}", async (
            Guid platformBusinessCustomerId,
            Guid saleId,
            Guid? organizationId,
            string? currency,
            GetLinkedCustomerSaleReceipt receipt,
            CancellationToken ct) =>
        {
            if (!TryGetOrganizationId(organizationId, out var orgId, out var problem))
            {
                return problem!;
            }

            var result = await receipt
                .ExecuteAsync(orgId, platformBusinessCustomerId, saleId, currency ?? "PHP", ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }

    private static bool TryGetOrganizationId(Guid? organizationId, out Guid orgId, out IResult? problem)
    {
        orgId = default;
        problem = null;
        if (organizationId is not Guid id || id == Guid.Empty)
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.OrganizationRequired,
                "Query parameter 'organizationId' is required.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        orgId = id;
        return true;
    }
}
