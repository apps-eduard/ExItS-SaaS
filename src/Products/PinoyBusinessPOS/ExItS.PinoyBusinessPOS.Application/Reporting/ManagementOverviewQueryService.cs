using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Reporting;

public sealed class ManagementOverviewQueryService
{
    private readonly IManagementOverviewReadStore _store;
    private readonly IClock _clock;

    public ManagementOverviewQueryService(IManagementOverviewReadStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public Task<PosManagementOverviewDto> GetAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var businessDate = InventoryLot.BusinessDateOf(_clock.UtcNow);
        return _store.GetAsync(orgId.Value, businessDate, cancellationToken);
    }
}
