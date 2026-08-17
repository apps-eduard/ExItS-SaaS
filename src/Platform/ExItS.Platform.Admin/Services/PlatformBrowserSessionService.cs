using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ExItS.Web.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ExItS.Platform.Admin.Services;

public sealed class PlatformBrowserSessionService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment)
{
    public const string SessionTokenClaimType = "exits_session_token";
    public const string SessionTokenCookieName = ".ExItS.Admin.Session";
    public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<(bool Ok, string? Error, string? SessionToken)> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required for login.");

        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var response = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail, password },
            ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return (false, "Invalid email or password.", null);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var login = await JsonSerializer.DeserializeAsync<LoginResponse>(stream, JsonOptions, ct).ConfigureAwait(false);
        if (login is null || string.IsNullOrWhiteSpace(login.SessionToken))
        {
            return (false, "Login response was invalid.", null);
        }

        await EstablishBrowserSessionAsync(
            http,
            login.UserId,
            login.Username,
            login.Email,
            login.SessionToken,
            login.ExpiresAtUtc).ConfigureAwait(false);
        return (true, null, login.SessionToken);
    }

    public async Task<(bool Ok, string? Error, string? SessionToken)> EstablishFromSessionTokenAsync(
        string sessionToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return (false, "Session token is missing.", null);
        }

        // Interactive Server circuit events have no HttpContext — callers must use a full
        // HTTP round-trip (see /admin/session/establish) to SignIn cookies.
        var http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return (false, "HTTP context is required to establish the browser session.", null);
        }

        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me");
        request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", sessionToken);
        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return (false, "External session is invalid.", null);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var me = await JsonSerializer.DeserializeAsync<MeResponse>(stream, JsonOptions, ct).ConfigureAwait(false);
        if (me is null || me.UserId == Guid.Empty)
        {
            return (false, "External session response was invalid.", null);
        }

        await EstablishBrowserSessionAsync(
            http,
            me.UserId,
            me.Username ?? string.Empty,
            me.Email,
            sessionToken,
            me.ExpiresAtUtc).ConfigureAwait(false);
        return (true, null, sessionToken);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return;
        }

        var token = ResolveSessionToken(http);
        if (!string.IsNullOrWhiteSpace(token))
        {
            var client = httpClientFactory.CreateClient("PlatformApi");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/logout");
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
            try
            {
                await client.SendAsync(request, ct).ConfigureAwait(false);
            }
            catch
            {
                // Local cookie sign-out still proceeds.
            }
        }

        ClearSessionTokenCookie(http);
        await http.SignOutAsync(CookieScheme).ConfigureAwait(false);
    }

    public bool RequireAuthenticationInThisEnvironment =>
        !(environment.IsDevelopment() || environment.IsEnvironment("Testing"));

    public static string? ResolveSessionToken(HttpContext http) =>
        http.User.FindFirstValue(SessionTokenClaimType)
        ?? (http.Request.Cookies.TryGetValue(SessionTokenCookieName, out var cookie) ? cookie : null);

    private async Task EstablishBrowserSessionAsync(
        HttpContext http,
        Guid userId,
        string username,
        string? email,
        string sessionToken,
        DateTimeOffset expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString("D")),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Email, email ?? string.Empty),
            new(SessionTokenClaimType, sessionToken)
        };

        var identity = new ClaimsIdentity(claims, CookieScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = expiresAtUtc,
            AllowRefresh = true
        };

        await http.SignInAsync(CookieScheme, principal, props).ConfigureAwait(false);
        // SignInAsync only writes the response cookie — same-request post-login routing
        // (WebPostLoginRouter) must see the token without waiting for the next HTTP round-trip.
        http.User = principal;
        AppendSessionTokenCookie(http, sessionToken, expiresAtUtc);
    }

    private void AppendSessionTokenCookie(HttpContext http, string sessionToken, DateTimeOffset expiresAtUtc)
    {
        http.Response.Cookies.Append(
            SessionTokenCookieName,
            sessionToken,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = ExItSLocalValidationCookies.SessionTokenSecure(http.Request),
                Expires = expiresAtUtc
            });
    }

    private static void ClearSessionTokenCookie(HttpContext http)
    {
        http.Response.Cookies.Delete(SessionTokenCookieName);
    }

    private sealed record LoginResponse(
        string SessionToken,
        Guid SessionId,
        Guid UserId,
        string Username,
        string DisplayName,
        string Email,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset AbsoluteExpiresAtUtc);

    private sealed record MeResponse(
        Guid SessionId,
        Guid UserId,
        string Username,
        string DisplayName,
        string Email,
        DateTimeOffset ExpiresAtUtc);
}

public sealed class PlatformSessionForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var http = httpContextAccessor.HttpContext;
        var token = http is null ? null : PlatformBrowserSessionService.ResolveSessionToken(http);
        if (!string.IsNullOrWhiteSpace(token)
            && !request.Headers.Contains("X-ExItS-Session-Token"))
        {
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
