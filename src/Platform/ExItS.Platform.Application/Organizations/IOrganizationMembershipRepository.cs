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

    Task<OrganizationMembership?> FindActiveOwnerByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>Current = Active or Suspended (not Removed).</summary>
    Task<OrganizationMembership?> FindCurrentByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OrganizationMembership> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        MembershipStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OrganizationMembership> Items, int TotalCount)> ListByUserAsync(
        PlatformUserId userId,
        MembershipStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Active memberships with OrganizationOwner or OrganizationAdministrator role.</summary>
    Task<int> CountActiveGoverningAdminsAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active Owner and Administrator memberships — recipients for organization business inbox events
    /// (customer-link responses remain inviter-specific; supplier connection uses this set).
    /// </summary>
    Task<IReadOnlyList<OrganizationMembership>> ListActiveBusinessInboxRecipientsAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationMembership membership, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationMembership membership, CancellationToken cancellationToken = default);
}
