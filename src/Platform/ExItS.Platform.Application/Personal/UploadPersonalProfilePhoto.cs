using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed class UploadPersonalProfilePhoto
{
    private readonly IPlatformUserRepository _users;
    private readonly IAccountProfileRepository _profiles;
    private readonly IPersonalUserProfileRepository _personalProfiles;
    private readonly IPersonalProfilePhotoStore _photos;
    private readonly IPersonalProfilePhotoNotifier _notifier;
    private readonly GetPersonalProfile _getProfile;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UploadPersonalProfilePhoto(
        IPlatformUserRepository users,
        IAccountProfileRepository profiles,
        IPersonalUserProfileRepository personalProfiles,
        IPersonalProfilePhotoStore photos,
        IPersonalProfilePhotoNotifier notifier,
        GetPersonalProfile getProfile,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _profiles = profiles;
        _personalProfiles = personalProfiles;
        _photos = photos;
        _notifier = notifier;
        _getProfile = getProfile;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalProfileDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        AccountProfileId accountProfileId,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (!PersonalProfilePhotoRules.TryDetectContentType(content, out var contentType))
        {
            return ApplicationResult<PersonalProfileDto>.Failure(
                ApplicationErrorCodes.PersonalProfilePhotoInvalid,
                "Upload a JPEG, PNG, or WebP image up to 2 MB.");
        }

        var user = await _users.GetByIdAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PersonalProfileDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User identity was not found.");
        }

        var profile = await _profiles.GetByIdAsync(accountProfileId, cancellationToken).ConfigureAwait(false);
        if (profile is null || profile.UserIdentityId != userIdentityId || profile.AccountClass is not AccountClass.Personal)
        {
            return ApplicationResult<PersonalProfileDto>.Failure(
                ApplicationErrorCodes.AccountProfileNotAvailable,
                "Personal account profile is not available.");
        }

        var utcNow = _clock.UtcNow;
        var photoUrl = $"/api/v1/personal/profile/photo?v={utcNow.ToUnixTimeMilliseconds()}";
        try
        {
            var personal = await _personalProfiles
                .GetByUserAsync(userIdentityId, cancellationToken)
                .ConfigureAwait(false);
            var created = personal is null;
            personal ??= PersonalUserProfile.Create(userIdentityId, utcNow);
            personal.SetProfilePhotoUrl(photoUrl, utcNow);
            if (created)
            {
                await _personalProfiles.AddAsync(personal, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _personalProfiles.UpdateAsync(personal, cancellationToken).ConfigureAwait(false);
            }

            await _photos.SaveAsync(userIdentityId.Value, contentType, content, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalProfileDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _notifier.PhotoUpdatedAsync(userIdentityId.Value, photoUrl, cancellationToken).ConfigureAwait(false);
        return await _getProfile.ExecuteAsync(userIdentityId, accountProfileId, cancellationToken).ConfigureAwait(false);
    }
}
