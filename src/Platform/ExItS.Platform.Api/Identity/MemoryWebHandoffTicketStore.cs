using System.Collections.Concurrent;
using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.Api.Identity;

/// <summary>
/// Process-local one-time handoff tickets. No database migration. Multi-instance production
/// would need a shared store later — not Redis in this work package.
/// </summary>
public sealed class MemoryWebHandoffTicketStore : IWebHandoffTicketStore
{
    private readonly ConcurrentDictionary<string, WebHandoffTicketRecord> _tickets = new(StringComparer.Ordinal);

    public Task StoreAsync(
        string ticketHash,
        WebHandoffTicketRecord record,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        _tickets[ticketHash] = record;
        _ = EvictLaterAsync(ticketHash, ttl);
        return Task.CompletedTask;
    }

    public Task<WebHandoffTicketRecord?> TakeAsync(string ticketHash, CancellationToken cancellationToken)
    {
        _tickets.TryRemove(ticketHash, out var record);
        return Task.FromResult(record);
    }

    private async Task EvictLaterAsync(string ticketHash, TimeSpan ttl)
    {
        try
        {
            await Task.Delay(ttl + TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            _tickets.TryRemove(ticketHash, out _);
        }
        catch (Exception)
        {
            // Best-effort eviction.
        }
    }
}
