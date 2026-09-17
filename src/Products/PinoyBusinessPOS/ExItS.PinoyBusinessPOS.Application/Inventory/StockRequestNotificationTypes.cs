namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>RelatedType values for warehouse stock-request lifecycle organization inbox events.</summary>
public static class StockRequestNotificationTypes
{
    public const string Submitted = "StockRequestSubmitted";
    public const string Approved = "StockRequestApproved";
    public const string Declined = "StockRequestDeclined";
    public const string Dispatched = "StockRequestDispatched";
    public const string Received = "StockRequestReceived";
    public const string PartiallyReceived = "StockRequestPartiallyReceived";

    public static bool IsKnown(string? relatedType) =>
        string.Equals(relatedType, Submitted, StringComparison.Ordinal)
        || string.Equals(relatedType, Approved, StringComparison.Ordinal)
        || string.Equals(relatedType, Declined, StringComparison.Ordinal)
        || string.Equals(relatedType, Dispatched, StringComparison.Ordinal)
        || string.Equals(relatedType, Received, StringComparison.Ordinal)
        || string.Equals(relatedType, PartiallyReceived, StringComparison.Ordinal);
}

/// <summary>RelatedType values for standalone inventory-transfer alerts (non-stock-request).</summary>
public static class InventoryTransferNotificationTypes
{
    public const string Dispatched = "InventoryTransferDispatched";
    public const string Received = "InventoryTransferReceived";
    public const string PartiallyReceived = "InventoryTransferPartiallyReceived";

    public static bool IsKnown(string? relatedType) =>
        string.Equals(relatedType, Dispatched, StringComparison.Ordinal)
        || string.Equals(relatedType, Received, StringComparison.Ordinal)
        || string.Equals(relatedType, PartiallyReceived, StringComparison.Ordinal);
}
