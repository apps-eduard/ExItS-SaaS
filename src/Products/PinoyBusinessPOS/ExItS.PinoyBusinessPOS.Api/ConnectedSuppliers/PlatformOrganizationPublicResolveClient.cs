using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.ConnectedSuppliers;

/// <summary>
/// Calls Platform public-organization / QR resolve APIs. Forwards Platform session the same
/// way as merchant catalog (header, PlatformSession auth, or React HttpOnly auth cookie).
/// </summary>
public sealed class PlatformOrganizationPublicResolveClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options) : IPlatformOrganizationPublicResolve
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveOrganizationForConnectedSupplierAsync(
        string publicOrganizationIdOrQrPayload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicOrganizationIdOrQrPayload))
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                DomainErrorCodes.ConnectedSupplierRequiresBusinessQr,
                "Scan or enter the supplier Business QR / organization ID (ORG######).");
        }

        EnsureBaseAddress();
        if (string.IsNullOrWhiteSpace(
                PlatformCallerCredentialForwarder.ResolvePlatformSessionToken(
                    httpContextAccessor.HttpContext?.Request)))
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                ConnectedSupplierErrorCodes.NotFound,
                "Your Platform sign-in is missing for this request. Refresh the page or sign in again, then retry.");
        }

        var payload = publicOrganizationIdOrQrPayload.Trim();

        // Prefer typed QR resolve when payload looks like an ExItS envelope; otherwise public-id resolve.
        if (payload.Contains("://", StringComparison.Ordinal)
            || payload.StartsWith("exits://", StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveViaQrAsync(payload, cancellationToken).ConfigureAwait(false);
        }

        return await ResolveViaPublicIdAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveViaQrAsync(
        string payload,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/v1/qr/resolve");
        request.Content = JsonContent.Create(
            new { payload, expectedPurpose = "Organization" },
            options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return await FailureFromResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }

        var dto = await response.Content
            .ReadFromJsonAsync<PlatformQrResolveDto>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        if (dto is null
            || dto.OrganizationId is null
            || dto.OrganizationId == Guid.Empty
            || string.IsNullOrWhiteSpace(dto.PublicOrganizationId))
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                DomainErrorCodes.ConnectedSupplierRequiresBusinessQr,
                "Platform did not return a business organization for that QR.");
        }

        return ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
            new PlatformOrganizationPublicResolveResult(
                dto.OrganizationId.Value,
                dto.PublicOrganizationId.Trim().ToUpperInvariant(),
                string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.PublicOrganizationId : dto.DisplayName.Trim()));
    }

    private async Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveViaPublicIdAsync(
        string publicOrganizationIdOrQrPayload,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/v1/organizations/resolve-public-id");
        request.Content = JsonContent.Create(
            new { publicOrganizationIdOrQrPayload, purpose = "Organization" },
            options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return await FailureFromResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }

        var dto = await response.Content
            .ReadFromJsonAsync<PlatformPublicOrgResolveDto>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        if (dto is null || dto.OrganizationId == Guid.Empty || string.IsNullOrWhiteSpace(dto.PublicOrganizationId))
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                DomainErrorCodes.ConnectedSupplierRequiresBusinessQr,
                "Platform did not return a business organization for that ID.");
        }

        return ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
            new PlatformOrganizationPublicResolveResult(
                dto.OrganizationId,
                dto.PublicOrganizationId.Trim().ToUpperInvariant(),
                string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.PublicOrganizationId : dto.DisplayName.Trim()));
    }

    public async Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> GetOrganizationPublicIdentityAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                ConnectedSupplierErrorCodes.NotFound,
                "Organization was not found.");
        }

        EnsureBaseAddress();
        using var request = CreateRequest(HttpMethod.Get, $"api/v1/organizations/{organizationId:D}/public-identity");
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return await FailureFromResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }

        var dto = await response.Content
            .ReadFromJsonAsync<PlatformPublicIdentityDto>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        if (dto is null || string.IsNullOrWhiteSpace(dto.PublicOrganizationId))
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                ConnectedSupplierErrorCodes.NotFound,
                "Platform did not return a public organization identity.");
        }

        return ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
            new PlatformOrganizationPublicResolveResult(
                organizationId,
                dto.PublicOrganizationId.Trim().ToUpperInvariant(),
                string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.PublicOrganizationId : dto.DisplayName.Trim()));
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
            throw new InvalidOperationException("PlatformAuth:BaseUrl is required for organization public resolve.");
        }

        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(method, relativePath);
        var httpRequest = httpContextAccessor.HttpContext?.Request;
        // Forward Cookie collection so Platform can authenticate even if token extraction fails.
        PlatformCallerCredentialForwarder.CopyTo(httpRequest, request);

        // Prefer PlatformSession Authorization so Platform POSTs skip cookie+CSRF.
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

    private static async Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> FailureFromResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var (title, detail, errorCode) = TryReadProblem(body);
        var message = FirstNonBlank(detail, title)
                      ?? $"Platform organization resolve failed with {(int)response.StatusCode} {response.StatusCode}.";

        if (!string.IsNullOrWhiteSpace(errorCode)
            && (errorCode.Contains("purpose.mismatch", StringComparison.OrdinalIgnoreCase)
                || errorCode.Contains("qr_purpose_mismatch", StringComparison.OrdinalIgnoreCase)))
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                DomainErrorCodes.ConnectedSupplierQrPurposeMismatch,
                "Connected suppliers require a Business QR, not a Personal or device code.");
        }

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                DomainErrorCodes.ConnectedSupplierRequiresBusinessQr,
                message);
        }

        return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
            ConnectedSupplierErrorCodes.NotFound,
            message);
    }

    private static (string? Title, string? Detail, string? ErrorCode) TryReadProblem(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            string? title = root.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
            string? detail = root.TryGetProperty("detail", out var detailEl) ? detailEl.GetString() : null;
            string? errorCode = null;
            if (root.TryGetProperty("errorCode", out var codeEl))
            {
                errorCode = codeEl.GetString();
            }
            else if (root.TryGetProperty("extensions", out var extensions)
                     && extensions.ValueKind == JsonValueKind.Object
                     && extensions.TryGetProperty("errorCode", out var extCode))
            {
                errorCode = extCode.GetString();
            }

            return (title, detail, errorCode);
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private sealed record PlatformQrResolveDto(
        string? Purpose,
        string? PublicOrganizationId,
        Guid? OrganizationId,
        string? DisplayName);

    private sealed record PlatformPublicOrgResolveDto(
        string PublicOrganizationId,
        Guid OrganizationId,
        string DisplayName,
        string? Status);

    private sealed record PlatformPublicIdentityDto(
        string PublicOrganizationId,
        string? QrPayload,
        string DisplayName);
}
