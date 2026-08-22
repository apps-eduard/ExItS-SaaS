using Microsoft.AspNetCore.Antiforgery;

namespace ExItS.Platform.Api.Common;

internal static class PlatformBrowserAntiforgeryExtensions
{
    internal static void AddPlatformBrowserAntiforgery(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = PlatformAntiforgeryDefaults.HeaderName;
            options.Cookie.Name = PlatformAntiforgeryDefaults.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment() || environment.IsEnvironment("Testing")
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });
    }

    internal static IApplicationBuilder UsePlatformBrowserAntiforgery(this IApplicationBuilder app) =>
        app.UseMiddleware<PlatformBrowserAntiforgeryMiddleware>();
}
