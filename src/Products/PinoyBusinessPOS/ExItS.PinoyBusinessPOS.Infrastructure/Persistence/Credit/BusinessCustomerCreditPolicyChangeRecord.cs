namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

internal sealed class BusinessCustomerCreditPolicyChangeRecord
{
    public Guid Id { get; set; }
    public Guid SellerOrganizationId { get; set; }
    public Guid BuyerOrganizationId { get; set; }
    public Guid BusinessCustomerCreditPolicyId { get; set; }
    public int Action { get; set; }
    public int? PreviousStatus { get; set; }
    public int NewStatus { get; set; }
    public decimal? PreviousCreditLimit { get; set; }
    public decimal? NewCreditLimit { get; set; }
    public int? PreviousTermDays { get; set; }
    public int? NewTermDays { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; set; }
}
