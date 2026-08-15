using System.Globalization;
using ExItS.PinoyBusinessPOS.ApiClient;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Platform;
using ExItS.PinoyBusinessPOS.Web.Components;
using ExItS.PinoyBusinessPOS.Web.Services;
using ExItS.Web.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration.GetValue<bool>("LocalValidation:Enabled") && builder.Environment.IsProduction())
{
    throw new InvalidOperationException("LocalValidation:Enabled=true is forbidden in Production.");
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration["DetailedErrors"] = "true";
}

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLocalization();
builder.Services.AddAntDesign();
builder.Services.Configure<ExItSWebHostOptions>(builder.Configuration.GetSection(ExItSWebHostOptions.SectionName));
builder.Services.AddScoped<ExitsWebThemeService>();

var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("fil-PH")
};
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});

var isLocalTestHost = builder.Environment.IsDevelopment()
    || builder.Environment.IsEnvironment("Testing");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".ExItS.OrgWeb.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = isLocalTestHost
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddAuthorization(options =>
{
    if (!isLocalTestHost)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
});

builder.Services.AddSingleton<IAppInfoService, WebAppInfoService>();
builder.Services.AddSingleton<IConnectivityService, AlwaysOnlineConnectivity>();
builder.Services.AddSingleton<IPosSyncStatusService, WebNoOpSyncStatus>();
builder.Services.AddScoped<IDeviceIdentityProvider, WebDeviceIdentityProvider>();
builder.Services.AddScoped<ISecureTokenStore, MemorySecureTokenStore>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IUtangCapabilityEvaluator, UtangCapabilityEvaluator>();
builder.Services.AddScoped<OrgWebCircuitSession>();
builder.Services.AddScoped<OrgWebShellState>();
builder.Services.AddScoped<OrgWebBrowserSessionService>();
builder.Services.AddScoped<OrgWebSessionHydrator>();
builder.Services.AddScoped<CircuitHandler, OrgWebSessionCircuitHandler>();

builder.Services.AddHttpClient("PlatformApiUnauthenticated", (services, client) =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExItS.PinoyBusinessPOS.Application.Options.PosApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddPosApiClient(builder.Configuration);
builder.Services.AddTransient<OrgWebCircuitSessionHeaderHandler>();
// Re-register typed Platform client so Blazor circuit session flows (factory handlers ≠ circuit scope).
builder.Services.AddHttpClient<IPosApiClient, PosApiClient>((provider, client) =>
    {
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExItS.PinoyBusinessPOS.Application.Options.PosApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    })
    .AddHttpMessageHandler<OrgWebCircuitSessionHeaderHandler>()
    .AddHttpMessageHandler<PlatformSessionHeaderHandler>()
    .AddHttpMessageHandler<DevPlatformUserHeaderHandler>()
    .AddHttpMessageHandler<PlatformBearerHandler>()
    .AddHttpMessageHandler<PosApiReachabilityHandler>();
builder.Services.AddScoped<IPlatformAccessClient, PlatformAccessClient>();
builder.Services.AddScoped<IMerchantCatalogDiscoveryClient, MerchantCatalogDiscoveryClient>();

var app = builder.Build();

if (isLocalTestHost)
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (!isLocalTestHost)
{
    app.UseHttpsRedirection();
}

app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets().AllowAnonymous();
app.MapExitsCultureSet();

app.MapGet("/", (HttpContext http, IOptions<ExItSWebHostOptions> hosts) =>
    http.User.Identity?.IsAuthenticated == true
        ? Results.Redirect("/overview")
        : Results.Redirect(hosts.Value.CanonicalLoginUrl(WebApps.Organization, "/overview")));

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "organization-web" }))
    .AllowAnonymous();

app.MapPost("/login/credentials", async (
    HttpContext http,
    OrgWebBrowserSessionService sessions) =>
{
    var form = await http.Request.ReadFormAsync().ConfigureAwait(false);
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();
    var (ok, error) = await sessions.LoginAsync(email, password).ConfigureAwait(false);
    if (!ok)
    {
        return Results.Redirect("/login?error=" + Uri.EscapeDataString(error ?? "Invalid email or password."));
    }

    return Results.Redirect("/overview");
}).AllowAnonymous().DisableAntiforgery();

app.MapPost("/logout", async (OrgWebBrowserSessionService sessions, IOptions<ExItSWebHostOptions> hosts) =>
{
    await sessions.LogoutAsync().ConfigureAwait(false);
    return Results.Redirect(hosts.Value.CanonicalLoginUrl());
}).DisableAntiforgery();

app.MapGet("/logout", async (OrgWebBrowserSessionService sessions, IOptions<ExItSWebHostOptions> hosts) =>
{
    await sessions.LogoutAsync().ConfigureAwait(false);
    return Results.Redirect(hosts.Value.CanonicalLoginUrl());
});

app.MapGet("/session/establish", async (
    string? ticket,
    string? returnPath,
    OrgWebBrowserSessionService sessions) =>
{
    if (string.IsNullOrWhiteSpace(ticket))
    {
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("Handoff ticket is missing."));
    }

    var (ok, error, path) = await sessions.RedeemHandoffAsync(ticket).ConfigureAwait(false);
    if (!ok)
    {
        return Results.Redirect("/login?error=" + Uri.EscapeDataString(error ?? "Could not establish the session."));
    }

    return Results.Redirect(SafeReturnPath.Sanitize(returnPath ?? path, "/overview"));
}).AllowAnonymous();

app.MapGet("/handoff/{app}", async (
    string app,
    HttpContext http,
    OrgWebBrowserSessionService sessions,
    IHttpClientFactory httpClientFactory,
    IOptions<ExItSWebHostOptions> hosts) =>
{
    if (!WebApps.IsKnown(app))
    {
        return Results.Redirect("/overview");
    }

    var token = OrgWebBrowserSessionService.ResolveSessionToken(http);
    if (string.IsNullOrWhiteSpace(token))
    {
        return Results.Redirect(hosts.Value.CanonicalLoginUrl(app));
    }

    var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
    var created = await WebHandoffHttp.CreateAsync(client, token, WebApps.Normalize(app), null, null)
        .ConfigureAwait(false);
    if (created is null)
    {
        return Results.Redirect("/access-denied");
    }

    return Results.Redirect(WebHandoffHttp.EstablishUrl(hosts.Value.GetOrigin(app), created.Ticket, created.ReturnPath));
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
