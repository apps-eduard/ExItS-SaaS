using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IPosDeviceRepository
{
    Task<PosDevice?> GetByIdAsync(PosDeviceId id, CancellationToken cancellationToken = default);
    Task<PosDevice?> GetByInstallationDeviceIdAsync(PlatformOrganizationId organizationId, string installationDeviceId, CancellationToken cancellationToken = default);
    /// <summary>All devices including revoked — for audit/history retrieval only.</summary>
    Task<IReadOnlyList<PosDevice>> ListByOrganizationAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default);
    /// <summary>Active registered POS devices only — customer Device Management list.</summary>
    Task<IReadOnlyList<PosDevice>> ListActiveByOrganizationAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default);
    Task<int> CountActiveAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(PosDevice device, CancellationToken cancellationToken = default);
    Task UpdateAsync(PosDevice device, CancellationToken cancellationToken = default);

    Task<PosDevice?> FindByInstallationDeviceIdAsync(string installationDeviceId, CancellationToken cancellationToken = default);
}
