using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class BranchProductPriceOverrideRepository : IBranchProductPriceOverrideRepository
{
    private readonly PosDbContext _db;

    public BranchProductPriceOverrideRepository(PosDbContext db) => _db = db;

    public async Task<BranchProductPriceOverride?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        Guid productUnitId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BranchProductPriceOverrides.AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.OrganizationId == organizationId.Value
                    && o.BranchId == branchId.Value
                    && o.ProductId == productId.Value
                    && o.ProductUnitId == productUnitId,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<BranchProductPriceOverride>> ListByBranchAndProductIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var ids = productIds.Select(p => p.Value).ToList();
        var records = await _db.BranchProductPriceOverrides.AsNoTracking()
            .Where(o => o.OrganizationId == organizationId.Value
                && o.BranchId == branchId.Value
                && ids.Contains(o.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<BranchProductPriceOverride>> ListByProductAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.BranchProductPriceOverrides.AsNoTracking()
            .Where(o => o.OrganizationId == organizationId.Value && o.ProductId == productId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(BranchProductPriceOverride priceOverride, CancellationToken cancellationToken = default)
    {
        _db.BranchProductPriceOverrides.Add(CatalogEntityMapper.ToRecord(priceOverride));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        BranchProductPriceOverride priceOverride,
        CancellationToken cancellationToken = default)
    {
        var record = _db.BranchProductPriceOverrides.Local.FirstOrDefault(o =>
                o.OrganizationId == priceOverride.OrganizationId.Value
                && o.BranchId == priceOverride.BranchId.Value
                && o.ProductId == priceOverride.ProductId.Value
                && o.ProductUnitId == priceOverride.ProductUnitId)
            ?? await _db.BranchProductPriceOverrides
                .FirstOrDefaultAsync(
                    o => o.OrganizationId == priceOverride.OrganizationId.Value
                        && o.BranchId == priceOverride.BranchId.Value
                        && o.ProductId == priceOverride.ProductId.Value
                        && o.ProductUnitId == priceOverride.ProductUnitId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CatalogConcurrencyConflict,
                "Branch product price override row was not found for update.");
        }

        CatalogEntityMapper.ApplyToRecord(priceOverride, record);
    }

    public async Task DeleteAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        Guid productUnitId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BranchProductPriceOverrides
            .FirstOrDefaultAsync(
                o => o.OrganizationId == organizationId.Value
                    && o.BranchId == branchId.Value
                    && o.ProductId == productId.Value
                    && o.ProductUnitId == productUnitId,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is not null)
        {
            _db.BranchProductPriceOverrides.Remove(record);
        }
    }
}
