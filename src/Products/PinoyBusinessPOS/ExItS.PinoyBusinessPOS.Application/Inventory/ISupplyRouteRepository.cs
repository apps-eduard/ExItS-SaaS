using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface ISupplyRouteRepository
{
    Task<SupplyRoute?> GetByIdAsync(
        PosOrganizationId organizationId,
        SupplyRouteId routeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplyRoute>> ListByDestinationAsync(
        PosOrganizationId organizationId,
        PosBranchId destinationLocationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplyRoute>> ListAllAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(SupplyRoute route, CancellationToken cancellationToken = default);

    Task UpdateAsync(SupplyRoute route, CancellationToken cancellationToken = default);
}
