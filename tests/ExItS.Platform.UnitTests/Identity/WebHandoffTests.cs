using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class WebHandoffTests
{
    [Theory]
    [InlineData("/overview", "/overview")]
    [InlineData("/", "/")]
    [InlineData(null, "/home")]
    [InlineData("https://evil.example", "/home")]
    [InlineData("//evil.example", "/home")]
    [InlineData("/\\evil", "/home")]
    [InlineData("overview", "/home")]
    public void Return_path_rejects_open_redirects(string? input, string expected)
    {
        Assert.Equal(expected, WebHandoffReturnPath.Sanitize(input, "/home"));
    }

    [Fact]
    public async Task Redeem_accepts_once_then_rejects_replay()
    {
        var store = new InMemoryStore();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-08-13T10:00:00Z"));
        var redeem = new RedeemWebHandoffTicket(store, clock);
        const string ticket = "live-ticket";
        store.Map[Hash(ticket)] = new WebHandoffTicketRecord(
            "personal",
            "session-token",
            Guid.NewGuid(),
            "Personal",
            null,
            "/home",
            clock.UtcNow.AddSeconds(60));

        var first = await redeem.ExecuteAsync(ticket);
        Assert.True(first.IsSuccess);
        Assert.Equal("session-token", first.Value!.SessionToken);
        Assert.Equal("personal", first.Value.TargetApp);
        Assert.Equal("/home", first.Value.ReturnPath);

        var replay = await redeem.ExecuteAsync(ticket);
        Assert.False(replay.IsSuccess);
        Assert.Equal("application.auth.web_handoff_replay", replay.ErrorCode);
    }

    [Fact]
    public async Task Redeem_rejects_missing_expired_and_tampered_tickets()
    {
        var store = new InMemoryStore();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-08-13T10:00:00Z"));
        var redeem = new RedeemWebHandoffTicket(store, clock);

        var missing = await redeem.ExecuteAsync("nope");
        Assert.False(missing.IsSuccess);
        Assert.Equal("application.auth.web_handoff_replay", missing.ErrorCode);

        const string ticket = "ticket-1";
        store.Map[Hash(ticket)] = new WebHandoffTicketRecord(
            "personal",
            "session-token",
            Guid.NewGuid(),
            "Personal",
            null,
            "/home",
            clock.UtcNow.AddMinutes(-1));

        var expired = await redeem.ExecuteAsync(ticket);
        Assert.False(expired.IsSuccess);
        Assert.Equal("application.auth.web_handoff_expired", expired.ErrorCode);

        store.Map[Hash(ticket)] = new WebHandoffTicketRecord(
            "organization",
            "session-token",
            Guid.NewGuid(),
            "Organization",
            Guid.NewGuid(),
            "/overview",
            clock.UtcNow.AddSeconds(60));

        var tampered = await redeem.ExecuteAsync(ticket + "x");
        Assert.False(tampered.IsSuccess);
        Assert.Equal("application.auth.web_handoff_replay", tampered.ErrorCode);

        var empty = await redeem.ExecuteAsync(" ");
        Assert.False(empty.IsSuccess);
        Assert.Equal("application.auth.web_handoff_invalid", empty.ErrorCode);
    }

    private static string Hash(string ticket) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ticket)));

    private sealed class InMemoryStore : IWebHandoffTicketStore
    {
        public Dictionary<string, WebHandoffTicketRecord> Map { get; } = new(StringComparer.Ordinal);

        public Task StoreAsync(string ticketHash, WebHandoffTicketRecord record, TimeSpan ttl, CancellationToken cancellationToken)
        {
            Map[ticketHash] = record;
            return Task.CompletedTask;
        }

        public Task<WebHandoffTicketRecord?> TakeAsync(string ticketHash, CancellationToken cancellationToken)
        {
            Map.Remove(ticketHash, out var record);
            return Task.FromResult(record);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ExItS.Platform.Domain.Abstractions.IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
