using System.Text.Json;

namespace ExItS.PinoyBusinessPOS.Application.Common;

/// <summary>Outcome classification for a single POS API call.</summary>
public enum ApiCallStatus
{
    Success,
    NotFound,
    Validation,
    Conflict,
    Unauthorized,
    Forbidden,
    Timeout,
    Offline,
    Unavailable,
    RateLimited,
    Cancelled,
    Failed
}

/// <summary>Normalized error payload surfaced to callers, sourced from ProblemDetails when available.</summary>
public sealed record ApiError(
    string? Title,
    string? Detail,
    string? ErrorCode,
    string? CorrelationId,
    int? StatusCode,
    IReadOnlyDictionary<string, string>? Details = null);

public static class ApiProblemParser
{
    private static readonly HashSet<string> Reserved =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "type", "title", "status", "detail", "instance", "traceId", "traceid"
        };

    public static ApiError Parse(string content, string? correlationId, int statusCode)
    {
        string? title = null;
        string? detail = null;
        string? errorCode = null;
        Dictionary<string, string>? details = null;
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

                foreach (var property in root.EnumerateObject())
                {
                    if (Reserved.Contains(property.Name)
                        || property.NameEquals("errorCode"))
                    {
                        continue;
                    }

                    var value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Number => property.Value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => null
                    };
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    details ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    details[property.Name] = value;
                }
            }
            catch (JsonException)
            {
            }
        }

        return new ApiError(title, detail ?? (string.IsNullOrWhiteSpace(content) ? null : content), errorCode, correlationId, statusCode, details);
    }
}

/// <summary>Result envelope returned by <c>IPosApiClient</c> so callers never need to catch HTTP exceptions.</summary>
public sealed class ApiResult<T>
{
    public required ApiCallStatus Status { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }

    public bool IsSuccess => Status == ApiCallStatus.Success;

    public static ApiResult<T> Success(T data) => new() { Status = ApiCallStatus.Success, Data = data };
    public static ApiResult<T> Failure(ApiCallStatus status, ApiError? error = null) => new() { Status = status, Error = error };

    public static ApiResult<T> NotFound(ApiError? error = null) => Failure(ApiCallStatus.NotFound, error);
    public static ApiResult<T> Validation(ApiError? error = null) => Failure(ApiCallStatus.Validation, error);
    public static ApiResult<T> Conflict(ApiError? error = null) => Failure(ApiCallStatus.Conflict, error);
    public static ApiResult<T> Unauthorized(ApiError? error = null) => Failure(ApiCallStatus.Unauthorized, error);
    public static ApiResult<T> Forbidden(ApiError? error = null) => Failure(ApiCallStatus.Forbidden, error);
    public static ApiResult<T> Timeout(ApiError? error = null) => Failure(ApiCallStatus.Timeout, error);
    public static ApiResult<T> Offline(ApiError? error = null) => Failure(ApiCallStatus.Offline, error);
    public static ApiResult<T> Unavailable(ApiError? error = null) => Failure(ApiCallStatus.Unavailable, error);
    public static ApiResult<T> RateLimited(ApiError? error = null) => Failure(ApiCallStatus.RateLimited, error);
    public static ApiResult<T> Cancelled(ApiError? error = null) => Failure(ApiCallStatus.Cancelled, error);
    public static ApiResult<T> Failed(ApiError? error = null) => Failure(ApiCallStatus.Failed, error);
}
