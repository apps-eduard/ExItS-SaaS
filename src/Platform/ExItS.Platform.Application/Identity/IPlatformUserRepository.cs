using ExItS.Platform.Domain.Authorization;
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
        UserDirectoryFilter? directoryFilter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, PlatformUserDirectoryExtras>> GetDirectoryExtrasAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformUser user, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformUser user, CancellationToken cancellationToken = default);
}
