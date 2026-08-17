using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Purchasing;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS purchasing client. Online-only for P10-WP02: offline calls fail fast.
/// </summary>
public sealed class PosPurchaseOrderClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosPurchaseOrderClient
{
    private const string PurchaseOrdersPath = "/api/v1/pos/purchase-orders";
    private const string GoodsReceiptsPath = "/api/v1/pos/goods-receipts";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PagedResult<PosPurchaseOrderDto>>> ListAsync(
        string? status = null,
        Guid? supplierId = null,
        string? poNumber = null,
        string? fromOrderDate = null,
        string? toOrderDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(PurchaseOrdersPath).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "status", status);
        AppendOptional(query, "poNumber", poNumber);
        AppendOptional(query, "fromOrderDate", fromOrderDate);
        AppendOptional(query, "toOrderDate", toOrderDate);
        if (supplierId is not null)
        {
            query.Append("&supplierId=").Append(supplierId.Value.ToString("D"));
        }

        return SendAsync<PagedResult<PosPurchaseOrderDto>>(HttpMethod.Get, query.ToString(), null, null, ct);
    }

    public Task<ApiResult<PosPurchaseOrderDto>> GetAsync(Guid purchaseOrderId, CancellationToken ct = default) =>
        SendAsync<PosPurchaseOrderDto>(HttpMethod.Get, $"{PurchaseOrdersPath}/{purchaseOrderId:D}", null, null, ct);

    public Task<ApiResult<PosPurchaseOrderDto>> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default) =>
        SendAsync<PosPurchaseOrderDto>(HttpMethod.Post, PurchaseOrdersPath, request, null, ct);

    public Task<ApiResult<PosPurchaseOrderDto>> UpdateAsync(
        Guid purchaseOrderId,
        UpdatePurchaseOrderRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosPurchaseOrderDto>(HttpMethod.Put, $"{PurchaseOrdersPath}/{purchaseOrderId:D}", request, null, ct);

    public Task<ApiResult<PosPurchaseOrderDto>> SubmitAsync(Guid purchaseOrderId, CancellationToken ct = default)
    {
        const string body = "{}";
        var headers = PosMutationIdempotencyHelper.BuildHeaders(
            purchaseOrderId,
            body,
            OfflineOperationTypes.PurchaseOrderSubmit);
        return SendAsync<PosPurchaseOrderDto>(
            HttpMethod.Post,
            $"{PurchaseOrdersPath}/{purchaseOrderId:D}/submit",
            null,
            headers,
            ct);
    }

    public Task<ApiResult<PosPurchaseOrderDto>> CancelAsync(Guid purchaseOrderId, CancellationToken ct = default) =>
        SendAsync<PosPurchaseOrderDto>(HttpMethod.Post, $"{PurchaseOrdersPath}/{purchaseOrderId:D}/cancel", null, null, ct);

    public Task<ApiResult<PosPurchaseOrderDto>> AcceptConnectedChangesAsync(Guid purchaseOrderId, CancellationToken ct = default) =>
        SendAsync<PosPurchaseOrderDto>(HttpMethod.Post, $"{PurchaseOrdersPath}/{purchaseOrderId:D}/accept-changes", null, null, ct);

    public Task<ApiResult<PosGoodsReceiptDto>> ReceiveAsync(
        Guid purchaseOrderId,
        ReceivePurchaseOrderRequest request,
        CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string>? headers = null;
        if (request.GoodsReceiptId is Guid grnId && grnId != Guid.Empty)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            headers = PosMutationIdempotencyHelper.BuildHeaders(
                grnId,
                json,
                OfflineOperationTypes.PurchaseOrderReceive);
        }

        return SendAsync<PosGoodsReceiptDto>(
            HttpMethod.Post,
            $"{PurchaseOrdersPath}/{purchaseOrderId:D}/receive",
            request,
            headers,
            ct);
    }

    public Task<ApiResult<PosGoodsReceiptDto>> GetGoodsReceiptAsync(Guid goodsReceiptId, CancellationToken ct = default) =>
        SendAsync<PosGoodsReceiptDto>(HttpMethod.Get, $"{GoodsReceiptsPath}/{goodsReceiptId:D}", null, null, ct);

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
        IReadOnlyDictionary<string, string>? extraHeaders,
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

            if (extraHeaders is not null)
            {
                foreach (var (key, value) in extraHeaders)
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
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
