using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public sealed class SuspendPlatformUser
{
    private readonly IPlatformUserRepository _users;
    private readonly IClock _clock;

    public SuspendPlatformUser(IPlatformUserRepository users, IClock clock)
    {
        _users = users;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        try
        {
            user.Suspend(_clock.UtcNow);
            await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformUser>.Success(user);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformUser>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
