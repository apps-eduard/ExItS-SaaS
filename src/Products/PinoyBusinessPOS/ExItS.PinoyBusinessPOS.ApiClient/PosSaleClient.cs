using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS sales client. Online-only for P8-WP02: offline calls fail fast with
/// <see cref="ApiCallStatus.Offline"/> and no sale is ever queued or cached locally.
/// </summary>
public sealed class PosSaleClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosSaleClient
{
    private const string SalesPath = "/api/v1/pos/sales";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PosSalePagedResult>> ListSalesAsync(
        string? status = null,
        string? paymentMethod = null,
        DateOnly? fromDateUtc = null,
        DateOnly? toDateUtc = null,
        string? saleNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(SalesPath).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "status", status);
        AppendOptional(query, "paymentMethod", paymentMethod);
        AppendOptional(query, "saleNumber", saleNumber);
        AppendOptional(query, "fromDate", fromDateUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendOptional(query, "toDate", toDateUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return SendAsync<PosSalePagedResult>(HttpMethod.Get, query.ToString(), null, null, ct);
    }

    public Task<ApiResult<PosSaleDto>> GetSaleAsync(Guid saleId, CancellationToken ct = default) =>
        SendAsync<PosSaleDto>(HttpMethod.Get, $"{SalesPath}/{saleId:D}", null, null, ct);

    public Task<ApiResult<PosSaleDto>> CheckoutAsync(
        CheckoutSaleRequest request,
        CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string>? headers = null;
        if (request.SaleId is Guid saleId && saleId != Guid.Empty)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            headers = PosMutationIdempotencyHelper.BuildHeaders(
                saleId,
                json,
                OfflineOperationTypes.SaleCheckout);
        }

        return SendAsync<PosSaleDto>(HttpMethod.Post, SalesPath, request, headers, ct);
    }

    public Task<ApiResult<PosSaleDto>> VoidSaleAsync(
        Guid saleId,
        VoidSaleRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosSaleDto>(HttpMethod.Post, $"{SalesPath}/{saleId:D}/void", request, null, ct);

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
