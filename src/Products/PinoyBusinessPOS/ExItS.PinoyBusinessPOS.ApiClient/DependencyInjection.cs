using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Options;
using ExItS.PinoyBusinessPOS.Application.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="PosApiOptions"/>, typed <see cref="IPosApiClient"/>, Platform access client,
    /// and Development/Testing <c>X-Dev-Platform-User-Id</c> header handler.
    /// Register <see cref="ICurrentUserContext"/> and <see cref="IConnectivityService"/> beforehand.
    /// </summary>
    public static IServiceCollection AddPosApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PosApiOptions>()
            .Bind(configuration.GetSection(PosApiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "PosApi:BaseUrl must be configured.")
            .Validate(o => o.TimeoutSeconds > 0, "PosApi:TimeoutSeconds must be greater than zero.");

        services.AddTransient<DevPlatformUserHeaderHandler>();

        services.AddHttpClient<IPosApiClient, PosApiClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>();

        services.AddSingleton<IPlatformAccessClient, PlatformAccessClient>();

        return services;
    }
}
