using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Api.Authentication;

/// <summary>
/// Platform session cookie Secure flag. Aligns with
/// <c>ExItSLocalValidationCookies.AllowHttpAuthCookies</c> without referencing Web.UI.
/// Production is always Secure. Generic Staging without Local Validation is Secure.
/// </summary>
internal static class PlatformSessionCookiePolicy
{
    public static bool CookieSecure(
        bool requestIsHttps,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        if (environment.IsProduction())
        {
            return true;
        }

        var allowHttpAuthCookies = environment.IsDevelopment()
            || environment.IsEnvironment("Testing")
            || configuration.GetValue("LocalValidation:Enabled", false);

        if (!allowHttpAuthCookies)
        {
            return true;
        }

        return requestIsHttps;
    }
}
