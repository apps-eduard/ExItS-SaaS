namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

internal sealed class BusinessCustomerCreditPolicyRecord
{
    public Guid Id { get; set; }
    public Guid SellerOrganizationId { get; set; }
    public Guid BuyerOrganizationId { get; set; }
    public Guid? ConnectionId { get; set; }
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
