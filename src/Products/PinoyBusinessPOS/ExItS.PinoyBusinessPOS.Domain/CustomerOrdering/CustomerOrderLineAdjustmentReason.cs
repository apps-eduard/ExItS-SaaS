namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>Seller reason for reducing or cancelling a line under partial-fulfillment policy.</summary>
public enum CustomerOrderLineAdjustmentReason
{
    OutOfStock = 0,
    InsufficientStock = 1,
    DamagedProduct = 2,
    QualityIssue = 3,
    Other = 4,
    /// <summary>Product not available to sell (alias path for Product Unavailable).</summary>
    ProductUnavailable = 5,
    CustomerRequested = 6
}
