using System.Globalization;
using System.Reflection;
using ExItS.DesignSystem.Abstractions;
using ExItS.PinoyBusinessPOS.ApiClient;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Options;
using ExItS.PinoyBusinessPOS.Application.Support;
using ExItS.PinoyBusinessPOS.LocalStore;
using ExItS.PinoyBusinessPOS.Maui.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;

namespace ExItS.PinoyBusinessPOS.Maui;

public static class MauiProgram
{
    private const string LocalValidationPublicHost = "100.120.79.81";
    private const string AndroidEmulatorHostLoopback = "10.0.2.2";
    // LEGACY-MAUI-ISO-01: MAUI Docker stack (Start-MauiLegacyLocalValidation), not React :8091/:8092.
    private const int LocalValidationPlatformPort = 8191;
    private const int LocalValidationPosPort = 8192;

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
        // LEGACY-MAUI-ISO-01: Debug targets exits-maui-local-validation (:8191/:8192), not React (:8091/:8092).
        // Physical device: Tailscale PublicHost. Emulator: 10.0.2.2 host loopback.
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
#if DEBUG
            // Matches deploy/docker/.env.maui-local-validation shared password (local only).
            ["LocalValidation:Enabled"] = "true",
            ["LocalValidation:SharedPassword"] = "LivePreviewLocal1!",
            // MAUI Mailpit UI host port (compose maps 8125→8025). React stack uses 8025.
            ["LocalValidation:MailpitUiPort"] = "8125",
            ["LocalValidation:AdminUiPort"] = "8190",
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

#if DEBUG && POS_LOCAL_VALIDATION_EMULATOR_LOOPBACK
        using (var emulator = Assembly.GetExecutingAssembly()
                   .GetManifestResourceStream("appsettings.LocalValidation.Emulator.json"))
        {
            if (emulator is not null)
            {
                configuration.AddJsonStream(emulator);
            }
        }
#endif

#if DEBUG
        // Final override wins over embedded appsettings.json (Tailscale defaults).
        ApplyLocalValidationApiEndpoints(configuration);
        AssertMauiStackPortsNotReact(configuration);
#endif
    }

#if DEBUG
    /// <summary>
    /// Fail closed if Debug Local Validation is pointed at the React stack (:8091/:8092).
    /// MAUI must use exits-maui-local-validation (:8191/:8192) only.
    /// </summary>
    private static void AssertMauiStackPortsNotReact(IConfiguration configuration)
    {
        var platform = configuration["PosApi:BaseUrl"] ?? string.Empty;
        var pos = configuration["PosBusinessApi:BaseUrl"] ?? string.Empty;
        if (ContainsPort(platform, 8091) || ContainsPort(pos, 8092))
        {
            throw new InvalidOperationException(
                "MAUI Debug Local Validation must use Platform :8191 and POS :8192 " +
                $"(exits-maui-local-validation), not React :8091/:8092. Configured PosApi={platform}; PosBusinessApi={pos}.");
        }
    }

    private static bool ContainsPort(string baseUrl, int port) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && uri.Port == port;

    private static void ApplyLocalValidationApiEndpoints(IConfigurationBuilder configuration)
    {
        var (platformBaseUrl, posBaseUrl) = ResolveLocalValidationApiBaseUrls();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PosApi:BaseUrl"] = platformBaseUrl,
            ["PosApi:TimeoutSeconds"] = "15",
            ["PosBusinessApi:BaseUrl"] = posBaseUrl,
            ["PosBusinessApi:TimeoutSeconds"] = "15",
        });
    }

    private static (string PlatformBaseUrl, string PosBaseUrl) ResolveLocalValidationApiBaseUrls()
    {
#if POS_LOCAL_VALIDATION_EMULATOR_LOOPBACK
        return (
            FormatLocalValidationBaseUrl(AndroidEmulatorHostLoopback, LocalValidationPlatformPort),
            FormatLocalValidationBaseUrl(AndroidEmulatorHostLoopback, LocalValidationPosPort));
#endif

        if (DeviceInfo.Current.Platform == DevicePlatform.Android
            && DeviceInfo.Current.DeviceType == DeviceType.Virtual)
        {
            return (
                FormatLocalValidationBaseUrl(AndroidEmulatorHostLoopback, LocalValidationPlatformPort),
                FormatLocalValidationBaseUrl(AndroidEmulatorHostLoopback, LocalValidationPosPort));
        }

        return (
            FormatLocalValidationBaseUrl(LocalValidationPublicHost, LocalValidationPlatformPort),
            FormatLocalValidationBaseUrl(LocalValidationPublicHost, LocalValidationPosPort));
    }

    private static string FormatLocalValidationBaseUrl(string host, int port) =>
        $"http://{host}:{port}";
