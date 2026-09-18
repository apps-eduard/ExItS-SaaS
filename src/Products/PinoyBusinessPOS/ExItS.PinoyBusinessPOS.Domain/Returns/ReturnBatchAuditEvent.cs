using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

public sealed class ReturnBatchAuditEvent
{
    public const int PayloadMaxLength = 2048;

    public Guid Id { get; }
    public ReturnBatchId ReturnBatchId { get; }
    public ReturnBatchAuditEventType EventType { get; }
    public string PayloadJson { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedBy { get; }

    private ReturnBatchAuditEvent(
        Guid id,
        ReturnBatchId returnBatchId,
        ReturnBatchAuditEventType eventType,
        string payloadJson,
        DateTimeOffset createdAtUtc,
        Guid createdBy)
    {
        Id = id;
        ReturnBatchId = returnBatchId;
        EventType = eventType;
        PayloadJson = payloadJson;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
    }

    public static ReturnBatchAuditEvent Create(
        ReturnBatchId returnBatchId,
        ReturnBatchAuditEventType eventType,
        string payloadJson,
        Guid createdBy,
        DateTimeOffset createdAtUtc,
        Guid? id = null) =>
        new(
            id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            returnBatchId,
            eventType,
            NormalizePayload(payloadJson),
            createdAtUtc,
            createdBy);

    public static ReturnBatchAuditEvent Rehydrate(
        Guid id,
        ReturnBatchId returnBatchId,
        ReturnBatchAuditEventType eventType,
        string payloadJson,
        DateTimeOffset createdAtUtc,
        Guid createdBy) =>
        new(id, returnBatchId, eventType, payloadJson, createdAtUtc, createdBy);

    private static string NormalizePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return "{}";
        }

        var trimmed = payloadJson.Trim();
        return trimmed.Length > PayloadMaxLength ? trimmed[..PayloadMaxLength] : trimmed;
    }
}
