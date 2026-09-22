namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Commercial settlement state of a purchase order, independent of <see cref="PurchaseOrderStatus"/>.
/// Goods can be <see cref="PurchaseOrderStatus.Received"/> while payment is still outstanding
/// (pay-on-delivery / pay-on-receipt), which must not be presented as commercially completed.
/// </summary>
public enum ConnectedPoFinancialSettlementStatus
{
    /// <summary>No separate post-receipt settlement gate applies (pay-before, supplier credit, legacy rows).</summary>
    NotRequired = 0,

    /// <summary>Goods received but the seller has not confirmed settlement of the amount due.</summary>
    AwaitingPayment = 1,

    /// <summary>Settlement confirmed (or nothing was due).</summary>
    Settled = 2
}
