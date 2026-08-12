using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Statements;

namespace ExItS.PinoyBusinessPOS.Api.Statements;

/// <summary>
/// Personal linked-customer Business Utang projection (WP04–WP10):
/// statement, free/entitled activity, open-debt explanation, older settled history, lazy receipt detail.
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

        // WP06: open-debt evidence only (Active credits/repayments). Never paywalled.
        app.MapGet("/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:guid}/open-debt-activity", async (
            Guid platformBusinessCustomerId,
            Guid? organizationId,
            int? page,
            int? pageSize,
            ListLinkedCustomerOpenDebtActivity openDebt,
            CancellationToken ct) =>
        {
            if (!TryGetOrganizationId(organizationId, out var orgId, out var problem))
            {
                return problem!;
            }

            var result = await openDebt
                .ExecuteAsync(orgId, platformBusinessCustomerId, page, pageSize, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        // WP10: older/settled history — explicit request; requires personal-digital-records-extended.
        app.MapGet("/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:guid}/older-activity", async (
            Guid platformBusinessCustomerId,
            Guid? organizationId,
            int? page,
            int? pageSize,
            ListLinkedCustomerOlderSettledActivity older,
            CancellationToken ct) =>
        {
            if (!TryGetOrganizationId(organizationId, out var orgId, out var problem))
            {
                return problem!;
            }

            var result = await older
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
