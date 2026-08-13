using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Reporting;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS dashboard/reports client. Online-only for P8-WP06: offline calls fail fast with
/// <see cref="ApiCallStatus.Offline"/> and never present stale totals as authoritative.
/// </summary>
public sealed class PosReportingClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosReportingClient
{
    private const string DashboardPath = "/api/v1/pos/dashboard";
    private const string ReportsPath = "/api/v1/pos/reports";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PosDashboardDto>> GetDashboardAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosDashboardDto>(HttpMethod.Get, BuildPath(DashboardPath, fromDate, toDate), null, ct);

    public Task<ApiResult<PosManagementOverviewDto>> GetManagementOverviewAsync(CancellationToken ct = default) =>
        SendAsync<PosManagementOverviewDto>(HttpMethod.Get, "/api/v1/pos/management/overview", null, ct);

    public Task<ApiResult<PosSalesReportDto>> GetSalesReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? paymentMethod = null,
        string? status = null,
        Guid? productId = null,
        Guid? categoryId = null,
        Guid? customerId = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{ReportsPath}/sales?");
        AppendDates(query, fromDate, toDate);
        AppendOptional(query, "paymentMethod", paymentMethod);
        AppendOptional(query, "status", status);
        AppendGuid(query, "productId", productId);
        AppendGuid(query, "categoryId", categoryId);
        AppendGuid(query, "customerId", customerId);
        return SendAsync<PosSalesReportDto>(HttpMethod.Get, TrimQuery(query), null, ct);
    }

    public Task<ApiResult<PosUtangReportDto>> GetUtangReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        Guid? customerId = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{ReportsPath}/utang?");
        AppendDates(query, fromDate, toDate);
        AppendGuid(query, "customerId", customerId);
        return SendAsync<PosUtangReportDto>(HttpMethod.Get, TrimQuery(query), null, ct);
    }

    public Task<ApiResult<PosInventoryReportDto>> GetInventoryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        bool? trackedOnly = true,
        bool? lowStockOnly = null,
        string? productStatus = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{ReportsPath}/inventory?");
        AppendDates(query, fromDate, toDate);
        if (trackedOnly is not null)
        {
            query.Append("&trackedOnly=").Append(trackedOnly.Value ? "true" : "false");
        }

        if (lowStockOnly is not null)
        {
            query.Append("&lowStockOnly=").Append(lowStockOnly.Value ? "true" : "false");
        }

        AppendOptional(query, "productStatus", productStatus);
        return SendAsync<PosInventoryReportDto>(HttpMethod.Get, TrimQuery(query), null, ct);
    }

    public Task<ApiResult<PosExpensesReportDto>> GetExpensesReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        Guid? expenseCategoryId = null,
        string? paymentMethod = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{ReportsPath}/expenses?");
        AppendDates(query, fromDate, toDate);
        AppendGuid(query, "expenseCategoryId", expenseCategoryId);
        AppendOptional(query, "paymentMethod", paymentMethod);
        AppendOptional(query, "status", status);
        return SendAsync<PosExpensesReportDto>(HttpMethod.Get, TrimQuery(query), null, ct);
    }

    public Task<ApiResult<PosOperationalOverviewDto>> GetOperationalOverviewAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosOperationalOverviewDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/overview", fromDate, toDate), null, ct);

    public Task<ApiResult<PosSalesSummaryReportDto>> GetSalesSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosSalesSummaryReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/sales-summary", fromDate, toDate), null, ct);

    public Task<ApiResult<PosSalesByPaymentReportDto>> GetSalesByPaymentReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosSalesByPaymentReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/sales-by-payment", fromDate, toDate), null, ct);

    public Task<ApiResult<PosSalesByProductReportDto>> GetSalesByProductOperationalAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        Guid? productId = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{ReportsPath}/sales-by-product?");
        AppendDates(query, fromDate, toDate);
        AppendGuid(query, "productId", productId);
        return SendAsync<PosSalesByProductReportDto>(HttpMethod.Get, TrimQuery(query), null, ct);
    }

    public Task<ApiResult<PosReturnsReportDto>> GetReturnsReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosReturnsReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/returns", fromDate, toDate), null, ct);

    public Task<ApiResult<PosShiftSummaryReportDto>> GetShiftSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosShiftSummaryReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/shifts-summary", fromDate, toDate), null, ct);

    public Task<ApiResult<PosCashVarianceReportDto>> GetCashVarianceReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosCashVarianceReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/cash-variance", fromDate, toDate), null, ct);

    public Task<ApiResult<PosInventoryStatusReportDto>> GetInventoryStatusReportAsync(CancellationToken ct = default) =>
        SendAsync<PosInventoryStatusReportDto>(HttpMethod.Get, $"{ReportsPath}/inventory-status", null, ct);

    public Task<ApiResult<PosInventoryMovementsReportDto>> GetInventoryMovementsReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosInventoryMovementsReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/inventory-movements", fromDate, toDate), null, ct);

    public Task<ApiResult<PosStockCountVarianceReportDto>> GetStockCountVarianceReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosStockCountVarianceReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/stock-count-variance", fromDate, toDate), null, ct);

    public Task<ApiResult<PosPurchasingSummaryReportDto>> GetPurchasingSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosPurchasingSummaryReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/purchasing-summary", fromDate, toDate), null, ct);

    public Task<ApiResult<PosPurchaseOutstandingReportDto>> GetPurchaseOutstandingReportAsync(CancellationToken ct = default) =>
        SendAsync<PosPurchaseOutstandingReportDto>(HttpMethod.Get, $"{ReportsPath}/purchase-outstanding", null, ct);

    public Task<ApiResult<PosSupplierPurchasingReportDto>> GetSupplierPurchasingReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosSupplierPurchasingReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/supplier-purchasing", fromDate, toDate), null, ct);

    public Task<ApiResult<PosExpenseSummaryReportDto>> GetExpenseSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosExpenseSummaryReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/expenses-summary", fromDate, toDate), null, ct);

    public Task<ApiResult<PosProductUtangSummaryReportDto>> GetProductUtangSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default) =>
        SendAsync<PosProductUtangSummaryReportDto>(HttpMethod.Get, BuildPath($"{ReportsPath}/utang-by-product", fromDate, toDate), null, ct);

    private static string BuildPath(string basePath, DateOnly? fromDate, DateOnly? toDate)
    {
        var query = new StringBuilder(basePath).Append('?');
        AppendDates(query, fromDate, toDate);
        return TrimQuery(query);
    }

    private static void AppendDates(StringBuilder query, DateOnly? fromDate, DateOnly? toDate)
    {
        AppendOptional(query, "fromDate", fromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendOptional(query, "toDate", toDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    private static void AppendOptional(StringBuilder query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Append('&').Append(name).Append('=').Append(Uri.EscapeDataString(value.Trim()));
        }
    }

    private static void AppendGuid(StringBuilder query, string name, Guid? value)
    {
        if (value is not null)
        {
            query.Append('&').Append(name).Append('=').Append(value.Value.ToString("D"));
        }
    }

    private static string TrimQuery(StringBuilder query)
    {
        var text = query.ToString();
        return text.EndsWith('?') || text.EndsWith('&')
            ? text.TrimEnd('?', '&')
            : text.Replace("?&", "?", StringComparison.Ordinal);
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
                else if (root.TryGetProperty("extensions", out var ext)
                         && ext.ValueKind == JsonValueKind.Object
                         && ext.TryGetProperty("errorCode", out var extCode)
                         && extCode.ValueKind == JsonValueKind.String)
                {
                    errorCode = extCode.GetString();
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
