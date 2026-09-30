using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryLotIdentityEditSupport : IInventoryLotIdentityEditSupport
{
    private readonly PosDbContext _db;

    public InventoryLotIdentityEditSupport(PosDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<StockMovementType>>> ListDistinctMovementTypesByLotIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> lotIds,
        CancellationToken cancellationToken = default)
    {
        if (lotIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<StockMovementType>>();
        }

        var org = organizationId.Value;
        var ids = lotIds as Guid[] ?? lotIds.ToArray();
        var rows = await _db.InventoryLotMovements.AsNoTracking()
            .Where(m => m.OrganizationId == org && ids.Contains(m.LotId))
            .Select(m => new { m.LotId, m.MovementType })
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new Dictionary<Guid, IReadOnlyList<StockMovementType>>();
        foreach (var group in rows.GroupBy(r => r.LotId))
        {
            var types = new List<StockMovementType>();
            foreach (var row in group)
            {
                if (StockMovementTypes.TryParse(row.MovementType, out var type))
                {
                    types.Add(type);
                }
                else
                {
                    // Unknown persisted code — fail closed by treating as locking via ManualDecrease proxy.
                    types.Add(StockMovementType.ManualDecrease);
                }
            }

            result[group.Key] = types;
        }

        return result;
    }

    public async Task<IReadOnlySet<Guid>> ListLotIdsReferencedByActiveTransferDraftAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> lotIds,
        CancellationToken cancellationToken = default)
    {
        if (lotIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var org = organizationId.Value;
        var ids = lotIds as Guid[] ?? lotIds.ToArray();
        var draft = InventoryTransferStatuses.ToCode(InventoryTransferStatus.Draft);

        var referenced = await (
                from line in _db.InventoryTransferLines.AsNoTracking()
                join transfer in _db.InventoryTransfers.AsNoTracking()
                    on line.TransferId equals transfer.Id
                where transfer.OrganizationId == org
                    && transfer.Status == draft
                    && line.SourceLotId != null
                    && ids.Contains(line.SourceLotId.Value)
                select line.SourceLotId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return referenced.ToHashSet();
    }
}
