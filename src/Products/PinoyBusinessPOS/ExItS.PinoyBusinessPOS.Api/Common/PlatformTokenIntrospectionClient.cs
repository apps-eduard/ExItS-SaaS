using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.Common;

internal sealed class PlatformTokenIntrospectionClient(
    HttpClient httpClient,
    IOptions<PlatformAuthOptions> options) : IPlatformTokenIntrospectionClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Sell-floor traffic introspects on nearly every POS call. Without a short cache,
    /// Local Validation / Staging burns Platform auth token-ops limits and surfaces as
    /// catalog "connection" failures (POS 503 pos.platform_auth.unavailable).
    /// </summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(45);
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.Ordinal);

    /// <summary>Clears the process-wide introspection cache (integration tests only).</summary>
    internal static void ClearCacheForTests() => Cache.Clear();

    public async Task<PlatformTokenIntrospectionResult> IntrospectAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.BaseUrl))
        {
            return Inactive();
        }

        var cacheKey = HashToken(accessToken);
        if (TryGetFresh(cacheKey, out var cached))
        {
            return cached;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/introspect");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { token = (string?)null }, options: JsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (TryGetFresh(cacheKey, out cached))
        {
            // Timeout / transport blip — keep sell floor alive on a fresh prior result.
            return cached;
        }

        using (response)
        {
            // Non-success is infrastructure (rate limit / Platform down), not proof the token is inactive.
            // Treating 429/5xx as Inactive falsely denies Owners with valid ProductAccess.
            if ((int)response.StatusCode == StatusCodes.Status429TooManyRequests
                || (int)response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                if (TryGetFresh(cacheKey, out cached))
                {
                    return cached;
                }

                throw new HttpRequestException(
                    $"Platform token introspection unavailable ({(int)response.StatusCode}).");
            }

            if (!response.IsSuccessStatusCode)
            {
                var inactive = Inactive();
                Cache[cacheKey] = new CacheEntry(inactive, DateTimeOffset.UtcNow.Add(CacheTtl));
                return inactive;
            }

            var dto = await response.Content
                .ReadFromJsonAsync<IntrospectionDto>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (dto is null)
            {
                var inactive = Inactive();
                Cache[cacheKey] = new CacheEntry(inactive, DateTimeOffset.UtcNow.Add(CacheTtl));
                return inactive;
            }

            var result = new PlatformTokenIntrospectionResult(
                dto.Active,
                dto.UserId,
                dto.OrganizationId,
                dto.ProductCode,
                dto.ProductAccessAllowed,
                dto.SubscriptionStatus,
                dto.EnabledFeatureCodes,
                dto.ProductLocalRoleCode,
                dto.MappedPosRoleCode,
                dto.MembershipRole,
                dto.OrganizationManagementAuthority);
            Cache[cacheKey] = new CacheEntry(result, DateTimeOffset.UtcNow.Add(CacheTtl));
            return result;
        }
    }

    private static bool TryGetFresh(string cacheKey, out PlatformTokenIntrospectionResult result)
    {
        result = Inactive();
        if (!Cache.TryGetValue(cacheKey, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            Cache.TryRemove(cacheKey, out _);
            return false;
        }

        result = entry.Result;
        return true;
    }

    private static string HashToken(string accessToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(accessToken));
        return Convert.ToHexString(hash);
    }

    private static PlatformTokenIntrospectionResult Inactive() =>
        new(false, null, null, null, null, null, null);

    private sealed record CacheEntry(PlatformTokenIntrospectionResult Result, DateTimeOffset ExpiresAtUtc);

    private sealed record IntrospectionDto(
        bool Active,
        Guid? UserId,
        Guid? OrganizationId,
        string? ProductCode,
        bool? ProductAccessAllowed,
        string? SubscriptionStatus,
        IReadOnlyList<string>? EnabledFeatureCodes,
        string? ProductLocalRoleCode,
        string? MappedPosRoleCode,
        string? MembershipRole = null,
        bool OrganizationManagementAuthority = false);
}
