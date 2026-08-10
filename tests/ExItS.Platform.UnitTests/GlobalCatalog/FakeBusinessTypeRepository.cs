using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

internal sealed class FakeBusinessTypeRepository : IBusinessTypeRepository
{
    private readonly Dictionary<Guid, BusinessType> _byId = new();
    private readonly Dictionary<string, BusinessType> _byCode =
        new(StringComparer.OrdinalIgnoreCase);

    public FakeBusinessTypeRepository(bool seedLegacy = true)
    {
        if (!seedLegacy)
        {
            return;
        }

        var now = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        foreach (var (id, code, name, sortOrder) in LegacyBusinessTypeSeeds.All)
        {
            var entity = BusinessType.Create(code, name, now, sortOrder: sortOrder, id: BusinessTypeId.From(id));
            _byId[entity.Id.Value] = entity;
            _byCode[entity.Code] = entity;
        }
    }

    public Task<BusinessType?> GetByIdAsync(BusinessTypeId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.TryGetValue(id.Value, out var e) ? e : null);

    public Task<BusinessType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byCode.TryGetValue(code.Trim(), out var e) ? e : null);

    public Task<BusinessType?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var match = _byId.Values.FirstOrDefault(e => e.Name.ToUpperInvariant() == normalizedName);
        return Task.FromResult(match);
    }

    public Task<bool> ExistsWithCodeAsync(
        string code,
        BusinessTypeId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_byCode.TryGetValue(code.Trim(), out var e))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(excludingId is null || e.Id != excludingId);
    }

    public Task<bool> ExistsWithNameAsync(
        string name,
        BusinessTypeId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        var match = _byId.Values.FirstOrDefault(e => e.Name.ToUpperInvariant() == normalized);
        if (match is null)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(excludingId is null || match.Id != excludingId);
    }

    public Task<(IReadOnlyList<BusinessType> Items, int TotalCount)> ListAsync(
        BusinessTypeStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        BusinessTypeListSortBy sortBy = BusinessTypeListSortBy.SortOrder,
        bool sortDescending = false)
    {
        IEnumerable<BusinessType> q = _byId.Values;
        if (status is not null)
        {
            q = q.Where(e => e.Status == status);
        }

        var list = q.OrderBy(e => e.SortOrder).ThenBy(e => e.Name).ToList();
        return Task.FromResult<(IReadOnlyList<BusinessType>, int)>((list.Skip(skip).Take(take).ToList(), list.Count));
    }

    public Task<IReadOnlyList<BusinessType>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BusinessType>>(
            ids.Where(_byId.ContainsKey).Select(id => _byId[id]).ToList());

    public Task<bool> IsReferencedAsync(BusinessTypeId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task AddAsync(BusinessType businessType, CancellationToken cancellationToken = default)
    {
        _byId[businessType.Id.Value] = businessType;
        _byCode[businessType.Code] = businessType;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BusinessType businessType, CancellationToken cancellationToken = default)
    {
        _byId[businessType.Id.Value] = businessType;
        _byCode[businessType.Code] = businessType;
        return Task.CompletedTask;
    }
}
