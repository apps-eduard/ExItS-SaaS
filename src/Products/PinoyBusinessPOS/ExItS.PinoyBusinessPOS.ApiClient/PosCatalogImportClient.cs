using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS catalog-import client. Online-only; no local mutation queue.
/// </summary>
public sealed class PosCatalogImportClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosCatalogImportClient
{
    private const string ImportsPath = "/api/v1/pos/catalog-imports";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PosCatalogImportJobDto>> ImportTemplateBatchAsync(
        ImportTemplateBatchRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosCatalogImportJobDto>(HttpMethod.Post, $"{ImportsPath}/template", request, ct);

    public Task<ApiResult<PosCatalogImportJobDto>> ImportSelectedProductsAsync(
        ImportSelectedProductsRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosCatalogImportJobDto>(HttpMethod.Post, $"{ImportsPath}/products", request, ct);

    public Task<ApiResult<PosCatalogImportJobDto>> ImportTemplateNextBatchAsync(
        Guid templateId,
        ImportTemplateBatchRequest? request = null,
        CancellationToken ct = default) =>
        SendAsync<PosCatalogImportJobDto>(
            HttpMethod.Post,
            $"{ImportsPath}/template/{templateId:D}/next-batch",
            request,
            ct);

    public Task<ApiResult<PosTemplateImportStatusDto>> GetTemplateImportStatusAsync(
        Guid templateId,
        CancellationToken ct = default) =>
        SendAsync<PosTemplateImportStatusDto>(
            HttpMethod.Get,
            $"{ImportsPath}/templates/{templateId:D}/status",
            null,
            ct);

    public Task<ApiResult<PosCatalogImportJobDto>> GetJobAsync(Guid jobId, CancellationToken ct = default) =>
        SendAsync<PosCatalogImportJobDto>(HttpMethod.Get, $"{ImportsPath}/{jobId:D}", null, ct);

    public Task<ApiResult<PagedResult<PosCatalogImportItemDto>>> GetJobItemsAsync(
        Guid jobId,
        string? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{ImportsPath}/{jobId:D}/items?");
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Append("&status=").Append(Uri.EscapeDataString(status.Trim()));
        }

        return SendAsync<PagedResult<PosCatalogImportItemDto>>(HttpMethod.Get, query.ToString(), null, ct);
    }

    private async Task<ApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct)
    {
        if (connectivityService is not null && !await connectivityService.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Offline", "No network connectivity detected.", null, null, null)
            };
        }

        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body, options: JsonOptions);
            }

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var correlationId = response.Headers.TryGetValues("X-Correlation-ID", out var values)
                ? values.FirstOrDefault()
                : null;

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<TResponse>
                {
                    Status = Classify(response.StatusCode),
                    Error = ParseProblem(content, correlationId, (int)response.StatusCode)
                };
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new ApiResult<TResponse>
                {
                    Status = ApiCallStatus.Failed,
                    Error = new ApiError("Invalid response", "The API returned no content.", null, null, null)
                };
            }

            var data = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
            return data is null
                ? new ApiResult<TResponse>
                {
                    Status = ApiCallStatus.Failed,
                    Error = new ApiError("Invalid response", "The API returned no content.", null, null, null)
                }
                : ApiResult<TResponse>.Success(data);
        }
        catch (JsonException ex)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Failed,
                Error = new ApiError("Invalid response", ex.Message, null, null, null)
            };
        }
        catch (HttpRequestException ex)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Network unavailable", ex.Message, null, null, null)
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new ApiResult<TResponse> { Status = ApiCallStatus.Cancelled };
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Timeout,
                Error = new ApiError("Request timed out", ex.Message, null, null, null)
            };
        }
        catch (Exception ex)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Failed,
                Error = new ApiError("Request failed", ex.Message, null, null, null)
            };
        }
    }

    private static ApiError ParseProblem(string content, string? correlationId, int statusCode)
    {
        string? title = null;
        string? detail = null;
        string? errorCode = null;
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                {
                    title = t.GetString();
                }

                if (root.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    detail = d.GetString();
                }

                if (root.TryGetProperty("errorCode", out var e) && e.ValueKind == JsonValueKind.String)
                {
                    errorCode = e.GetString();
                }
            }
            catch (JsonException)
            {
            }
        }

        return new ApiError(title, detail, errorCode, correlationId, statusCode);
    }

    private static ApiCallStatus Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => ApiCallStatus.NotFound,
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiCallStatus.Validation,
        HttpStatusCode.Conflict => ApiCallStatus.Conflict,
        HttpStatusCode.Unauthorized => ApiCallStatus.Unauthorized,
        HttpStatusCode.Forbidden => ApiCallStatus.Forbidden,
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ApiCallStatus.Timeout,
        _ when (int)statusCode >= 500 => ApiCallStatus.Unavailable,
        _ => ApiCallStatus.Failed
    };
}
