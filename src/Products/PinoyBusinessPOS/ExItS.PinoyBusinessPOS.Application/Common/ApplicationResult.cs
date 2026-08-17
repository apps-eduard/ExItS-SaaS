namespace ExItS.PinoyBusinessPOS.Application.Common;

public sealed class ApplicationResult
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyDictionary<string, string>? ErrorDetails { get; }

    private ApplicationResult(
        bool isSuccess,
        string? errorCode,
        string? errorMessage,
        IReadOnlyDictionary<string, string>? errorDetails = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
    }

    public static ApplicationResult Success() => new(true, null, null);

    public static ApplicationResult Failure(
        string errorCode,
        string message,
        IReadOnlyDictionary<string, string>? errorDetails = null) =>
        new(false, errorCode, message, errorDetails);
}

public sealed class ApplicationResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyDictionary<string, string>? ErrorDetails { get; }

    private ApplicationResult(
        bool isSuccess,
        T? value,
        string? errorCode,
        string? errorMessage,
        IReadOnlyDictionary<string, string>? errorDetails = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
    }

    public static ApplicationResult<T> Success(T value) => new(true, value, null, null);

    public static ApplicationResult<T> Failure(
        string errorCode,
        string message,
        IReadOnlyDictionary<string, string>? errorDetails = null) =>
        new(false, default, errorCode, message, errorDetails);

    public static ApplicationResult<T> Failure(ApplicationResult other) =>
        new(false, default, other.ErrorCode, other.ErrorMessage, other.ErrorDetails);

    public static ApplicationResult<T> Failure<TOther>(ApplicationResult<TOther> other) =>
        new(false, default, other.ErrorCode, other.ErrorMessage, other.ErrorDetails);
}
