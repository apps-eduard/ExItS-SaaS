using Microsoft.AspNetCore.Antiforgery;

namespace ExItS.Platform.Api.Common;

internal static class PlatformBrowserAntiforgeryExtensions
{
    internal static void AddPlatformBrowserAntiforgery(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = PlatformAntiforgeryDefaults.HeaderName;
            options.Cookie.Name = PlatformAntiforgeryDefaults.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            // Shared LocalValidation/HTTP-safe policy (React Admin + React POS).
            options.Cookie.SecurePolicy = PlatformAuthCookiePolicy.SecurePolicy(environment, configuration);
        });
    }

    internal static IApplicationBuilder UsePlatformBrowserAntiforgery(this IApplicationBuilder app) =>
        app.UseMiddleware<PlatformBrowserAntiforgeryMiddleware>();
}
