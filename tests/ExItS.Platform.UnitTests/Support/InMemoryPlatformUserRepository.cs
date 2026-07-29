using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformUserRepository : IPlatformUserRepository
{
    private readonly Dictionary<Guid, PlatformUser> _byId = new();
    private readonly Dictionary<string, Guid> _emailIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> _usernameIndex = new(StringComparer.Ordinal);

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

    public Task<PlatformUser?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
    {
        if (_usernameIndex.TryGetValue(normalizedUsername, out var id) && _byId.TryGetValue(id, out var user))
        {
            return Task.FromResult<PlatformUser?>(user);
        }

        return Task.FromResult<PlatformUser?>(null);
    }

    public Task<(IReadOnlyList<PlatformUser> Items, int TotalCount)> ListAsync(
        AccountStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.AsEnumerable();
        if (status is not null)
        {
            query = query.Where(u => u.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.NormalizedUsername.Contains(term, StringComparison.Ordinal)
                || u.NormalizedEmail.Contains(term, StringComparison.Ordinal)
                || u.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderBy(u => u.NormalizedUsername, StringComparer.Ordinal).ToList();
        return Task.FromResult<(IReadOnlyList<PlatformUser>, int)>((ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task AddAsync(PlatformUser user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id.Value] = user;
        _emailIndex[user.NormalizedEmail] = user.Id.Value;
        _usernameIndex[user.NormalizedUsername] = user.Id.Value;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformUser user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id.Value] = user;
        _emailIndex[user.NormalizedEmail] = user.Id.Value;
        _usernameIndex[user.NormalizedUsername] = user.Id.Value;
        UpdateCount++;
        return Task.CompletedTask;
    }
}
