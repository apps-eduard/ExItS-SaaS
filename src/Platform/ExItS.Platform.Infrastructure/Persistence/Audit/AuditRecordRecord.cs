namespace ExItS.Platform.Infrastructure.Persistence.Audit;

internal sealed class AuditRecordRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ActorIdentifier { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public string? ProductCode { get; set; }
    public string? CorrelationId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Summary { get; set; }
}
