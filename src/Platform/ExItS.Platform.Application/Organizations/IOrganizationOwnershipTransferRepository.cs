using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationOwnershipTransferRepository
{
    Task<OrganizationOwnershipTransfer?> GetByIdAsync(
        OrganizationOwnershipTransferId id,
        CancellationToken cancellationToken = default);

    Task<OrganizationOwnershipTransfer?> FindPendingByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationOwnershipTransfer>> ListPendingByRecipientAsync(
        PlatformUserId toUserId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationOwnershipTransfer transfer, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationOwnershipTransfer transfer, CancellationToken cancellationToken = default);
}
