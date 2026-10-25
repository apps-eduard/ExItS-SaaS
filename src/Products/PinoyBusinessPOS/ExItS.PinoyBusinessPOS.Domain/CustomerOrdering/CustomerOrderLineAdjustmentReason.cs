namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>Seller reason for reducing or cancelling a line under partial-fulfillment policy.</summary>
public enum CustomerOrderLineAdjustmentReason
{
    OutOfStock = 0,
    InsufficientStock = 1,
    DamagedProduct = 2,
    QualityIssue = 3,
    Other = 4
}
