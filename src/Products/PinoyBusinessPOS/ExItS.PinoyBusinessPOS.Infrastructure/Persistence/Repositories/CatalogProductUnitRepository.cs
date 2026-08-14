using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CatalogProductUnitRepository : ICatalogProductUnitRepository
{
    private readonly PosDbContext _db;

    public CatalogProductUnitRepository(PosDbContext db) => _db = db;

    public async Task<CatalogProductUnit?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductUnitId unitId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProductUnits.AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == unitId.Value && u.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogProductUnitEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.CatalogProductUnits.AsNoTracking()
            .Where(u => u.OrganizationId == organizationId.Value && u.ProductId == productId.Value)
            .OrderBy(u => u.Kind)
            .ThenBy(u => u.SortOrder)
            .ThenBy(u => u.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(CatalogProductUnitEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<CatalogProductUnit>>();
        }

        var ids = productIds.Select(p => p.Value).Distinct().ToList();
        var records = await _db.CatalogProductUnits.AsNoTracking()
            .Where(u => u.OrganizationId == organizationId.Value && ids.Contains(u.ProductId))
            .OrderBy(u => u.ProductId)
            .ThenBy(u => u.Kind)
            .ThenBy(u => u.SortOrder)
            .ThenBy(u => u.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(r => r.ProductId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CatalogProductUnit>)g.Select(CatalogProductUnitEntityMapper.ToDomain).ToList());
    }

    public async Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default)
    {
        await _db.CatalogProductUnits.AddAsync(CatalogProductUnitEntityMapper.ToRecord(unit), cancellationToken)
            .ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProductUnits
            .FirstOrDefaultAsync(
                u => u.Id == unit.Id.Value && u.OrganizationId == unit.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new InvalidOperationException($"Product unit {unit.Id.Value} was not found.");
        }

        CatalogProductUnitEntityMapper.ApplyToRecord(unit, record);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ReplaceActiveUnitsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        ProductUnitKind kind,
        IReadOnlyList<CatalogProductUnit> units,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.CatalogProductUnits
            .Where(u =>
                u.OrganizationId == organizationId.Value
                && u.ProductId == productId.Value
                && u.Kind == (int)kind
                && u.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var row in existing)
        {
            row.IsActive = false;
            row.UpdatedAtUtc = utcNow;
        }

        foreach (var unit in units)
        {
            await _db.CatalogProductUnits.AddAsync(CatalogProductUnitEntityMapper.ToRecord(unit), cancellationToken)
                .ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
