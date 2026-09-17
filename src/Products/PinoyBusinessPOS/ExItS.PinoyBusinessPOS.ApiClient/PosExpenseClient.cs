using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS expenses client. Online-only for P8-WP05: offline calls fail fast with
/// <see cref="ApiCallStatus.Offline"/> and no expense mutation is ever queued locally.
/// </summary>
public sealed class PosExpenseClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosExpenseClient
{
    private const string CategoriesPath = "/api/v1/pos/expense-categories";
    private const string ExpensesPath = "/api/v1/pos/expenses";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PosExpenseCategoryPagedResult>> ListCategoriesAsync(
        string? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(CategoriesPath).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "status", status);
        AppendOptional(query, "search", search);
        return SendAsync<PosExpenseCategoryPagedResult>(HttpMethod.Get, query.ToString(), null, null, ct);
    }

    public Task<ApiResult<PosExpenseCategoryDto>> GetCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        SendAsync<PosExpenseCategoryDto>(HttpMethod.Get, $"{CategoriesPath}/{categoryId:D}", null, null, ct);

    public Task<ApiResult<PosExpenseCategoryDto>> CreateCategoryAsync(
        CreatePosExpenseCategoryRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosExpenseCategoryDto>(HttpMethod.Post, CategoriesPath, request, null, ct);

    public Task<ApiResult<PosExpenseCategoryDto>> UpdateCategoryAsync(
        Guid categoryId,
        UpdatePosExpenseCategoryRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosExpenseCategoryDto>(HttpMethod.Put, $"{CategoriesPath}/{categoryId:D}", request, null, ct);

    public Task<ApiResult<PosExpenseCategoryDto>> DeactivateCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        SendAsync<PosExpenseCategoryDto>(HttpMethod.Post, $"{CategoriesPath}/{categoryId:D}/deactivate", null, null, ct);

    public Task<ApiResult<PosExpenseCategoryDto>> ReactivateCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        SendAsync<PosExpenseCategoryDto>(HttpMethod.Post, $"{CategoriesPath}/{categoryId:D}/reactivate", null, null, ct);

    public Task<ApiResult<PosExpensePagedResult>> ListExpensesAsync(
        string? status = null,
        string? paymentMethod = null,
        Guid? categoryId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? expenseNumber = null,
        int page = 1,
        int pageSize = 20,
        string? scope = null,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(ExpensesPath).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "status", status);
        AppendOptional(query, "paymentMethod", paymentMethod);
        AppendOptional(query, "expenseNumber", expenseNumber);
        AppendOptional(query, "fromDate", fromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendOptional(query, "toDate", toDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendOptional(query, "scope", scope);
        if (categoryId is not null)
        {
            query.Append("&categoryId=").Append(categoryId.Value.ToString("D"));
        }

        if (branchId is not null)
        {
            query.Append("&branchId=").Append(branchId.Value.ToString("D"));
        }

        return SendAsync<PosExpensePagedResult>(HttpMethod.Get, query.ToString(), null, null, ct);
    }

    public Task<ApiResult<PosExpenseScopeOptionsDto>> GetScopeOptionsAsync(CancellationToken ct = default) =>
        SendAsync<PosExpenseScopeOptionsDto>(HttpMethod.Get, $"{ExpensesPath}/scope-options", null, null, ct);

    public Task<ApiResult<PosExpenseDto>> GetExpenseAsync(Guid expenseId, CancellationToken ct = default) =>
        SendAsync<PosExpenseDto>(HttpMethod.Get, $"{ExpensesPath}/{expenseId:D}", null, null, ct);

    public Task<ApiResult<PosExpenseDto>> RecordExpenseAsync(RecordExpenseRequest request, CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string>? headers = null;
        if (request.ExpenseId is Guid expenseId && expenseId != Guid.Empty)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            headers = PosMutationIdempotencyHelper.BuildHeaders(
                expenseId,
                json,
                OfflineOperationTypes.ExpenseCreate);
        }

        return SendAsync<PosExpenseDto>(HttpMethod.Post, ExpensesPath, request, headers, ct);
    }

    public Task<ApiResult<PosExpenseDto>> VoidExpenseAsync(
        Guid expenseId,
        VoidExpenseRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosExpenseDto>(HttpMethod.Post, $"{ExpensesPath}/{expenseId:D}/void", request, null, ct);

    public Task<ApiResult<PosExpenseSummaryDto>> GetSummaryAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? scope = null,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (fromDate is not null)
        {
            parts.Add("fromDate=" + Uri.EscapeDataString(fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        if (toDate is not null)
        {
            parts.Add("toDate=" + Uri.EscapeDataString(toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrWhiteSpace(scope))
        {
            parts.Add("scope=" + Uri.EscapeDataString(scope.Trim()));
        }

        if (branchId is not null)
        {
            parts.Add("branchId=" + branchId.Value.ToString("D"));
        }

        var path = parts.Count == 0
            ? $"{ExpensesPath}/summary"
            : $"{ExpensesPath}/summary?{string.Join("&", parts)}";
        return SendAsync<PosExpenseSummaryDto>(HttpMethod.Get, path, null, null, ct);
    }

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
        IReadOnlyDictionary<string, string>? headers,
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

            if (headers is not null)
            {
                foreach (var pair in headers)
                {
                    request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
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
