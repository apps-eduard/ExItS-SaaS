using System.Net;

namespace ExItS.Platform.Admin.Services;

public sealed class PlatformApiException : Exception
{
    public PlatformApiException(HttpStatusCode? statusCode, string title, string? detail = null, string? correlationId = null, Exception? innerException = null)
        : base(detail ?? title, innerException)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        CorrelationId = correlationId;
    }

    public HttpStatusCode? StatusCode { get; }
    public string Title { get; }
    public string? Detail { get; }
    public string? CorrelationId { get; }
}
