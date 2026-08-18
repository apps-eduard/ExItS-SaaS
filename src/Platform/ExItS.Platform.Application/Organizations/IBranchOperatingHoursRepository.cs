using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IBranchOperatingHoursRepository
{
    Task<BranchOperatingHoursSchedule?> GetByBranchIdAsync(
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        BranchOperatingHoursSchedule schedule,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);
}
