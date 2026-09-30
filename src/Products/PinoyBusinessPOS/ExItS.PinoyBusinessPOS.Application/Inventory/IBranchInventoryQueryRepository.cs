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
    /// Same filter composition as <see cref="ListAsync"/>; returns only the matching product count.
    /// </summary>
    Task<int> CountAsync(
        BranchInventoryContext context,
        BranchInventoryListFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Product ids matching the same filters as <see cref="ListAsync"/> (org + branch scoped).
    /// Used for server-side bulk low-stock updates without loading full rows into the client.
    /// </summary>
    Task<(IReadOnlyList<Guid> ProductIds, int TotalCount)> ListProductIdsAsync(
        BranchInventoryContext context,
        BranchInventoryListFilter filter,
        int maxTake,
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
