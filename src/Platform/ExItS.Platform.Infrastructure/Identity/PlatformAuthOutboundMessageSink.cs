using ExItS.Platform.Application.Identity;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.Infrastructure.Identity;

/// <summary>
/// No-op outbound auth message sink. Email vendor delivery is out of scope for P13-WP04.
/// Tokens are still created and hashed; operators may use debug exposure in non-Production only.
/// </summary>
internal sealed class NullPlatformAuthOutboundMessageSink(ILogger<NullPlatformAuthOutboundMessageSink> logger)
    : IPlatformAuthOutboundMessageSink
{
    public Task PublishAsync(PlatformAuthOutboundMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Auth outbound message queued without vendor delivery. Kind={Kind} UserId={UserId} ExpiresAtUtc={ExpiresAtUtc}",
            message.Kind,
            message.UserId,
            message.ExpiresAtUtc);
        return Task.CompletedTask;
    }
}

/// <summary>In-memory capture for Development/Testing verification of auth workflow tokens.</summary>
public sealed class CapturingPlatformAuthOutboundMessageSink : IPlatformAuthOutboundMessageSink
{
    private readonly List<PlatformAuthOutboundMessage> _messages = [];
    private readonly object _gate = new();

    public IReadOnlyList<PlatformAuthOutboundMessage> Messages
    {
        get
        {
            lock (_gate)
            {
                return _messages.ToList();
            }
        }
    }

    public Task PublishAsync(PlatformAuthOutboundMessage message, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _messages.Add(message);
        }

        return Task.CompletedTask;
    }

    public PlatformAuthOutboundMessage? LastOfKind(string kind)
    {
        lock (_gate)
        {
            return _messages.LastOrDefault(m => string.Equals(m.Kind, kind, StringComparison.Ordinal));
        }
    }
}
