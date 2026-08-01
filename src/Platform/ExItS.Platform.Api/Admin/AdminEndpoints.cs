using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace ExItS.Platform.Api.Admin;

/// <summary>
/// Focused read-only Platform Admin aggregation endpoints (P4-WP01). Development-stage only: actor
/// identity is unauthenticated, but reads enforce <see cref="PlatformPermission.ViewPortfolio"/>.
/// No mutation, delivery, invoice, or product-local operational data.
/// </summary>
internal static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/platform/admin");

        admin.MapGet("/portfolio-summary", async (
            AdminPortfolioQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                "AdminPortfolio",
                "summary",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var summary = await queries.GetPortfolioSummaryAsync(ct).ConfigureAwait(false);
            return Results.Ok(summary);
        })
        .DisableRateLimiting();

        admin.MapGet("/products/{productCode}/overview", async (
            string productCode,
            AdminPortfolioQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                "AdminPortfolio",
                productCode,
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var overview = await queries.GetProductOverviewAsync(productCode, ct).ConfigureAwait(false);
                return overview is null
                    ? PlatformApiResults.Problem(
                        ApplicationErrorCodes.ProductNotFound,
                        "Product was not found.",
                        StatusCodes.Status404NotFound)
                    : Results.Ok(overview);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        admin.MapGet("/organizations/{organizationId:guid}/commercial-summary", async (
            Guid organizationId,
            AdminPortfolioQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                "AdminPortfolio",
                organizationId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var summary = await queries
                .GetOrganizationCommercialSummaryAsync(organizationId, ct)
                .ConfigureAwait(false);
            return summary is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.OrganizationNotFound,
                    "Platform Organization was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(summary);
        });

        admin.MapGet("/entitlements/latest", async (
            int? page,
            int? pageSize,
            AdminPortfolioQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                "AdminPortfolio",
                "entitlements-latest",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries.ListLatestEntitlementsAsync(page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }
}
