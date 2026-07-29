namespace ExItS.Platform.Admin.Services;

public enum ApiCallStatus { Success, NotFound, Unavailable, Validation, Failed }

public sealed record ApiCallResult<T>(ApiCallStatus Status, T? Data = default, PlatformApiException? Error = null)
{
    public bool IsSuccess => Status == ApiCallStatus.Success;
    public static ApiCallResult<T> Success(T data) => new(ApiCallStatus.Success, data);
    public static ApiCallResult<T> NotFound(PlatformApiException error) => new(ApiCallStatus.NotFound, default, error);
    public static ApiCallResult<T> Unavailable(PlatformApiException error) => new(ApiCallStatus.Unavailable, default, error);
    public static ApiCallResult<T> Validation(PlatformApiException error) => new(ApiCallStatus.Validation, default, error);
    public static ApiCallResult<T> Failed(PlatformApiException error) => new(ApiCallStatus.Failed, default, error);
}
