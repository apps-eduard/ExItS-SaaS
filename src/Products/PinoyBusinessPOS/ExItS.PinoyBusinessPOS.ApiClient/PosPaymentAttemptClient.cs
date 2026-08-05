using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Payments;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS payment-attempt client. Online-only; Paid state is accepted only from the server.
/// </summary>
public sealed class PosPaymentAttemptClient(
    HttpClient httpClient,
    IConnectivityService? connectivityService = null) : IPosPaymentAttemptClient
{
    private const string AttemptsPath = "/api/v1/pos/payment-attempts";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PaymentAttemptDto>> CreateAsync(
        Guid saleId,
        CreatePaymentAttemptRequest request,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var headers = PosMutationIdempotencyHelper.BuildHeaders(
            Guid.TryParse(request.IdempotencyKey, out var keyGuid) && keyGuid != Guid.Empty
                ? keyGuid
                : Guid.NewGuid(),
            json,
            OfflineOperationTypes.PaymentAttemptCreate);

        return SendAsync<PaymentAttemptDto>(
            HttpMethod.Post,
            $"/api/v1/pos/sales/{saleId:D}/payment-attempts",
            request,
            headers,
            ct);
    }

    public Task<ApiResult<PaymentAttemptDto>> GetAsync(Guid attemptId, CancellationToken ct = default) =>
        SendAsync<PaymentAttemptDto>(HttpMethod.Get, $"{AttemptsPath}/{attemptId:D}", null, null, ct);

    public Task<ApiResult<PaymentAttemptDto>> CancelAsync(Guid attemptId, CancellationToken ct = default) =>
        SendAsync<PaymentAttemptDto>(
            HttpMethod.Post,
            $"{AttemptsPath}/{attemptId:D}/cancel",
            null,
            null,
            ct);

    public Task<ApiResult<PaymentAttemptDto>> SimulateAsync(
        Guid attemptId,
        SimulatePaymentRequest request,
        CancellationToken ct = default) =>
        SendAsync<PaymentAttemptDto>(
            HttpMethod.Post,
            $"{AttemptsPath}/{attemptId:D}/simulate",
            request,
            null,
            ct);

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
