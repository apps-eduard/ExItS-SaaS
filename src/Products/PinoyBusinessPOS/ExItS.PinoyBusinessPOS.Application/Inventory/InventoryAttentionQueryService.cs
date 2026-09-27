using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Canonical branch-scoped inventory attention summary for manager/warehouse/shell operational UI.
/// </summary>
public sealed class InventoryAttentionQueryService
{
    private readonly IBranchInventoryQueryRepository _branchInventory;
    private readonly IInventoryLotRepository _lots;
    private readonly IClock _clock;

    public InventoryAttentionQueryService(
        IBranchInventoryQueryRepository branchInventory,
        IInventoryLotRepository lots,
        IClock clock)
    {
        _branchInventory = branchInventory;
        _lots = lots;
        _clock = clock;
    }

    public async Task<PosInventoryAttentionSummaryDto> GetAsync(
        BranchInventoryContext context,
        CancellationToken cancellationToken = default)
    {
        // Sequential awaits: repositories share one scoped DbContext (EF Core is not concurrent-safe).
        var lowStock = await _branchInventory
            .CountAsync(
                context,
                new BranchInventoryListFilter(TrackedOnly: true, LowStockOnly: true),
                cancellationToken)
            .ConfigureAwait(false);
        var outOfStock = await _branchInventory
            .CountAsync(
                context,
                new BranchInventoryListFilter(
                    TrackedOnly: true,
                    StockStatus: nameof(InventoryStockStatus.OutOfStock)),
                cancellationToken)
            .ConfigureAwait(false);

        var today = InventoryLot.BusinessDateOf(_clock.UtcNow);
        var (expired, near) = await _lots
            .CountExpiryAsync(
                PosOrganizationId.From(context.OrganizationId),
                today,
                PosBranchId.From(context.BranchId),
                cancellationToken)
            .ConfigureAwait(false);

        return new PosInventoryAttentionSummaryDto(lowStock, outOfStock, expired, near);
    }
}
