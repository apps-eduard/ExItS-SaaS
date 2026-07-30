using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PosCustomerClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosCustomerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PosCustomerPagedResult>> ListAsync(
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder("/api/v1/pos/customers?");
        query.Append("page=").Append(page);
        query.Append("&pageSize=").Append(pageSize);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Append("&search=").Append(Uri.EscapeDataString(search.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Append("&status=").Append(Uri.EscapeDataString(status.Trim()));
        }

        return SendAsync<PosCustomerPagedResult>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosCustomerDetailDto>> GetAsync(Guid customerId, CancellationToken ct = default) =>
        SendAsync<PosCustomerDetailDto>(HttpMethod.Get, $"/api/v1/pos/customers/{customerId:D}", null, ct);

    public Task<ApiResult<PosCustomerDetailDto>> CreateAsync(CreatePosCustomerRequest request, CancellationToken ct = default) =>
        SendAsync<PosCustomerDetailDto>(HttpMethod.Post, "/api/v1/pos/customers", request, ct);

    public Task<ApiResult<PosCustomerDetailDto>> UpdateAsync(
        Guid customerId,
        UpdatePosCustomerRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosCustomerDetailDto>(HttpMethod.Put, $"/api/v1/pos/customers/{customerId:D}", request, ct);

    public Task<ApiResult<PosCustomerDetailDto>> DeactivateAsync(Guid customerId, CancellationToken ct = default) =>
        SendAsync<PosCustomerDetailDto>(HttpMethod.Post, $"/api/v1/pos/customers/{customerId:D}/deactivate", null, ct);

    public Task<ApiResult<PosCustomerDetailDto>> ReactivateAsync(Guid customerId, CancellationToken ct = default) =>
        SendAsync<PosCustomerDetailDto>(HttpMethod.Post, $"/api/v1/pos/customers/{customerId:D}/reactivate", null, ct);

    public Task<ApiResult<PosCustomerCreditSummaryDto>> GetCreditSummaryAsync(Guid customerId, CancellationToken ct = default) =>
        SendAsync<PosCustomerCreditSummaryDto>(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customerId:D}/credit-summary",
            null,
            ct);

    public Task<ApiResult<PosCreditEntryPagedResult>> ListCreditEntriesAsync(
        Guid customerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default) =>
        SendAsync<PosCreditEntryPagedResult>(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customerId:D}/credit-entries?page={page}&pageSize={pageSize}",
            null,
            ct);

    public Task<ApiResult<PosCreditEntryDto>> CreateCreditEntryAsync(
        Guid customerId,
        CreatePosCreditEntryRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosCreditEntryDto>(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customerId:D}/credit-entries",
            request,
            ct);

    public Task<ApiResult<PosCreditEntryDto>> ReverseCreditEntryAsync(
        Guid customerId,
        Guid entryId,
        ReversePosCreditEntryRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosCreditEntryDto>(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customerId:D}/credit-entries/{entryId:D}/reverse",
            request,
            ct);

    public Task<ApiResult<PosCustomerUtangSummaryDto>> GetUtangSummaryAsync(Guid customerId, CancellationToken ct = default) =>
        SendAsync<PosCustomerUtangSummaryDto>(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customerId:D}/utang-summary",
            null,
            ct);

    public Task<ApiResult<PosLedgerPagedResult>> ListLedgerAsync(
        Guid customerId,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default) =>
        SendAsync<PosLedgerPagedResult>(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customerId:D}/ledger?page={page}&pageSize={pageSize}",
            null,
            ct);

    public Task<ApiResult<PosRepaymentPagedResult>> ListRepaymentsAsync(
        Guid customerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default) =>
        SendAsync<PosRepaymentPagedResult>(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customerId:D}/repayments?page={page}&pageSize={pageSize}",
            null,
            ct);

    public Task<ApiResult<PosRepaymentDto>> CreateRepaymentAsync(
        Guid customerId,
        CreatePosRepaymentRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosRepaymentDto>(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customerId:D}/repayments",
            request,
            ct);

    public Task<ApiResult<PosRepaymentDto>> GetRepaymentAsync(Guid repaymentId, CancellationToken ct = default) =>
        SendAsync<PosRepaymentDto>(HttpMethod.Get, $"/api/v1/pos/repayments/{repaymentId:D}", null, ct);

    public Task<ApiResult<PosRepaymentDto>> ReverseRepaymentAsync(
        Guid repaymentId,
        ReversePosRepaymentRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosRepaymentDto>(
            HttpMethod.Post,
            $"/api/v1/pos/repayments/{repaymentId:D}/reverse",
            request,
            ct);

    public Task<ApiResult<PosCreditEntryDto>> SetCreditDueDateAsync(
        Guid creditEntryId,
        SetPosCreditDueDateRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosCreditEntryDto>(
            HttpMethod.Put,
            $"/api/v1/pos/credit/{creditEntryId:D}/due-date",
            request,
            ct);

    public Task<ApiResult<PosCreditEntryDto>> ClearCreditDueDateAsync(
        Guid creditEntryId,
        string reason,
        CancellationToken ct = default) =>
        SendAsync<PosCreditEntryDto>(
            HttpMethod.Delete,
            $"/api/v1/pos/credit/{creditEntryId:D}/due-date?reason={Uri.EscapeDataString(reason)}",
            null,
            ct);

    public Task<ApiResult<PosCreditDueDateHistoryPagedResult>> ListCreditDueDateHistoryAsync(
        Guid creditEntryId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default) =>
        SendAsync<PosCreditDueDateHistoryPagedResult>(
            HttpMethod.Get,
            $"/api/v1/pos/credit/{creditEntryId:D}/due-date-history?page={page}&pageSize={pageSize}",
            null,
            ct);

    public Task<ApiResult<PosCustomerOverdueSummaryDto>> GetOverdueSummaryAsync(
        Guid customerId,
        CancellationToken ct = default) =>
        SendAsync<PosCustomerOverdueSummaryDto>(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customerId:D}/overdue-summary",
            null,
            ct);

    public Task<ApiResult<PosAgedCreditPagedResult>> ListAgedCreditsAsync(
        Guid customerId,
        string? filter = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var path = $"/api/v1/pos/customers/{customerId:D}/aged-credits?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(filter))
        {
            path += "&filter=" + Uri.EscapeDataString(filter.Trim());
        }

        return SendAsync<PosAgedCreditPagedResult>(HttpMethod.Get, path, null, ct);
    }

    public Task<ApiResult<PosOverdueCustomerPagedResult>> ListOverdueCustomersAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default) =>
        SendAsync<PosOverdueCustomerPagedResult>(
            HttpMethod.Get,
            $"/api/v1/pos/overdue/customers?page={page}&pageSize={pageSize}",
            null,
            ct);

    public Task<ApiResult<PosAgedCreditPagedResult>> ListOverdueCreditsAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default) =>
        SendAsync<PosAgedCreditPagedResult>(
            HttpMethod.Get,
            $"/api/v1/pos/overdue/credits?page={page}&pageSize={pageSize}",
            null,
            ct);

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
