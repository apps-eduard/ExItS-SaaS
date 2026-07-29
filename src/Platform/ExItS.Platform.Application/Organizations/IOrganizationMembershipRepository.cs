using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationMembershipRepository
{
    Task<OrganizationMembership?> GetByIdAsync(OrganizationMembershipId id, CancellationToken cancellationToken = default);

    Task<OrganizationMembership?> FindActiveByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationMembership membership, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationMembership membership, CancellationToken cancellationToken = default);
}
