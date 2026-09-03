using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryOrganizationAreaRepository : IOrganizationAreaRepository
{
    private readonly List<OrganizationArea> _items = [];

    public InMemoryOrganizationAreaRepository(params OrganizationArea[] seed) => _items.AddRange(seed);

    public IReadOnlyList<OrganizationArea> Items => _items;

    public Task<OrganizationArea?> GetByIdAsync(OrganizationAreaId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<OrganizationArea>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationArea>>(
            _items.Where(x => x.OrganizationId == organizationId).ToList());

    public Task<int> CountActiveAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Count(x => x.OrganizationId == organizationId && x.Status == OrganizationAreaStatus.Active));

    public Task AddAsync(OrganizationArea area, CancellationToken cancellationToken = default)
    {
        _items.Add(area);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OrganizationArea area, CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(x => x.Id == area.Id);
        if (index >= 0)
        {
            _items[index] = area;
        }

        return Task.CompletedTask;
    }
}
