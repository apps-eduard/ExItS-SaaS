using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryAccountProfileRepository : IAccountProfileRepository
{
    private readonly Dictionary<Guid, AccountProfile> _byId = new();

    public Task<AccountProfile?> GetByIdAsync(AccountProfileId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.TryGetValue(id.Value, out var profile) ? profile : null);

    public Task<AccountProfile?> GetByUserAndClassAsync(
        PlatformUserId userIdentityId,
        AccountClass accountClass,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            _byId.Values.FirstOrDefault(p =>
                p.UserIdentityId == userIdentityId && p.AccountClass == accountClass));

    public Task<IReadOnlyList<AccountProfile>> ListByUserAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AccountProfile>>(
            _byId.Values.Where(p => p.UserIdentityId == userIdentityId).ToList());

    public Task AddAsync(AccountProfile profile, CancellationToken cancellationToken = default)
    {
        _byId[profile.Id.Value] = profile;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AccountProfile profile, CancellationToken cancellationToken = default)
    {
        _byId[profile.Id.Value] = profile;
        return Task.CompletedTask;
    }
}
