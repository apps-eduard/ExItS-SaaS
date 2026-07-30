using System.Globalization;
using System.Reflection;
using ExItS.DesignSystem.Abstractions;
using ExItS.PinoyBusinessPOS.ApiClient;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Maui.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExItS.PinoyBusinessPOS.Maui;

public static class MauiProgram
{
    /// <summary>
    /// Development-stage notice (P6-WP05): authentication uses the approved Development/Testing
    /// Platform identity mechanism only (<c>X-Dev-Platform-User-Id</c>). Production JWT/MFA/SSO
    /// authentication is not implemented. Remarks-based credit, repayments, unified ledger, due dates,
    /// overdue monitoring, statements, and repayment receipts are available; interest, credit limits,
    /// sales, inventory, and offline sync are not.
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

    private static void ConfigureAppConfiguration(ConfigurationManager configuration)
    {
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PosApi:BaseUrl"] = "http://10.0.2.2:5288",
            ["PosApi:TimeoutSeconds"] = "15",
            ["PosBusinessApi:BaseUrl"] = "http://10.0.2.2:5290",
            ["PosBusinessApi:TimeoutSeconds"] = "15"
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
        services.AddSingleton<IOnboardingPreferenceStore, MauiOnboardingPreferenceStore>();
        services.AddSingleton<IConnectivityService, MauiConnectivityService>();
        services.AddSingleton<IAppInfoService>(_ => new MauiAppInfoService(environmentName));
        services.AddSingleton<ISecureTokenStore, MauiSecureTokenStore>();
        services.AddSingleton<ISessionStore, SecureSessionStore>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IAuthEventSink, LoggingAuthEventSink>();
        services.AddSingleton<IProductAccessResolver, ProductAccessResolver>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IUtangCapabilityEvaluator, UtangCapabilityEvaluator>();
        services.AddSingleton<IDocumentHandoffService, MauiDocumentHandoffService>();
        services.AddSingleton<NavigationGate>();

        services.AddSingleton<ThemeController>();
        services.AddSingleton<DensityController>();
        services.AddSingleton<CultureController>();
        services.AddSingleton<ApiStatusLocalizer>();

        services.AddPosApiClient(configuration);
        services.AddSingleton<PosStatusState>();
    }
}
