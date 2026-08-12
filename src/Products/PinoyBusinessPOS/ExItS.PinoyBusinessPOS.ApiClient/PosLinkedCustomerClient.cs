using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Statements;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PosLinkedCustomerClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosLinkedCustomerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<LinkedCustomerStatementSummaryDto>> GetStatementAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        string? currency = null,
        CancellationToken ct = default)
    {
        var path = new StringBuilder(
            $"/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:D}/statement?organizationId={organizationId:D}");
        if (!string.IsNullOrWhiteSpace(currency))
        {
            path.Append("&currency=").Append(Uri.EscapeDataString(currency.Trim()));
        }

        return SendAsync<LinkedCustomerStatementSummaryDto>(HttpMethod.Get, path.ToString(), ct);
    }

    public Task<ApiResult<LinkedCustomerRecentActivityPageDto>> GetRecentActivityAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var path =
            $"/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:D}/activity" +
            $"?organizationId={organizationId:D}&page={page}&pageSize={pageSize}";
        return SendAsync<LinkedCustomerRecentActivityPageDto>(HttpMethod.Get, path, ct);
    }

    public Task<ApiResult<LinkedCustomerOpenDebtActivityPageDto>> GetOpenDebtActivityAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var path =
            $"/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:D}/open-debt-activity" +
            $"?organizationId={organizationId:D}&page={page}&pageSize={pageSize}";
        return SendAsync<LinkedCustomerOpenDebtActivityPageDto>(HttpMethod.Get, path, ct);
    }

    private async Task<ApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
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

    private static ApiCallStatus Classify(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => ApiCallStatus.Validation,
        HttpStatusCode.Unauthorized => ApiCallStatus.Unauthorized,
        HttpStatusCode.Forbidden => ApiCallStatus.Forbidden,
        HttpStatusCode.NotFound => ApiCallStatus.NotFound,
        >= HttpStatusCode.InternalServerError => ApiCallStatus.Unavailable,
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

                if (root.TryGetProperty("errorCode", out var c) && c.ValueKind == JsonValueKind.String)
                {
                    errorCode = c.GetString();
                }
                else if (root.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
                {
                    errorCode = code.GetString();
                }
            }
            catch (JsonException)
            {
                // keep defaults
            }
        }

        return new ApiError(
            title ?? statusCode.ToString(CultureInfo.InvariantCulture),
            detail,
            errorCode,
            correlationId,
            statusCode);
    }
}
