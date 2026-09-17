using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// Organization-level fulfillment capability flags (POS product DB).
/// Branch delivery configuration remains separate and is preserved when Offer Delivery is OFF.
/// </summary>
public sealed class OrganizationFulfillmentSettings
{
    private OrganizationFulfillmentSettings(
        Guid settingId,
        PosOrganizationId organizationId,
        bool offerDelivery,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        SettingId = settingId;
        OrganizationId = organizationId;
        OfferDelivery = offerDelivery;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid SettingId { get; }
    public PosOrganizationId OrganizationId { get; }
    /// <summary>Canonical org Offer Delivery switch. Default OFF when no row exists.</summary>
    public bool OfferDelivery { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static OrganizationFulfillmentSettings CreateDefault(
        PosOrganizationId organizationId,
        DateTimeOffset nowUtc) =>
        new(Guid.NewGuid(), organizationId, offerDelivery: false, nowUtc, nowUtc);

    public static OrganizationFulfillmentSettings Rehydrate(
        Guid settingId,
        PosOrganizationId organizationId,
        bool offerDelivery,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(settingId, organizationId, offerDelivery, createdAtUtc, updatedAtUtc);

    public void SetOfferDelivery(bool offerDelivery, DateTimeOffset nowUtc)
    {
        OfferDelivery = offerDelivery;
        UpdatedAtUtc = nowUtc;
    }
}
