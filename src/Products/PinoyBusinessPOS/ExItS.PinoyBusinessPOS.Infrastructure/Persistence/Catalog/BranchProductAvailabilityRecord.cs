namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;

internal sealed class BranchProductAvailabilityRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public bool IsOffered { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid? UpdatedByActorId { get; set; }
    public uint Xmin { get; set; }
}
