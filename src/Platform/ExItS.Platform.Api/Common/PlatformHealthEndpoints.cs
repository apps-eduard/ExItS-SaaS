using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ExItS.Platform.Api.Common;

internal static class PlatformHealthEndpoints
{
    public const string ReadyTag = "ready";

    public static WebApplication MapPlatformHealthEndpoints(this WebApplication app)
    {
        // Liveness: process is up — no dependency checks (must not flap on temporary DB issues).
        app.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => false
            })
            .DisableRateLimiting();

        // Readiness: can safely serve protected work (database reachable).
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyTag)
            })
            .DisableRateLimiting();

        return app;
    }
}
