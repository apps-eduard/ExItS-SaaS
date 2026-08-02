using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public interface IPersonalAccountSettingsRepository
{
    Task<PersonalAccountSettings?> GetByUserAsync(PlatformUserId userIdentityId, CancellationToken cancellationToken = default);

    Task AddAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default);
}
