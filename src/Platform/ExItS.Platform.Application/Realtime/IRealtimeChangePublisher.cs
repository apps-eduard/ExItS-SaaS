namespace ExItS.Platform.Application.Realtime;

/// <summary>
/// Post-commit change notifications. Implementations must never throw into the business
/// transaction path in a way that rolls back an already-committed change; prefer fire-and-forget
/// logging on transport failure.
/// </summary>
public interface IRealtimeChangePublisher
{
    Task PublishToGroupsAsync(
        IReadOnlyList<string> groups,
        RealtimeEventEnvelope envelope,
        CancellationToken cancellationToken = default);
}

/// <summary>No-op publisher for unit tests and hosts without SignalR.</summary>
public sealed class NullRealtimeChangePublisher : IRealtimeChangePublisher
{
    public static NullRealtimeChangePublisher Instance { get; } = new();

    public Task PublishToGroupsAsync(
        IReadOnlyList<string> groups,
        RealtimeEventEnvelope envelope,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
