using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

public sealed class ProductionDefinitionRepository : IProductionDefinitionRepository
{
    private readonly PosDbContext _db;

    public ProductionDefinitionRepository(PosDbContext db) => _db = db;

    public async Task<ProductionDefinition?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductionDefinitionId definitionId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductionDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == definitionId.Value && r.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var components = await _db.ProductionComponents.AsNoTracking()
            .Where(c => c.ProductionDefinitionId == record.Id && c.OrganizationId == organizationId.Value)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ProductionEntityMapper.ToDomain(record, components);
    }

    public async Task<(IReadOnlyList<ProductionDefinition> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ProductionDefinitionFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductionDefinitions.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value);

        if (filter.IsActive is true)
        {
            query = query.Where(r => r.Status == nameof(ProductionDefinitionStatus.Active));
        }
        else if (filter.IsActive is false)
        {
            query = query.Where(r => r.Status == nameof(ProductionDefinitionStatus.Inactive));
        }

        if (filter.OutputProductId is Guid outputId)
        {
            query = query.Where(r => r.OutputProductId == outputId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(r => r.Name.ToLower().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var ids = records.Select(r => r.Id).ToList();
        var componentsByDefinition = await LoadComponentsAsync(ids, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var items = records
            .Select(r => ProductionEntityMapper.ToDomain(
                r,
                componentsByDefinition.TryGetValue(r.Id, out var comps) ? comps : []))
            .ToList();
        return (items, total);
    }

    public async Task<IReadOnlyList<ProductionDefinition>> ListAllForCycleValidationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.ProductionDefinitions.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (records.Count == 0)
        {
            return [];
        }

        var ids = records.Select(r => r.Id).ToList();
        var componentsByDefinition = await LoadComponentsAsync(ids, organizationId, cancellationToken)
            .ConfigureAwait(false);
        return records
            .Select(r => ProductionEntityMapper.ToDomain(
                r,
                componentsByDefinition.TryGetValue(r.Id, out var comps) ? comps : []))
            .ToList();
    }

    public async Task AddAsync(ProductionDefinition definition, CancellationToken cancellationToken = default)
    {
        _db.ProductionDefinitions.Add(ProductionEntityMapper.ToRecord(definition));
        foreach (var component in definition.Components)
        {
            _db.ProductionComponents.Add(ProductionEntityMapper.ToRecord(component));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProductionDefinition definition, CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductionDefinitions
            .FirstOrDefaultAsync(
                r => r.Id == definition.Id.Value && r.OrganizationId == definition.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Production definition was not found for update.");

        ProductionEntityMapper.Apply(definition, record);

        var existing = await _db.ProductionComponents
            .Where(c => c.ProductionDefinitionId == definition.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.ProductionComponents.RemoveRange(existing);
        foreach (var component in definition.Components)
        {
            _db.ProductionComponents.Add(ProductionEntityMapper.ToRecord(component));
        }
    }

    private async Task<Dictionary<Guid, List<ProductionComponentRecord>>> LoadComponentsAsync(
        IReadOnlyCollection<Guid> definitionIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.ProductionComponents.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && definitionIds.Contains(c.ProductionDefinitionId))
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(c => c.ProductionDefinitionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
