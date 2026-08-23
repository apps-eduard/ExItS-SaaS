using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Settings;

namespace ExItS.Platform.Application.Settings;

public sealed class PlatformSettingsProvisioner
{
    private readonly IPlatformSettingsRepository _repository;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PlatformSettingsProvisioner(
        IPlatformSettingsRepository repository,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<PlatformSettings> EnsureAsync(
        string actorId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetAsync(cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var created = PlatformSettings.CreateDefaults(_clock.UtcNow, actorId);
        await _repository.AddAsync(created, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }
}
