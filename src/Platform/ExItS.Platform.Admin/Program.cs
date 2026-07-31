using ExItS.Platform.Admin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using ExItS.Platform.Admin.Components;

var builder = WebApplication.CreateBuilder(args);

AdminProductionSecurityGuard.ValidateOrThrow(builder);
if (builder.Configuration.GetValue<bool>("LivePreview:Enabled") && builder.Environment.IsProduction())
{
    throw new InvalidOperationException("LivePreview:Enabled=true is forbidden in Production.");
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
builder.Services.Configure<LivePreviewAdminOptions>(builder.Configuration.GetSection(LivePreviewAdminOptions.SectionName));
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

var livePreviewEnabled = builder.Configuration.GetValue<bool>("LivePreview:Enabled")
    && !builder.Environment.IsProduction();

if (livePreviewEnabled)
{
    // File-system key ring for the container lifetime (tmpfs-friendly) so antiforgery stays consistent while up.
    var keysPath = builder.Configuration["DataProtection:KeysPath"]
        ?? Path.Combine(Path.GetTempPath(), "exits-admin-dp-keys");
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("ExItS.Platform.Admin");
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".ExItS.Admin.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            || builder.Environment.IsEnvironment("Testing")
            || livePreviewEnabled
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
    // Live preview and Staging/Production require authentication; Development/Testing remain open unless LivePreview is on.
    if (livePreviewEnabled
        || !(builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")))
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
});

builder.Services.AddTransient<PlatformSessionForwardingHandler>();
builder.Services.AddScoped<PlatformBrowserSessionService>();

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
builder.Services.AddScoped<ToastService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    if (!livePreviewEnabled)
    {
        app.UseHsts();
    }
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!livePreviewEnabled)
{
    app.UseHttpsRedirection();
}

app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// FallbackPolicy RequireAuthenticatedUser applies to MapStaticAssets; login CSS/JS must stay anonymous.
app.MapStaticAssets().AllowAnonymous();

app.MapGet("/", () => Results.Redirect("/admin"));

app.MapPost("/admin/login/credentials", async (
    HttpContext http,
    PlatformBrowserSessionService sessions) =>
{
    var form = await http.Request.ReadFormAsync().ConfigureAwait(false);
    var usernameOrEmail = form["UsernameOrEmail"].ToString();
    var password = form["Password"].ToString();
    var (ok, error) = await sessions.LoginAsync(usernameOrEmail, password).ConfigureAwait(false);
    if (!ok)
    {
        return Results.Redirect(
            "/admin/login?error=" + Uri.EscapeDataString(error ?? "Invalid username/email or password."));
    }

    return Results.Redirect("/admin");
}).AllowAnonymous();

app.MapPost("/admin/login/live-preview", async (
    HttpContext http,
    PlatformBrowserSessionService sessions,
    IOptions<LivePreviewAdminOptions> livePreview) =>
{
    if (!livePreview.Value.Enabled)
    {
        return Results.NotFound();
    }

    var form = await http.Request.ReadFormAsync().ConfigureAwait(false);
    var identityKey = form["IdentityKey"].ToString();
    if (string.IsNullOrWhiteSpace(identityKey))
    {
        return Results.Redirect(
            "/admin/login?error=" + Uri.EscapeDataString("Select a live preview test identity."));
    }

    var (ok, error) = await sessions.LivePreviewLoginAsync(identityKey).ConfigureAwait(false);
    if (!ok)
    {
        return Results.Redirect(
            "/admin/login?error=" + Uri.EscapeDataString(error ?? "Live preview sign-in failed."));
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

app.MapGet("/culture/set", (string culture, string? redirectUri, HttpContext context) =>
{
    var normalized = culture == "fil-PH" ? "fil-PH" : "en";
    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalized)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

    var target = string.IsNullOrWhiteSpace(redirectUri) || !Uri.IsWellFormedUriString(redirectUri, UriKind.Relative)
        ? "/"
        : redirectUri;
    return Results.LocalRedirect(target);
});

app.Run();

public partial class Program;
