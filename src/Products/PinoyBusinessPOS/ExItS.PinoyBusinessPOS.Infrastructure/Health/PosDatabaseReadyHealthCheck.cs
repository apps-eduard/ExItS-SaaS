using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Health;

/// <summary>POS readiness check — database connectivity only; never returns secrets.</summary>
internal sealed class PosDatabaseReadyHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public const string ReadyTag = "ready";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
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

public static class PosHealthCheckServiceCollectionExtensions
{
    public static IServiceCollection AddPosHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<PosDatabaseReadyHealthCheck>(
                "pos-database",
                failureStatus: HealthStatus.Unhealthy,
                tags: [PosDatabaseReadyHealthCheck.ReadyTag]);
        return services;
    }
}
