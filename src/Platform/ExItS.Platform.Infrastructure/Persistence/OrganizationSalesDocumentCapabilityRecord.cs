namespace ExItS.Platform.Infrastructure.Persistence;

internal sealed class OrganizationSalesDocumentCapabilityRecord
{
    public Guid OrganizationId { get; set; }
    public string ComplianceEligibilityStatus { get; set; } = "NotRequested";
    public bool TaxDocumentIssuanceEnabled { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? UpdatedByActorReference { get; set; }
}
