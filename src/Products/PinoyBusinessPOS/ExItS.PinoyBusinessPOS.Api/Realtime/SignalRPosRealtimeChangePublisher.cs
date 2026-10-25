using ExItS.PinoyBusinessPOS.Application.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ExItS.PinoyBusinessPOS.Api.Realtime;

/// <summary>
/// Fire-and-forget friendly SignalR publisher. Transport failures are logged and swallowed
/// so an already-committed business mutation is not failed by a disconnected client.
/// </summary>
public sealed class SignalRPosRealtimeChangePublisher(
    IHubContext<ExitsPosRealtimeHub> hub,
    ILogger<SignalRPosRealtimeChangePublisher> logger) : IPosRealtimeChangePublisher
{
    public async Task PublishToGroupsAsync(
        IReadOnlyList<string> groups,
        PosRealtimeEventEnvelope envelope,
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
                    .SendAsync(ExitsPosRealtimeHub.ClientMethod, envelope, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "POS realtime publish failed for {EventType} resource {ResourceType}/{ResourceId}",
                envelope.EventType,
                envelope.ResourceType,
                envelope.ResourceId);
        }
    }
}
