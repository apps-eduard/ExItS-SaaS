using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IStockRequestRepository
{
    Task<StockRequest?> GetByIdAsync(
        PosOrganizationId organizationId,
        StockRequestId stockRequestId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StockRequest> Items, int TotalCount)> ListByDestinationAsync(
        PosOrganizationId organizationId,
        PosBranchId destinationLocationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StockRequest> Items, int TotalCount)> ListBySourceAsync(
        PosOrganizationId organizationId,
        PosBranchId sourceLocationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(StockRequest stockRequest, CancellationToken cancellationToken = default);

    Task UpdateAsync(StockRequest stockRequest, CancellationToken cancellationToken = default);

    Task<string> AllocateNextNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default);
}
