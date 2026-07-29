using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformOrganizationRepository : IPlatformOrganizationRepository
{
    private readonly Dictionary<Guid, PlatformOrganization> _byId = new();
    private readonly Dictionary<string, Guid> _slugIndex = new(StringComparer.Ordinal);

    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<PlatformOrganization?> GetByIdAsync(PlatformOrganizationId id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var organization);
        return Task.FromResult(organization);
    }

    public Task<PlatformOrganization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (_slugIndex.TryGetValue(slug, out var id) && _byId.TryGetValue(id, out var organization))
        {
            return Task.FromResult<PlatformOrganization?>(organization);
        }

        return Task.FromResult<PlatformOrganization?>(null);
    }

    public Task AddAsync(PlatformOrganization organization, CancellationToken cancellationToken = default)
    {
        _byId[organization.Id.Value] = organization;
        _slugIndex[organization.Slug] = organization.Id.Value;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformOrganization organization, CancellationToken cancellationToken = default)
    {
        _byId[organization.Id.Value] = organization;
        _slugIndex[organization.Slug] = organization.Id.Value;
        UpdateCount++;
        return Task.CompletedTask;
    }
}
