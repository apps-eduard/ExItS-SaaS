using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>Typed POS cashier shift client. Online-only for P10-WP04.</summary>
public sealed class PosCashierShiftClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosCashierShiftClient
{
    private const string ShiftsPath = "/api/v1/pos/cashier-shifts";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PagedResult<PosCashierShiftDto>>> ListAsync(
        string? status = null,
        Guid? actorId = null,
        string? shiftNumber = null,
        string? fromBusinessDate = null,
        string? toBusinessDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(ShiftsPath).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "status", status);
        AppendOptional(query, "shiftNumber", shiftNumber);
        AppendOptional(query, "fromBusinessDate", fromBusinessDate);
        AppendOptional(query, "toBusinessDate", toBusinessDate);
        if (actorId is not null)
        {
            query.Append("&actorId=").Append(actorId.Value.ToString("D"));
        }

        return SendAsync<PagedResult<PosCashierShiftDto>>(HttpMethod.Get, query.ToString(), null, null, ct);
    }

    public Task<ApiResult<PosCashierShiftDto>> GetCurrentAsync(CancellationToken ct = default) =>
        SendAsync<PosCashierShiftDto>(HttpMethod.Get, $"{ShiftsPath}/current", null, null, ct);

    public Task<ApiResult<PosCashierShiftDto>> GetAsync(Guid shiftId, CancellationToken ct = default) =>
        SendAsync<PosCashierShiftDto>(HttpMethod.Get, $"{ShiftsPath}/{shiftId:D}", null, null, ct);

    public Task<ApiResult<PosCashierShiftSummaryDto>> GetSummaryAsync(Guid shiftId, CancellationToken ct = default) =>
        SendAsync<PosCashierShiftSummaryDto>(HttpMethod.Get, $"{ShiftsPath}/{shiftId:D}/summary", null, null, ct);

    public Task<ApiResult<PosCashierShiftDto>> OpenAsync(OpenCashierShiftRequest request, CancellationToken ct = default) =>
        SendAsync<PosCashierShiftDto>(HttpMethod.Post, ShiftsPath, request, null, ct);

    public Task<ApiResult<PosCashierShiftDto>> CloseAsync(
        Guid shiftId,
        CloseCashierShiftRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosCashierShiftDto>(HttpMethod.Post, $"{ShiftsPath}/{shiftId:D}/close", request, null, ct);

    public Task<ApiResult<PosCashierShiftDto>> CancelAsync(Guid shiftId, CancellationToken ct = default) =>
        SendAsync<PosCashierShiftDto>(HttpMethod.Post, $"{ShiftsPath}/{shiftId:D}/cancel", null, null, ct);

    public Task<ApiResult<PosCashierShiftMovementDto>> RecordMovementAsync(
        Guid shiftId,
        RecordCashierShiftMovementRequest request,
        CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string>? headers = null;
        if (request.MovementId is Guid movementId && movementId != Guid.Empty)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            headers = PosMutationIdempotencyHelper.BuildHeaders(
                movementId,
                json,
                OfflineOperationTypes.CashierShiftMovement);
        }

        return SendAsync<PosCashierShiftMovementDto>(
            HttpMethod.Post,
            $"{ShiftsPath}/{shiftId:D}/movements",
            request,
            headers,
            ct);
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
