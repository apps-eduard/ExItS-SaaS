using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.Customers;

/// <summary>
/// Forwards the inbound Personal PlatformSession to Platform linked-customer authorization.
/// Fail-closed on auth errors, timeouts, network failures, and malformed payloads.
/// </summary>
public sealed class LinkedCustomerPlatformAuthorizationClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options) : ILinkedCustomerPlatformAuthorization
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<LinkedCustomerPlatformAuthorizationResult> VerifyAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty || platformBusinessCustomerId == Guid.Empty)
        {
            return NotFound();
        }

        try
        {
            EnsureBaseAddress();
            var path =
                $"api/v1/personal/linked-merchants/authorization?organizationId={organizationId:D}&businessCustomerId={platformBusinessCustomerId:D}";
            using var request = CreateRequest(HttpMethod.Get, path);
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return Denied();
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.NotFound
                or HttpStatusCode.Conflict
                or HttpStatusCode.BadRequest)
            {
                return NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                // 5xx / timeout-like statuses / unexpected → fail closed
                return NotFound();
            }

            var body = await response.Content
                .ReadFromJsonAsync<AuthorizedLinkedCustomerPlatformResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (body is null
                || body.PersonalUserId == Guid.Empty
                || body.LinkedCustomerAppUserId == Guid.Empty
                || body.OrganizationId != organizationId
                || body.PlatformBusinessCustomerId != platformBusinessCustomerId)
            {
                return NotFound();
            }

            return new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Authorized,
                new LinkedCustomerPlatformAuthorizationProof(
                    body.PersonalUserId,
                    body.OrganizationId,
                    body.PlatformBusinessCustomerId,
                    body.LinkedCustomerAppUserId));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout
            return NotFound();
        }
        catch (HttpRequestException)
        {
            return NotFound();
        }
        catch (JsonException)
        {
            return NotFound();
        }
        catch (InvalidOperationException)
        {
            // Missing BaseUrl / misconfiguration
            return NotFound();
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
            throw new InvalidOperationException("PlatformAuth:BaseUrl is required for linked-customer authorization.");
        }

        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(method, relativePath);
        // React Personal keeps the Platform session in an HttpOnly cookie and sends
        // product Bearer to POS. Forward Cookie / PlatformSession / session header —
        // never product Bearer — so Platform linked-customer proof can authenticate.
        PlatformCallerCredentialForwarder.CopyTo(httpContextAccessor.HttpContext?.Request, request);

        var token = ResolveSessionToken();
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

        // Same cookie name Platform uses for browser sessions (HttpOnly).
        if (http.Request.Cookies.TryGetValue(".ExItS.Platform.Auth", out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken.Trim();
        }

        return null;
    }

    private static LinkedCustomerPlatformAuthorizationResult NotFound() =>
        new(LinkedCustomerPlatformAuthorizationOutcome.NotFound, null);

    private static LinkedCustomerPlatformAuthorizationResult Denied() =>
        new(LinkedCustomerPlatformAuthorizationOutcome.Denied, null);

    private sealed record AuthorizedLinkedCustomerPlatformResponse(
        Guid PersonalUserId,
        Guid OrganizationId,
        Guid PlatformBusinessCustomerId,
        Guid LinkedCustomerAppUserId);
}
