using ExItS.PinoyBusinessPOS.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="PosApiOptions"/> bound to the "PosApi" configuration section and a typed
    /// <see cref="IPosApiClient"/>/<see cref="PosApiClient"/> backed by <c>HttpClientFactory</c>.
    /// Register an <c>IConnectivityService</c> beforehand to enable the offline short-circuit.
    /// </summary>
    public static IServiceCollection AddPosApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PosApiOptions>()
            .Bind(configuration.GetSection(PosApiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "PosApi:BaseUrl must be configured.")
            .Validate(o => o.TimeoutSeconds > 0, "PosApi:TimeoutSeconds must be greater than zero.");

        services.AddHttpClient<IPosApiClient, PosApiClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<PosApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
