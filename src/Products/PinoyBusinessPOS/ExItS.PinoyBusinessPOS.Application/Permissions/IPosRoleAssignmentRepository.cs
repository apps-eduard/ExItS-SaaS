using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Application.Permissions;

public interface IPosRoleAssignmentRepository
{
    Task<PosRoleAssignment?> GetByIdAsync(PosOrganizationId organizationId, PosRoleAssignmentId id, CancellationToken ct = default);

    Task<PosRoleAssignment?> GetActiveForActorAsync(PosOrganizationId organizationId, Guid actorId, CancellationToken ct = default);

    Task<int> CountActiveOwnersAsync(PosOrganizationId organizationId, CancellationToken ct = default);

    Task<bool> HasAnyAssignmentsAsync(PosOrganizationId organizationId, CancellationToken ct = default);

    Task<(IReadOnlyList<PosRoleAssignment> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        PosRoleAssignmentStatus? status,
        Guid? actorId,
        PosRole? role,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task AddAsync(PosRoleAssignment assignment, CancellationToken ct = default);

    Task UpdateAsync(PosRoleAssignment assignment, CancellationToken ct = default);

    /// <summary>Takes a transaction-scoped advisory lock for org role mutations.</summary>
    Task AcquireOrganizationLockAsync(PosOrganizationId organizationId, CancellationToken ct = default);
}
