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
    Cancelled,
    Failed
}

/// <summary>Normalized error payload surfaced to callers, sourced from ProblemDetails when available.</summary>
public sealed record ApiError(string? Title, string? Detail, string? ErrorCode, string? CorrelationId, int? StatusCode);

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
    public static ApiResult<T> Cancelled(ApiError? error = null) => Failure(ApiCallStatus.Cancelled, error);
    public static ApiResult<T> Failed(ApiError? error = null) => Failure(ApiCallStatus.Failed, error);
}
