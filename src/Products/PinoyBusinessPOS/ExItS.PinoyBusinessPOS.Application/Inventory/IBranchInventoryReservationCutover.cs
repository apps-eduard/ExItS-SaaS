namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Reconstructs <c>InventoryBranchBalance.ReservedQuantity</c> from active Reserved
/// Sales and CustomerOrders. Idempotent; never mutates OnHand or creates movements.
/// </summary>
public interface IBranchInventoryReservationCutover
{
    /// <summary>Audits active reservations without writing. Optional org scope for re-runs.</summary>
    Task<BranchInventoryReservationCutoverResult> AuditAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes branch reserved quantities from active reservation documents.
    /// Fails closed on unresolved branch, missing balance, over-reserve, or org mismatch.
    /// </summary>
    Task<BranchInventoryReservationCutoverResult> ReconcileAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
}

public sealed record BranchInventoryReservationCutoverResult(
    int ActiveDocumentCount,
    int BranchProductGroups,
    int BalancesUpdated,
    IReadOnlyList<BranchInventoryReservationAggregate> Aggregates);

public sealed record BranchInventoryReservationAggregate(
    Guid OrganizationId,
    Guid BranchId,
    Guid ProductId,
    decimal ReservedQuantity);
