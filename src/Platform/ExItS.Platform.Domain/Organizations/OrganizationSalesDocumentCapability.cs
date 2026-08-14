namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Platform-controlled, organization-scoped authority for sales-document issuance.
/// Tax calculation configuration does not grant this capability.
/// </summary>
public sealed class OrganizationSalesDocumentCapability
{
    public PlatformOrganizationId OrganizationId { get; }
    public bool TaxDocumentIssuanceEnabled { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? UpdatedByActorReference { get; private set; }

    private OrganizationSalesDocumentCapability(
        PlatformOrganizationId organizationId,
        bool taxDocumentIssuanceEnabled,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference)
    {
        OrganizationId = organizationId;
        TaxDocumentIssuanceEnabled = taxDocumentIssuanceEnabled;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByActorReference = updatedByActorReference;
    }

    public static OrganizationSalesDocumentCapability CreateDefault(
        PlatformOrganizationId organizationId,
        DateTimeOffset utcNow) =>
        new(organizationId, taxDocumentIssuanceEnabled: false, EnsureUtc(utcNow), null);

    public static OrganizationSalesDocumentCapability Rehydrate(
        PlatformOrganizationId organizationId,
        bool taxDocumentIssuanceEnabled,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference) =>
        new(organizationId, taxDocumentIssuanceEnabled, EnsureUtc(updatedAtUtc), updatedByActorReference);

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(value));
        }

        return value;
    }
}
