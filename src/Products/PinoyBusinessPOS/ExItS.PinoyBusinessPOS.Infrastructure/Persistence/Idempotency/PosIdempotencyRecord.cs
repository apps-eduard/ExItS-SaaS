namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Idempotency;

internal sealed class PosIdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public Guid? OperationId { get; set; }
    public string OutcomeCode { get; set; } = string.Empty;
    public string? OutcomeBodyJson { get; set; }
    public string? ServerReference { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
