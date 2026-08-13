namespace ExItS.PinoyBusinessPOS.Application.Reporting;

/// <summary>
/// Bounded SQL aggregates for Organization Web Admin overview. Does not load sale or inventory histories.
/// </summary>
public interface IManagementOverviewReadStore
{
    Task<PosManagementOverviewDto> GetAsync(
        Guid organizationId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default);
}
