using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IInventoryLotIdentityCorrectionRepository
{
    Task AddAsync(InventoryLotIdentityCorrection correction, CancellationToken cancellationToken = default);
}
