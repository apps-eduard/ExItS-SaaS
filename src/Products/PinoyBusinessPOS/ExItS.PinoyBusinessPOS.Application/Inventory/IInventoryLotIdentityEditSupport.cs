using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Efficient eligibility queries for lot identity correction (batch-friendly; no N+1).
/// </summary>
public interface IInventoryLotIdentityEditSupport
{
    /// <summary>
    /// Distinct lot-ledger movement types present for each lot (org-scoped).
    /// Lots with no movements are omitted or mapped to empty lists.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<StockMovementType>>> ListDistinctMovementTypesByLotIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> lotIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lot ids referenced by an active (non-cancelled) transfer <see cref="InventoryTransferStatus.Draft"/>
    /// via <c>InventoryTransferLine.SourceLotId</c>.
    /// </summary>
    Task<IReadOnlySet<Guid>> ListLotIdsReferencedByActiveTransferDraftAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> lotIds,
        CancellationToken cancellationToken = default);
}
