using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationStaffIdSettingsRepository
{
    Task<OrganizationStaffIdSettings?> GetAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationStaffIdSettings settings, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationStaffIdSettings settings, CancellationToken cancellationToken = default);
}
