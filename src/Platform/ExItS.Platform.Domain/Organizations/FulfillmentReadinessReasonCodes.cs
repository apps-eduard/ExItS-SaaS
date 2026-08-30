namespace ExItS.Platform.Domain.Organizations;

/// <summary>Stable machine reason codes for branch fulfillment readiness. Never embed UI copy.</summary>
public static class FulfillmentReadinessReasonCodes
{
    public const string BranchInactive = "branch_inactive";
    public const string BranchAddressIncomplete = "branch_address_incomplete";
    public const string TimezoneMissing = "timezone_missing";
    public const string StoreHoursMissing = "store_hours_missing";
    public const string StoreHoursInvalid = "store_hours_invalid";
    public const string StoreContactMissing = "store_contact_missing";
    public const string OrderingEntitlementMissing = "ordering_entitlement_missing";
    public const string DeliveryEntitlementMissing = "delivery_entitlement_missing";
    public const string CustomerOrderingDisabled = "customer_ordering_disabled";
    public const string PickupDisabled = "pickup_disabled";
    public const string DeliveryDisabled = "delivery_disabled";
    public const string MapLocationMissing = "map_location_missing";
    public const string DeliveryPolicyMissing = "delivery_policy_missing";
    public const string DeliveryPolicyIncomplete = "delivery_policy_incomplete";
    public const string DeliveryAreaMissing = "delivery_area_missing";
    public const string OnlineOrdersPaused = "online_orders_paused";
    public const string StoreClosed = "store_closed";
}
