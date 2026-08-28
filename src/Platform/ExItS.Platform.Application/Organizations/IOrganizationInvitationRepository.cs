using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationInvitationRepository
{
    Task<OrganizationInvitation?> GetByIdAsync(
        OrganizationInvitationId id,
        CancellationToken cancellationToken = default);

    Task<OrganizationInvitation?> FindPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<OrganizationInvitation?> FindPendingByOrganizationAndEmailAsync(
        PlatformOrganizationId organizationId,
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<OrganizationInvitation?> FindPendingByOrganizationAndTargetUserAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId targetPersonalUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationInvitation>> ListPendingByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationInvitation>> ListPendingByTargetPersonalUserIdAsync(
        PlatformUserId targetPersonalUserId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OrganizationInvitation> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        InvitationStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default);
}
