using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IStaffPasswordResetRequestRepository
{
    Task<StaffPasswordResetRequest?> GetByIdAsync(
        StaffPasswordResetRequestId id,
        CancellationToken cancellationToken = default);

    Task<StaffPasswordResetRequest?> FindOpenByStaffUserAsync(
        PlatformUserId staffUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffPasswordResetRequest>> ListOpenByStaffUsersAsync(
        IReadOnlyCollection<PlatformUserId> staffUserIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffPasswordResetRequest>> ListOpenByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(StaffPasswordResetRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(StaffPasswordResetRequest request, CancellationToken cancellationToken = default);
}
