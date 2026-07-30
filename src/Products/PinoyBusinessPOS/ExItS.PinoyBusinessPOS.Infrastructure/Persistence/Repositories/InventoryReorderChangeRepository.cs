using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryReorderChangeRepository : IInventoryReorderChangeRepository
{
    private readonly PosDbContext _db;

    public InventoryReorderChangeRepository(PosDbContext db) => _db = db;

    public Task AddAsync(InventoryReorderChange change, CancellationToken cancellationToken = default)
    {
        _db.InventoryReorderChanges.Add(InventoryReorderChangeEntityMapper.ToRecord(change));
        return Task.CompletedTask;
    }
}
