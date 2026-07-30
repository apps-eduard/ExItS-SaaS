using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IInventoryReorderChangeRepository
{
    Task AddAsync(InventoryReorderChange change, CancellationToken cancellationToken = default);
}
