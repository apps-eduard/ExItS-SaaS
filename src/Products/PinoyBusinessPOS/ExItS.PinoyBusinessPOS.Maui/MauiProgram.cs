using System.Globalization;
using System.Reflection;
using ExItS.DesignSystem.Abstractions;
using ExItS.PinoyBusinessPOS.ApiClient;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Options;
using ExItS.PinoyBusinessPOS.Application.Support;
using ExItS.PinoyBusinessPOS.LocalStore;
using ExItS.PinoyBusinessPOS.Maui.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExItS.PinoyBusinessPOS.Maui;

public static class MauiProgram
{
    /// <summary>
    /// Development-stage notice (P7-WP04): authentication uses the approved Development/Testing
    /// Platform identity mechanism only. Local SQLite foundation, DeviceId, encrypted offline queue,
    /// customer/credit/repayment offline workflows, and Dev probe sync are available.
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

        var app = builder.Build();
        // Start reconnect auto-sync as soon as the DI container is live.
        app.Services.GetRequiredService<IOfflineReconnectAutoSync>().Start();
        return app;
    }

    private static void ConfigureAppConfiguration(ConfigurationManager configuration)
    {
        // Emulator Local Validation default: 10.0.2.2 (emulator → host loopback).
        // adb reverse to 127.0.0.1 is unreliable on some Windows/ADB setups; 10.0.2.2 works without reverse.
        // PhysicalDevice Debug builds overlay wwwroot/appsettings.LocalValidation.PhysicalDevice.json.
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Local Validation stack (Start-LocalValidation.ps1): Platform :8091, POS :8092.
            ["PosApi:BaseUrl"] = "http://10.0.2.2:8091",
            ["PosApi:TimeoutSeconds"] = "15",
            ["PosBusinessApi:BaseUrl"] = "http://10.0.2.2:8092",
            ["PosBusinessApi:TimeoutSeconds"] = "15",
#if DEBUG
            // Matches deploy/docker/.env.local-validation LOCAL_VALIDATION_SHARED_PASSWORD (local only).
            ["LocalValidation:Enabled"] = "true",
            ["LocalValidation:SharedPassword"] = "LivePreviewLocal1!",
#else
            // Production/Release: API base URLs must be HTTPS (ApiClient MAUI-HTTPS validation).
            ["Security:RequireHttpsApiUrls"] = "true",
#endif
        });

        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("appsettings.json"))
        {
            if (stream is not null)
            {
                configuration.AddJsonStream(stream);
            }
        }

#if DEBUG && POS_LOCAL_VALIDATION_PHYSICAL_DEVICE
        // Separate PhysicalDevice Local Validation profile (Tailscale/LAN). Never embedded in Release.
        using (var physical = Assembly.GetExecutingAssembly()
                   .GetManifestResourceStream("appsettings.LocalValidation.PhysicalDevice.json"))
        {
            if (physical is not null)
            {
                configuration.AddJsonStream(physical);
            }
        }
