namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// Rules for enabling branch Delivery relative to organization Offer Delivery.
/// Preserves existing DeliveryEnabled=ON when org Offer Delivery is OFF;
/// blocks OFF→ON while the org master is OFF.
/// </summary>
public static class BranchDeliveryEnablement
{
    public const string OrganizationDeliveryNotOfferedMessage =
        "Turn on Offer Delivery before enabling Delivery for this branch.";

    /// <summary>
    /// True when the request would turn Delivery on while organization Offer Delivery is off.
    /// Turning Delivery off (true→false) is always allowed.
    /// </summary>
    public static bool IsEnableBlockedByOrgOffer(
        bool orgOfferDelivery,
        bool currentlyDeliveryEnabled,
        bool requestedDeliveryEnabled) =>
        requestedDeliveryEnabled
        && !currentlyDeliveryEnabled
        && !orgOfferDelivery;
}
