using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Api.Identity;

/// <summary>
/// HttpOnly Platform session cookie Secure flag. Aligned with
/// <c>ExItSLocalValidationCookies.AllowHttpAuthCookies</c>: Production and generic Staging HTTP
/// stay Secure; explicit Local Validation HTTP (non-Production) uses SameAsRequest.
/// </summary>
public static class PlatformSessionCookiePolicy
{
    public static bool AllowHttpAuthCookies(
        bool isProduction,
        bool isDevelopment,
        bool isTesting,
        bool localValidationEnabled) =>
        isDevelopment
        || isTesting
        || (localValidationEnabled && !isProduction);

    public static bool IsSecure(
        bool isProduction,
        bool isDevelopment,
        bool isTesting,
        bool localValidationEnabled,
        bool requestIsHttps)
    {
        if (isProduction)
        {
            return true;
        }

        if (!AllowHttpAuthCookies(isProduction, isDevelopment, isTesting, localValidationEnabled))
        {
            return true;
        }

        return requestIsHttps;
    }

    public static bool IsSecure(
        IHostEnvironment environment,
        IConfiguration configuration,
        bool requestIsHttps) =>
        IsSecure(
            environment.IsProduction(),
            environment.IsDevelopment(),
            environment.IsEnvironment("Testing"),
            configuration.GetValue("LocalValidation:Enabled", false),
            requestIsHttps);
}
