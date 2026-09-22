namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

internal sealed class ReturnBatchAuditEventRecord
{
    public Guid Id { get; set; }
    public Guid ReturnBatchId { get; set; }
    public Guid OrganizationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
}
