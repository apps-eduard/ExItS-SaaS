using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// Organization-level fulfillment capability flags and defaults for NEW branches (POS product DB).
/// Branch delivery configuration remains separate and is preserved when Offer Delivery is OFF.
/// Defaults never silently rewrite existing branches.
/// </summary>
public sealed class OrganizationFulfillmentSettings
{
    private OrganizationFulfillmentSettings(
        Guid settingId,
        PosOrganizationId organizationId,
        bool offerDelivery,
        bool defaultPickupEnabled,
        bool defaultDeliveryEnabled,
        bool defaultOnlineOrdersEnabled,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        SettingId = settingId;
        OrganizationId = organizationId;
        OfferDelivery = offerDelivery;
        DefaultPickupEnabled = defaultPickupEnabled;
        DefaultDeliveryEnabled = defaultDeliveryEnabled;
        DefaultOnlineOrdersEnabled = defaultOnlineOrdersEnabled;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid SettingId { get; }
    public PosOrganizationId OrganizationId { get; }
    /// <summary>Canonical org Offer Delivery switch. Default OFF when no row exists.</summary>
    public bool OfferDelivery { get; private set; }
    /// <summary>Applied only when creating a new branch. Does not mutate existing branches.</summary>
    public bool DefaultPickupEnabled { get; private set; }
    /// <summary>Applied only when creating a new branch. Does not mutate existing branches.</summary>
    public bool DefaultDeliveryEnabled { get; private set; }
    /// <summary>
    /// Applied only when creating a new branch as Platform CustomerOrderingEnabled.
    /// Does not mutate existing branches. Not an org-wide Online Orders kill-switch.
    /// </summary>
    public bool DefaultOnlineOrdersEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Safe defaults preserve historical create behavior (all branch channels OFF).
    /// </summary>
    public static OrganizationFulfillmentSettings CreateDefault(
        PosOrganizationId organizationId,
        DateTimeOffset nowUtc) =>
        new(
            Guid.NewGuid(),
            organizationId,
            offerDelivery: false,
            defaultPickupEnabled: false,
            defaultDeliveryEnabled: false,
            defaultOnlineOrdersEnabled: false,
            nowUtc,
            nowUtc);

    public static OrganizationFulfillmentSettings Rehydrate(
        Guid settingId,
        PosOrganizationId organizationId,
        bool offerDelivery,
        bool defaultPickupEnabled,
        bool defaultDeliveryEnabled,
        bool defaultOnlineOrdersEnabled,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            settingId,
            organizationId,
            offerDelivery,
            defaultPickupEnabled,
            defaultDeliveryEnabled,
            defaultOnlineOrdersEnabled,
            createdAtUtc,
            updatedAtUtc);

    public void SetOfferDelivery(bool offerDelivery, DateTimeOffset nowUtc)
    {
        OfferDelivery = offerDelivery;
        UpdatedAtUtc = nowUtc;
    }

    public void SetBranchFulfillmentDefaults(
        bool defaultPickupEnabled,
        bool defaultDeliveryEnabled,
        bool defaultOnlineOrdersEnabled,
        DateTimeOffset nowUtc)
    {
        DefaultPickupEnabled = defaultPickupEnabled;
        DefaultDeliveryEnabled = defaultDeliveryEnabled;
        DefaultOnlineOrdersEnabled = defaultOnlineOrdersEnabled;
        UpdatedAtUtc = nowUtc;
    }
}
