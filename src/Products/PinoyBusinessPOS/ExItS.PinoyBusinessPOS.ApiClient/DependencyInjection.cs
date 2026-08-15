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
    /// Bearer token forwarding, Development/Testing <c>X-Dev-Platform-User-Id</c>, and organization scope header.
    /// Register <see cref="ICurrentUserContext"/> and <see cref="IConnectivityService"/> beforehand.
    /// </summary>
    public static IServiceCollection AddPosApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var requireHttps = configuration.GetValue("Security:RequireHttpsApiUrls", false)
            || string.Equals(configuration["DOTNET_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase);

        services.AddOptions<PosApiOptions>()
            .Bind(configuration.GetSection(PosApiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "PosApi:BaseUrl must be configured.")
            .Validate(o => o.TimeoutSeconds > 0, "PosApi:TimeoutSeconds must be greater than zero.")
            .Validate(o => !requireHttps || IsHttpsAbsoluteUri(o.BaseUrl), "PosApi:BaseUrl must use HTTPS in Production (MAUI-HTTPS).");

        services.AddOptions<PosBusinessApiOptions>()
            .Bind(configuration.GetSection(PosBusinessApiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "PosBusinessApi:BaseUrl must be configured.")
            .Validate(o => o.TimeoutSeconds > 0, "PosBusinessApi:TimeoutSeconds must be greater than zero.")
            .Validate(o => !requireHttps || IsHttpsAbsoluteUri(o.BaseUrl), "PosBusinessApi:BaseUrl must use HTTPS in Production (MAUI-HTTPS).");

        services.AddTransient<PlatformBearerHandler>();
        services.AddTransient<PlatformSessionHeaderHandler>();
        services.AddTransient<PosPlatformSessionForwardingHandler>();
        services.AddTransient<DevPlatformUserHeaderHandler>();
        services.AddTransient<PosOrganizationHeaderHandler>();
        services.AddTransient<PosInstallationDeviceHeaderHandler>();
        services.AddTransient<PosCommercialHeaderHandler>();
        services.AddTransient<PosApiReachabilityHandler>();

        services.AddHttpClient<IPosApiClient, PosApiClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<PlatformSessionHeaderHandler>()
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PlatformBearerHandler>()
            .AddHttpMessageHandler<PosApiReachabilityHandler>();

        AddBusinessClient<IPosCustomerClient, PosCustomerClient>(services);
        AddBusinessClient<IPosCatalogClient, PosCatalogClient>(services);
        AddBusinessClient<IPosCatalogImportClient, PosCatalogImportClient>(services);
        AddBusinessClient<IPosSaleClient, PosSaleClient>(services);
        AddBusinessClient<IPosPaymentAttemptClient, PosPaymentAttemptClient>(services);
        AddBusinessClient<IPosInventoryClient, PosInventoryClient>(services);
        AddBusinessClient<IPosExpenseClient, PosExpenseClient>(services);
        AddBusinessClient<IPosSupplierClient, PosSupplierClient>(services);
        AddBusinessClient<IPosConnectedSupplierClient, PosConnectedSupplierClient>(services);
        AddBusinessClient<IPosRegisterClient, PosRegisterClient>(services);
        AddBusinessClient<IPosOperationalSetupClient, PosOperationalSetupClient>(services);
        AddBusinessClient<IPosPrivacyReadinessClient, PosPrivacyReadinessClient>(services);
        AddBusinessClient<IPosPurchaseOrderClient, PosPurchaseOrderClient>(services);
        AddBusinessClient<IPosCashierShiftClient, PosCashierShiftClient>(services);
        AddBusinessClient<IPosSaleReturnClient, PosSaleReturnClient>(services);
        AddBusinessClient<IPosPermissionClient, PosPermissionClient>(services);
        AddBusinessClient<IPosReportingClient, PosReportingClient>(services);
        AddBusinessClient<IPosOfflineProbeClient, PosOfflineProbeClient>(services);
        AddBusinessClient<IPosLinkedCustomerClient, PosLinkedCustomerClient>(services);

        services.AddSingleton<IPlatformAccessClient, PlatformAccessClient>();
        services.AddSingleton<IMerchantCatalogDiscoveryClient, MerchantCatalogDiscoveryClient>();

        return services;
    }

    private static bool IsHttpsAbsoluteUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static void AddBusinessClient<TClient, TImplementation>(IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<PosBusinessApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
            .AddHttpMessageHandler<PosOrganizationHeaderHandler>()
            .AddHttpMessageHandler<PosInstallationDeviceHeaderHandler>()
            .AddHttpMessageHandler<PosCommercialHeaderHandler>()
            .AddHttpMessageHandler<PosPlatformSessionForwardingHandler>()
            .AddHttpMessageHandler<PlatformBearerHandler>()
            .AddHttpMessageHandler<PosApiReachabilityHandler>();
    }
}
