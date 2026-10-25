namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>RelatedType values for customer-order lifecycle organization inbox events.</summary>
public static class CustomerOrderNotificationTypes
{
    public const string Submitted = "CustomerOrderSubmitted";
    public const string Accepted = "CustomerOrderAccepted";
    public const string Rejected = "CustomerOrderRejected";
    public const string Cancelled = "CustomerOrderCancelled";
    public const string Ready = "CustomerOrderReady";
    public const string OutForDelivery = "CustomerOrderOutForDelivery";
    public const string Delivered = "CustomerOrderDelivered";
    public const string Collected = "CustomerOrderCollected";
    public const string Completed = "CustomerOrderCompleted";
    public const string ScheduleProposed = "CustomerOrderScheduleProposed";
    public const string ScheduleAccepted = "CustomerOrderScheduleAccepted";
    public const string ScheduleDeclined = "CustomerOrderScheduleDeclined";
    public const string LinesAdjusted = "CustomerOrderLinesAdjusted";

    public static bool IsKnown(string? relatedType) =>
        string.Equals(relatedType, Submitted, StringComparison.Ordinal)
        || string.Equals(relatedType, Accepted, StringComparison.Ordinal)
        || string.Equals(relatedType, Rejected, StringComparison.Ordinal)
        || string.Equals(relatedType, Cancelled, StringComparison.Ordinal)
        || string.Equals(relatedType, Ready, StringComparison.Ordinal)
        || string.Equals(relatedType, OutForDelivery, StringComparison.Ordinal)
        || string.Equals(relatedType, Delivered, StringComparison.Ordinal)
        || string.Equals(relatedType, Collected, StringComparison.Ordinal)
        || string.Equals(relatedType, Completed, StringComparison.Ordinal)
        || string.Equals(relatedType, ScheduleProposed, StringComparison.Ordinal)
        || string.Equals(relatedType, ScheduleAccepted, StringComparison.Ordinal)
        || string.Equals(relatedType, ScheduleDeclined, StringComparison.Ordinal)
        || string.Equals(relatedType, LinesAdjusted, StringComparison.Ordinal);
}
