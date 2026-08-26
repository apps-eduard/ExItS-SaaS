using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Operations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;

namespace ExItS.Platform.Api.Operations;

internal static class PlatformOperationsEndpoints
{
    public static IEndpointRouteBuilder MapPlatformOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var operations = app.MapGroup("/api/v1/platform/operations");

        operations.MapGet("/usage-limits", async (
            Guid? organizationId,
            string? productCode,
            int? page,
            int? pageSize,
            PlatformUsageLimitsQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                "UsageLimits",
                "portfolio",
                organizationId: organizationId,
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries
                .ListAsync(organizationId, productCode, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        operations.MapPost("/support/lookup", async (
            PlatformSupportLookupRequest body,
            PlatformSupportLookupService lookup,
            PlatformAuthz authz,
            IPlatformActorAccessor actorAccessor,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsurePlatformAdministratorAsync(
                PlatformAuditActions.PlatformSupportLookup,
                "SupportConsole",
                body.Mode ?? "unknown",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = actorAccessor.GetCurrent();
            if (actor.PlatformUserId is null)
            {
                return Results.Unauthorized();
            }

            var result = await lookup
                .LookupAsync(actor.PlatformUserId, body, ct)
                .ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return PlatformApiResults.Problem(
                    result.ErrorCode!,
                    result.ErrorMessage!,
                    result.ErrorCode == ApplicationErrorCodes.OperationsRequestInvalid
                        ? StatusCodes.Status400BadRequest
                        : StatusCodes.Status404NotFound);
            }

            await authz.AuditSucceededAsync(
                PlatformAuditActions.PlatformSupportLookup,
                "SupportConsole",
                body.Mode ?? "unknown",
                summary: "Support lookup succeeded.",
                cancellationToken: ct).ConfigureAwait(false);

            return Results.Ok(result.Value);
        });

        operations.MapGet("/jobs", async (
            string? status,
            string? search,
            int? page,
            int? pageSize,
            PlatformBackgroundJobsQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsurePlatformAdministratorAsync(
                PlatformAuditActions.PlatformAccessChecked,
                "BackgroundJobs",
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries.ListAsync(status, search, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        operations.MapGet("/jobs/{jobId:guid}", async (
            Guid jobId,
            PlatformBackgroundJobsQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsurePlatformAdministratorAsync(
                PlatformAuditActions.PlatformAccessChecked,
                "BackgroundJobs",
                jobId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var detail = await queries.GetAsync(jobId, ct).ConfigureAwait(false);
            return detail is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportJobNotFound,
                    "Background job was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(detail);
        });

        return app;
    }
}
