using System.Globalization;
using System.Reflection;
using ExItS.DesignSystem.Abstractions;
using ExItS.PinoyBusinessPOS.ApiClient;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Maui.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExItS.PinoyBusinessPOS.Maui;

public static class MauiProgram
{
    /// <summary>
    /// Development-stage notice (P5-WP02): this app ships no authentication, no sales/inventory
    /// data entry, and no offline synchronization. It establishes the Android app shell,
    /// design tokens, density, connectivity/health surfacing, theme, and language foundation only.
    /// </summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        ConfigureAppConfiguration(builder.Configuration);

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddLocalization();

        var supportedCultures = new[] { CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("fil-PH") };
        CultureInfo.CurrentCulture = supportedCultures[0];
        CultureInfo.CurrentUICulture = supportedCultures[0];
        CultureInfo.DefaultThreadCurrentCulture = supportedCultures[0];
        CultureInfo.DefaultThreadCurrentUICulture = supportedCultures[0];

        RegisterApplicationServices(builder.Services, builder.Configuration);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Merges bundled default configuration (in-memory) with the embedded
    /// <c>wwwroot/appsettings.json</c> resource. In-memory defaults guarantee a working
    /// <c>PosApi:BaseUrl</c> even if the embedded resource is ever missing; the JSON resource is
    /// the editable source of truth for local overrides.
    /// </summary>
    private static void ConfigureAppConfiguration(ConfigurationManager configuration)
    {
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PosApi:BaseUrl"] = "http://10.0.2.2:5288",
            ["PosApi:TimeoutSeconds"] = "15"
        });

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("appsettings.json");
        if (stream is not null)
        {
            configuration.AddJsonStream(stream);
        }
    }

    private static void RegisterApplicationServices(IServiceCollection services, IConfiguration configuration)
    {
        const string environmentName =
#if DEBUG
            "Development";
#else
            "Production";
#endif

        services.AddSingleton<IThemePreferenceStore, MauiThemePreferenceStore>();
        services.AddSingleton<IDensityPreferenceStore, MauiDensityPreferenceStore>();
        services.AddSingleton<ICulturePreferenceStore, MauiCulturePreferenceStore>();
        services.AddSingleton<IConnectivityService, MauiConnectivityService>();
        services.AddSingleton<IAppInfoService>(_ => new MauiAppInfoService(environmentName));

        // NOT USED IN P5-WP02 — see NullSecureTokenStore remarks. Registered only so DI can
        // satisfy ISecureTokenStore if a future component requests it.
        services.AddSingleton<ISecureTokenStore, NullSecureTokenStore>();

        services.AddSingleton<ThemeController>();
        services.AddSingleton<DensityController>();
        services.AddSingleton<CultureController>();
        services.AddSingleton<ApiStatusLocalizer>();

        services.AddPosApiClient(configuration);
        services.AddSingleton<PosStatusState>();
    }
}
