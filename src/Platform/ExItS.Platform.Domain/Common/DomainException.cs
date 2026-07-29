namespace ExItS.Platform.Domain.Common;

/// <summary>
/// Explicit domain validation / invariant failure with a stable error code for later API mapping.
/// Not coupled to ASP.NET Core ProblemDetails.
/// </summary>
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
