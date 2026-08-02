using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Local Validation operator convenience: list approved identities and sign in through
/// normal Platform credential login. Not a session bypass.
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

        var list = await FetchSeedIdentitiesAsync(ct).ConfigureAwait(false);
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
                    i.Username ?? string.Empty);
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

        var list = await FetchSeedIdentitiesAsync(ct).ConfigureAwait(false);
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
        foreach (var candidate in candidates)
        {
            // Normal Platform credential login — password never leaves Admin server process.
            var (ok, error) = await sessions.LoginAsync(candidate, password, ct).ConfigureAwait(false);
            if (ok)
            {
                return (true, null);
            }

            lastError = error;
        }

        return (false, lastError ?? "Invalid username/email or password.");
    }

    private async Task<List<SeedIdentityDto>> FetchSeedIdentitiesAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PlatformApiUnauthenticated");
        using var response = await client
            .GetAsync("/api/v1/platform/local-validation/seed-identities", ct)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content
                   .ReadFromJsonAsync<List<SeedIdentityDto>>(JsonOptions, ct)
                   .ConfigureAwait(false)
               ?? [];
    }

    private sealed record SeedIdentityDto(
        string? Key,
        string? Username,
        string? DisplayName,
        string? Email,
        string? Summary,
        string? ListLabel);
}

public sealed record LocalValidationIdentityOption(
    string Key,
    string DisplayName,
    string Email,
    string Username);
