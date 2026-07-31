using ExItS.Platform.Api.Authentication;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Api.Identity;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/platform/auth/login", async (
            LoginRequest body,
            HttpContext http,
            LoginPlatformUser useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(
                body.UsernameOrEmail,
                body.Password,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString(),
                ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return PlatformApiResults.FromResult(result, _ => Results.Ok());
            }

            AppendSessionCookie(http, result.Value.SessionToken, result.Value.ExpiresAtUtc, sessionOptions.Value, env);
            return Results.Ok(result.Value);
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthLoginRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/logout", async (
            HttpContext http,
            LogoutPlatformSession useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            CancellationToken ct) =>
        {
            var token = ExtractSessionToken(http, sessionOptions.Value);
            var result = await useCase.ExecuteAsync(token, ct).ConfigureAwait(false);
            DeleteSessionCookie(http, sessionOptions.Value);
            if (!result.IsSuccess)
            {
                // Idempotent logout: missing/invalid session still clears cookie.
                return Results.NoContent();
            }

            return Results.NoContent();
        })
        .AllowAnonymous();

        app.MapGet("/api/v1/platform/auth/me", async (
            HttpContext http,
            ValidateAndRenewPlatformSession useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            CancellationToken ct) =>
        {
            var token = ExtractSessionToken(http, sessionOptions.Value);
            var result = await useCase.ExecuteAsync(token, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .AllowAnonymous();

        return app;
    }

    internal static string? ExtractSessionToken(HttpContext http, PlatformSessionOptions options)
    {
            if (http.Items.TryGetValue(PlatformSessionClaimTypes.RequestTokenItemKey, out var cached)
            && cached is string cachedToken
            && !string.IsNullOrWhiteSpace(cachedToken))
        {
            return cachedToken;
        }

        if (http.Request.Cookies.TryGetValue(options.CookieName, out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        if (http.Request.Headers.TryGetValue(options.SessionTokenHeaderName, out var headerValues))
        {
            var headerToken = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(headerToken))
            {
                return headerToken;
            }
        }

        var authorization = http.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith(PlatformSessionDefaults.AuthorizationScheme + " ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization[(PlatformSessionDefaults.AuthorizationScheme.Length + 1)..].Trim();
        }

        return null;
    }

    internal static void AppendSessionCookie(
        HttpContext http,
        string sessionToken,
        DateTimeOffset expiresAtUtc,
        PlatformSessionOptions options,
        IHostEnvironment env)
    {
        var secure = !(env.IsDevelopment() || env.IsEnvironment("Testing"));
        http.Response.Cookies.Append(
            options.CookieName,
            sessionToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAtUtc,
                IsEssential = true,
                Path = "/"
            });
    }

    internal static void DeleteSessionCookie(HttpContext http, PlatformSessionOptions options)
    {
        http.Response.Cookies.Delete(options.CookieName, new CookieOptions { Path = "/" });
    }

    internal sealed record LoginRequest(string? UsernameOrEmail, string? Password);
}
