using System.Security.Claims;
using System.Threading.RateLimiting;

namespace ExItS.Platform.Api.Common;

/// <summary>
/// Shared fixed-window limiter for public-ID resolution probes (resolve endpoints and identified contact add).
/// </summary>
internal static class PublicIdResolveRateLimitGate
{
    private static readonly PartitionedRateLimiter<string> Limiter =
        PartitionedRateLimiter.Create<string, string>(partitionKey =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0
                }));

    internal static string BuildPartitionKey(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var user = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon";
        return "public-id:" + user + ":" + ip;
    }

    internal static async ValueTask<RateLimitLease> AcquireAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        return await Limiter
            .AcquireAsync(BuildPartitionKey(httpContext), 1, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static IResult RejectionResult() =>
        Results.Json(
            new
            {
                title = "Too Many Requests",
                status = StatusCodes.Status429TooManyRequests,
                detail = "Request rate limit exceeded. Retry later.",
                errorCode = "platform.rate_limit.exceeded"
            },
            statusCode: StatusCodes.Status429TooManyRequests,
            contentType: "application/problem+json");
}
