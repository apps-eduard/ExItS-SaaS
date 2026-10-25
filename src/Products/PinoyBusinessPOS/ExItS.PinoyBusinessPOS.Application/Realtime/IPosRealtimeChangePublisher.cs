namespace ExItS.PinoyBusinessPOS.Application.Realtime;

/// <summary>
/// Post-commit POS change notifications. Implementations must never throw into the business
/// transaction path in a way that rolls back an already-committed change; prefer fire-and-forget
/// logging on transport failure.
/// </summary>
public interface IPosRealtimeChangePublisher
{
    Task PublishToGroupsAsync(
        IReadOnlyList<string> groups,
        PosRealtimeEventEnvelope envelope,
        CancellationToken cancellationToken = default);
}

/// <summary>No-op publisher for unit tests and hosts without SignalR.</summary>
public sealed class NullPosRealtimeChangePublisher : IPosRealtimeChangePublisher
{
    public static NullPosRealtimeChangePublisher Instance { get; } = new();

    public Task PublishToGroupsAsync(
        IReadOnlyList<string> groups,
        PosRealtimeEventEnvelope envelope,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
