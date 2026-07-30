using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Infrastructure.Authorization;

/// <summary>
/// Development-stage actor accessor. Platform APIs remain without production authentication.
/// Optional header <c>X-Dev-Platform-User-Id</c> selects a Platform User principal for permission testing
/// only in Development/Testing; otherwise a labeled DevelopmentOperator is returned.
/// This is not production authentication.
/// </summary>
internal sealed class DevelopmentPlatformActorAccessor(
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment) : IPlatformActorAccessor
{
    public const string DevPlatformUserIdHeader = "X-Dev-Platform-User-Id";
    public const string CorrelationIdHeader = "X-Correlation-ID";
    private const string DevelopmentOperatorIdentifier = "development-operator:unauthenticated";

    public PlatformActorContext GetCurrent()
    {
        var http = httpContextAccessor.HttpContext;
        var correlationId = http?.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? http?.TraceIdentifier;

        var isDevLike = environment.IsDevelopment()
                        || environment.IsEnvironment("Testing");

        // Outside Development/Testing, ignore forged Development identity headers.
        if (isDevLike)
        {
            var userHeader = http?.Request.Headers[DevPlatformUserIdHeader].FirstOrDefault();
            if (Guid.TryParse(userHeader, out var userId) && userId != Guid.Empty)
            {
                return new PlatformActorContext(
                    $"platform-user:{userId:D}",
                    AuditActorType.PlatformUser,
                    PlatformUserId.From(userId),
                    correlationId);
            }
        }

        return new PlatformActorContext(
            DevelopmentOperatorIdentifier,
            AuditActorType.DevelopmentOperator,
            null,
            correlationId);
    }
}
