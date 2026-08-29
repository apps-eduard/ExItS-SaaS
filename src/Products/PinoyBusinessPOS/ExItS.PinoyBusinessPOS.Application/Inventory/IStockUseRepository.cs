using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IStockUseRepository
{
    Task<StockUse?> GetByIdAsync(
        PosOrganizationId organizationId,
        StockUseId stockUseId,
        CancellationToken cancellationToken = default);

    Task<StockUse?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StockUse> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        StockUseFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(StockUse stockUse, CancellationToken cancellationToken = default);

    Task UpdateAsync(StockUse stockUse, CancellationToken cancellationToken = default);

    Task<string> AllocateNextNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default);

    Task<InventoryDocumentCostPeriodAggregate> AggregatePostedCostForPeriodAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);
}
