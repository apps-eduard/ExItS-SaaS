using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IStockCountRepository
{
    Task<StockCount?> GetByIdAsync(
        PosOrganizationId organizationId,
        StockCountId stockCountId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StockCount> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        StockCountFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(StockCount stockCount, CancellationToken cancellationToken = default);

    Task UpdateAsync(StockCount stockCount, CancellationToken cancellationToken = default);

    Task<StockCount> StartAsync(
        PosOrganizationId organizationId,
        StockCountId stockCountId,
        DateOnly businessDateUtc,
        Func<string, StockCount> applyStart,
        CancellationToken cancellationToken = default);

    Task<StockCount> CompleteAsync(
        PosOrganizationId organizationId,
        StockCountId stockCountId,
        Func<StockCount, CancellationToken, Task> afterMarkedComplete,
        CancellationToken cancellationToken = default);
}
