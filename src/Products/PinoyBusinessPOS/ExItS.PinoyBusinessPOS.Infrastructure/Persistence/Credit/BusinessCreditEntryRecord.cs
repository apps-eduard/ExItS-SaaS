namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

internal sealed class BusinessCreditEntryRecord
{
    public Guid Id { get; set; }
    public Guid SellerOrganizationId { get; set; }
    public Guid BuyerOrganizationId { get; set; }
    public Guid? ConnectionId { get; set; }
    public decimal Amount { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReversedAtUtc { get; set; }
    public string? ReversalReason { get; set; }
    public DateOnly? CurrentDueDate { get; set; }
    public Guid? SourceSaleId { get; set; }
    public uint Xmin { get; set; }
}
