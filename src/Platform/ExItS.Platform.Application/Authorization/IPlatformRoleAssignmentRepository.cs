using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Authorization;

public interface IPlatformRoleAssignmentRepository
{
    Task<PlatformRoleAssignment?> GetByIdAsync(
        PlatformRoleAssignmentId id,
        CancellationToken cancellationToken = default);

    Task<PlatformRoleAssignment?> FindActiveAsync(
        PlatformUserId userId,
        PlatformSystemRole role,
        PlatformOrganizationId? organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformRoleAssignment>> ListActiveByUserAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default);

    Task<int> CountActivePlatformAdministratorsAsync(CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlatformRoleAssignment> Items, int TotalCount)> ListAsync(
        PlatformUserId? userId,
        PlatformSystemRole? role,
        PlatformOrganizationId? organizationId,
        PlatformRoleAssignmentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformRoleAssignment assignment, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformRoleAssignment assignment, CancellationToken cancellationToken = default);
}
