using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IInventoryTransferDamageCustodyRepository
{
    Task<InventoryTransferDamageCustody?> GetByIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferDamageCustodyId custodyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransferDamageCustody>> ListByTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransferDamageCustody>> ListByRootTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId rootTransferId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransferDamageCustody>> ListByStockRequestIdAsync(
        PosOrganizationId organizationId,
        StockRequestId stockRequestId,
        CancellationToken cancellationToken = default);

    Task AddAsync(InventoryTransferDamageCustody custody, CancellationToken cancellationToken = default);

    Task UpdateAsync(InventoryTransferDamageCustody custody, CancellationToken cancellationToken = default);
}
