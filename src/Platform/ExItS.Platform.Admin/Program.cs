using ExItS.Platform.Admin.Components;
using ExItS.Platform.Admin.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
builder.Services.AddHttpClient<IPlatformApiClient, PlatformApiClient>((services, client) =>
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRequestLocalization();

app.UseAntiforgery();

app.MapStaticAssets();

// Prefer an immediate HTTP redirect over the Blazor template-style home page.
app.MapGet("/", () => Results.Redirect("/admin"));

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
