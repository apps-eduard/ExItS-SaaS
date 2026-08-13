using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS inventory client. Online-only for P8-WP04: offline calls fail fast with
/// <see cref="ApiCallStatus.Offline"/> and no inventory mutation is ever queued locally.
/// </summary>
public sealed class PosInventoryClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosInventoryClient
{
    private const string InventoryPath = "/api/v1/pos/inventory";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PosInventoryAccountPagedResult>> ListAsync(
        string? search = null,
        bool? tracked = null,
        bool? lowStock = null,
        string? productStatus = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(InventoryPath).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "search", search);
        AppendOptional(query, "productStatus", productStatus);
        if (tracked is not null)
        {
            query.Append("&tracked=").Append(tracked.Value ? "true" : "false");
        }

        if (lowStock is not null)
        {
            query.Append("&lowStock=").Append(lowStock.Value ? "true" : "false");
        }

        return SendAsync<PosInventoryAccountPagedResult>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosInventoryAccountPagedResult>> ListLowStockAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{InventoryPath}/low-stock?");
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "search", search);
        return SendAsync<PosInventoryAccountPagedResult>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosInventoryAccountDto>> GetAsync(Guid productId, CancellationToken ct = default) =>
        SendAsync<PosInventoryAccountDto>(HttpMethod.Get, $"{InventoryPath}/{productId:D}", null, ct);

    public Task<ApiResult<PosInventoryAccountDto>> EnableAsync(
        Guid productId,
        EnableInventoryTrackingRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosInventoryAccountDto>(HttpMethod.Post, $"{InventoryPath}/{productId:D}/enable", request, ct);

    public Task<ApiResult<PosInventoryAccountDto>> DisableAsync(Guid productId, CancellationToken ct = default) =>
        SendAsync<PosInventoryAccountDto>(HttpMethod.Post, $"{InventoryPath}/{productId:D}/disable", null, ct);

    public Task<ApiResult<PosInventoryAccountDto>> AdjustAsync(
        Guid productId,
        AdjustInventoryRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosInventoryAccountDto>(
            HttpMethod.Post,
            $"{InventoryPath}/{productId:D}/adjustments",
            request,
            ct);

    public Task<ApiResult<PosStockMovementPagedResult>> ListMovementsAsync(
        Guid productId,
        string? movementType = null,
        string? sourceType = null,
        string? fromDateUtc = null,
        string? toDateUtc = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{InventoryPath}/{productId:D}/movements?");
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "movementType", movementType);
        AppendOptional(query, "sourceType", sourceType);
        AppendOptional(query, "fromDateUtc", fromDateUtc);
        AppendOptional(query, "toDateUtc", toDateUtc);
        return SendAsync<PosStockMovementPagedResult>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosInventoryAccountDto>> SetReorderAsync(
        Guid productId,
        SetInventoryReorderRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosInventoryAccountDto>(HttpMethod.Put, $"{InventoryPath}/{productId:D}/reorder", request, ct);

    public Task<ApiResult<PosInventoryAccountPagedResult>> ListReorderSuggestionsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{InventoryPath}/reorder-suggestions?");
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "search", search);
        return SendAsync<PosInventoryAccountPagedResult>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosInventoryReconciliationDto>> GetReconciliationAsync(
        Guid productId,
        CancellationToken ct = default) =>
        SendAsync<PosInventoryReconciliationDto>(
            HttpMethod.Get,
            $"{InventoryPath}/{productId:D}/reconciliation",
            null,
            ct);

    public Task<ApiResult<PagedResult<PosStockCountDto>>> ListStockCountsAsync(
        string? status = null,
        string? countNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{InventoryPath}/stock-counts?");
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "status", status);
        AppendOptional(query, "countNumber", countNumber);
        return SendAsync<PagedResult<PosStockCountDto>>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosStockCountDto>> GetStockCountAsync(Guid stockCountId, CancellationToken ct = default) =>
        SendAsync<PosStockCountDto>(HttpMethod.Get, $"{InventoryPath}/stock-counts/{stockCountId:D}", null, ct);

    public Task<ApiResult<PosStockCountDto>> CreateStockCountAsync(
        CreateStockCountRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosStockCountDto>(HttpMethod.Post, $"{InventoryPath}/stock-counts", request, ct);

    public Task<ApiResult<PosStockCountDto>> UpdateStockCountAsync(
        Guid stockCountId,
        UpdateStockCountRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosStockCountDto>(HttpMethod.Put, $"{InventoryPath}/stock-counts/{stockCountId:D}", request, ct);

    public Task<ApiResult<PosStockCountDto>> StartStockCountAsync(Guid stockCountId, CancellationToken ct = default) =>
        SendAsync<PosStockCountDto>(HttpMethod.Post, $"{InventoryPath}/stock-counts/{stockCountId:D}/start", null, ct);

    public Task<ApiResult<PosStockCountDto>> CompleteStockCountAsync(Guid stockCountId, CancellationToken ct = default) =>
        SendAsync<PosStockCountDto>(HttpMethod.Post, $"{InventoryPath}/stock-counts/{stockCountId:D}/complete", null, ct);

    public Task<ApiResult<PosStockCountDto>> CancelStockCountAsync(Guid stockCountId, CancellationToken ct = default) =>
        SendAsync<PosStockCountDto>(HttpMethod.Post, $"{InventoryPath}/stock-counts/{stockCountId:D}/cancel", null, ct);

    public Task<ApiResult<PagedResult<InventoryTransferListItemDto>>> ListTransfersAsync(
        string? status = null,
        string? transferNumber = null,
        string? direction = null,
        Guid? sourceBranchId = null,
        Guid? destinationBranchId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{InventoryPath}/transfers?");
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "status", status);
        AppendOptional(query, "transferNumber", transferNumber);
        AppendOptional(query, "direction", direction);
        if (sourceBranchId is Guid source && source != Guid.Empty)
        {
            query.Append("&sourceBranchId=").Append(source.ToString("D"));
        }

        if (destinationBranchId is Guid dest && dest != Guid.Empty)
        {
            query.Append("&destinationBranchId=").Append(dest.ToString("D"));
        }

        return SendAsync<PagedResult<InventoryTransferListItemDto>>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<InventoryTransferDto>> GetTransferAsync(Guid transferId, CancellationToken ct = default) =>
        SendAsync<InventoryTransferDto>(HttpMethod.Get, $"{InventoryPath}/transfers/{transferId:D}", null, ct);

    public Task<ApiResult<InventoryTransferDto>> CreateTransferAsync(
        CreateInventoryTransferRequest request,
        CancellationToken ct = default) =>
        SendAsync<InventoryTransferDto>(HttpMethod.Post, $"{InventoryPath}/transfers", request, ct);

    public Task<ApiResult<InventoryTransferDto>> DispatchTransferAsync(Guid transferId, CancellationToken ct = default) =>
        SendAsync<InventoryTransferDto>(HttpMethod.Post, $"{InventoryPath}/transfers/{transferId:D}/dispatch", null, ct);

    public Task<ApiResult<InventoryTransferDto>> ReceiveTransferAsync(
        Guid transferId,
        ReceiveInventoryTransferRequest request,
        CancellationToken ct = default) =>
        SendAsync<InventoryTransferDto>(
            HttpMethod.Post,
            $"{InventoryPath}/transfers/{transferId:D}/receive",
            request,
            ct);

    public Task<ApiResult<InventoryTransferDto>> CancelTransferAsync(Guid transferId, CancellationToken ct = default) =>
        SendAsync<InventoryTransferDto>(HttpMethod.Post, $"{InventoryPath}/transfers/{transferId:D}/cancel", null, ct);

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
