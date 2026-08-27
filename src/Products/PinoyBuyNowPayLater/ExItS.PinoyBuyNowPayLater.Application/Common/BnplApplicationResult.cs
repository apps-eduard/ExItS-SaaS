namespace ExItS.PinoyBuyNowPayLater.Application.Common;

public sealed class BnplApplicationResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public int? SuggestedHttpStatus { get; }

    private BnplApplicationResult(
        bool isSuccess,
        T? value,
        string? errorCode,
        string? errorMessage,
        int? suggestedHttpStatus)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        SuggestedHttpStatus = suggestedHttpStatus;
    }

    public static BnplApplicationResult<T> Success(T value) =>
        new(true, value, null, null, null);

    public static BnplApplicationResult<T> Failure(
        string errorCode,
        string message,
        int? suggestedHttpStatus = null) =>
        new(false, default, errorCode, message, suggestedHttpStatus);
}
