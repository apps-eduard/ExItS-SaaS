using System.Globalization;
using ExItS.Personal.Web.Components;
using ExItS.Personal.Web.Services;
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

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
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

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("fil-PH") };
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

var isLocalTestHost = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");
var allowHttpAuthCookies = ExItSLocalValidationCookies.AllowHttpAuthCookies(
    builder.Environment,
    builder.Configuration);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".ExItS.PersonalWeb.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = ExItSLocalValidationCookies.AuthCookieSecurePolicy(
            builder.Environment,
            builder.Configuration);
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
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    }
});

builder.Services.AddScoped<PersonalCircuitSession>();
builder.Services.AddScoped<PersonalWebSessionService>();
builder.Services.AddScoped<PersonalApiClient>();
builder.Services.AddScoped<CircuitHandler, PersonalSessionCircuitHandler>();
builder.Services.AddHttpClient("PlatformApi", (services, client) =>
{
    var baseUrl = services.GetRequiredService<IConfiguration>()["PlatformApi:BaseUrl"] ?? "http://127.0.0.1:5288";
    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();
if (isLocalTestHost) { app.UseDeveloperExceptionPage(); }
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    if (!allowHttpAuthCookies) { app.UseHsts(); }
}
if (!allowHttpAuthCookies) { app.UseHttpsRedirection(); }

app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets().AllowAnonymous();
app.MapExitsCultureSet();

app.MapGet("/", (HttpContext http, IOptions<ExItSWebHostOptions> hosts) =>
    http.User.Identity?.IsAuthenticated == true
        ? Results.Redirect("/home")
        : Results.Redirect(hosts.Value.CanonicalLoginUrl(WebApps.Personal, "/home")));

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "personal-web" })).AllowAnonymous();

app.MapGet("/logout", async (PersonalWebSessionService sessions, IOptions<ExItSWebHostOptions> hosts) =>
{
    await sessions.LogoutAsync().ConfigureAwait(false);
    return Results.Redirect(hosts.Value.CanonicalLoginUrl());
});

app.MapGet("/session/establish", async (string? ticket, string? returnPath, PersonalWebSessionService sessions) =>
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

    return Results.Redirect(SafeReturnPath.Sanitize(returnPath ?? path, "/home"));
}).AllowAnonymous();

app.MapGet("/handoff/{appName}", async (
    string appName,
    HttpContext http,
    IHttpClientFactory httpClientFactory,
    IOptions<ExItSWebHostOptions> hosts) =>
{
    if (!WebApps.IsKnown(appName))
    {
        return Results.Redirect("/home");
    }

    var token = PersonalWebSessionService.ResolveSessionToken(http);
    if (string.IsNullOrWhiteSpace(token))
    {
        return Results.Redirect(hosts.Value.CanonicalLoginUrl(appName));
    }

    var client = httpClientFactory.CreateClient("PlatformApi");
    var created = await WebHandoffHttp.CreateAsync(client, token, WebApps.Normalize(appName), null, null)
        .ConfigureAwait(false);
    if (created is null)
    {
        return Results.Redirect("/home");
    }

    return Results.Redirect(WebHandoffHttp.EstablishUrl(hosts.Value.GetOrigin(appName), created.Ticket, created.ReturnPath));
});

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

public partial class Program;
