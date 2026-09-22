namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

internal sealed class ReturnBatchRefundRecord
{
    public Guid Id { get; set; }
    public Guid ReturnBatchId { get; set; }
    public Guid OrganizationId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public string? ClientRefundId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
}
