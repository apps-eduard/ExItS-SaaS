using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.ConnectedSuppliers;

/// <summary>
/// Forwards Connected Supplier lifecycle events to Platform organization business notifications.
/// Best-effort: failures are logged and swallowed so relationship mutations remain authoritative.
/// </summary>
public sealed class PlatformOrganizationBusinessNotificationClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options,
    ILogger<PlatformOrganizationBusinessNotificationClient> logger) : IOrganizationBusinessNotificationPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task PublishAsync(
        Guid sourceOrganizationId,
        Guid recipientOrganizationId,
        string relatedType,
        string relatedId,
        string title,
        string preview,
        CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureBaseAddress();
            using var request = CreateRequest(
                HttpMethod.Post,
                $"api/v1/organizations/{sourceOrganizationId:D}/business-notifications");
            request.Content = JsonContent.Create(
                new
                {
                    recipientOrganizationId,
                    relatedType,
                    relatedId,
                    title,
                    preview
                },
                options: JsonOptions);
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                logger.LogWarning(
                    "Platform business notification publish failed ({Status}): {Body}",
                    (int)response.StatusCode,
                    Truncate(body));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Platform business notification publish threw for related {RelatedType}/{RelatedId}.", relatedType, relatedId);
        }
    }

    public async Task MarkRelatedReadAsync(
        Guid organizationId,
        string relatedType,
        string relatedId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureBaseAddress();
            using var request = CreateRequest(
                HttpMethod.Post,
                $"api/v1/organizations/{organizationId:D}/notifications/related/read");
            request.Content = JsonContent.Create(
                new { relatedType, relatedId },
                options: JsonOptions);
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                logger.LogWarning(
                    "Platform mark-related-read failed ({Status}): {Body}",
                    (int)response.StatusCode,
                    Truncate(body));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Platform mark-related-read threw for related {RelatedType}/{RelatedId}.", relatedType, relatedId);
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
            throw new InvalidOperationException("PlatformAuth:BaseUrl is required for organization business notifications.");
        }

        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(method, relativePath);
        var token = httpContextAccessor.HttpContext?.Request.Headers["X-ExItS-Session-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
        {
            var auth = httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(auth)
                && auth.StartsWith("PlatformSession ", StringComparison.OrdinalIgnoreCase))
            {
                token = auth["PlatformSession ".Length..].Trim();
            }
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("PlatformSession", token);
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
        }

        return request;
    }

    private static string Truncate(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= 400 ? value : value[..400];
}
