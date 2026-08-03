using ExItS.Platform.Admin;
using ExItS.Platform.Admin.Services;
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

app.MapGet("/", (HttpContext http) =>
    http.User.Identity?.IsAuthenticated == true
        ? Results.Redirect("/admin")
        : Results.Redirect("/admin/login"));

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "platform-admin" }))
    .AllowAnonymous();

// Full HTTP round-trip so auth cookies are set (Interactive Server cannot SignIn from a circuit event).
app.MapPost("/admin/login/credentials", async (
    HttpContext http,
    PlatformBrowserSessionService sessions) =>
{
    var form = await http.Request.ReadFormAsync().ConfigureAwait(false);
    // Public login is email-only; UsernameOrEmail kept for older clients.
    var email = form["Email"].ToString();
    if (string.IsNullOrWhiteSpace(email))
    {
        email = form["UsernameOrEmail"].ToString();
    }

    var password = form["Password"].ToString();
    var (ok, error) = await sessions.LoginAsync(email, password).ConfigureAwait(false);
    if (!ok)
    {
        return Results.Redirect(
            "/admin/login?error=" + Uri.EscapeDataString(error ?? "Invalid email or password."));
    }

    return Results.Redirect("/admin");
}).AllowAnonymous().DisableAntiforgery();

// Local Validation only: full HTTP round-trip so auth cookies are set (Interactive Server cannot).
app.MapGet("/admin/login/as/{key}", async (
    string key,
    LocalValidationSignInService localValidation,
    IHostEnvironment env) =>
{
    if (env.IsProduction() || !localValidation.IsAvailable)
    {
        return Results.NotFound();
    }

    var (ok, error) = await localValidation.SignInAsKeyAsync(key).ConfigureAwait(false);
    if (!ok)
    {
        return Results.Redirect(
            "/admin/login?error=" + Uri.EscapeDataString(error ?? "Invalid username/email or password."));
    }

    return Results.Redirect("/admin");
}).AllowAnonymous();

app.MapPost("/admin/logout", async (PlatformBrowserSessionService sessions) =>
{
    await sessions.LogoutAsync().ConfigureAwait(false);
    return Results.Redirect("/admin/login");
}).DisableAntiforgery();

app.MapGet("/admin/logout", async (PlatformBrowserSessionService sessions) =>
{
    await sessions.LogoutAsync().ConfigureAwait(false);
    return Results.Redirect("/admin/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
