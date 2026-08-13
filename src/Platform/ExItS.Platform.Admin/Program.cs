using ExItS.Platform.Admin;
using ExItS.Platform.Admin.Services;
using ExItS.Web.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using ExItS.Platform.Admin.Components;

var builder = WebApplication.CreateBuilder(args);

AdminProductionSecurityGuard.ValidateOrThrow(builder);
if (builder.Configuration.GetValue<bool>("LocalValidation:Enabled") && builder.Environment.IsProduction())
{
    throw new InvalidOperationException("LocalValidation:Enabled=true is forbidden in Production.");
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Development/Testing: surface real circuit exceptions in the browser console
// (binds to CircuitOptions.DetailedErrors via the DetailedErrors configuration key).
if (builder.Environment.IsDevelopment()
    || builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration["DetailedErrors"] = "true";
}

builder.Services.AddAntDesign();
builder.Services.Configure<ExItSWebHostOptions>(builder.Configuration.GetSection(ExItSWebHostOptions.SectionName));
builder.Services.AddScoped<WebPostLoginRouter>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLocalization();

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

builder.Services.Configure<PlatformApiOptions>(builder.Configuration.GetSection(PlatformApiOptions.SectionName));
builder.Services.Configure<LocalValidationAdminOptions>(
    builder.Configuration.GetSection(LocalValidationAdminOptions.SectionName));
builder.Services.AddScoped<LocalValidationSignInService>();
builder.Services.Configure<DevelopmentOperatorOptions>(options =>
{
    if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
    {
        options.DisplayName = "Dev Operator";
    }
    else
    {
        options.DisplayName = string.Empty;
    }
});

var isLocalTestHost = builder.Environment.IsDevelopment()
    || builder.Environment.IsEnvironment("Testing");

if (!isLocalTestHost)
{
    // Production-like hosts: persist DataProtection keys when configured.
    var keysPath = builder.Configuration["DataProtection:KeysPath"];
    if (!string.IsNullOrWhiteSpace(keysPath))
    {
        Directory.CreateDirectory(keysPath);
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("ExItS.Platform.Admin");
    }
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".ExItS.Admin.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = isLocalTestHost
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddAuthorization(options =>
{
    // Production-like hosts require authentication. Development/Testing remain open for local work.
    if (!isLocalTestHost)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
});

builder.Services.AddTransient<PlatformSessionForwardingHandler>();
builder.Services.AddScoped<PlatformBrowserSessionService>();
builder.Services.AddScoped<PlatformCircuitSession>();
builder.Services.AddScoped<AdminSessionExpiryCoordinator>();
builder.Services.AddScoped<CircuitHandler, PlatformSessionCircuitHandler>();


builder.Services.AddHttpClient<IPlatformApiClient, PlatformApiClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<PlatformApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).AddHttpMessageHandler<PlatformSessionForwardingHandler>();

builder.Services.AddHttpClient("PlatformApi", (services, client) =>
{
    var options = services.GetRequiredService<IOptions<PlatformApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).AddHttpMessageHandler<PlatformSessionForwardingHandler>();

builder.Services.AddHttpClient("PlatformApiUnauthenticated", (services, client) =>
{
    var options = services.GetRequiredService<IOptions<PlatformApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<CultureService>();
builder.Services.AddScoped<PlatformPermissionState>();
builder.Services.AddScoped<AdminShellContext>();
builder.Services.AddScoped<UserTimeZoneState>();
builder.Services.AddScoped<OrganizationDeepLinkGuard>();
builder.Services.AddScoped<ToastService>();

builder.AddAdminForwardedHeaders();

var app = builder.Build();

app.UseAdminForwardedHeaders();

if (isLocalTestHost)
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!isLocalTestHost)
{
    app.UseHttpsRedirection();
}

app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// FallbackPolicy RequireAuthenticatedUser applies to MapStaticAssets; login CSS/JS must stay anonymous.
app.MapStaticAssets().AllowAnonymous();
app.MapExitsCultureSet();

app.MapGet("/", (HttpContext http) =>
    http.User.Identity?.IsAuthenticated == true
        ? Results.Redirect("/admin")
        : Results.Redirect("/admin/login"));

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "platform-admin" }))
    .AllowAnonymous();

// Full HTTP round-trip so auth cookies are set (Interactive Server cannot SignIn from a circuit event).
app.MapPost("/admin/login/credentials", async (
    HttpContext http,
    PlatformBrowserSessionService sessions,
    WebPostLoginRouter router) =>
{
    var form = await http.Request.ReadFormAsync().ConfigureAwait(false);
    // Public login is email-only; UsernameOrEmail kept for older clients.
    var email = form["Email"].ToString();
    if (string.IsNullOrWhiteSpace(email))
    {
        email = form["UsernameOrEmail"].ToString();
    }

    var password = form["Password"].ToString();
    var returnApp = form["ReturnApp"].ToString();
    var returnPath = form["ReturnPath"].ToString();
    var (ok, error) = await sessions.LoginAsync(email, password).ConfigureAwait(false);
    if (!ok)
    {
        return Results.Redirect(
            "/admin/login?error=" + Uri.EscapeDataString(error ?? "Invalid email or password."));
    }

    var next = await router.ResolveAsync(http, returnApp, returnPath).ConfigureAwait(false);
    return Results.Redirect(next);
}).AllowAnonymous().DisableAntiforgery();

