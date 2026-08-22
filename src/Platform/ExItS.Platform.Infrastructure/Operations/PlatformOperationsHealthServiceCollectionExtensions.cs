using ExItS.Platform.Application.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.Infrastructure.Operations;

public static class PlatformOperationsHealthServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformOperationsHealth(this IServiceCollection services)
    {
        services.AddSingleton<IHostResourceMetrics, HostResourceMetricsCollector>();
        services.AddHttpClient(PosHealthProbe.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(3);
        });
        services.AddSingleton<IPosHealthProbe, PosHealthProbe>();
        services.AddScoped<ISystemHealthQueryService, SystemHealthQueryService>();
        return services;
    }
}
