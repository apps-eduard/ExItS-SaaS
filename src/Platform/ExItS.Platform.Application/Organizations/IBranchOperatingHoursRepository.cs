using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IBranchOperatingHoursRepository
{
    Task<BranchOperatingHoursSchedule?> GetByBranchIdAsync(
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One-shot load of operating-hour schedules for an organization (avoids ListBranches N+1).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, BranchOperatingHoursSchedule>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        BranchOperatingHoursSchedule schedule,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);
}
