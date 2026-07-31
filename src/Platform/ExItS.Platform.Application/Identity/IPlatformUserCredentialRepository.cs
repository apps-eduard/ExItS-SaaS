using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public interface IPlatformUserCredentialRepository
{
    Task<PlatformUserCredential?> GetByUserIdAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default);

    Task<PlatformUserId?> FindUserIdByVerifiedRecoveryEmailAsync(
        string normalizedRecoveryEmail,
        CancellationToken cancellationToken = default);

    Task<bool> IsRecoveryEmailInUseAsync(
        string normalizedRecoveryEmail,
        PlatformUserId? excludingUserId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformUserCredential credential, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformUserCredential credential, CancellationToken cancellationToken = default);
}
