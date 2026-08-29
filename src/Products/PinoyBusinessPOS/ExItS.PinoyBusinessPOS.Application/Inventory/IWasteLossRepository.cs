using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record InventoryDocumentCostPeriodAggregate(
    decimal KnownCost,
    int PostedCount,
    int CompleteCostCount,
    int PartialCostCount,
    int UnavailableCostCount);

public interface IWasteLossRepository
{
    Task<WasteLoss?> GetByIdAsync(
        PosOrganizationId organizationId,
        WasteLossId wasteLossId,
        CancellationToken cancellationToken = default);

    Task<WasteLoss?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<WasteLoss> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        WasteLossFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default);

    Task UpdateAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default);

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
