using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IInventoryTransferExceptionCustodyRepository
{
    Task<InventoryTransferExceptionCustody?> GetByIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferExceptionCustodyId custodyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransferExceptionCustody>> ListByTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransferExceptionCustody>> ListByRootTransferIdAsync(
        PosOrganizationId organizationId,
        InventoryTransferId rootTransferId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransferExceptionCustody>> ListByStockRequestIdAsync(
        PosOrganizationId organizationId,
        StockRequestId stockRequestId,
        CancellationToken cancellationToken = default);

    Task AddAsync(InventoryTransferExceptionCustody custody, CancellationToken cancellationToken = default);

    Task UpdateAsync(InventoryTransferExceptionCustody custody, CancellationToken cancellationToken = default);
}
