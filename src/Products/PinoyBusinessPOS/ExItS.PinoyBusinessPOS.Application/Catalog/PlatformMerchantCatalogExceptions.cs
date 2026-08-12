using System.Net;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Platform merchant catalog call failed in a way that should not be reported as a generic
/// "temporarily unavailable" outage (auth, entitlement, validation).
/// </summary>
public sealed class PlatformMerchantCatalogRequestException : Exception
{
    public PlatformMerchantCatalogRequestException(
        HttpStatusCode statusCode,
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }
    public string? ErrorCode { get; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsClientError => (int)StatusCode is >= 400 and < 500;
}

/// <summary>
/// Network / timeout / 5xx-class Platform merchant catalog failure.
/// </summary>
public sealed class PlatformMerchantCatalogTransientException : Exception
{
    public PlatformMerchantCatalogTransientException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
