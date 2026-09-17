using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Options;
using ExItS.PinoyBusinessPOS.Application.Privacy;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.Privacy;

/// <summary>
/// POS API privacy signals: membership from auth items; sales-doc education from Platform when configured.
/// </summary>
internal sealed class PosApiOrganizationPrivacyContext(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    IOptions<PlatformAuthOptions> platformAuth) : IOrganizationPrivacyContext
{
    public const string HttpClientName = "PosApiOrganizationPrivacyContext";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public Task<bool> IsExactOrganizationOwnerAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        _ = organizationId;
        var role = httpContextAccessor.HttpContext?.Items[PosAuthItems.MembershipRole] as string;
        return Task.FromResult(
            string.Equals(role, "OrganizationOwner", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> SalesDocumentEducationRequiresOwnerActionAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(platformAuth.Value.BaseUrl))
        {
            return false;
        }

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/v1/platform/organizations/{organizationId:D}/sales-document-education");

            var auth = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(auth)
                && AuthenticationHeaderValue.TryParse(auth, out var header))
            {
                request.Headers.Authorization = header;
            }

            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var dto = await response.Content
                .ReadFromJsonAsync<SalesDocumentEducationPayload>(JsonOptions, ct)
                .ConfigureAwait(false);
            return dto?.RequiresOwnerAction == true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record SalesDocumentEducationPayload(
        [property: JsonPropertyName("requiresOwnerAction")] bool RequiresOwnerAction);
}
