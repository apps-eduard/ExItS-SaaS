using System.Security.Claims;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Organization-scoped Personal Utang → Business Credit migration (P16-WP08).
/// </summary>
internal static class UtangMigrationEndpoints
{
    public static IEndpointRouteBuilder MapUtangMigrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/organizations/{organizationId:guid}/utang-migrations/preview", async (
            Guid organizationId,
            PreviewUtangMigrationRequest body,
            HttpContext http,
            PreviewPersonalUtangMigration preview,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.UtangMigrationPreviewed,
                nameof(PersonalUtangMigrationBatch),
                organizationId.ToString("D"),
                organizationId,
                summary: "Preview Personal Utang migration into organization Business Credit.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await preview.ExecuteAsync(
                PlatformUserId.From(userId),
                PlatformOrganizationId.From(organizationId),
                body,
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/utang-migrations", async (
            Guid organizationId,
            ExecuteUtangMigrationRequest body,
            HttpContext http,
            ExecutePersonalUtangMigration execute,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.UtangMigrationExecuted,
                nameof(PersonalUtangMigrationBatch),
                body.BatchId.ToString("D"),
                organizationId,
                summary: "Execute Personal Utang migration into organization Business Credit.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await execute.ExecuteAsync(
                PlatformUserId.From(userId),
                PlatformOrganizationId.From(organizationId),
                body,
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.UtangMigrationExecuted,
                    nameof(PersonalUtangMigrationBatch),
                    body.BatchId.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }

    private static bool TryGetUserId(HttpContext http, out Guid userId)
    {
        userId = Guid.Empty;
        var raw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