// Local Validation only: full HTTP round-trip so auth cookies are set (Interactive Server cannot).
app.MapGet("/admin/login/as/{key}", async (
    string key,
    string? returnApp,
    string? returnPath,
    LocalValidationSignInService localValidation,
    WebPostLoginRouter router,
    IHostEnvironment env,
    HttpContext http) =>
{
    if (env.IsProduction() || !localValidation.IsAvailable)
    {
        return Results.NotFound();
    }

    var (ok, error) = await localValidation.SignInAsKeyAsync(key).ConfigureAwait(false);
    if (!ok)
    {
        return Results.Redirect(
            "/admin/login?error=" + Uri.EscapeDataString(error ?? "Invalid email or password."));
    }

    // Route by the selected identity's account class so one picker can land on 8090/8093/8094.
    var identities = await localValidation.ListIdentitiesAsync().ConfigureAwait(false);
    var selected = identities.FirstOrDefault(i =>
        string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
    var appFromIdentity = selected?.AccountClass?.Trim() switch
    {
        "Organization" => WebApps.Organization,
        "Personal" => WebApps.Personal,
        "Platform" => WebApps.Platform,
        _ => null
    };

    var next = await router.ResolveAsync(
            http,
            appFromIdentity ?? returnApp,
            returnPath,
            selected?.OrganizationId)
        .ConfigureAwait(false);
    return Results.Redirect(next);
}).AllowAnonymous();

app.MapPost("/admin/logout", async (HttpContext http, PlatformBrowserSessionService sessions) =>
{
    await sessions.LogoutAsync().ConfigureAwait(false);
    return Results.Redirect(LoginRedirectWithOptionalNotice(http.Request.Query["notice"]));
}).DisableAntiforgery();

app.MapGet("/admin/logout", async (HttpContext http, PlatformBrowserSessionService sessions) =>
{
    await sessions.LogoutAsync().ConfigureAwait(false);
    return Results.Redirect(LoginRedirectWithOptionalNotice(http.Request.Query["notice"]));
});

// Full HTTP round-trip so auth cookies are set after Interactive Server flows that mint a new
// Platform session (e.g. Start a Business). Circuit events cannot SignIn.
app.MapGet("/admin/handoff/{app}", async (
    string app,
    Guid? organizationId,
    string? returnPath,
    HttpContext http,
    IHttpClientFactory httpClientFactory,
    IOptions<ExItSWebHostOptions> hosts) =>
{
    if (!WebApps.IsKnown(app))
    {
        return Results.Redirect("/admin/workspaces");
    }

    var token = PlatformBrowserSessionService.ResolveSessionToken(http);
    if (string.IsNullOrWhiteSpace(token))
    {
        return Results.Redirect(hosts.Value.CanonicalLoginUrl(app, returnPath));
    }

    if (string.Equals(WebApps.Normalize(app), WebApps.Platform, StringComparison.OrdinalIgnoreCase))
    {
        return Results.Redirect(SafeReturnPath.Sanitize(returnPath, "/admin"));
    }

    var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
    var created = await WebHandoffHttp.CreateAsync(
        client,
        token,
        WebApps.Normalize(app),
        organizationId,
        returnPath).ConfigureAwait(false);
    if (created is null)
    {
        return Results.Redirect("/admin/workspaces");
    }

    return Results.Redirect(WebHandoffHttp.EstablishUrl(hosts.Value.GetOrigin(app), created.Ticket, created.ReturnPath));
});

app.MapGet("/admin/session/establish", async (
    string sessionToken,
    string? returnUrl,
    PlatformBrowserSessionService sessions) =>
{
    if (string.IsNullOrWhiteSpace(sessionToken))
    {
        return Results.Redirect(
            "/admin/login?error=" + Uri.EscapeDataString("Session token is missing."));
    }

    var (ok, error) = await sessions.EstablishFromSessionTokenAsync(sessionToken).ConfigureAwait(false);
    if (!ok)
    {
        return Results.Redirect(
            "/admin/login?error=" + Uri.EscapeDataString(error ?? "Could not establish the browser session."));
    }

    return Results.Redirect(SanitizeAdminReturnUrl(returnUrl));
}).AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

static string LoginRedirectWithOptionalNotice(string? notice)
{
    if (string.IsNullOrWhiteSpace(notice))
    {
        return "/admin/login";
    }

    var trimmed = notice.Trim();
    if (trimmed.Length > 280)
    {
        trimmed = trimmed[..280];
    }

    return "/admin/login?notice=" + Uri.EscapeDataString(trimmed);
}

static string SanitizeAdminReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl)
        || !returnUrl.StartsWith('/')
        || returnUrl.StartsWith("//", StringComparison.Ordinal)
        || returnUrl.Contains('\\', StringComparison.Ordinal)
        || returnUrl.Contains('\n')
        || returnUrl.Contains('\r')
        || !returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
    {
        return "/admin";
    }

    return returnUrl;
}

app.Run();

public partial class Program;
