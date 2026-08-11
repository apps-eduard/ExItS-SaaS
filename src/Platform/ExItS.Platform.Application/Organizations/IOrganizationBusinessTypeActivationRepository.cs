using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationBusinessTypeActivationRepository
{
    Task<IReadOnlyList<OrganizationBusinessTypeActivation>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<OrganizationBusinessTypeActivation?> GetAsync(
        PlatformOrganizationId organizationId,
        BusinessTypeId businessTypeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationBusinessTypeActivation activation,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        PlatformOrganizationId organizationId,
        BusinessTypeId businessTypeId,
        CancellationToken cancellationToken = default);
}
