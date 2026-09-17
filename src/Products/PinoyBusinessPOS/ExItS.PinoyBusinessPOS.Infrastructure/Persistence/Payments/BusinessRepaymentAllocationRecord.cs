namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;

internal sealed class BusinessRepaymentAllocationRecord
{
    public Guid Id { get; set; }
    public Guid SellerOrganizationId { get; set; }
    public Guid RepaymentId { get; set; }
    public Guid CreditEntryId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
