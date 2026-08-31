using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class BranchProductAvailabilityRepository : IBranchProductAvailabilityRepository
{
    private readonly PosDbContext _db;

    public BranchProductAvailabilityRepository(PosDbContext db) => _db = db;

    public async Task<BranchProductAvailability?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BranchProductAvailabilities.AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.OrganizationId == organizationId.Value
                    && a.BranchId == branchId.Value
                    && a.ProductId == productId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<BranchProductAvailability>> ListByBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.BranchProductAvailabilities.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value && a.BranchId == branchId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<BranchProductAvailability>> ListByProductIdsAsync(
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
        var records = await _db.BranchProductAvailabilities.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value
                && a.BranchId == branchId.Value
                && ids.Contains(a.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default)
    {
        _db.BranchProductAvailabilities.Add(CatalogEntityMapper.ToRecord(availability));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default)
    {
        var record = _db.BranchProductAvailabilities.Local.FirstOrDefault(a =>
                a.OrganizationId == availability.OrganizationId.Value
                && a.BranchId == availability.BranchId.Value
                && a.ProductId == availability.ProductId.Value)
            ?? await _db.BranchProductAvailabilities
                .FirstOrDefaultAsync(
                    a => a.OrganizationId == availability.OrganizationId.Value
                        && a.BranchId == availability.BranchId.Value
                        && a.ProductId == availability.ProductId.Value,
                    cancellationToken)
                .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CatalogConcurrencyConflict,
                "Branch product availability row was not found for update.");
        }

        CatalogEntityMapper.ApplyToRecord(availability, record);
    }
}
