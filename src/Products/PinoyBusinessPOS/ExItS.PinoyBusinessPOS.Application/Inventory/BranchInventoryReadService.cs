using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Central bulk branch inventory resolver for read paths (MB2-02A). Uses <see cref="BranchStockResolver"/>
/// and branch reorder settings without per-product repository round-trips.
/// Adjusts reserved/available for time-expired temporary holds before ledger cleanup.
/// </summary>
public sealed class BranchInventoryReadService
{
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly IInventoryBranchReorderRepository _reorder;
    private readonly IInventoryBranchReorderDefaultRepository? _branchDefaults;
    private readonly IConnectedPoInventoryReservationRepository? _reservations;
    private readonly TimeProvider _clock;

    public BranchInventoryReadService(
        IInventoryBranchBalanceRepository balances,
        IInventoryBranchReorderRepository reorder,
        IInventoryBranchReorderDefaultRepository? branchDefaults = null,
        IConnectedPoInventoryReservationRepository? reservations = null,
        TimeProvider? clock = null)
    {
        _balances = balances;
        _reorder = reorder;
        _branchDefaults = branchDefaults;
        _reservations = reservations;
        _clock = clock ?? TimeProvider.System;
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
        var branchDefault = _branchDefaults is null
            ? null
            : await _branchDefaults.GetAsync(orgId, branchId, cancellationToken).ConfigureAwait(false);

        var balancesByProduct = balances
            .GroupBy(b => b.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        IReadOnlyDictionary<Guid, decimal> expiredStillActive = new Dictionary<Guid, decimal>();
        if (_reservations is not null)
        {
            expiredStillActive = await _reservations
                .SumExpiredStillActiveRemainingByProductAsync(
                    orgId,
                    branchId,
                    productIds,
                    _clock.GetUtcNow(),
                    cancellationToken)
                .ConfigureAwait(false);
        }

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

            var branchReservedRaw = BranchStockResolver.ResolveReserved(
                branchId,
                productBalances,
                account.ProductId);
            expiredStillActive.TryGetValue(account.ProductId.Value, out var expiredQty);
            var branchReserved = Math.Max(0m, branchReservedRaw - expiredQty);
            var branchAvailable = BranchStockResolver.ResolveAvailable(branchOnHand, branchReserved);

            var (reorderLevel, reorderQuantity) = ResolveReorderConfiguration(
                context,
                branchReorder,
                account,
                branchDefault);

            var isLow = account.IsTracked
                && reorderLevel is not null
                && branchAvailable > 0m
                && branchAvailable <= reorderLevel.Value;
            var isSuggested = account.IsTracked
                && InventoryStockStatuses.IsReorderSuggested(branchAvailable, reorderLevel);
            var suggested = account.IsTracked
                ? InventoryStockStatuses.SuggestedOrderQuantity(branchAvailable, reorderLevel, reorderQuantity)
                : null;

            result[account.ProductId.Value] = new BranchInventoryProductRead(
                account.ProductId.Value,
                branchOnHand,
                account.OnHandQuantity,
                branchReserved,
                branchAvailable,
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
        InventoryAccount account,
        InventoryBranchReorderDefault? branchDefault = null)
    {
        if (branchSetting is not null)
        {
            return (branchSetting.ReorderLevel, branchSetting.ReorderQuantity);
        }

        if (branchDefault is not null
            && (branchDefault.ReorderLevel is not null || branchDefault.ReorderQuantity is not null))
        {
            return (branchDefault.ReorderLevel, branchDefault.ReorderQuantity);
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
