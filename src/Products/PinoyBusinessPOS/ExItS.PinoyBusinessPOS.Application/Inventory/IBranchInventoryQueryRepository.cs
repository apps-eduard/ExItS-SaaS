namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IBranchInventoryQueryRepository
{
    Task<(IReadOnlyList<BranchInventoryListRow> Items, int TotalCount)> ListAsync(
        BranchInventoryContext context,
        BranchInventoryListFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
