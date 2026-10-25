namespace ExItS.PinoyBusinessPOS.Application.Realtime;

/// <summary>
/// Minimal POS change-notification envelope. SignalR is not the source of truth —
/// clients must refetch authoritative POS APIs after receiving an event.
/// </summary>
public sealed record PosRealtimeEventEnvelope(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    DateTimeOffset OccurredAtUtc,
    string ResourceType,
    string ResourceId,
    string ScopeType,
    string? ScopeId = null,
    string? ResourceVersion = null,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, string>? Data = null)
{
    public const int CurrentSchemaVersion = 1;

    public static PosRealtimeEventEnvelope Create(
        string eventType,
        string resourceType,
        string resourceId,
        string scopeType,
        string? scopeId = null,
        string? resourceVersion = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? data = null,
        DateTimeOffset? occurredAtUtc = null) =>
        new(
            Guid.NewGuid(),
            eventType,
            CurrentSchemaVersion,
            occurredAtUtc ?? DateTimeOffset.UtcNow,
            resourceType,
            resourceId,
            scopeType,
            scopeId,
            resourceVersion,
            correlationId,
            data);
}
