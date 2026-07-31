using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ExItS.Platform.Admin.Services;

public sealed class PlatformBrowserSessionService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment,
    IConfiguration configuration)
{
    public const string SessionTokenClaimType = "exits_session_token";
    public const string SessionTokenCookieName = ".ExItS.Admin.Session";
    public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<(bool Ok, string? Error)> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default)
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
            return (false, "Invalid username/email or password.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var login = await JsonSerializer.DeserializeAsync<LoginResponse>(stream, JsonOptions, ct).ConfigureAwait(false);
        if (login is null || string.IsNullOrWhiteSpace(login.SessionToken))
        {
            return (false, "Login response was invalid.");
        }

        await EstablishBrowserSessionAsync(
            http,
            login.UserId,
            login.Username,
            login.Email,
            login.SessionToken,
            login.ExpiresAtUtc).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> LivePreviewLoginAsync(string identityKey, CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required for live preview login.");

        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var response = await client.PostAsJsonAsync(
            "/api/v1/platform/live-preview/sessions",
            new { identityKey },
            ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return (false, "Live preview sign-in failed.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var login = await JsonSerializer.DeserializeAsync<LoginResponse>(stream, JsonOptions, ct).ConfigureAwait(false);
        if (login is null || string.IsNullOrWhiteSpace(login.SessionToken))
        {
            return (false, "Live preview login response was invalid.");
        }

        await EstablishBrowserSessionAsync(
            http,
            login.UserId,
            login.Username,
            login.Email,
            login.SessionToken,
            login.ExpiresAtUtc).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<IReadOnlyList<LivePreviewIdentityOptionDto>> ListLivePreviewIdentitiesAsync(CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var response = await client.GetAsync("/api/v1/platform/live-preview/identities", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var list = await JsonSerializer.DeserializeAsync<List<LivePreviewIdentityOptionDto>>(stream, JsonOptions, ct)
            .ConfigureAwait(false);
        return list ?? [];
    }

    public async Task<(bool Ok, string? Error)> EstablishFromSessionTokenAsync(
        string sessionToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return (false, "Session token is missing.");
        }

        var http = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required for external login.");

        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me");
        request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", sessionToken);
        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return (false, "External session is invalid.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var me = await JsonSerializer.DeserializeAsync<MeResponse>(stream, JsonOptions, ct).ConfigureAwait(false);
        if (me is null || me.UserId == Guid.Empty)
        {
            return (false, "External session response was invalid.");
        }

        await EstablishBrowserSessionAsync(
            http,
            me.UserId,
            me.Username ?? string.Empty,
            me.Email,
            sessionToken,
            me.ExpiresAtUtc).ConfigureAwait(false);
        return (true, null);
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
        AppendSessionTokenCookie(http, sessionToken, expiresAtUtc);
    }

    private void AppendSessionTokenCookie(HttpContext http, string sessionToken, DateTimeOffset expiresAtUtc)
    {
        var livePreviewEnabled = configuration.GetValue<bool>("LivePreview:Enabled")
            && !environment.IsProduction();

        http.Response.Cookies.Append(
            SessionTokenCookieName,
            sessionToken,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = !(environment.IsDevelopment()
                    || environment.IsEnvironment("Testing")
                    || livePreviewEnabled),
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
