using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Identity;
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
    IHttpContextAccessor httpContextAccessor)
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
                Secure = ExItSLocalValidationCookies.SessionTokenSecure(http.Request),
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

    public Task<(bool Ok, PersonalProfileDto? Data, string? Error)> UpdateProfileAsync(
        string displayName,
        CancellationToken ct = default) =>
        PutAsync<PersonalProfileDto>("/api/v1/personal/profile", new UpdatePersonalProfileRequest(displayName), ct);

    public Task<PersonalAccountSettingsDto?> GetSettingsAsync(CancellationToken ct = default) =>
        GetAsync<PersonalAccountSettingsDto>("/api/v1/personal/settings", ct);

    public Task<IReadOnlyList<PersonalContactDto>?> GetContactsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalContactDto>>("/api/v1/personal/utang/contacts", ct);

    public Task<(bool Ok, PersonalContactDto? Data, string? Error)> CreateContactAsync(
        CreatePersonalContactRequest request,
        CancellationToken ct = default) =>
        PostAsync<PersonalContactDto>("/api/v1/personal/utang/contacts", request, ct);

    public Task<(bool Ok, ResolvedPublicUserDto? Data, string? Error)> ResolvePublicUserIdAsync(
        string publicUserIdOrQrPayload,
        CancellationToken ct = default) =>
        PostAsync<ResolvedPublicUserDto>(
            "/api/v1/users/resolve-public-id",
            new ResolvePublicUserIdRequest(publicUserIdOrQrPayload, "utang-people"),
            ct);

    public Task<IReadOnlyList<PersonalDebtRelationshipSummaryDto>?> GetLentAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>("/api/v1/personal/utang/relationships/lent", ct);

    public Task<IReadOnlyList<PersonalDebtRelationshipSummaryDto>?> GetBorrowedAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>("/api/v1/personal/utang/relationships/borrowed", ct);

    public Task<(bool Ok, PersonalDebtRelationshipSummaryDto? Data, string? Error)> CreateRelationshipAsync(
        CreatePersonalDebtRelationshipRequest request,
        CancellationToken ct = default) =>
        PostAsync<PersonalDebtRelationshipSummaryDto>("/api/v1/personal/utang/relationships", request, ct);

    public Task<(bool Ok, PersonalUtangInvitationDto? Data, string? Error)> CreateInvitationAsync(
        Guid relationshipId,
        Guid inviteeContactId,
        CancellationToken ct = default) =>
        PostAsync<PersonalUtangInvitationDto>(
            $"/api/v1/personal/utang/relationships/{relationshipId:D}/invitations",
            new CreatePersonalUtangInvitationRequest(inviteeContactId),
            ct);

    public Task<IReadOnlyList<PersonalUtangInvitationDto>?> GetInvitationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalUtangInvitationDto>>("/api/v1/personal/utang/invitations", ct);

    public Task<(bool Ok, PersonalUtangInvitationAcceptResultDto? Data, string? Error)> AcceptInvitationByIdAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        PostAsync<PersonalUtangInvitationAcceptResultDto>(
            "/api/v1/personal/utang/invitations/accept-by-id",
            new AcceptPersonalUtangInvitationByIdRequest(invitationId),
            ct);

    public Task<(bool Ok, PersonalUtangInvitationDto? Data, string? Error)> DeclineInvitationByIdAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        PostAsync<PersonalUtangInvitationDto>(
            "/api/v1/personal/utang/invitations/decline-by-id",
            new DeclinePersonalUtangInvitationByIdRequest(invitationId),
            ct);

    public Task<(bool Ok, PersonalUtangInvitationDto? Data, string? Error)> ResendInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        PostAsync<PersonalUtangInvitationDto>(
            $"/api/v1/personal/utang/invitations/{invitationId:D}/resend",
            new { },
            ct);

    public Task<(bool Ok, PersonalUtangInvitationDto? Data, string? Error)> RevokeInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        PostAsync<PersonalUtangInvitationDto>(
            $"/api/v1/personal/utang/invitations/{invitationId:D}/revoke",
            new { },
            ct);

    public Task<IReadOnlyList<PersonalInAppNotificationDto>?> GetNotificationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalInAppNotificationDto>>("/api/v1/personal/notifications", ct);

    public Task<IReadOnlyList<PlanDto>?> GetCommercialPlansAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PlanDto>>("/api/v1/commercial/plans?productCode=pinoy-business-pos", ct);

    public Task<IReadOnlyList<BusinessTypeDto>?> GetOnboardingBusinessTypesAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<BusinessTypeDto>>("/api/v1/personal/onboarding/business-types", ct);

    public Task<(bool Ok, StartBusinessResultDto? Data, string? Error)> StartBusinessAsync(
        StartBusinessRequest request,
        CancellationToken ct = default) =>
        PostAsync<StartBusinessResultDto>("/api/v1/personal/start-business", request, ct);

    private async Task<(bool Ok, T? Data, string? Error)> PostAsync<T>(string path, object body, CancellationToken ct) =>
        await SendJsonAsync<T>(HttpMethod.Post, path, body, ct).ConfigureAwait(false);

    private async Task<(bool Ok, T? Data, string? Error)> PutAsync<T>(string path, object body, CancellationToken ct) =>
        await SendJsonAsync<T>(HttpMethod.Put, path, body, ct).ConfigureAwait(false);

    private async Task<(bool Ok, T? Data, string? Error)> SendJsonAsync<T>(
        HttpMethod method,
        string path,
        object body,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PlatformApi");
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
        if (!string.IsNullOrWhiteSpace(circuit.SessionToken))
        {
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", circuit.SessionToken);
        }

        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return (false, default, TryProblemDetail(raw) ?? "Request failed.");
        }

        var data = await response.Content.ReadFromJsonAsync<T>(ct).ConfigureAwait(false);
        return (true, data, null);
    }

    private static string? TryProblemDetail(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.GetString();
            }

            if (doc.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString();
            }

            if (doc.RootElement.TryGetProperty("errorCode", out var errorCode))
            {
                return errorCode.GetString();
            }
        }
        catch
        {
            // Fall through.
        }

        return null;
    }

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
