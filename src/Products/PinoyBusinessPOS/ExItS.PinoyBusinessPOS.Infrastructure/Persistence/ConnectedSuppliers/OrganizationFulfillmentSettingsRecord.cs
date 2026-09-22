namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;

internal sealed class OrganizationFulfillmentSettingsRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public bool OfferDelivery { get; set; }
    public bool DefaultPickupEnabled { get; set; }
    public bool DefaultDeliveryEnabled { get; set; }
    public bool DefaultOnlineOrdersEnabled { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
