using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Platform;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.ConnectedSuppliers;

/// <summary>
/// Proxies branch fulfillment readiness/settings to Platform using the caller's Platform session.
/// </summary>
public sealed class PlatformBranchFulfillmentClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options) : IPlatformBranchFulfillmentGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Task<ApplicationResult<BranchFulfillmentReadinessDto>> GetReadinessAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            HttpMethod.Get,
            $"api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/fulfillment-readiness",
            body: null,
            cancellationToken);

    public Task<ApplicationResult<BranchFulfillmentReadinessDto>> UpdateSettingsAsync(
        Guid organizationId,
        Guid branchId,
        UpdateBranchFulfillmentSettingsRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            HttpMethod.Put,
            $"api/v1/platform/organizations/{organizationId:D}/branches/{branchId:D}/fulfillment-settings",
            request,
            cancellationToken);

    private async Task<ApplicationResult<BranchFulfillmentReadinessDto>> SendAsync(
        HttpMethod method,
        string relativePath,
        object? body,
        CancellationToken cancellationToken)
    {
        EnsureBaseAddress();
        using var request = CreateRequest(method, relativePath);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                    "platform.branch_fulfillment_empty",
                    "Platform returned an empty fulfillment response.");
            }

            try
            {
                var dto = JsonSerializer.Deserialize<BranchFulfillmentReadinessDto>(content, JsonOptions);
                if (dto is null)
                {
                    return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                        "platform.branch_fulfillment_invalid",
                        "Platform returned an invalid fulfillment response.");
                }

                return ApplicationResult<BranchFulfillmentReadinessDto>.Success(dto);
            }
            catch (JsonException)
            {
                return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                    "platform.branch_fulfillment_invalid",
                    "Platform returned an invalid fulfillment response.");
            }
        }

        var (title, detail, errorCode) = TryReadProblem(content);
        return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
            errorCode ?? MapStatusErrorCode(response.StatusCode),
            FirstNonBlank(detail, title)
            ?? $"Branch fulfillment request failed with {(int)response.StatusCode}.");
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
                "PlatformAuth:BaseUrl is required for branch fulfillment proxy.");
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
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
        }

        return request;
    }

    private static string MapStatusErrorCode(HttpStatusCode status) => status switch
    {
        HttpStatusCode.NotFound => "platform.branch_fulfillment_not_found",
        HttpStatusCode.Forbidden => "platform.branch_fulfillment_forbidden",
        HttpStatusCode.Unauthorized => "platform.branch_fulfillment_unauthorized",
        HttpStatusCode.Conflict => "platform.branch_fulfillment_conflict",
        _ when (int)status >= 500 => ApplicationErrorCodes.PlatformAuthUnavailable,
        _ => "platform.branch_fulfillment_failed"
    };

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
            if (root.TryGetProperty("errorCode", out var codeEl) && codeEl.ValueKind == JsonValueKind.String)
            {
                errorCode = codeEl.GetString();
            }

            return (title, detail, errorCode);
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(static v => !string.IsNullOrWhiteSpace(v));
}
