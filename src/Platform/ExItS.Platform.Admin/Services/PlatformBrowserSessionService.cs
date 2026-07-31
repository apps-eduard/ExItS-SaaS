using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ExItS.Platform.Admin.Services;

public sealed class PlatformBrowserSessionService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment)
{
    public const string SessionTokenClaimType = "exits_session_token";
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

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login.UserId.ToString("D")),
            new(ClaimTypes.Name, login.Username),
            new(ClaimTypes.Email, login.Email ?? string.Empty),
            new(SessionTokenClaimType, login.SessionToken)
        };

        var identity = new ClaimsIdentity(claims, CookieScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = login.ExpiresAtUtc,
            AllowRefresh = true
        };

        await http.SignInAsync(CookieScheme, principal, props).ConfigureAwait(false);
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

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login.UserId.ToString("D")),
            new(ClaimTypes.Name, login.Username),
            new(ClaimTypes.Email, login.Email ?? string.Empty),
            new(SessionTokenClaimType, login.SessionToken)
        };

        var identity = new ClaimsIdentity(claims, CookieScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = login.ExpiresAtUtc,
            AllowRefresh = true
        };

        await http.SignInAsync(CookieScheme, principal, props).ConfigureAwait(false);
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

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, me.UserId.ToString("D")),
            new(ClaimTypes.Name, me.Username ?? string.Empty),
            new(ClaimTypes.Email, me.Email ?? string.Empty),
            new(SessionTokenClaimType, sessionToken)
        };

        var identity = new ClaimsIdentity(claims, CookieScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = me.ExpiresAtUtc,
            AllowRefresh = true
        };

        await http.SignInAsync(CookieScheme, principal, props).ConfigureAwait(false);
        return (true, null);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return;
        }

        var token = http.User.FindFirstValue(SessionTokenClaimType);
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

        await http.SignOutAsync(CookieScheme).ConfigureAwait(false);
    }

    public bool RequireAuthenticationInThisEnvironment =>
        !(environment.IsDevelopment() || environment.IsEnvironment("Testing"));

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
        var token = http?.User.FindFirstValue(PlatformBrowserSessionService.SessionTokenClaimType);
        if (!string.IsNullOrWhiteSpace(token)
            && !request.Headers.Contains("X-ExItS-Session-Token"))
        {
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
