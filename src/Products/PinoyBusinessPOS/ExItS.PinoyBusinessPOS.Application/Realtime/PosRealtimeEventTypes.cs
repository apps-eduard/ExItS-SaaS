namespace ExItS.PinoyBusinessPOS.Application.Realtime;

/// <summary>Stable POS realtime event type names (envelope EventType).</summary>
public static class PosRealtimeEventTypes
{
    public const string CustomerOrderSubmitted = "CustomerOrderSubmitted";
    public const string CustomerOrderAccepted = "CustomerOrderAccepted";
    public const string CustomerOrderRejected = "CustomerOrderRejected";
    public const string CustomerOrderCancelled = "CustomerOrderCancelled";
    public const string CustomerOrderPreparing = "CustomerOrderPreparing";
    public const string CustomerOrderReadyForPickup = "CustomerOrderReadyForPickup";
    public const string CustomerOrderOutForDelivery = "CustomerOrderOutForDelivery";
    public const string CustomerOrderDelivered = "CustomerOrderDelivered";
    public const string CustomerOrderCollected = "CustomerOrderCollected";
    public const string CustomerOrderCompleted = "CustomerOrderCompleted";
    public const string CustomerOrderScheduleProposed = "CustomerOrderScheduleProposed";
    public const string CustomerOrderScheduleAccepted = "CustomerOrderScheduleAccepted";
    public const string CustomerOrderScheduleDeclined = "CustomerOrderScheduleDeclined";
    public const string CustomerOrderLinesAdjusted = "CustomerOrderLinesAdjusted";
    public const string PaymentStatusChanged = "PaymentStatusChanged";
    public const string ConnectedCommerceSettingsChanged = "ConnectedCommerceSettingsChanged";
    public const string PaymentMethodSettingsChanged = "PaymentMethodSettingsChanged";
    public const string InventoryStockChanged = "InventoryStockChanged";
    public const string CatalogPricingChanged = "CatalogPricingChanged";
}

public static class PosRealtimeScopeTypes
{
    public const string Organization = "organization";
    public const string Order = "order";
    public const string PersonalBuyer = "personal-buyer";
}

public static class PosRealtimeResourceTypes
{
    public const string CustomerOrder = "customer-order";
    public const string OrganizationSettings = "organization-settings";
    public const string Inventory = "inventory";
    public const string Catalog = "catalog";
}
