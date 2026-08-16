using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.OperationalSetup;

/// <summary>
/// Reads Platform-owned TaxConfigurationEnabled via compliance-status.
/// Fail-closed: unreachable/malformed → disabled.
/// </summary>
public sealed class PlatformOrganizationTaxConfigurationCapabilityClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options) : IOrganizationTaxConfigurationCapabilityReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<bool> IsTaxConfigurationEnabledAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return false;
        }

        try
        {
            EnsureBaseAddress();
            var path = $"api/v1/platform/organizations/{organizationId:D}/compliance-status";
            using var request = CreateRequest(HttpMethod.Get, path);
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var body = await response.Content
                .ReadFromJsonAsync<ComplianceStatusResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return body?.TaxConfigurationEnabled == true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void EnsureBaseAddress()
    {
        if (httpClient.BaseAddress is not null)
        {
            return;
        }

        var baseUrl = options.Value.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "PlatformAuth:BaseUrl is required for organization tax configuration checks.");
        }

        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(method, relativePath);
        var token = ResolveSessionToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("PlatformSession", token);
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
        }

        return request;
    }

    private string? ResolveSessionToken()
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return null;
        }

        var header = http.Request.Headers["X-ExItS-Session-Token"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(header))
        {
            return header.Trim();
        }

        var auth = http.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(auth)
            && auth.StartsWith("PlatformSession ", StringComparison.OrdinalIgnoreCase))
        {
            return auth["PlatformSession ".Length..].Trim();
        }

        return null;
    }

    private sealed record ComplianceStatusResponse(bool TaxConfigurationEnabled);
}
