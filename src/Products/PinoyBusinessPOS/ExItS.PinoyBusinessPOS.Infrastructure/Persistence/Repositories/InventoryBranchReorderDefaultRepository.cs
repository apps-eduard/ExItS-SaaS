using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryBranchReorderDefaultRepository : IInventoryBranchReorderDefaultRepository
{
    private readonly PosDbContext _db;

    public InventoryBranchReorderDefaultRepository(PosDbContext db) => _db = db;

    public async Task<InventoryBranchReorderDefault?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await _db.InventoryBranchReorderDefaults
                .FirstOrDefaultAsync(
                    r => r.OrganizationId == organizationId.Value && r.BranchId == branchId.Value,
                    cancellationToken)
                .ConfigureAwait(false);
            return record is null ? null : InventoryTransferEntityMapper.ToDomain(record);
        }
        catch (Exception ex) when (IsMissingRelation(ex))
        {
            // Migration not applied yet — treat as no branch default.
            return null;
        }
    }

    public async Task UpsertAsync(InventoryBranchReorderDefault setting, CancellationToken cancellationToken = default)
    {
        var record = _db.InventoryBranchReorderDefaults.Local.FirstOrDefault(r =>
                r.OrganizationId == setting.OrganizationId.Value
                && r.BranchId == setting.BranchId.Value)
            ?? await _db.InventoryBranchReorderDefaults
                .FirstOrDefaultAsync(
                    r => r.OrganizationId == setting.OrganizationId.Value
                        && r.BranchId == setting.BranchId.Value,
                    cancellationToken)
                .ConfigureAwait(false);
        if (record is null)
        {
            _db.InventoryBranchReorderDefaults.Add(InventoryTransferEntityMapper.ToRecord(setting));
            return;
        }

        record.ReorderLevel = setting.ReorderLevel;
        record.ReorderQuantity = setting.ReorderQuantity;
        record.UpdatedAtUtc = setting.UpdatedAtUtc;
        record.UpdatedBy = setting.UpdatedBy;
    }

    private static bool IsMissingRelation(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException!)
        {
            var typeName = current.GetType().FullName ?? current.GetType().Name;
            if (typeName.Contains("PostgresException", StringComparison.Ordinal)
                && current.Message.Contains("inventory_branch_reorder_defaults", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (current.Message.Contains("42P01", StringComparison.Ordinal)
                && current.Message.Contains("inventory_branch_reorder_defaults", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
