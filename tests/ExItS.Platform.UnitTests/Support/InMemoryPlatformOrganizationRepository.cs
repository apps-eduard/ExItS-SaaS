using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

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

    public Task<PlatformOrganization?> GetByPublicOrganizationIdAsync(
        string publicOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (!PublicOrganizationIdRules.TryNormalize(publicOrganizationId, out var normalized))
        {
            return Task.FromResult<PlatformOrganization?>(null);
        }

        var match = _byId.Values.FirstOrDefault(o =>
            string.Equals(o.PublicOrganizationId, normalized, StringComparison.Ordinal));
        return Task.FromResult(match);
    }

    public Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        ListAsync(null, null, OrganizationListSortBy.DisplayName, false, skip, take, productCode: null, cancellationToken);

    public Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
        OrganizationStatus? status,
        string? search,
        OrganizationListSortBy sortBy,
        bool sortDescending,
        int skip,
        int take,
        ProductCode? productCode = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<PlatformOrganization> query = _byId.Values;
        if (status is not null)
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o =>
                o.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || o.Slug.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (o.Profile.LegalName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (o.Profile.ContactEmail?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        query = (sortBy, sortDescending) switch
        {
            (OrganizationListSortBy.Slug, false) => query.OrderBy(o => o.Slug).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.Slug, true) => query.OrderByDescending(o => o.Slug).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.Status, false) => query.OrderBy(o => o.Status).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.Status, true) => query.OrderByDescending(o => o.Status).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.CreatedAtUtc, false) => query.OrderBy(o => o.CreatedAtUtc).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.CreatedAtUtc, true) => query.OrderByDescending(o => o.CreatedAtUtc).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.UpdatedAtUtc, false) => query.OrderBy(o => o.UpdatedAtUtc).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.UpdatedAtUtc, true) => query.OrderByDescending(o => o.UpdatedAtUtc).ThenBy(o => o.DisplayName),
            (_, true) => query.OrderByDescending(o => o.DisplayName).ThenBy(o => o.Slug),
            _ => query.OrderBy(o => o.DisplayName).ThenBy(o => o.Slug)
        };

        var ordered = query.ToList();
        var page = ordered.Skip(skip).Take(take).ToList();
        return Task.FromResult<(IReadOnlyList<PlatformOrganization>, int)>((page, ordered.Count));
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
        // Keep slug index consistent when slug changes.
        foreach (var pair in _slugIndex.Where(p => p.Value == organization.Id.Value).ToList())
        {
            _slugIndex.Remove(pair.Key);
        }

        _byId[organization.Id.Value] = organization;
        _slugIndex[organization.Slug] = organization.Id.Value;
        UpdateCount++;
        return Task.CompletedTask;
    }
}
