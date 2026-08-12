using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Api.Personal;

/// <summary>
/// Platform Admin configuration for Personal feature definitions (WP11).
/// Not callable as a Personal self-service API.
/// </summary>
internal static class PersonalFeatureAdminEndpoints
{
    public static IEndpointRouteBuilder MapPersonalFeatureAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/personal/features");

        group.MapGet("/", async (
            ListPersonalFeatureDefinitions list,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PersonalFeatureDefinition),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await list.ExecuteAsync(ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapGet("/{featureCode}", async (
            string featureCode,
            GetPersonalFeatureDefinition get,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PersonalFeatureDefinition),
                featureCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await get.ExecuteAsync(featureCode, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPatch("/{featureCode}", async (
            string featureCode,
            UpdatePersonalFeatureDefinitionRequest body,
            UpdatePersonalFeatureDefinition update,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.PersonalFeatureDefinitionUpdated,
                nameof(PersonalFeatureDefinition),
                featureCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await update.ExecuteAsync(
                featureCode,
                new UpdatePersonalFeatureDefinitionCommand(
                    body.DisplayName,
                    body.IsActive,
                    body.RewardPointsPrice,
                    body.DefaultEntitlementDurationDays,
                    body.ExpectedUpdatedAtUtc),
                ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PersonalFeatureDefinitionUpdated,
                    nameof(PersonalFeatureDefinition),
                    featureCode,
                    summary:
                    $"Updated Personal feature {featureCode}: active={result.Value!.IsActive}, rewardPrice={result.Value.RewardPointsPrice?.ToString() ?? "null"}, durationDays={result.Value.DefaultEntitlementDurationDays?.ToString() ?? "indefinite"}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }
}

internal sealed record UpdatePersonalFeatureDefinitionRequest(
    string DisplayName,
    bool IsActive,
    int? RewardPointsPrice,
    int? DefaultEntitlementDurationDays,
    DateTimeOffset? ExpectedUpdatedAtUtc);
