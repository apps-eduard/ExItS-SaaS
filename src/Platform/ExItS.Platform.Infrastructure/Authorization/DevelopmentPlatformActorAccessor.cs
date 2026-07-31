using System.Security.Claims;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Infrastructure.Authorization;

/// <summary>
/// Resolves the current Platform actor from an authenticated browser session when present.
/// In Development/Testing only, falls back to optional <c>X-Dev-Platform-User-Id</c> or a labeled DevelopmentOperator.
/// Dev headers are never Production authentication.
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

        var user = http?.User;
        if (user?.Identity?.IsAuthenticated == true
            && string.Equals(user.Identity.AuthenticationType, PlatformSessionClaimTypes.AuthenticationScheme, StringComparison.Ordinal))
        {
            var idValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(idValue, out var userId) && userId != Guid.Empty)
            {
                return new PlatformActorContext(
                    $"platform-user:{userId:D}",
                    AuditActorType.PlatformUser,
                    PlatformUserId.From(userId),
                    correlationId);
            }
        }

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
