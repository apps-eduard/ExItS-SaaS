using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IPosDeviceRegistrationTokenRepository
{
    Task<PosDeviceRegistrationToken?> GetByIdAsync(
        PosDeviceRegistrationTokenId id,
        CancellationToken cancellationToken = default);

    Task<PosDeviceRegistrationToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(PosDeviceRegistrationToken token, CancellationToken cancellationToken = default);

    Task UpdateAsync(PosDeviceRegistrationToken token, CancellationToken cancellationToken = default);
}
