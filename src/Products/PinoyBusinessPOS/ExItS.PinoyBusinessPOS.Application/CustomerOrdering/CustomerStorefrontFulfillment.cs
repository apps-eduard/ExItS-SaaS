namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Customer-facing fulfillment state for a storefront. Values are stable keys, not error codes.
/// </summary>
public static class CustomerStorefrontFulfillment
{
    public const string Ready = "ready";
    public const string Paused = "paused";
    public const string NoMethod = "no-method";
    public const string PickupUnavailable = "pickup-unavailable";
    public const string DeliveryUnavailable = "delivery-unavailable";
    public const string StoreClosed = "store-closed";

    public const string PausedMessage = "Online ordering is temporarily paused.";
    public const string NoMethodMessage = "This store has not enabled pickup or delivery yet.";

    public static string Classify(
        IReadOnlyList<CustomerOrderBranchSnapshot> branches,
        bool canCustomerDelivery)
    {
        if (HasPlaceableFulfillment(branches, canCustomerDelivery))
        {
            return Ready;
        }

        var enabled = branches.Where(b => b.CustomerOrderingEnabled).ToList();
        if (enabled.Count > 0 && enabled.All(b => b.OnlineOrdersPaused))
        {
            return Paused;
        }

        var open = enabled.Where(b => !b.OnlineOrdersPaused).ToList();
        if (open.Count == 0)
        {
            return Paused;
        }

        var pickupConfigured = open.Any(b => b.PickupEnabled);
        var deliveryConfigured = canCustomerDelivery && open.Any(b => b.DeliveryEnabled);
        if (!pickupConfigured && !deliveryConfigured)
        {
            return NoMethod;
        }

        if (open.All(b => !b.CustomerOrderingOperational))
        {
            return StoreClosed;
        }

        if (pickupConfigured && !deliveryConfigured)
        {
            return PickupUnavailable;
        }

        if (deliveryConfigured && !pickupConfigured)
        {
            return DeliveryUnavailable;
        }

        return StoreClosed;
    }

    public static bool HasPlaceableFulfillment(
        IReadOnlyList<CustomerOrderBranchSnapshot> branches,
        bool canCustomerDelivery) =>
        branches.Any(b =>
            b.CustomerOrderingEnabled
            && !b.OnlineOrdersPaused
            && b.CustomerOrderingOperational
            && (b.PickupEnabled && b.PickupOperational
                || (canCustomerDelivery && b.DeliveryEnabled && b.DeliveryOperational)));

    public static bool AllEnabledBranchesPaused(IReadOnlyList<CustomerOrderBranchSnapshot> branches)
    {
        var enabled = branches.Where(b => b.CustomerOrderingEnabled).ToList();
        return enabled.Count > 0 && enabled.All(b => b.OnlineOrdersPaused);
    }
}
