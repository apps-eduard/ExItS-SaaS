namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Thrown inside an idempotency execute callback when the use case fails so the outcome is not persisted.
/// </summary>
public sealed class PosIdempotencyAbortException : Exception
{
    public string ErrorCode { get; }

    public PosIdempotencyAbortException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ErrorCode = errorCode;
    }
}
