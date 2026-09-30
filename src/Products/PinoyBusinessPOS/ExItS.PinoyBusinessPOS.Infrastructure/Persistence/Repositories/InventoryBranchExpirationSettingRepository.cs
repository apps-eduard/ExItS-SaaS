using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryBranchExpirationSettingRepository : IInventoryBranchExpirationSettingRepository
{
    private readonly PosDbContext _db;

    public InventoryBranchExpirationSettingRepository(PosDbContext db)
    {
        _db = db;
    }

    public async Task<InventoryBranchExpirationSetting?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryBranchExpirationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value
                    && r.BranchId == branchId.Value
                    && r.ProductId == productId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : InventoryTransferEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<InventoryBranchExpirationSetting>> ListByBranchAndProductIdsAsync(
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
        var records = await _db.InventoryBranchExpirationSettings
            .AsNoTracking()
            .Where(r =>
                r.OrganizationId == organizationId.Value
                && r.BranchId == branchId.Value
                && ids.Contains(r.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, InventoryBranchExpirationSetting>> ListEnabledByBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.InventoryBranchExpirationSettings
            .AsNoTracking()
            .Where(r =>
                r.OrganizationId == organizationId.Value
                && r.BranchId == branchId.Value
                && r.TracksExpiration)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records
            .Select(InventoryTransferEntityMapper.ToDomain)
            .ToDictionary(s => s.ProductId.Value);
    }

    public async Task UpsertAsync(
        InventoryBranchExpirationSetting setting,
        CancellationToken cancellationToken = default)
    {
        var record = _db.InventoryBranchExpirationSettings.Local.FirstOrDefault(r =>
                r.OrganizationId == setting.OrganizationId.Value
                && r.BranchId == setting.BranchId.Value
                && r.ProductId == setting.ProductId.Value)
            ?? await _db.InventoryBranchExpirationSettings
                .FirstOrDefaultAsync(
                    r => r.OrganizationId == setting.OrganizationId.Value
                        && r.BranchId == setting.BranchId.Value
                        && r.ProductId == setting.ProductId.Value,
                    cancellationToken)
                .ConfigureAwait(false);

        if (record is null)
        {
            _db.InventoryBranchExpirationSettings.Add(InventoryTransferEntityMapper.ToRecord(setting));
            return;
        }

        record.TracksExpiration = setting.TracksExpiration;
        record.ExpirationWarningDays = setting.ExpirationWarningDays;
        record.EnabledAtUtc = setting.EnabledAtUtc;
        record.EnabledBy = setting.EnabledBy;
        record.UpdatedAtUtc = setting.UpdatedAtUtc;
        record.UpdatedBy = setting.UpdatedBy;
    }
}
