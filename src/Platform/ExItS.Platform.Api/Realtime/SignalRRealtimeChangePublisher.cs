using ExItS.Platform.Application.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.Api.Realtime;

/// <summary>
/// Fire-and-forget friendly SignalR publisher. Transport failures are logged and swallowed
/// so an already-committed business mutation is not failed by a disconnected client.
/// </summary>
public sealed class SignalRRealtimeChangePublisher(
    IHubContext<ExitsRealtimeHub> hub,
    ILogger<SignalRRealtimeChangePublisher> logger) : IRealtimeChangePublisher
{
    public async Task PublishToGroupsAsync(
        IReadOnlyList<string> groups,
        RealtimeEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (groups.Count == 0)
        {
            return;
        }

        try
        {
            foreach (var group in groups.Distinct(StringComparer.Ordinal))
            {
                await hub.Clients
                    .Group(group)
                    .SendAsync(ExitsRealtimeHub.ClientMethod, envelope, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Realtime publish failed for {EventType} resource {ResourceType}/{ResourceId}",
                envelope.EventType,
                envelope.ResourceType,
                envelope.ResourceId);
        }
    }
}
