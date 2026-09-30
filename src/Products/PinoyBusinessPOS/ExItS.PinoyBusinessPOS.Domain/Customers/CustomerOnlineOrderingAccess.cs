namespace ExItS.PinoyBusinessPOS.Domain.Customers;

/// <summary>
/// Organization-owned Personal online-ordering commerce override for a POSCustomer.
/// Platform link is identity/consent only — this policy is commercial authority.
/// </summary>
public enum CustomerOnlineOrderingAccess
{
    /// <summary>Follow store/master acceptance when the Personal link and branch gates pass.</summary>
    Default = 0,

    /// <summary>Explicitly allow when master/branch gates pass.</summary>
    Allowed = 1,

    /// <summary>Deny storefront/catalog/quote/place even when master is on.</summary>
    Blocked = 2,
}

/// <summary>
/// Pure effective shopping authorization over master + per-customer override.
/// Master OFF always denies (even Allowed). Blocked always denies when master is on.
/// </summary>
public static class CustomerOnlineOrderingAccessRules
{
    public const string StoreNotAcceptingMessage = "This store is not accepting online orders.";
    public const string CustomerBlockedMessage = "Online ordering is blocked for this customer.";

    public static bool IsShoppingAllowed(bool masterAcceptingOrders, CustomerOnlineOrderingAccess access)
    {
        if (!masterAcceptingOrders)
        {
            return false;
        }

        return access != CustomerOnlineOrderingAccess.Blocked;
    }

    public static string? DenialMessage(bool masterAcceptingOrders, CustomerOnlineOrderingAccess access)
    {
        if (!masterAcceptingOrders)
        {
            return StoreNotAcceptingMessage;
        }

        if (access == CustomerOnlineOrderingAccess.Blocked)
        {
            return CustomerBlockedMessage;
        }

        return null;
    }
}
