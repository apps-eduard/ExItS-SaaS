using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformCredentialTokenRepository : IPlatformCredentialTokenRepository
{
    private readonly Dictionary<string, PlatformCredentialToken> _byHash = new(StringComparer.Ordinal);

    public Task<PlatformCredentialToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byHash.TryGetValue(tokenHash, out var token) ? token : null);

    public Task AddAsync(PlatformCredentialToken token, CancellationToken cancellationToken = default)
    {
        _byHash[token.TokenHash] = token;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformCredentialToken token, CancellationToken cancellationToken = default)
    {
        _byHash[token.TokenHash] = token;
        return Task.CompletedTask;
    }

    public Task InvalidateActiveForUserAsync(
        PlatformUserId userId,
        PlatformCredentialTokenPurpose purpose,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        foreach (var token in _byHash.Values.Where(t => t.UserId == userId && t.Purpose == purpose && t.IsRedeemable(utcNow)))
        {
            token.Consume(utcNow);
        }

        return Task.CompletedTask;
    }
}

internal sealed class CapturingAuthOutboundMessageSink : IPlatformAuthOutboundMessageSink
{
    public PlatformAuthOutboundMessage? Last { get; private set; }

    public Task PublishAsync(PlatformAuthOutboundMessage message, CancellationToken cancellationToken = default)
    {
        Last = message;
        return Task.CompletedTask;
    }
}
