using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ExItS.Web.UI;

/// <summary>
/// Local Validation Docker runs Staging over HTTP on LAN/Tailscale hosts.
/// Browsers drop Secure cookies on http://100.x / LAN IPs (localhost is a special case),
/// which bounces sign-in back to the login page.
/// </summary>
public static class ExItSLocalValidationCookies
{
    public static bool AllowHttpAuthCookies(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment()
        || environment.IsEnvironment("Testing")
        || (configuration.GetValue("LocalValidation:Enabled", false) && !environment.IsProduction());

    public static CookieSecurePolicy AuthCookieSecurePolicy(
        IHostEnvironment environment,
        IConfiguration configuration) =>
        AllowHttpAuthCookies(environment, configuration)
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

    /// <summary>
    /// SameAsRequest when HTTP auth cookies are allowed; otherwise always Secure.
    /// Production remains Secure even over HTTP. Generic Staging HTTP stays Secure.
    /// </summary>
    public static bool SessionCookieSecure(
        IHostEnvironment environment,
        IConfiguration configuration,
        bool requestIsHttps)
    {
        if (environment.IsProduction())
        {
            return true;
        }

        if (!AllowHttpAuthCookies(environment, configuration))
        {
            return true;
        }

        return requestIsHttps;
    }

    public static bool SessionTokenSecure(HttpRequest request) => request.IsHttps;
}
