using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IInventoryBranchReorderRepository
{
    Task<InventoryBranchReorderSetting?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryBranchReorderSetting>> ListByBranchAndProductIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(InventoryBranchReorderSetting setting, CancellationToken cancellationToken = default);
}
