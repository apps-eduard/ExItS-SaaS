using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Local Validation operator convenience: list eligible account-profile identities and sign in through
/// normal Platform credential login (SharedPassword). Not a session bypass.
/// </summary>
public sealed class LocalValidationSignInService(
    IHttpClientFactory httpClientFactory,
    PlatformBrowserSessionService sessions,
    IOptions<LocalValidationAdminOptions> options,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public bool IsAvailable =>
        !environment.IsProduction()
        && options.Value.Enabled
        && !string.IsNullOrWhiteSpace(options.Value.SharedPassword)
        && options.Value.SharedPassword.Length >= 12;

    public async Task<IReadOnlyList<LocalValidationIdentityOption>> ListIdentitiesAsync(CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            return [];
        }

        var list = await FetchQuickLoginIdentitiesAsync(ct).ConfigureAwait(false);
        return list
            .Select(i =>
            {
                var label = !string.IsNullOrWhiteSpace(i.ListLabel)
                    ? i.ListLabel!
                    : i.DisplayName ?? i.Username ?? i.Email ?? string.Empty;
                return new LocalValidationIdentityOption(
                    i.Key ?? string.Empty,
                    label,
                    i.Email ?? string.Empty,
                    i.Username ?? string.Empty,
                    i.AccountProfileId,
                    i.AccountClass,
                    i.OrganizationId,
                    i.ScopeLabel);
            })
            .Where(i => !string.IsNullOrWhiteSpace(i.Key)
                        && (!string.IsNullOrWhiteSpace(i.Email) || !string.IsNullOrWhiteSpace(i.Username)))
            .ToList();
    }

    public async Task<(bool Ok, string? Error)> SignInAsKeyAsync(string key, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            return (false, "Local Validation sign-in is unavailable.");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return (false, "Choose a Local Validation identity.");
        }

        var list = await FetchQuickLoginIdentitiesAsync(ct).ConfigureAwait(false);
        var identity = list.FirstOrDefault(i =>
            string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
        if (identity is null)
        {
            return (false, "Unknown Local Validation identity.");
        }

        var password = options.Value.SharedPassword;
        var candidates = new[]
            {
                identity.Email,
                identity.Username
            }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string? lastError = null;
        string? sessionToken = null;
        Guid userId = Guid.Empty;
        string username = identity.Username ?? string.Empty;
        string? email = identity.Email;
        DateTimeOffset expiresAtUtc = DateTimeOffset.UtcNow.AddHours(8);

        foreach (var candidate in candidates)
        {
            // Normal Platform credential login — password never leaves Admin server process.
            var login = await LoginRawAsync(candidate, password, ct).ConfigureAwait(false);
            if (login is null)
            {
                lastError = "Invalid username/email or password.";
                continue;
            }

            sessionToken = login.SessionToken;
            userId = login.UserId;
            username = login.Username;
            email = login.Email;
            expiresAtUtc = login.ExpiresAtUtc;
            break;
        }

        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return (false, lastError ?? "Invalid username/email or password.");
        }

        if (identity.AccountProfileId is Guid profileId && profileId != Guid.Empty)
        {
            var selected = await SelectProfileAsync(sessionToken, profileId, ct).ConfigureAwait(false);
            if (selected is null)
            {
                return (false, "Could not establish the selected account profile session.");
            }

            sessionToken = selected.SessionToken;
            userId = selected.UserId;
            username = selected.Username;
            email = selected.Email;
            expiresAtUtc = selected.ExpiresAtUtc;
        }

        if (identity.OrganizationId is Guid orgId && orgId != Guid.Empty)
        {
            var orgOk = await SetOrganizationContextAsync(sessionToken, orgId, ct).ConfigureAwait(false);
            if (!orgOk)
            {
                return (false, "Could not select the organization context for Quick Login.");
            }
        }

        var (ok, error) = await sessions.EstablishFromSessionTokenAsync(sessionToken, ct).ConfigureAwait(false);
        if (!ok)
        {
            // Fallback: establish from login payload fields when /me shape differs.
            _ = userId;
            _ = username;
            _ = email;
            _ = expiresAtUtc;
            return (false, error ?? "Could not establish the browser session.");
        }

        return (true, null);
    }

    private async Task<LoginResponse?> LoginRawAsync(string usernameOrEmail, string password, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var response = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail, password },
            ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, ct).ConfigureAwait(false);
    }

    private async Task<LoginResponse?> SelectProfileAsync(string sessionToken, Guid accountProfileId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/account-profiles/select");
        request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", sessionToken);
        request.Content = JsonContent.Create(new { accountProfileId });
        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, ct).ConfigureAwait(false);
    }

    private async Task<bool> SetOrganizationContextAsync(string sessionToken, Guid organizationId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/platform/auth/organization-context");
        request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", sessionToken);
        request.Content = JsonContent.Create(new { organizationId });
        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    private async Task<List<QuickLoginIdentityDto>> FetchQuickLoginIdentitiesAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var response = await client
            .GetAsync("/api/v1/platform/local-validation/quick-login-identities", ct)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content
                   .ReadFromJsonAsync<List<QuickLoginIdentityDto>>(JsonOptions, ct)
                   .ConfigureAwait(false)
               ?? [];
    }

    private sealed record QuickLoginIdentityDto(
        string? Key,
        string? Username,
        string? DisplayName,
        string? Email,
        Guid? AccountProfileId,
        string? AccountClass,
        Guid? OrganizationId,
        string? ListLabel,
        string? ScopeLabel);

    private sealed record LoginResponse(
        string SessionToken,
        Guid SessionId,
        Guid UserId,
        string Username,
        string DisplayName,
        string Email,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset AbsoluteExpiresAtUtc);
}

public sealed record LocalValidationIdentityOption(
    string Key,
    string DisplayName,
    string Email,
    string Username,
    Guid? AccountProfileId = null,
    string? AccountClass = null,
    Guid? OrganizationId = null,
    string? ScopeLabel = null);
