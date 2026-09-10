using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IInventoryBranchReorderDefaultRepository
{
    Task<InventoryBranchReorderDefault?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(InventoryBranchReorderDefault setting, CancellationToken cancellationToken = default);
}
