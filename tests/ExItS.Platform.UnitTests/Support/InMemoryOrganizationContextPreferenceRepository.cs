using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryOrganizationContextPreferenceRepository : IOrganizationContextPreferenceRepository
{
    private readonly Dictionary<Guid, PlatformOrganizationId?> _byUser = new();

    public Task<PlatformOrganizationId?> GetLastActiveOrganizationIdAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byUser.TryGetValue(userIdentityId.Value, out var id) ? id : null);

    public Task UpsertLastActiveOrganizationAsync(
        PlatformUserId userIdentityId,
        PlatformOrganizationId? organizationId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        _byUser[userIdentityId.Value] = organizationId;
        return Task.CompletedTask;
    }
}
