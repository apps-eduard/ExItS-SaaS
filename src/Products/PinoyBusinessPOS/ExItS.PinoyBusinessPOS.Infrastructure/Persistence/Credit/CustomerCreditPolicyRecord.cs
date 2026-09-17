namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

internal sealed class CustomerCreditPolicyRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CustomerId { get; set; }
    public int Status { get; set; }
    public decimal CreditLimit { get; set; }
    public int DefaultTermDays { get; set; }
    public Guid ConfiguredByUserId { get; set; }
    public DateTimeOffset ConfiguredAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
