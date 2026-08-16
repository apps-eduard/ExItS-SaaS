using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

/// <summary>
/// Gateway transport/provider failure. When <see cref="SessionMayExist"/> is true, the session may
/// already have been created at the provider and must be recovered via GetSession — do not release
/// inventory reservation solely on this signal.
/// </summary>
public sealed class PaymentGatewayException : Exception
{
    public string ErrorCode { get; }
    public bool SessionMayExist { get; }

    public PaymentGatewayException(
        string errorCode,
        string message,
        bool sessionMayExist,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode)
            ? DomainErrorCodes.PaymentGatewayFailure
            : errorCode;
        SessionMayExist = sessionMayExist;
    }

    public static PaymentGatewayException DefiniteFailure(string message) =>
        new(DomainErrorCodes.PaymentGatewayFailure, message, sessionMayExist: false);

    public static PaymentGatewayException TimeoutBeforeCreate(string message) =>
        new(DomainErrorCodes.PaymentGatewayTimeout, message, sessionMayExist: false);

    public static PaymentGatewayException TimeoutAfterCreate(string message) =>
        new(DomainErrorCodes.PaymentGatewayTimeout, message, sessionMayExist: true);
}
