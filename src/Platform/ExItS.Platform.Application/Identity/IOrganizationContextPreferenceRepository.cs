using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Identity;

/// <summary>Server-side last-active organization preference (ADR / architecture §11.2).</summary>
public interface IOrganizationContextPreferenceRepository
{
    Task<PlatformOrganizationId?> GetLastActiveOrganizationIdAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default);

    Task UpsertLastActiveOrganizationAsync(
        PlatformUserId userIdentityId,
        PlatformOrganizationId? organizationId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}
