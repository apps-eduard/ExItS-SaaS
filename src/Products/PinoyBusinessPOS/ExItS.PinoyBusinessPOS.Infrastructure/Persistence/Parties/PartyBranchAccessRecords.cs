namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Parties;

internal sealed class CustomerBranchAccessRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerId { get; set; }
    public string GrantSource { get; set; } = null!;
    public DateTimeOffset GrantedAtUtc { get; set; }
    public Guid? GrantedByActorId { get; set; }
}

internal sealed class SupplierBranchAccessRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SupplierId { get; set; }
    public string GrantSource { get; set; } = null!;
    public DateTimeOffset GrantedAtUtc { get; set; }
    public Guid? GrantedByActorId { get; set; }
}
