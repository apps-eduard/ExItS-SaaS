using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
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
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
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
                : Results.Ok(MapCommercialSummary(summary));
        });

        admin.MapGet("/billing/summary", async (
            BillingOperationsQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageManualPayments,
                PlatformAuditActions.PlatformAccessChecked,
                "BillingOperations",
                "summary",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var summary = await queries.GetSummaryAsync(ct).ConfigureAwait(false);
            return Results.Ok(summary);
        });

        admin.MapGet("/billing/issues", async (
            string? issueType,
            int? page,
            int? pageSize,
            BillingOperationsQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageManualPayments,
                PlatformAuditActions.PlatformAccessChecked,
                "BillingOperations",
                issueType ?? "all",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries.ListIssuesAsync(issueType, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        admin.MapGet("/action-center", async (
            ActionCenterQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            // Gate: actor must hold ViewPortfolio to open Action Center.
            // Category composition is permission-aware — missing a category never 403s the whole call.
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                "ActionCenter",
                "summary",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var permissions = await authz.ResolvePermissionsAsync(ct).ConfigureAwait(false);
            var access = ActionCenterAccessScope.FromPermissions(permissions);
            var result = await queries.GetAsync(access, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        admin.MapGet("/entitlements/latest", async (
            int? page,
            int? pageSize,
            string? sortBy,
            bool? sortDesc,
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

            var result = await queries.ListLatestEntitlementsAsync(
                page,
                pageSize,
                ParseEntitlementSortBy(sortBy),
                sortDesc,
                ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }

    private static object MapCommercialSummary(OrganizationCommercialSummaryDto summary)
    {
        var org = summary.Organization;
        return new
        {
            organization = new
            {
                id = org.Id,
                displayName = org.DisplayName,
                slug = org.Slug,
                status = org.Status,
                profile = org.Profile,
                branding = org.Branding,
                createdAtUtc = org.CreatedAtUtc,
                updatedAtUtc = org.UpdatedAtUtc
            },
            subscriptions = summary.Subscriptions,
            payments = summary.Payments,
            latestEntitlements = summary.LatestEntitlements
        };
    }

    private static EntitlementListSortBy? ParseEntitlementSortBy(string? sortBy) =>
        sortBy?.Trim().ToLowerInvariant() switch
        {
            "product" or "productdisplayname" => EntitlementListSortBy.ProductDisplayName,
            "organization" or "organizationdisplayname" => EntitlementListSortBy.OrganizationDisplayName,
            "status" or "subscriptionstatus" => EntitlementListSortBy.Status,
            "generated" or "generatedatutc" => EntitlementListSortBy.GeneratedAtUtc,
            "revision" or "snapshotversion" or "version" => EntitlementListSortBy.Revision,
            null or "" => null,
            _ => null
        };
}
