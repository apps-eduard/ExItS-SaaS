using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationMembershipBranchAssignmentRepository
{
    Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByMembershipAsync(
        OrganizationMembershipId membershipId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByBranchAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationMembershipBranchAssignment>> ListByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task ReplaceForMembershipAsync(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        IReadOnlyCollection<OrganizationBranchId> branchIds,
        DateTimeOffset utcNow,
        string? actorReference,
        CancellationToken cancellationToken = default);

    Task AssignPrimaryBranchForNewStaffAsync(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        DateTimeOffset utcNow,
        string? actorReference,
        CancellationToken cancellationToken = default);
}
