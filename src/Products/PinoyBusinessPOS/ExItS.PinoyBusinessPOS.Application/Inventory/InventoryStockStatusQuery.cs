using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public static class InventoryStockStatusStates
{
    public const string All = "All";
    public const string Available = "Available";
    public const string LowStock = "LowStock";
    public const string OutOfStock = "OutOfStock";
    public const string Reserved = "Reserved";
    public const string Damaged = "Damaged";
    public const string InspectionHold = "InspectionHold";
    public const string PendingReturn = "PendingReturn";
    public const string Expired = "Expired";
    public const string SaleBlocked = "SaleBlocked";

    public static readonly IReadOnlyList<string> Codes =
    [
        All,
        Available,
        LowStock,
        OutOfStock,
        Reserved,
        Damaged,
        InspectionHold,
        PendingReturn,
        Expired,
        SaleBlocked,
    ];

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = All;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var match = Codes.FirstOrDefault(c =>
            string.Equals(c, value.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        normalized = match;
        return true;
    }
}

public sealed record InventoryStockStatusFilter(
    Guid? BranchId = null,
    Guid? AreaId = null,
    Guid? CategoryId = null,
    Guid? ProductId = null,
    string? Search = null,
    string StockState = InventoryStockStatusStates.All);

/// <summary>
/// One product × authorized branch stock snapshot. Quantities come from
/// <see cref="BranchStockResolver"/> (+ lot sellable cap when expiration is tracked).
/// </summary>
public sealed record InventoryStockStatusRowDto(
    Guid ProductId,
    string ProductName,
    string? Sku,
    Guid? CategoryId,
    string? CategoryName,
    string UnitOfMeasure,
    Guid BranchId,
    string BranchName,
    Guid? AreaId,
    string? AreaName,
    decimal OnHandQuantity,
    decimal SellableQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal DamagedQuantity,
    decimal InspectionHoldQuantity,
    decimal PendingReturnQuantity,
    decimal ExpiredQuantity,
    decimal SaleBlockedQuantity,
    decimal InTransitInboundQuantity,
    decimal InTransitOutboundQuantity,
    decimal StockRequestCommittedQuantity,
    decimal? ReorderLevel,
    bool IsLowStock);

public sealed record InventoryStockStatusResultDto(
    DateTimeOffset GeneratedAtUtc,
    bool IsCurrentOnly,
    int TotalCount,
    IReadOnlyList<InventoryStockStatusRowDto> Rows);

/// <summary>
/// Current-stock snapshot across authorized branches. No historical as-of reconstruction.
/// Bulk-loads balances/lots/commitments — no per-product repository round-trips.
/// </summary>
public sealed class InventoryStockStatusQuery
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IProductCategoryRepository _categories;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly IAuthorizedBranchGroupingDirectory _grouping;
    private readonly IInventoryLotRepository _lots;
    private readonly IInventoryTransferRepository _transfers;
    private readonly StockRequestCommitmentQuery _stockRequestCommitments;
    private readonly BranchExpirationPolicyResolver _expirationPolicies;
    private readonly ExpirySalePolicyResolver _expirySalePolicies;
    private readonly IInventoryBranchReorderRepository _reorder;
    private readonly IClock _clock;

    public InventoryStockStatusQuery(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IProductCategoryRepository categories,
        IInventoryBranchBalanceRepository balances,
        IAuthorizedBranchGroupingDirectory grouping,
        IInventoryLotRepository lots,
        IInventoryTransferRepository transfers,
        StockRequestCommitmentQuery stockRequestCommitments,
        BranchExpirationPolicyResolver expirationPolicies,
        ExpirySalePolicyResolver expirySalePolicies,
        IInventoryBranchReorderRepository reorder,
        IClock clock)
    {
        _inventory = inventory;
        _products = products;
        _categories = categories;
        _balances = balances;
        _grouping = grouping;
        _lots = lots;
        _transfers = transfers;
        _stockRequestCommitments = stockRequestCommitments;
        _expirationPolicies = expirationPolicies;
        _expirySalePolicies = expirySalePolicies;
        _reorder = reorder;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryStockStatusResultDto>> ExecuteAsync(
        Guid organizationId,
        InventoryStockStatusFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);

        if (!InventoryStockStatusStates.TryNormalize(filter.StockState, out var stockState))
        {
            return ApplicationResult<InventoryStockStatusResultDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                $"Stock state must be one of: {string.Join(", ", InventoryStockStatusStates.Codes)}.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var scope = await _grouping
            .ListAuthorizedAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var authorized = scope.Branches
            .GroupBy(b => b.BranchId)
            .Select(g => g.First())
            .ToList();

        if (filter.BranchId is Guid branchFilter)
        {
            authorized = authorized.Where(b => b.BranchId == branchFilter).ToList();
            if (authorized.Count == 0)
            {
                return ApplicationResult<InventoryStockStatusResultDto>.Success(
                    EmptyResult());
            }
        }

        if (filter.AreaId is Guid areaFilter)
        {
            authorized = authorized.Where(b => b.AreaId == areaFilter).ToList();
            if (authorized.Count == 0)
            {
                return ApplicationResult<InventoryStockStatusResultDto>.Success(
                    EmptyResult());
            }
        }

        var accounts = await _inventory.ListAllAccountsAsync(orgId, cancellationToken).ConfigureAwait(false);
        var tracked = accounts.Where(a => a.IsTracked).ToList();
        if (tracked.Count == 0)
        {
            return ApplicationResult<InventoryStockStatusResultDto>.Success(EmptyResult());
        }

        var productIds = tracked.Select(a => a.ProductId).ToList();
        if (filter.ProductId is Guid productFilter)
        {
            var pid = CatalogProductId.From(productFilter);
            productIds = productIds.Where(p => p == pid).ToList();
            tracked = tracked.Where(a => a.ProductId == pid).ToList();
        }

        if (productIds.Count == 0)
        {
            return ApplicationResult<InventoryStockStatusResultDto>.Success(EmptyResult());
        }

        var catalog = await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);

        IEnumerable<CatalogProduct> products = catalog;
        if (filter.CategoryId is Guid categoryId)
        {
            products = products.Where(p => p.CategoryId?.Value == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            products = products.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (p.Sku?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.Barcode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var filteredProducts = products.ToList();
        if (filteredProducts.Count == 0)
        {
            return ApplicationResult<InventoryStockStatusResultDto>.Success(EmptyResult());
        }

        var categoryIds = filteredProducts
            .Where(p => p.CategoryId is not null)
            .Select(p => p.CategoryId!)
            .GroupBy(id => id.Value)
            .Select(g => g.First())
            .ToList();
        var categoryNameById = categoryIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _categories.ListByIdsAsync(orgId, categoryIds, cancellationToken).ConfigureAwait(false))
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .ToDictionary(c => c.Id.Value, c => c.Name);

        var filteredIds = filteredProducts.Select(p => p.Id).ToList();
        var accountByProduct = tracked
            .Where(a => filteredIds.Contains(a.ProductId))
            .ToDictionary(a => a.ProductId.Value);

        var balances = await _balances
            .ListByProductIdsAsync(orgId, filteredIds, cancellationToken)
            .ConfigureAwait(false);
        var balancesByProduct = balances
            .GroupBy(b => b.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var branchIds = authorized.Select(b => PosBranchId.From(b.BranchId)).ToList();
        var today = InventoryLot.BusinessDateOf(_clock.UtcNow);

        // Bulk expiration policies per product×branch (branch count × products, but one resolver call pattern).
        var expirationByProductBranch = new Dictionary<(Guid ProductId, Guid BranchId), BranchExpirationPolicy>();
        foreach (var product in filteredProducts)
        {
            var map = await _expirationPolicies
                .ResolveManyBranchesAsync(orgId, product.Id, branchIds, cancellationToken)
                .ConfigureAwait(false);
            foreach (var (branchId, policy) in map)
            {
                expirationByProductBranch[(product.Id.Value, branchId)] = policy;
            }
        }

        var anyTracks = expirationByProductBranch.Values.Any(p => p.TracksExpiration);
        IReadOnlyList<InventoryLot> allLots = [];
        if (anyTracks)
        {
            allLots = await _lots
                .ListOnHandForProductsAsync(
                    orgId,
                    filteredIds,
                    branchId: null,
                    includeDepleted: false,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var lotsByProductBranch = allLots
            .Where(l => l.BranchId is not null)
            .GroupBy(l => (l.ProductId.Value, l.BranchId!.Value))
            .ToDictionary(g => g.Key, g => (IReadOnlyList<InventoryLot>)g.ToList());

        var salePolicyByBranchCategory = new Dictionary<(Guid BranchId, Guid? CategoryId), EffectiveExpirySalePolicy>();
        async Task<EffectiveExpirySalePolicy> ResolveSalePolicyAsync(Guid branchId, Guid? categoryId)
        {
            var key = (branchId, categoryId);
            if (salePolicyByBranchCategory.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var resolved = await _expirySalePolicies
                .ResolveAsync(orgId, PosBranchId.From(branchId), categoryId, cancellationToken)
                .ConfigureAwait(false);
            salePolicyByBranchCategory[key] = resolved;
            return resolved;
        }

        // Reorder levels for low-stock filter (bulk per branch).
        var reorderByBranchProduct = new Dictionary<(Guid BranchId, Guid ProductId), InventoryBranchReorderSetting>();
        foreach (var branch in authorized)
        {
            var settings = await _reorder
                .ListByBranchAndProductIdsAsync(
                    orgId,
                    PosBranchId.From(branch.BranchId),
                    filteredIds,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (var setting in settings)
            {
                reorderByBranchProduct[(branch.BranchId, setting.ProductId.Value)] = setting;
            }
        }

        // In-transit: one commitment query per authorized branch (not per product).
        var commitmentsByBranchProduct = new Dictionary<(Guid BranchId, Guid ProductId), (decimal In, decimal Out)>();
        var srCommittedByBranchProduct = new Dictionary<(Guid BranchId, Guid ProductId), decimal>();
        foreach (var branch in authorized)
        {
            var branchId = PosBranchId.From(branch.BranchId);
            var commitments = await _transfers
                .ListOpenCommitmentsForBranchAsync(
                    orgId,
                    branchId,
                    filteredIds,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (var group in commitments.GroupBy(c => c.ProductId))
            {
                var inbound = group.Where(c => c.Direction == "Inbound").Sum(c => c.OutstandingQuantity);
                var outbound = group.Where(c => c.Direction == "Outbound").Sum(c => c.OutstandingQuantity);
                commitmentsByBranchProduct[(branch.BranchId, group.Key)] = (inbound, outbound);
            }

            var srCommitted = await _stockRequestCommitments
                .SumRemainingToDispatchByProductAsync(orgId, branchId, filteredIds, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            foreach (var (productId, qty) in srCommitted)
            {
                if (qty > 0m)
                {
                    srCommittedByBranchProduct[(branch.BranchId, productId)] = qty;
                }
            }
        }

        var rows = new List<InventoryStockStatusRowDto>();
        foreach (var product in filteredProducts)
        {
            if (!accountByProduct.TryGetValue(product.Id.Value, out var account))
            {
                continue;
            }

            balancesByProduct.TryGetValue(product.Id.Value, out var productBalances);
            productBalances ??= [];

            foreach (var branch in authorized)
            {
                var branchId = PosBranchId.From(branch.BranchId);
                var onHand = BranchStockResolver.ResolveOnHand(
                    branchId,
                    primaryBranchId: null,
                    account.OnHandQuantity,
                    productBalances,
                    product.Id);
                // Fail-closed for unallocated: stock status only uses explicit balances.
                var explicitBalance = productBalances.FirstOrDefault(b => b.BranchId == branchId);
                if (explicitBalance is null && onHand == 0m)
                {
                    // Still emit zero row when product filter is exact, so prefilter is visible.
                    if (filter.ProductId is null
                        && stockState != InventoryStockStatusStates.All
                        && stockState != InventoryStockStatusStates.OutOfStock)
                    {
                        continue;
                    }
                }

                var reserved = BranchStockResolver.ResolveReserved(branchId, productBalances, product.Id);
                var (pending, hold, damaged) = BranchStockResolver.ResolveNonSellableBuckets(
                    branchId,
                    productBalances,
                    product.Id);
                var branchAvailable = BranchStockResolver.ResolveAvailable(onHand, reserved, pending, hold, damaged);
                var srCommitted = srCommittedByBranchProduct.GetValueOrDefault((branch.BranchId, product.Id.Value));
                // Commitment before expiry/sellable cap (do not subtract after the cap).
                var operationalAvailable = Math.Max(0m, branchAvailable - srCommitted);
                var available = operationalAvailable;

                decimal sellable = operationalAvailable;
                decimal expired = 0m;
                decimal saleBlocked = 0m;
                expirationByProductBranch.TryGetValue((product.Id.Value, branch.BranchId), out var expPolicy);
                if (expPolicy.TracksExpiration
                    && lotsByProductBranch.TryGetValue((product.Id.Value, branch.BranchId), out var lots)
                    && lots.Count > 0)
                {
                    var salePolicy = await ResolveSalePolicyAsync(branch.BranchId, product.CategoryId?.Value)
                        .ConfigureAwait(false);
                    var buckets = InventoryLotFefo.ProjectSaleBuckets(
                        lots,
                        today,
                        salePolicy.StopSellingDaysBeforeExpiry);
                    sellable = buckets.Sellable;
                    expired = buckets.Expired;
                    saleBlocked = buckets.PolicyBlocked;
                    available = Math.Min(operationalAvailable, buckets.Sellable);
                }

                commitmentsByBranchProduct.TryGetValue((branch.BranchId, product.Id.Value), out var transit);
                reorderByBranchProduct.TryGetValue((branch.BranchId, product.Id.Value), out var reorder);
                var reorderLevel = reorder?.ReorderLevel ?? account.ReorderLevel;
                var isLow = reorderLevel is not null
                    && available > 0m
                    && available <= reorderLevel.Value;

                if (!MatchesState(
                        stockState,
                        available,
                        onHand,
                        reserved,
                        damaged,
                        hold,
                        pending,
                        expired,
                        saleBlocked,
                        isLow))
                {
                    continue;
                }

                rows.Add(new InventoryStockStatusRowDto(
                    product.Id.Value,
                    product.Name,
                    product.Sku,
                    product.CategoryId?.Value,
                    product.CategoryId is ProductCategoryId catId
                        ? categoryNameById.GetValueOrDefault(catId.Value)
                        : null,
                    UnitOfMeasures.ToCode(product.UnitOfMeasure),
                    branch.BranchId,
                    branch.BranchName,
                    branch.AreaId,
                    branch.AreaName,
                    onHand,
                    sellable,
                    reserved,
                    available,
                    damaged,
                    hold,
                    pending,
                    expired,
                    saleBlocked,
                    transit.In,
                    transit.Out,
                    srCommitted,
                    reorderLevel,
                    isLow));
            }
        }

        var ordered = rows
            .OrderBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.BranchName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var total = ordered.Count;
        var pageRows = ordered.Skip(Math.Max(0, skip)).Take(take).ToList();

        return ApplicationResult<InventoryStockStatusResultDto>.Success(
            new InventoryStockStatusResultDto(
                _clock.UtcNow,
                IsCurrentOnly: true,
                total,
                pageRows));
    }

    private InventoryStockStatusResultDto EmptyResult() =>
        new(_clock.UtcNow, IsCurrentOnly: true, 0, []);

    private static bool MatchesState(
        string state,
        decimal available,
        decimal onHand,
        decimal reserved,
        decimal damaged,
        decimal hold,
        decimal pending,
        decimal expired,
        decimal saleBlocked,
        bool isLow) =>
        state switch
        {
            InventoryStockStatusStates.All => true,
            InventoryStockStatusStates.Available => available > 0m,
            InventoryStockStatusStates.LowStock => isLow,
            InventoryStockStatusStates.OutOfStock => onHand <= 0m || available <= 0m,
            InventoryStockStatusStates.Reserved => reserved > 0m,
            InventoryStockStatusStates.Damaged => damaged > 0m,
            InventoryStockStatusStates.InspectionHold => hold > 0m,
            InventoryStockStatusStates.PendingReturn => pending > 0m,
            InventoryStockStatusStates.Expired => expired > 0m,
            InventoryStockStatusStates.SaleBlocked => saleBlocked > 0m,
            _ => true,
        };
}
