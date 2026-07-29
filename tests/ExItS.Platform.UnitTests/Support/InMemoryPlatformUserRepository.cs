using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformUserRepository : IPlatformUserRepository
{
    private readonly Dictionary<Guid, PlatformUser> _byId = new();
    private readonly Dictionary<string, Guid> _emailIndex = new(StringComparer.Ordinal);

    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<PlatformUser?> GetByIdAsync(PlatformUserId id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var user);
        return Task.FromResult(user);
    }

    public Task<PlatformUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        if (_emailIndex.TryGetValue(normalizedEmail, out var id) && _byId.TryGetValue(id, out var user))
        {
            return Task.FromResult<PlatformUser?>(user);
        }

        return Task.FromResult<PlatformUser?>(null);
    }

    public Task AddAsync(PlatformUser user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id.Value] = user;
        _emailIndex[user.NormalizedEmail] = user.Id.Value;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformUser user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id.Value] = user;
        _emailIndex[user.NormalizedEmail] = user.Id.Value;
        UpdateCount++;
        return Task.CompletedTask;
    }
}
