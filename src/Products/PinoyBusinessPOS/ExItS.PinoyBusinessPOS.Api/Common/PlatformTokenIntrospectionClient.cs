using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

    public async Task<PlatformTokenIntrospectionResult> IntrospectAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.BaseUrl))
        {
            return Inactive();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/introspect");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { token = (string?)null }, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return Inactive();
        }

        var dto = await response.Content
            .ReadFromJsonAsync<IntrospectionDto>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (dto is null)
        {
            return Inactive();
        }

        return new PlatformTokenIntrospectionResult(
            dto.Active,
            dto.UserId,
            dto.OrganizationId,
            dto.ProductCode,
            dto.ProductAccessAllowed,
            dto.SubscriptionStatus,
            dto.EnabledFeatureCodes,
            dto.ProductLocalRoleCode,
            dto.MappedPosRoleCode);
    }

    private static PlatformTokenIntrospectionResult Inactive() =>
        new(false, null, null, null, null, null, null);

    private sealed record IntrospectionDto(
        bool Active,
        Guid? UserId,
        Guid? OrganizationId,
        string? ProductCode,
        bool? ProductAccessAllowed,
        string? SubscriptionStatus,
        IReadOnlyList<string>? EnabledFeatureCodes,
        string? ProductLocalRoleCode,
        string? MappedPosRoleCode);
}
