using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Authorization;

/// <summary>
/// The actor performing the current operation. Authenticated browser sessions populate
/// <see cref="PlatformUserId"/> and optional membership-validated <see cref="OrganizationId"/>.
/// Development/Testing may fall back to a labeled DevelopmentOperator — never Production authentication.
/// </summary>
public sealed record PlatformActorContext(
    string ActorIdentifier,
    AuditActorType ActorType,
    PlatformUserId? PlatformUserId,
    string? CorrelationId,
    PlatformOrganizationId? OrganizationId = null,
    AccountClass? AccountClass = null);

/// <summary>Resolves the actor associated with the current operation (for audit and authorization).</summary>
public interface IPlatformActorAccessor
{
    PlatformActorContext GetCurrent();
}
