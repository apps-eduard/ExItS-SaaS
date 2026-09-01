using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Central bulk branch inventory resolver for read paths (MB2-02A). Uses <see cref="BranchStockResolver"/>
/// and branch reorder settings without per-product repository round-trips.
/// </summary>
public sealed class BranchInventoryReadService
{
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly IInventoryBranchReorderRepository _reorder;

    public BranchInventoryReadService(
        IInventoryBranchBalanceRepository balances,
        IInventoryBranchReorderRepository reorder)
    {
        _balances = balances;
        _reorder = reorder;
    }

    public async Task<IReadOnlyDictionary<Guid, BranchInventoryProductRead>> ResolveAsync(
        BranchInventoryContext context,
        IReadOnlyList<InventoryAccount> accounts,
        CancellationToken cancellationToken = default)
    {
        if (accounts.Count == 0)
        {
            return new Dictionary<Guid, BranchInventoryProductRead>();
        }

        var orgId = PosOrganizationId.From(context.OrganizationId);
        var branchId = PosBranchId.From(context.BranchId);
        var productIds = accounts.Select(a => a.ProductId).Distinct().ToList();

        var balances = await _balances
            .ListByProductIdsAsync(orgId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var reorderSettings = await _reorder
            .ListByBranchAndProductIdsAsync(orgId, branchId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var reorderByProduct = reorderSettings.ToDictionary(s => s.ProductId.Value);

        var balancesByProduct = balances
            .GroupBy(b => b.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new Dictionary<Guid, BranchInventoryProductRead>(accounts.Count);
        foreach (var account in accounts)
        {
            balancesByProduct.TryGetValue(account.ProductId.Value, out var productBalances);
            productBalances ??= [];
            reorderByProduct.TryGetValue(account.ProductId.Value, out var branchReorder);

            var branchOnHand = BranchStockResolver.ResolveOnHand(
                branchId,
                context.PrimaryBranchId,
                account.OnHandQuantity,
                productBalances,
                account.ProductId);

            var (reorderLevel, reorderQuantity) = ResolveReorderConfiguration(
                context,
                branchReorder,
                account);

            var isLow = account.IsTracked
                && reorderLevel is not null
                && branchOnHand > 0m
                && branchOnHand <= reorderLevel.Value;
            var isSuggested = account.IsTracked
                && InventoryStockStatuses.IsReorderSuggested(branchOnHand, reorderLevel);
            var suggested = account.IsTracked
                ? InventoryStockStatuses.SuggestedOrderQuantity(branchOnHand, reorderLevel, reorderQuantity)
                : null;

            result[account.ProductId.Value] = new BranchInventoryProductRead(
                account.ProductId.Value,
                branchOnHand,
                account.OnHandQuantity,
                reorderLevel,
                reorderQuantity,
                isLow,
                isSuggested,
                suggested);
        }

        return result;
    }

    public async Task<BranchInventoryProductRead?> ResolveSingleAsync(
        BranchInventoryContext context,
        InventoryAccount account,
        CancellationToken cancellationToken = default)
    {
        var map = await ResolveAsync(context, [account], cancellationToken).ConfigureAwait(false);
        return map.TryGetValue(account.ProductId.Value, out var read) ? read : null;
    }

    public static (decimal? ReorderLevel, decimal? ReorderQuantity) ResolveReorderConfiguration(
        BranchInventoryContext context,
        InventoryBranchReorderSetting? branchSetting,
        InventoryAccount account)
    {
        if (branchSetting is not null)
        {
            return (branchSetting.ReorderLevel, branchSetting.ReorderQuantity);
        }

        var isPrimary = context.PrimaryBranchId is not null
            && context.PrimaryBranchId.Value == context.BranchId;
        if (isPrimary)
        {
            return (account.ReorderLevel, account.ReorderQuantity);
        }

        return (null, null);
    }
}
