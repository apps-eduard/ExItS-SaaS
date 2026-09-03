using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationMembershipAreaAssignmentRepository
{
    Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByMembershipAsync(
        OrganizationMembershipId membershipId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByAreaAsync(
        PlatformOrganizationId organizationId,
        OrganizationAreaId areaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationMembershipAreaAssignment>> ListByUserAndOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task ReplaceForMembershipAsync(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        IReadOnlyCollection<OrganizationAreaId> areaIds,
        DateTimeOffset utcNow,
        string? actorReference,
        CancellationToken cancellationToken = default);
}
