using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Returns;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>Typed POS sale return client. Online-only.</summary>
public sealed class PosSaleReturnClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosSaleReturnClient
{
    private const string ReturnsPath = "/api/v1/pos/sale-returns";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PosSaleReturnPagedResult>> ListReturnsAsync(
        Guid? saleId = null,
        string? returnNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(ReturnsPath).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        if (saleId is Guid id && id != Guid.Empty)
        {
            query.Append("&saleId=").Append(id.ToString("D", CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(returnNumber))
        {
            query.Append("&returnNumber=").Append(Uri.EscapeDataString(returnNumber.Trim()));
        }

        return SendAsync<PosSaleReturnPagedResult>(HttpMethod.Get, query.ToString(), null, null, ct);
    }

    public Task<ApiResult<PosSaleReturnDto>> GetReturnAsync(Guid returnId, CancellationToken ct = default) =>
        SendAsync<PosSaleReturnDto>(HttpMethod.Get, $"{ReturnsPath}/{returnId:D}", null, null, ct);

    public Task<ApiResult<PosRefundableSaleDto>> GetRefundableAsync(Guid saleId, CancellationToken ct = default) =>
        SendAsync<PosRefundableSaleDto>(HttpMethod.Get, $"{ReturnsPath}/refundable/{saleId:D}", null, null, ct);

    public Task<ApiResult<PosSaleReturnDto>> CreateReturnAsync(
        CreateSaleReturnRequest request,
        CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string>? headers = null;
        if (request.ReturnId is Guid returnId && returnId != Guid.Empty)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            headers = PosMutationIdempotencyHelper.BuildHeaders(
                returnId,
                json,
                OfflineOperationTypes.SaleReturnCreate);
        }

        return SendAsync<PosSaleReturnDto>(HttpMethod.Post, ReturnsPath, request, headers, ct);
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

            var payload = string.IsNullOrWhiteSpace(content)
                ? default
                : JsonSerializer.Deserialize<TResponse>(content, JsonOptions);

            return payload is null
                ? new ApiResult<TResponse>
                {
                    Status = ApiCallStatus.Failed,
                    Error = new ApiError("Invalid response", "The API returned no content.", null, correlationId, (int)response.StatusCode)
                }
                : ApiResult<TResponse>.Success(payload);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Timeout,
                Error = new ApiError("Timeout", "The request timed out.", null, null, null)
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
    }

    private static ApiCallStatus Classify(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ApiCallStatus.Forbidden,
            HttpStatusCode.NotFound => ApiCallStatus.NotFound,
            HttpStatusCode.Conflict => ApiCallStatus.Conflict,
            _ => ApiCallStatus.Failed
        };

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
}
