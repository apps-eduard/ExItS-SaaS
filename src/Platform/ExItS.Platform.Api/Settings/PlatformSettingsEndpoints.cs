using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Settings;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using PlatformSettingsAggregate = ExItS.Platform.Domain.Settings.PlatformSettings;

namespace ExItS.Platform.Api.Settings;

internal static class PlatformSettingsEndpoints
{
    public static IEndpointRouteBuilder MapPlatformSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/settings");

        group.MapGet("/general", async (GetPlatformGeneralSettings query, PlatformAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPlatformSettings,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformSettingsAggregate),
                "general",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await query.ExecuteAsync(authz.CurrentActor, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPut("/general", async (
            UpdatePlatformGeneralSettingsRequest body,
            UpdatePlatformGeneralSettings useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformSettings,
                PlatformAuditActions.PlatformSettingsGeneralUpdated,
                nameof(PlatformSettingsAggregate),
                "general",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, authz.CurrentActor, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapGet("/email", async (GetPlatformEmailSettings query, PlatformAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPlatformSettings,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformSettingsAggregate),
                "email",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await query.ExecuteAsync(authz.CurrentActor, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPut("/email", async (
            UpdatePlatformEmailSettingsRequest body,
            UpdatePlatformEmailSettings useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformSettings,
                PlatformAuditActions.PlatformSettingsEmailUpdated,
                nameof(PlatformSettingsAggregate),
                "email",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, authz.CurrentActor, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/email/test", async (
            PlatformEmailTestRequest body,
            SendPlatformEmailTest useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformSettings,
                PlatformAuditActions.PlatformSettingsEmailTestSent,
                nameof(PlatformSettingsAggregate),
                "email-test",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, authz.CurrentActor, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapGet("/regional", async (GetPlatformRegionalSettings query, PlatformAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPlatformSettings,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformSettingsAggregate),
                "regional",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await query.ExecuteAsync(authz.CurrentActor, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPut("/regional", async (
            UpdatePlatformRegionalSettingsRequest body,
            UpdatePlatformRegionalSettings useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformSettings,
                PlatformAuditActions.PlatformSettingsRegionalUpdated,
                nameof(PlatformSettingsAggregate),
                "regional",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, authz.CurrentActor, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }
}
