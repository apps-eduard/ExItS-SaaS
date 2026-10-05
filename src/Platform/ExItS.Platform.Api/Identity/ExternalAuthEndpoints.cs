using System.Security.Claims;
using ExItS.Platform.Api.Authentication;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Api.Identity;

internal static class ExternalAuthEndpoints
{
    private static readonly MemoryCache ReplayCache = new(new MemoryCacheOptions());
    private static readonly TimeSpan ReplayLifetime = TimeSpan.FromMinutes(2);
    public static IEndpointRouteBuilder MapExternalAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/platform/auth/external/{provider}/challenge", async (
            string provider,
            string? returnUrl,
            HttpContext http,
            IOptions<PlatformExternalAuthOptions> options,
            IHostEnvironment env) =>
        {
            if (!TryResolveScheme(provider, options.Value, out var scheme, out var error))
            {
                return PlatformApiResults.Problem(
                    error!,
                    "External authentication provider is unavailable.",
                    StatusCodes.Status404NotFound);
            }

            var safeReturn = SanitizeReturnUrl(returnUrl, env);
            var props = new AuthenticationProperties
            {
                RedirectUri = ExternalAuthCallbackUri.BrowserCompletePath(
                    provider,
                    ExternalAuthCallbackUri.TryCreate(options.Value.PublicBrowserOrigin, out _))
            };
            props.Items[PlatformExternalAuthDefaults.ReturnUrlItemKey] = safeReturn;
            return Results.Challenge(props, [scheme]);
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthLoginRateLimitPolicy)
        .AllowAnonymous();

        app.MapGet("/api/v1/platform/auth/external/{provider}/complete", async (
            string provider,
            HttpContext http,
            CompleteExternalLogin useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            IOptions<PlatformExternalAuthOptions> options,
            IHostEnvironment env,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!TryResolveScheme(provider, options.Value, out var scheme, out var error))
            {
                return PlatformApiResults.Problem(
                    error!,
                    "External authentication provider is unavailable.",
                    StatusCodes.Status404NotFound);
            }

            var auth = await http.AuthenticateAsync(PlatformExternalAuthDefaults.CorrelationScheme)
                .ConfigureAwait(false);
            if (!auth.Succeeded || auth.Principal is null)
            {
                // Fallback: some handlers leave identity on the provider scheme after callback.
                auth = await http.AuthenticateAsync(scheme).ConfigureAwait(false);
            }

            if (!auth.Succeeded || auth.Principal is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.ExternalAuthFailed,
                    "External authentication failed.",
                    StatusCodes.Status401Unauthorized);
            }

            var identity = MapPrincipal(provider, auth.Principal);
            if (identity is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.ExternalAuthFailed,
                    "External authentication failed.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase.ExecuteAsync(
                identity,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString(),
                ct).ConfigureAwait(false);

            await http.SignOutAsync(PlatformExternalAuthDefaults.CorrelationScheme).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return PlatformApiResults.FromResult(result, _ => Results.Ok());
            }

            AuthEndpoints.AppendSessionCookie(
                http,
                result.Value.SessionToken,
                result.Value.ExpiresAtUtc,
                sessionOptions.Value,
                env,
                configuration);

            var returnUrl = auth.Properties?.Items.TryGetValue(
                PlatformExternalAuthDefaults.ReturnUrlItemKey,
                out var stored) == true
                ? stored
                : null;
            returnUrl = SanitizeReturnUrl(returnUrl, env);
            var separator = returnUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            var redirect = $"{returnUrl}{separator}sessionToken={Uri.EscapeDataString(result.Value.SessionToken)}";
            return Results.Redirect(redirect);
        })
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/external/testing/complete", async (
            TestingExternalLoginRequest body,
            HttpContext http,
            CompleteExternalLogin useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            IOptions<PlatformExternalAuthOptions> options,
            IHostEnvironment env,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!(env.IsDevelopment() || env.IsEnvironment("Testing")) || !options.Value.TestingEndpointEnabled)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.ExternalAuthDisabled,
                    "External authentication testing endpoint is unavailable.",
                    StatusCodes.Status403Forbidden);
            }

            var identity = new ExternalLoginIdentity(
                body.Provider ?? string.Empty,
                body.ProviderSubject ?? string.Empty,
                body.Email ?? string.Empty,
                body.EmailVerified,
                body.DisplayName);

            var result = await useCase.ExecuteAsync(
                identity,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString(),
                ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return PlatformApiResults.FromResult(result, _ => Results.Ok());
            }

            AuthEndpoints.AppendSessionCookie(
                http,
                result.Value.SessionToken,
                result.Value.ExpiresAtUtc,
                sessionOptions.Value,
                env,
                configuration);
            return Results.Ok(result.Value);
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthLoginRateLimitPolicy)
        .AllowAnonymous();

        return app;
    }

    /// <summary>
    /// The phone service worker replays the Google callback after the first request
    /// already stored the external login. Finish that replay as a browser redirect.
    /// </summary>
    internal static void RememberGoogleReplay(ClaimsPrincipal? principal, AuthenticationProperties? properties)
    {
        if (principal is null || properties is null)
        {
            return;
        }

        if (!properties.Items.TryGetValue(".xsrf", out var correlationId)
            || string.IsNullOrWhiteSpace(correlationId))
        {
            return;
        }

        var identity = MapPrincipal("google", principal);
        if (identity is null)
        {
            return;
        }

        properties.Items.TryGetValue(PlatformExternalAuthDefaults.ReturnUrlItemKey, out var returnUrl);
        ReplayCache.Set(
            correlationId,
            new GoogleReplay(identity, returnUrl),
            ReplayLifetime);
    }

    internal static async Task ResumeExternalCallbackAsync(
        HttpContext http,
        string provider,
        AuthenticationProperties? failedProperties)
    {
        var resumed = await TryResumeExternalLoginAsync(http, provider, failedProperties)
            .ConfigureAwait(false);
        if (!resumed)
        {
            await RedirectBrowserAsync(http, "/sign-in?external=failed").ConfigureAwait(false);
        }
    }

    private static async Task<bool> TryResumeExternalLoginAsync(
        HttpContext http,
        string provider,
        AuthenticationProperties? failedProperties)
    {
        var auth = await http.AuthenticateAsync(PlatformExternalAuthDefaults.CorrelationScheme)
            .ConfigureAwait(false);
        ExternalLoginIdentity? identity = null;
        string? returnUrl = null;
        if (auth.Succeeded && auth.Principal is not null)
        {
            identity = MapPrincipal(provider, auth.Principal);
            if (auth.Properties?.Items.TryGetValue(
                    PlatformExternalAuthDefaults.ReturnUrlItemKey,
                    out var stored) == true)
            {
                returnUrl = stored;
            }
        }

        if (identity is null
            && failedProperties?.Items.TryGetValue(".xsrf", out var correlationId) == true
            && ReplayCache.TryGetValue(correlationId, out GoogleReplay? replay)
            && replay is not null)
        {
            identity = replay.Identity;
            returnUrl = replay.ReturnUrl;
        }

        if (identity is null)
        {
            return false;
        }

        var useCase = http.RequestServices.GetRequiredService<CompleteExternalLogin>();
        var sessionOptions = http.RequestServices.GetRequiredService<IOptions<PlatformSessionOptions>>();
        var env = http.RequestServices.GetRequiredService<IHostEnvironment>();
        var configuration = http.RequestServices.GetRequiredService<IConfiguration>();
        var result = await useCase.ExecuteAsync(
            identity,
            http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(),
            http.RequestAborted).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return false;
        }

        AuthEndpoints.AppendSessionCookie(
            http,
            result.Value.SessionToken,
            result.Value.ExpiresAtUtc,
            sessionOptions.Value,
            env,
            configuration);

        returnUrl = SanitizeReturnUrl(returnUrl, env);
        var separator = returnUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var redirect = $"{returnUrl}{separator}sessionToken={Uri.EscapeDataString(result.Value.SessionToken)}";
        await RedirectBrowserAsync(http, redirect).ConfigureAwait(false);
        return true;
    }

    private static async Task RedirectBrowserAsync(HttpContext http, string location)
    {
        var safe = location.StartsWith('/') && !location.StartsWith("//", StringComparison.Ordinal)
            ? location
            : "/sign-in";
        http.Response.StatusCode = StatusCodes.Status302Found;
        http.Response.Headers.Location = safe;
        http.Response.Headers.CacheControl = "no-store";
        http.Response.ContentType = "text/html; charset=utf-8";
        var encoded = WebUtility.HtmlEncode(safe);
        await http.Response.WriteAsync(
                $"<!doctype html><meta http-equiv=\"refresh\" content=\"0;url={encoded}\"><p>Completing sign-in…</p>")
            .ConfigureAwait(false);
    }

    private static bool TryResolveScheme(
        string provider,
        PlatformExternalAuthOptions options,
        out string scheme,
        out string? errorCode)
    {
        scheme = string.Empty;
        errorCode = null;
        string normalized;
        try
        {
            normalized = PlatformExternalLogin.NormalizeProvider(provider);
        }
        catch (DomainException)
        {
            errorCode = ApplicationErrorCodes.ExternalAuthProviderUnsupported;
            return false;
        }

        if (normalized == PlatformExternalLogin.ProviderGoogle)
        {
            if (!options.Google.Enabled
                || string.IsNullOrWhiteSpace(options.Google.ClientId)
                || string.IsNullOrWhiteSpace(options.Google.ClientSecret))
            {
                errorCode = ApplicationErrorCodes.ExternalAuthDisabled;
                return false;
            }

            scheme = GoogleDefaults.AuthenticationScheme;
            return true;
        }

        if (normalized == PlatformExternalLogin.ProviderFacebook)
        {
            if (!options.Facebook.Enabled
                || string.IsNullOrWhiteSpace(options.Facebook.ClientId)
                || string.IsNullOrWhiteSpace(options.Facebook.ClientSecret))
            {
                errorCode = ApplicationErrorCodes.ExternalAuthDisabled;
                return false;
            }

            scheme = FacebookDefaults.AuthenticationScheme;
            return true;
        }

        errorCode = ApplicationErrorCodes.ExternalAuthProviderUnsupported;
        return false;
    }

    private static ExternalLoginIdentity? MapPrincipal(string provider, ClaimsPrincipal principal)
    {
        string normalized;
        try
        {
            normalized = PlatformExternalLogin.NormalizeProvider(provider);
        }
        catch (DomainException)
        {
            return null;
        }

        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue("sub");
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var emailVerified = true;
        var verifiedClaim = principal.FindFirstValue("email_verified");
        if (!string.IsNullOrWhiteSpace(verifiedClaim)
            && !string.Equals(verifiedClaim, "true", StringComparison.OrdinalIgnoreCase)
            && verifiedClaim != "1")
        {
            emailVerified = false;
        }

        // Facebook Graph email is only returned when granted; treat presence as verified for MVP.
        var displayName = principal.FindFirstValue(ClaimTypes.Name)
                          ?? principal.FindFirstValue("name");

        return new ExternalLoginIdentity(normalized, subject, email, emailVerified, displayName);
    }

    private static string SanitizeReturnUrl(string? returnUrl, IHostEnvironment env) =>
        ExternalAuthReturnUrl.Sanitize(
            returnUrl,
            allowDevLocalhostAbsolute: env.IsDevelopment() || env.IsEnvironment("Testing"));

    private sealed record GoogleReplay(ExternalLoginIdentity Identity, string? ReturnUrl);

    private sealed record TestingExternalLoginRequest(
        string? Provider,
        string? ProviderSubject,
        string? Email,
        bool EmailVerified,
        string? DisplayName);
}
