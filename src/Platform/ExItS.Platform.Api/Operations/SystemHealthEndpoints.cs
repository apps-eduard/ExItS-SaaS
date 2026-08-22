using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Operations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;

namespace ExItS.Platform.Api.Operations;

/// <summary>
/// Read-only Platform operations system-health aggregation.
/// Authorization: existing <see cref="PlatformPermission.ViewPortfolio"/> (Platform Admin operational read).
/// Never exposes secrets, environment dumps, Docker internals, or filesystem paths.
/// </summary>
internal static class SystemHealthEndpoints
{
    public const string Route = "/api/v1/platform/operations/system-health";

    public static IEndpointRouteBuilder MapSystemHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Route, async (
            ISystemHealthQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                "SystemHealth",
                "snapshot",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var snapshot = await queries.GetSnapshotAsync(ct).ConfigureAwait(false);
            return Results.Ok(snapshot);
        })
        .DisableRateLimiting();

        return app;
    }
}
