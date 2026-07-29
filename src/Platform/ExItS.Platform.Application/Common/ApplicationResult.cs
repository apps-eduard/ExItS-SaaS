namespace ExItS.Platform.Application.Common;

/// <summary>
/// Small application-layer result for use-case outcomes. Not a generic Result framework.
/// </summary>
public sealed class ApplicationResult
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private ApplicationResult(bool isSuccess, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static ApplicationResult Success() => new(true, null, null);

    public static ApplicationResult Failure(string errorCode, string message) =>
        new(false, errorCode, message);
}

/// <summary>Application result carrying a value on success.</summary>
public sealed class ApplicationResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private ApplicationResult(bool isSuccess, T? value, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static ApplicationResult<T> Success(T value) => new(true, value, null, null);

    public static ApplicationResult<T> Failure(string errorCode, string message) =>
        new(false, default, errorCode, message);
}
