using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public sealed class CreatePlatformUser
{
    private readonly IPlatformUserRepository _users;
    private readonly IClock _clock;

    public CreatePlatformUser(IPlatformUserRepository users, IClock clock)
    {
        _users = users;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        string displayName,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = PlatformUser.Create(displayName, email, _clock.UtcNow);
            var existing = await _users.GetByNormalizedEmailAsync(user.NormalizedEmail, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<PlatformUser>.Failure(
                    ApplicationErrorCodes.EmailConflict,
                    "A Platform User with this email already exists.");
            }

            await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformUser>.Success(user);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformUser>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
