using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed <see cref="IPosApiClient"/> implementation. Classifies every outcome into an
/// <see cref="ApiCallStatus"/> instead of throwing, short-circuits to <see cref="ApiCallStatus.Offline"/>
/// when connectivity is known to be down, and retries safe idempotent GET requests exactly once
/// on transient (<see cref="ApiCallStatus.Unavailable"/> or <see cref="ApiCallStatus.Timeout"/>) failures.
/// </summary>
public sealed class PosApiClient(HttpClient httpClient, IConnectivityService? connectivityService = null) : IPosApiClient
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);
    private const string HealthPath = "/health";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, ct);

    public async Task<ApiResult<HealthStatusDto>> GetHealthAsync(CancellationToken ct = default)
    {
        var offline = await TryOfflineShortCircuitAsync(ct).ConfigureAwait(false);
        if (offline is not null)
        {
            return new ApiResult<HealthStatusDto> { Status = offline.Status, Error = offline.Error };
        }

        var raw = await ExecuteWithRetryAsync(HttpMethod.Get, HealthPath, null, ct).ConfigureAwait(false);
        return raw.IsSuccess
            ? ApiResult<HealthStatusDto>.Success(ParseHealth(raw.Data))
            : new ApiResult<HealthStatusDto> { Status = raw.Status, Error = raw.Error };
    }

    public async Task<ApiResult<TResponse>> SendAsync<TResponse>(HttpMethod method, string path, object? body = null, CancellationToken ct = default)
    {
        var offline = await TryOfflineShortCircuitAsync(ct).ConfigureAwait(false);
        if (offline is not null)
        {
            return ToTypedResult<TResponse>(offline);
        }

        var raw = await ExecuteWithRetryAsync(method, path, body, ct).ConfigureAwait(false);
        return ToTypedResult<TResponse>(raw);
    }

    private async Task<ApiResult<string>?> TryOfflineShortCircuitAsync(CancellationToken ct)
    {
        if (connectivityService is null)
        {
            return null;
        }

        var connected = await connectivityService.IsConnectedAsync(ct).ConfigureAwait(false);
        return connected
            ? null
            : new ApiResult<string>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Offline", "No network connectivity detected.", null, null, null)
            };
    }

    private async Task<ApiResult<string>> ExecuteWithRetryAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var raw = await ExecuteRawAsync(method, path, body, ct).ConfigureAwait(false);
        if (!IsRetryableGet(method, raw.Status))
        {
            return raw;
        }

        try
        {
            await Task.Delay(RetryDelay, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new ApiResult<string> { Status = ApiCallStatus.Cancelled };
        }

        return await ExecuteRawAsync(method, path, body, ct).ConfigureAwait(false);
    }

    private static bool IsRetryableGet(HttpMethod method, ApiCallStatus status) =>
        method == HttpMethod.Get && status is ApiCallStatus.Unavailable or ApiCallStatus.Timeout;

    private async Task<ApiResult<string>> ExecuteRawAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body, options: JsonOptions);
            }

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var correlationId = ExtractCorrelationId(response);
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<string>.Success(content);
            }

            var error = ParseProblemDetails(content, correlationId, (int)response.StatusCode);
            return new ApiResult<string> { Status = ClassifyStatusCode(response.StatusCode), Error = error };
        }
        catch (HttpRequestException ex)
        {
            // Any transport-level failure (no route to host, connection refused, DNS failure, TLS
            // failure) is treated as an offline condition for a client application: the API cannot
            // be reached, regardless of the precise transport reason.
            return new ApiResult<string>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Network unavailable", ex.Message, null, null, null)
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new ApiResult<string> { Status = ApiCallStatus.Cancelled };
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // The caller did not cancel; this is the HttpClient timeout firing.
            return new ApiResult<string>
            {
                Status = ApiCallStatus.Timeout,
                Error = new ApiError("Request timed out", ex.Message, null, null, null)
            };
        }
    }

    private static ApiResult<T> ToTypedResult<T>(ApiResult<string> raw)
    {
        if (!raw.IsSuccess)
        {
            return new ApiResult<T> { Status = raw.Status, Error = raw.Error };
        }

        if (typeof(T) == typeof(string))
        {
            return ApiResult<T>.Success((T)(object)(raw.Data ?? string.Empty));
        }

        if (string.IsNullOrWhiteSpace(raw.Data))
        {
            return new ApiResult<T>
            {
                Status = ApiCallStatus.Failed,
                Error = new ApiError("Invalid response", "The API returned no content.", null, null, null)
            };
        }

        try
        {
            var data = JsonSerializer.Deserialize<T>(raw.Data, JsonOptions);
            return data is null
                ? new ApiResult<T> { Status = ApiCallStatus.Failed, Error = new ApiError("Invalid response", "The API returned no content.", null, null, null) }
                : ApiResult<T>.Success(data);
        }
        catch (JsonException ex)
        {
            return new ApiResult<T>
            {
                Status = ApiCallStatus.Failed,
                Error = new ApiError("Invalid response", ex.Message, null, null, null)
            };
        }
    }

    private static HealthStatusDto ParseHealth(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new HealthStatusDto("Unknown");
        }

        try
        {
            var dto = JsonSerializer.Deserialize<HealthStatusDto>(content, JsonOptions);
            if (dto is not null)
            {
                return dto;
            }
        }
        catch (JsonException)
        {
            // Not a JSON object — fall through and treat the body as a bare status string.
        }

        return new HealthStatusDto(content.Trim().Trim('"'));
    }

    private static ApiError ParseProblemDetails(string content, string? correlationId, int statusCode)
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
                title = TryGetString(root, "title");
                detail = TryGetString(root, "detail");
                errorCode = TryGetString(root, "errorCode");
            }
            catch (JsonException)
            {
                // Non-ProblemDetails error body — surface with title/detail unset.
            }
        }

        return new ApiError(title, detail, errorCode, correlationId, statusCode);
    }

    private static string? TryGetString(JsonElement root, string propertyName) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static string? ExtractCorrelationId(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Correlation-ID", out var values) ? values.FirstOrDefault() : null;

    private static ApiCallStatus ClassifyStatusCode(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => ApiCallStatus.NotFound,
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiCallStatus.Validation,
        HttpStatusCode.Conflict => ApiCallStatus.Conflict,
        HttpStatusCode.Unauthorized => ApiCallStatus.Unauthorized,
        HttpStatusCode.Forbidden => ApiCallStatus.Forbidden,
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ApiCallStatus.Timeout,
        HttpStatusCode.TooManyRequests => ApiCallStatus.RateLimited,
        _ when (int)statusCode >= 500 => ApiCallStatus.Unavailable,
        _ => ApiCallStatus.Failed
    };
}
