using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Primary effective lots = branch-scoped lots plus remaining BranchId=null legacy lots.
/// Secondary branches never include org-level null lots. Dedup is by lot identity, not count.
/// </summary>
public static class InventoryLotCompatibility
{
    public static IReadOnlyList<InventoryLot> UnionByLotId(
        IReadOnlyList<InventoryLot> branchScoped,
        IReadOnlyList<InventoryLot> legacyNull)
    {
        ArgumentNullException.ThrowIfNull(branchScoped);
        ArgumentNullException.ThrowIfNull(legacyNull);

        var seen = new HashSet<Guid>();
        var result = new List<InventoryLot>(branchScoped.Count + legacyNull.Count);
        foreach (var lot in branchScoped)
        {
            if (seen.Add(lot.Id.Value))
            {
                result.Add(lot);
            }
        }

        foreach (var lot in legacyNull)
        {
            if (lot.BranchId is not null)
            {
                continue;
            }

            if (seen.Add(lot.Id.Value))
            {
                result.Add(lot);
            }
        }

        return result;
    }

    public static bool IncludeLegacyNullLots(Guid? primaryBranchId, PosBranchId? targetBranchId) =>
        primaryBranchId is Guid primary
        && targetBranchId is not null
        && primary == targetBranchId.Value;
}
