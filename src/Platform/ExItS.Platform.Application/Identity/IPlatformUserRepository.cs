using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public interface IPlatformUserRepository
{
    Task<PlatformUser?> GetByIdAsync(PlatformUserId id, CancellationToken cancellationToken = default);

    Task<PlatformUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<PlatformUser?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlatformUser> Items, int TotalCount)> ListAsync(
        AccountStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformUser user, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformUser user, CancellationToken cancellationToken = default);
}
