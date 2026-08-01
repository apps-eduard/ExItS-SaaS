using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public interface IAccountProfileRepository
{
    Task<AccountProfile?> GetByIdAsync(AccountProfileId id, CancellationToken cancellationToken = default);

    Task<AccountProfile?> GetByUserAndClassAsync(
        PlatformUserId userIdentityId,
        AccountClass accountClass,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountProfile>> ListByUserAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AccountProfile profile, CancellationToken cancellationToken = default);

    Task UpdateAsync(AccountProfile profile, CancellationToken cancellationToken = default);
}
