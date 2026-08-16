using System.Net;
using System.Net.Http.Headers;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Central Platform 401 recovery: at most one AccessToken reissue and one original-request retry.
/// Does not run when the device is offline. Never loops. Skips auth bootstrap endpoints.
/// Explicit 401 may safely retry POST/PUT/PATCH/DELETE because the server rejected auth before processing.
/// Ambiguous transport failures are not retried here.
/// </summary>
public sealed class PlatformAccessTokenRecoveryHandler(
    IPlatformAccessTokenRecovery recovery,
    IConnectivityService connectivity,
    ILogger<PlatformAccessTokenRecoveryHandler>? logger = null) : DelegatingHandler
{
    private const string RecoveryAttemptHeader = "X-ExItS-Platform-Recovery-Attempt";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var bufferedContent = await BufferContentAsync(request, cancellationToken).ConfigureAwait(false);
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        if (HasRecoveryAttempt(request) || IsAuthBootstrapPath(request.RequestUri))
        {
            LogClassification(request, refreshAttempted: false, retryAttempted: false);
            return response;
        }

        if (!await connectivity.IsConnectedAsync(cancellationToken).ConfigureAwait(false))
        {
            LogClassification(request, refreshAttempted: false, retryAttempted: false, offline: true);
            return response;
        }

        response.Dispose();

        var refreshed = false;
        try
        {
            refreshed = await recovery.TryReissueAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Platform AccessToken reissue failed for {Path}", SafePath(request.RequestUri));
        }

        if (!refreshed)
        {
            LogClassification(request, refreshAttempted: true, retryAttempted: false);
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request,
                Content = new StringContent(
                    """{"title":"Unauthorized","detail":"Authentication is required.","errorCode":"auth.required"}""",
                    System.Text.Encoding.UTF8,
                    "application/problem+json")
            };
        }

        LogClassification(request, refreshAttempted: true, retryAttempted: true);
        var clone = await CloneAsync(request, bufferedContent, markRecovery: true, cancellationToken)
            .ConfigureAwait(false);
        return await base.SendAsync(clone, cancellationToken).ConfigureAwait(false);
    }

    private void LogClassification(
        HttpRequestMessage request,
        bool refreshAttempted,
        bool retryAttempted,
        bool offline = false)
    {
        logger?.LogInformation(
            "Platform request classification ApiTarget=Platform Path={Path} Status=401 Offline={Offline} RefreshAttempted={Refresh} RetryAttempted={Retry}",
            SafePath(request.RequestUri),
            offline,
            refreshAttempted,
            retryAttempted);
    }

    private static string SafePath(Uri? uri)
    {
        if (uri is null)
        {
            return "(null)";
        }

        return uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
    }

    private static bool HasRecoveryAttempt(HttpRequestMessage request) =>
        request.Headers.Contains(RecoveryAttemptHeader);

    private static bool IsAuthBootstrapPath(Uri? uri)
    {
        if (uri is null)
        {
            return false;
        }

        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
        return path.Equals("/api/v1/platform/auth/token", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/api/v1/platform/auth/login", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/api/v1/platform/auth/logout", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/api/v1/platform/auth/register", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/api/v1/platform/auth/activate-account", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/api/v1/platform/auth/token/revoke", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/api/v1/platform/auth/introspect", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<byte[]?> BufferContentAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Content is null)
        {
            return null;
        }

        var bytes = await request.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        var contentType = request.Content.Headers.ContentType;
        request.Content = new ByteArrayContent(bytes);
        if (contentType is not null)
        {
            request.Content.Headers.ContentType = contentType;
        }

        return bytes;
    }

    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage request,
        byte[]? bufferedContent,
        bool markRecovery,
        CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            if (header.Key.Equals(RecoveryAttemptHeader, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (markRecovery)
        {
            clone.Headers.TryAddWithoutValidation(RecoveryAttemptHeader, "1");
        }

        // Drop Authorization so PlatformSessionHeaderHandler / PlatformBearerHandler re-attach
        // from the updated CurrentUserContext after a successful reissue.
        clone.Headers.Remove("Authorization");
        clone.Headers.Remove("X-ExItS-Session-Token");

        if (bufferedContent is not null)
        {
            clone.Content = new ByteArrayContent(bufferedContent);
            if (request.Content?.Headers.ContentType is MediaTypeHeaderValue contentType)
            {
                clone.Content.Headers.ContentType = contentType;
            }
        }
        else if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            clone.Content = new ByteArrayContent(bytes);
            if (request.Content.Headers.ContentType is not null)
            {
                clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
            }
        }

        return clone;
    }
}
