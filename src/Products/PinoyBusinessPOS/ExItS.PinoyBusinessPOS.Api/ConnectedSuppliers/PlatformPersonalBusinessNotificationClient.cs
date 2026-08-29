using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.ConnectedSuppliers;

/// <summary>
/// Forwards Personal customer-order lifecycle events to Platform personal business notifications.
/// Best-effort: failures are logged and swallowed so order mutations remain authoritative.
/// </summary>
public sealed class PlatformPersonalBusinessNotificationClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options,
    ILogger<PlatformPersonalBusinessNotificationClient> logger) : IPersonalBusinessNotificationPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task PublishAsync(
        Guid sourceOrganizationId,
        Guid recipientPlatformUserId,
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
                $"api/v1/organizations/{sourceOrganizationId:D}/personal-business-notifications");
            request.Content = JsonContent.Create(
                new
                {
                    recipientPlatformUserId,
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
                    "Platform personal business notification publish failed ({Status}): {Body}",
                    (int)response.StatusCode,
                    Truncate(body));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Platform personal business notification publish threw for related {RelatedType}/{RelatedId}.",
                relatedType,
                relatedId);
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
            throw new InvalidOperationException("PlatformAuth:BaseUrl is required for personal business notifications.");
        }

        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(method, relativePath);
        var httpRequest = httpContextAccessor.HttpContext?.Request;
        PlatformCallerCredentialForwarder.CopyTo(httpRequest, request);
        var token = PlatformCallerCredentialForwarder.ResolvePlatformSessionToken(httpRequest);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("PlatformSession", token);
            if (!request.Headers.Contains("X-ExItS-Session-Token"))
            {
                request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
            }
        }

        return request;
    }

    private static string Truncate(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= 400 ? value : value[..400];
}
