using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryBranchReorderRepository : IInventoryBranchReorderRepository
{
    private readonly PosDbContext _db;

    public InventoryBranchReorderRepository(PosDbContext db) => _db = db;

    public async Task<InventoryBranchReorderSetting?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryBranchReorderSettings
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value
                    && r.BranchId == branchId.Value
                    && r.ProductId == productId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : InventoryTransferEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<InventoryBranchReorderSetting>> ListByBranchAndProductIdsAsync(
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
        var records = await _db.InventoryBranchReorderSettings
            .Where(r => r.OrganizationId == organizationId.Value
                && r.BranchId == branchId.Value
                && ids.Contains(r.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryTransferEntityMapper.ToDomain).ToList();
    }

    public async Task UpsertAsync(InventoryBranchReorderSetting setting, CancellationToken cancellationToken = default)
    {
        var record = _db.InventoryBranchReorderSettings.Local.FirstOrDefault(r =>
                r.OrganizationId == setting.OrganizationId.Value
                && r.BranchId == setting.BranchId.Value
                && r.ProductId == setting.ProductId.Value)
            ?? await _db.InventoryBranchReorderSettings
                .FirstOrDefaultAsync(
                    r => r.OrganizationId == setting.OrganizationId.Value
                        && r.BranchId == setting.BranchId.Value
                        && r.ProductId == setting.ProductId.Value,
                    cancellationToken)
                .ConfigureAwait(false);
        if (record is null)
        {
            _db.InventoryBranchReorderSettings.Add(InventoryTransferEntityMapper.ToRecord(setting));
            return;
        }

        record.ReorderLevel = setting.ReorderLevel;
        record.ReorderQuantity = setting.ReorderQuantity;
        record.UpdatedAtUtc = setting.UpdatedAtUtc;
        record.UpdatedBy = setting.UpdatedBy;
    }
}
