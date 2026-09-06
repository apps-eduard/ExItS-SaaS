namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IBranchInventoryQueryRepository
{
    Task<(IReadOnlyList<BranchInventoryListRow> Items, int TotalCount)> ListAsync(
        BranchInventoryContext context,
        BranchInventoryListFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked products for retail→warehouse replenishment, with warehouse available qty
    /// batch-loaded for the current page only.
    /// </summary>
    Task<(IReadOnlyList<ReplenishmentCatalogRow> Items, int TotalCount)> ListReplenishmentCatalogAsync(
        BranchInventoryContext retailContext,
        ReplenishmentCatalogFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