#endif

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
        services.AddSingleton<BuyerShareDraftState>();
        services.AddSingleton<PurchaseOrderDraftSession>();
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
        services.AddSingleton<IDeviceRecoveryCredentialStore, DeviceRecoveryCredentialStore>();
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
        services.AddSingleton<SellingModeService>();
        services.AddSingleton<IAccessibleBranchResolver, OwnerAccessibleBranchResolver>();
        services.AddSingleton<IWorkspaceSelectionService, WorkspaceSelectionService>();
        services.AddSingleton<AuthenticationService>();
        services.AddSingleton<IAuthenticationService>(sp => sp.GetRequiredService<AuthenticationService>());
        services.AddSingleton<IPlatformAccessTokenRecovery>(sp => sp.GetRequiredService<AuthenticationService>());
        services.AddSingleton<PostSignInReturnRoute>();
        services.AddSingleton<IUtangCapabilityEvaluator, UtangCapabilityEvaluator>();
        services.Configure<LocalValidationClientOptions>(configuration.GetSection(LocalValidationClientOptions.SectionName));
        services.AddSingleton<IDocumentHandoffService, MauiDocumentHandoffService>();
        services.AddSingleton<StoreHeaderState>();
        services.AddSingleton<ShellNotificationUnreadState>();
        services.AddSingleton<AuthShellIdentityState>();
        services.AddSingleton<RoleHomeResolver>();
        services.AddSingleton<NavigationGate>();
        services.AddSingleton<OfflineFoundationDiagnostics>();
        services.AddSingleton<IOrganizationOwnerProbe, PlatformOrganizationOwnerProbe>();
        services.AddSingleton<IWorkspaceGovernanceGate, WorkspaceGovernanceGate>();
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
        services.AddSingleton<IOfflineOperationDispatcher, CatalogProductCreateOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, PersonalContactUpsertOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, PersonalRelationshipCreateOfflineDispatcher>();
        services.AddSingleton<IOfflineOperationDispatcher, PersonalEntryRecordOfflineDispatcher>();
        services.AddSingleton<ICustomerCreditOfflineSyncService, CustomerCreditOfflineSyncService>();
        services.AddSingleton<IPersonalOfflineSyncService, PersonalOfflineSyncService>();
        services.AddSingleton<IOfflineReconnectAutoSync, OfflineReconnectAutoSyncService>();
        services.AddSingleton<ILocalSellingCatalogSyncService, LocalSellingCatalogSyncService>();
        services.AddSingleton<ILinkedSupplierProductSyncService, LinkedSupplierProductSyncService>();
        services.AddSingleton<PosStatusState>();
        // The checkout cart lives only in memory for the signed-in session and clears itself on
        // sign-out or organization switch; it is never persisted or queued.
        services.AddSingleton<SaleCartService>();
        services.AddSingleton<PersonalMerchantCart>();
        services.AddSingleton<IProductImageCacheRoot, MauiProductImageCacheRoot>();
        services.AddSingleton<ProductImageThumbnailCache>();
        services.AddSingleton<PendingProductImageStore>();
        services.AddSingleton<AdoptedTemplateThumbnailPrefetch>();
        services.AddSingleton<ICatalogProductOfflineSyncService, CatalogProductOfflineSyncService>();
        services.AddSingleton<IProductImagePicker, MauiProductImagePicker>();
        services.AddSingleton<MauiPendingPaymentStore>();
        // Phase marker: P8-WP02-simple-sales
    }
}
