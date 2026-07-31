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
    /// Registers Platform API client (health/access), POS business API customer client,
    /// Development/Testing <c>X-Dev-Platform-User-Id</c>, and organization scope header.
    /// Register <see cref="ICurrentUserContext"/> and <see cref="IConnectivityService"/> beforehand.
    /// </summary>
    public static IServiceCollection AddPosApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PosApiOptions>()
            .Bind(configuration.GetSection(PosApiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "PosApi:BaseUrl must be configured.")
            .Validate(o => o.TimeoutSeconds > 0, "PosApi:TimeoutSeconds must be greater than zero.");

        services.AddOptions<PosBusinessApiOptions>()
            .Bind(configuration.GetSection(PosBusinessApiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "PosBusinessApi:BaseUrl must be configured.")
            .Validate(o => o.TimeoutSeconds > 0, "PosBusinessApi:TimeoutSeconds must be greater than zero.");

        services.AddTransient<DevPlatformUserHeaderHandler>();
        services.AddTransient<PosOrganizationHeaderHandler>();
        services.AddTransient<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosApiClient, PosApiClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>();

        services.AddHttpClient<IPosCustomerClient, PosCustomerClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosCatalogClient, PosCatalogClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosSaleClient, PosSaleClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosInventoryClient, PosInventoryClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosExpenseClient, PosExpenseClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosSupplierClient, PosSupplierClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosPurchaseOrderClient, PosPurchaseOrderClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosCashierShiftClient, PosCashierShiftClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosSaleReturnClient, PosSaleReturnClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosReportingClient, PosReportingClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddHttpClient<IPosOfflineProbeClient, PosOfflineProbeClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>();

        services.AddSingleton<IPlatformAccessClient, PlatformAccessClient>();

        return services;
    }
}
