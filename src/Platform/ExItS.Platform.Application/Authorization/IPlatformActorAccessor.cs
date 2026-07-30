using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Authorization;

/// <summary>
/// The actor performing the current operation. During this development stage, Platform APIs are
/// unauthenticated, so implementations may report a labeled development-operator actor; this must
/// never be treated as production authentication.
/// </summary>
public sealed record PlatformActorContext(
    string ActorIdentifier,
    AuditActorType ActorType,
    PlatformUserId? PlatformUserId,
    string? CorrelationId);

/// <summary>Resolves the actor associated with the current operation (for audit and authorization).</summary>
public interface IPlatformActorAccessor
{
    PlatformActorContext GetCurrent();
}