#endif
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
        services.AddSingleton<IQrCodeScanService, MauiQrCodeScanService>();
        services.AddSingleton<IAppInfoService>(_ => new MauiAppInfoService(environmentName));
        services.AddSingleton<ISecureTokenStore, MauiSecureTokenStore>();
        services.AddSingleton<ISessionStore, SecureSessionStore>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IAuthEventSink, LoggingAuthEventSink>();
        services.AddSingleton<IProductAccessResolver, ProductAccessResolver>();
        services.AddSingleton<ILocalStoreRootPathProvider, MauiLocalStoreRootPathProvider>();
        services.AddSingleton(TimeProvider.System);
        services.Configure<OfflineOperatingGrantOptions>(
            configuration.GetSection(OfflineOperatingGrantOptions.SectionName));
        services.AddSingleton<IOfflineOperatingGrantStore, OfflineOperatingGrantStore>();
        services.AddSingleton<IOfflineOperatingGrantService, OfflineOperatingGrantService>();
        services.AddSingleton<OfflineSessionUxState>();
        services.AddSingleton<IPosOfflineCapabilityPolicy, PosOfflineCapabilityPolicy>();
        services.AddSingleton<OnlineRequiredGuard>();
        // Scoped: captures BlazorWebView NavigationManager (scoped). Singleton would keep a
        // stale/uninitialized manager and Personal/POS tab clicks would appear dead.
        services.AddScoped<OfflineAwareNavigation>();
        services.AddPinoyBusinessPosLocalStore();
        services.AddSingleton<ProtectedShellAccessPolicy>();
        services.AddSingleton<IProtectedShellAccessPolicy>(sp => sp.GetRequiredService<ProtectedShellAccessPolicy>());
        services.AddSingleton<IPosSyncStatusService>(sp =>
            new PosSyncStatusService(
                sp.GetRequiredService<IConnectivityService>(),
                sp.GetRequiredService<IProtectedShellAccessPolicy>(),
                sp.GetRequiredService<IOfflineOperationQueue>()));
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IUtangCapabilityEvaluator, UtangCapabilityEvaluator>();
        services.Configure<LocalValidationClientOptions>(configuration.GetSection(LocalValidationClientOptions.SectionName));
        services.AddSingleton<IDocumentHandoffService, MauiDocumentHandoffService>();
        services.AddSingleton<SellingModeService>();
        services.AddSingleton<StoreHeaderState>();
        services.AddSingleton<ShellNotificationUnreadState>();
        services.AddSingleton<AuthShellIdentityState>();
        services.AddSingleton<RoleHomeResolver>();
        services.AddSingleton<NavigationGate>();
        services.AddSingleton<OfflineFoundationDiagnostics>();
        services.AddSingleton<IOrganizationOwnerProbe, PlatformOrganizationOwnerProbe>();
        services.AddSingleton<IPersonalDiagnosticsSyncRetry, PersonalDiagnosticsSyncRetry>();
        services.AddSingleton<IOrganizationDiagnosticsSyncRetry, OrganizationDiagnosticsSyncRetry>();
        services.AddSingleton<ISupportDiagnosticsRoleReader, PosEffectiveRoleReader>();
        services.AddSingleton<ISupportDiagnosticsProvider, PersonalSupportDiagnosticsProvider>();
        services.AddSingleton<ISupportDiagnosticsProvider, OrganizationSupportDiagnosticsProvider>();
        services.AddSingleton<ISupportDiagnosticsService, SupportDiagnosticsService>();

        services.AddSingleton<ThemeController>();
        services.AddSingleton<DensityController>();
        services.AddSingleton<CultureController>();
        services.AddSingleton<ApiStatusLocalizer>();

        services.AddPosApiClient(configuration);
        services.AddSingleton<IOfflineOperationDispatcher, DevOfflineProbeDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, CustomerCreateOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, CustomerUpdateOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, CreditCreateOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, RepaymentCreateOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, RepaymentReverseOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, CreditReverseOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, CreditDueDateSetOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, SaleCheckoutOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, PersonalContactUpsertOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, PersonalRelationshipCreateOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, PersonalEntryRecordOfflineDispatcher>();
        services.AddSingleton<ICustomerCreditOfflineSyncService, CustomerCreditOfflineSyncService>();
        services.AddSingleton<IPersonalOfflineSyncService, PersonalOfflineSyncService>();
        services.AddSingleton<IOfflineReconnectAutoSync, OfflineReconnectAutoSyncService>();
        services.AddSingleton<ILocalSellingCatalogSyncService, LocalSellingCatalogSyncService>();
        services.AddSingleton<PosStatusState>();
        // The checkout cart lives only in memory for the signed-in session and clears itself on
        // sign-out or organization switch; it is never persisted or queued.
        services.AddSingleton<SaleCartService>();
        services.AddSingleton<MauiPendingPaymentStore>();
        // Phase marker: P8-WP02-simple-sales
    }
}
