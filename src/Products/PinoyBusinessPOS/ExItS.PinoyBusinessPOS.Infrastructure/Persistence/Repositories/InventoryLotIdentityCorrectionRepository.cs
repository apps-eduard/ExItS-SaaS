using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryLotIdentityCorrectionRepository : IInventoryLotIdentityCorrectionRepository
{
    private readonly PosDbContext _db;

    public InventoryLotIdentityCorrectionRepository(PosDbContext db) => _db = db;

    public Task AddAsync(InventoryLotIdentityCorrection correction, CancellationToken cancellationToken = default)
    {
        _db.InventoryLotIdentityCorrections.Add(InventoryLotIdentityCorrectionEntityMapper.ToRecord(correction));
        return Task.CompletedTask;
    }
}
