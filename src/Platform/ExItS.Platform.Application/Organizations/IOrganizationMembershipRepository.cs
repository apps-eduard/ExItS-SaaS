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

    /// <summary>
    /// Every Active membership in the organization, unpaged. Access rollups must not silently
    /// truncate the roster, so this has no page ceiling.
    /// </summary>
    Task<IReadOnlyList<OrganizationMembership>> ListActiveByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when at least one of the given memberships is still Active in the organization.
    /// Used to block destructive changes without materializing the whole roster.
    /// </summary>
    Task<bool> AnyActiveAsync(
        PlatformOrganizationId organizationId,
        IReadOnlyCollection<OrganizationMembershipId> membershipIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Memberships for the given users in one organization (any status, including Removed).
    /// Used for operational actor display-name resolution — not a staff-management roster.
    /// </summary>
    Task<IReadOnlyList<OrganizationMembership>> ListByOrganizationAndUserIdsAsync(
        PlatformOrganizationId organizationId,
        IReadOnlyCollection<PlatformUserId> userIds,
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
