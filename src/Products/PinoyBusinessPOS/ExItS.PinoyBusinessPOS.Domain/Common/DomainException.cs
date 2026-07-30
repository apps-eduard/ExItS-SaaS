namespace ExItS.PinoyBusinessPOS.Domain.Common;

/// <summary>Explicit domain validation / invariant failure with a stable error code for API mapping.</summary>
public sealed class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ErrorCode = errorCode;
    }
}
