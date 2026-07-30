using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExItS.Platform.Infrastructure.Health;

/// <summary>Platform readiness check — database connectivity only; never returns secrets.</summary>
internal sealed class PlatformDatabaseReadyHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public const string ReadyTag = "ready";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var canConnect = await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
            return canConnect
                ? HealthCheckResult.Healthy("database")
                : HealthCheckResult.Unhealthy("database unavailable");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("database unavailable");
        }
    }
}

public static class PlatformHealthCheckServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<PlatformDatabaseReadyHealthCheck>(
                "platform-database",
                failureStatus: HealthStatus.Unhealthy,
                tags: [PlatformDatabaseReadyHealthCheck.ReadyTag]);
        return services;
    }
}
