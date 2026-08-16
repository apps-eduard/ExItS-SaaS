using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PosDirectPurchaseReceiptClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosDirectPurchaseReceiptClient
{
    private const string Path = "/api/v1/pos/direct-purchase-receipts";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PagedResult<DirectPurchaseReceiptListItemDto>>> ListAsync(
        string? fromPurchaseDate = null,
        string? toPurchaseDate = null,
        Guid? supplierId = null,
        string? sourceSearch = null,
        string? referenceNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(Path).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "fromPurchaseDate", fromPurchaseDate);
        AppendOptional(query, "toPurchaseDate", toPurchaseDate);
        AppendOptional(query, "sourceSearch", sourceSearch);
        AppendOptional(query, "referenceNumber", referenceNumber);
        if (supplierId is not null)
        {
            query.Append("&supplierId=").Append(supplierId.Value.ToString("D"));
        }

        return SendAsync<PagedResult<DirectPurchaseReceiptListItemDto>>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<DirectPurchaseReceiptDto>> GetAsync(Guid receiptId, CancellationToken ct = default) =>
        SendAsync<DirectPurchaseReceiptDto>(HttpMethod.Get, $"{Path}/{receiptId:D}", null, ct);

    public Task<ApiResult<DirectPurchaseReceiptDto>> CreateAsync(
        CreateDirectPurchaseReceiptRequest request,
        CancellationToken ct = default) =>
        SendAsync<DirectPurchaseReceiptDto>(HttpMethod.Post, Path, request, ct);

    private static void AppendOptional(StringBuilder query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Append('&').Append(name).Append('=').Append(Uri.EscapeDataString(value.Trim()));
        }
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
    }

    private static ApiCallStatus Classify(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ApiCallStatus.Unauthorized,
            HttpStatusCode.NotFound => ApiCallStatus.NotFound,
            HttpStatusCode.Conflict => ApiCallStatus.Conflict,
            _ => ApiCallStatus.Failed
        };

    private static ApiError ParseProblem(string content, string? correlationId, int statusCode)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var title = root.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
            var detail = root.TryGetProperty("detail", out var detailEl) ? detailEl.GetString() : null;
            var code = root.TryGetProperty("errorCode", out var codeEl)
                ? codeEl.GetString()
                : root.TryGetProperty("code", out var code2) ? code2.GetString() : null;
            return new ApiError(title ?? "Request failed", detail ?? content, code, correlationId, statusCode);
        }
        catch (JsonException)
        {
            return new ApiError("Request failed", content, null, correlationId, statusCode);
        }
    }
}
