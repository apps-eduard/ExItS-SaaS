using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformAccessTokenRepository : IPlatformAccessTokenRepository
{
    private readonly Dictionary<Guid, PlatformAccessToken> _byId = new();

    public Task<PlatformAccessToken?> GetByIdAsync(
        PlatformAccessTokenId tokenId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.TryGetValue(tokenId.Value, out var token) ? token : null);

    public Task<PlatformAccessToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task AddAsync(PlatformAccessToken token, CancellationToken cancellationToken = default)
    {
        _byId[token.Id.Value] = token;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformAccessToken token, CancellationToken cancellationToken = default)
    {
        _byId[token.Id.Value] = token;
        return Task.CompletedTask;
    }

    public Task<int> RevokeAllActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var token in _byId.Values.Where(t => t.UserId == userId && t.IsActive(utcNow)).ToList())
        {
            token.Revoke(utcNow);
            count++;
        }

        return Task.FromResult(count);
    }

    public Task<int> ClearOrganizationBindingAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var token in _byId.Values
                     .Where(t => t.UserId == userId && t.OrganizationId == organizationId && t.RevokedAtUtc is null)
                     .ToList())
        {
            token.ClearProductContext();
            count++;
        }

        return Task.FromResult(count);
    }

    public Task<int> ClearOrganizationBindingForOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var token in _byId.Values
                     .Where(t => t.OrganizationId == organizationId && t.RevokedAtUtc is null)
                     .ToList())
        {
            token.ClearProductContext();
            count++;
        }

        return Task.FromResult(count);
    }
}
