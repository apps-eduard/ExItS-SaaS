using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ExItS.Platform.Application.Personal;
using ExItS.Web.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ExItS.Personal.Web.Services;

public sealed class PersonalCircuitSession
{
    public string? SessionToken { get; set; }
}

public sealed class PersonalSessionCircuitHandler(
    PersonalCircuitSession circuitSession,
    IHttpContextAccessor httpContextAccessor) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is not null)
        {
            var token = PersonalWebSessionService.ResolveSessionToken(http);
            if (!string.IsNullOrWhiteSpace(token))
            {
                circuitSession.SessionToken = token;
            }
        }

        return Task.CompletedTask;
    }
}

public sealed class PersonalWebSessionService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment)
{
    public const string SessionTokenClaimType = "exits_session_token";
    public const string SessionTokenCookieName = ".ExItS.PersonalWeb.Session";
    public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public static string? ResolveSessionToken(HttpContext http) =>
        http.User.FindFirstValue(SessionTokenClaimType)
        ?? (http.Request.Cookies.TryGetValue(SessionTokenCookieName, out var cookie) ? cookie : null);

    public async Task<(bool Ok, string? Error)> EstablishFromSessionTokenAsync(string sessionToken, CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required.");
        var client = httpClientFactory.CreateClient("PlatformApi");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/auth/me");
        request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", sessionToken);
        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return (false, "Session is invalid.");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        var root = doc.RootElement;
        var userId = root.GetProperty("userId").GetGuid();
        var username = root.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "";
        var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
        var expires = root.TryGetProperty("expiresAtUtc", out var exp)
            ? exp.GetDateTimeOffset()
            : DateTimeOffset.UtcNow.AddMinutes(30);
        var accountClass = root.TryGetProperty("accountClass", out var ac) ? ac.GetString() : null;
        if (!string.Equals(accountClass, "Personal", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "This host requires a Personal account session.");
        }

        await SignInAsync(http, userId, username, email, sessionToken, expires).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, string? ReturnPath)> RedeemHandoffAsync(string ticket, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("PlatformApi");
        var redeemed = await WebHandoffHttp.RedeemAsync(client, ticket, ct).ConfigureAwait(false);
        if (redeemed is null)
        {
            return (false, "Handoff ticket is invalid or expired.", null);
        }

        if (!string.Equals(redeemed.TargetApp, WebApps.Personal, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Handoff ticket is not for Personal Web.", null);
        }

        var established = await EstablishFromSessionTokenAsync(redeemed.SessionToken, ct).ConfigureAwait(false);
        return (established.Ok, established.Error, redeemed.ReturnPath);
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
            try { await client.SendAsync(request, ct).ConfigureAwait(false); }
            catch { /* local sign-out still proceeds */ }
        }

        http.Response.Cookies.Delete(SessionTokenCookieName);
        await http.SignOutAsync(CookieScheme).ConfigureAwait(false);
    }

    private async Task SignInAsync(
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
        await http.SignInAsync(
            CookieScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieScheme)),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = expiresAtUtc,
                AllowRefresh = true
            }).ConfigureAwait(false);
        http.Response.Cookies.Append(
            SessionTokenCookieName,
            sessionToken,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = !(environment.IsDevelopment() || environment.IsEnvironment("Testing")),
                Expires = expiresAtUtc
            });
    }
}

public sealed class PersonalApiClient(IHttpClientFactory httpClientFactory, PersonalCircuitSession circuit)
{
    public Task<PersonalDashboardDto?> GetDashboardAsync(CancellationToken ct = default) =>
        GetAsync<PersonalDashboardDto>("/api/v1/personal/dashboard", ct);

    public Task<PersonalProfileDto?> GetProfileAsync(CancellationToken ct = default) =>
        GetAsync<PersonalProfileDto>("/api/v1/personal/profile", ct);

    public Task<PersonalAccountSettingsDto?> GetSettingsAsync(CancellationToken ct = default) =>
        GetAsync<PersonalAccountSettingsDto>("/api/v1/personal/settings", ct);

    public Task<IReadOnlyList<PersonalContactDto>?> GetContactsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalContactDto>>("/api/v1/personal/utang/contacts", ct);

    public Task<IReadOnlyList<PersonalDebtRelationshipSummaryDto>?> GetLentAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>("/api/v1/personal/utang/relationships/lent", ct);

    public Task<IReadOnlyList<PersonalDebtRelationshipSummaryDto>?> GetBorrowedAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>("/api/v1/personal/utang/relationships/borrowed", ct);

    public Task<IReadOnlyList<PersonalUtangInvitationDto>?> GetInvitationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalUtangInvitationDto>>("/api/v1/personal/utang/invitations", ct);

    public Task<IReadOnlyList<PersonalInAppNotificationDto>?> GetNotificationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalInAppNotificationDto>>("/api/v1/personal/notifications", ct);

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PlatformApi");
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrWhiteSpace(circuit.SessionToken))
        {
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", circuit.SessionToken);
        }

        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(ct).ConfigureAwait(false);
    }
}
