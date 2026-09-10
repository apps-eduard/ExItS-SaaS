using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class BranchInventoryQueryRepository : IBranchInventoryQueryRepository
{
    private readonly PosDbContext _db;

    public BranchInventoryQueryRepository(PosDbContext db) => _db = db;

    public async Task<(IReadOnlyList<BranchInventoryListRow> Items, int TotalCount)> ListAsync(
        BranchInventoryContext context,
        BranchInventoryListFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var (query, total) = await BuildFilteredListQueryAsync(context, filter, cancellationToken)
            .ConfigureAwait(false);

        var rows = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.ProductId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return ([], total);
        }

        var productIds = rows.Select(r => CatalogProductId.From(r.ProductId)).ToList();
        var summaries = await LoadMovementSummariesAsync(context.OrganizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var openingFlags = await LoadOpeningFlagsAsync(
                context.OrganizationId,
                productIds,
                context.BranchId,
                context.PrimaryBranchId,
                cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(row =>
        {
            summaries.TryGetValue(row.ProductId, out var summary);
            openingFlags.TryGetValue(row.ProductId, out var hasOpening);
            var isLow = row.IsTracked
                && row.ReorderLevel is not null
                && row.BranchOnHand > 0m
                && row.BranchOnHand <= row.ReorderLevel.Value;
            var isSuggested = row.IsTracked
                && InventoryStockStatuses.IsReorderSuggested(row.BranchOnHand, row.ReorderLevel);
            var suggested = row.IsTracked
                ? InventoryStockStatuses.SuggestedOrderQuantity(
                    row.BranchOnHand,
                    row.ReorderLevel,
                    row.ReorderQuantity)
                : null;

            return new BranchInventoryListRow(
                row.ProductId,
                row.OrganizationId,
                row.Name,
                row.UnitOfMeasure,
                row.ProductStatus,
                row.IsTracked,
                row.BranchOnHand,
                row.OrgOnHand,
                row.ReorderLevel,
                row.ReorderQuantity,
                isLow,
                isSuggested,
                suggested,
                summary.LatestAt,
                summary.Count,
                row.CreatedAtUtc,
                row.UpdatedAtUtc,
                row.TracksExpiration,
                row.ExpirationWarningDays,
                hasOpening,
                row.Sku,
                row.Barcode,
                row.CategoryId,
                row.CategoryName,
                row.MonitoringMode);
        }).ToList();

        return (items, total);
    }

    public async Task<(IReadOnlyList<Guid> ProductIds, int TotalCount)> ListProductIdsAsync(
        BranchInventoryContext context,
        BranchInventoryListFilter filter,
        int maxTake,
        CancellationToken cancellationToken = default)
    {
        var (query, total) = await BuildFilteredListQueryAsync(context, filter, cancellationToken)
            .ConfigureAwait(false);
        var ids = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.ProductId)
            .Select(x => x.ProductId)
            .Take(maxTake)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (ids, total);
    }

    private async Task<(IQueryable<BranchInventoryListQueryRow> Query, int TotalCount)> BuildFilteredListQueryAsync(
        BranchInventoryContext context,
        BranchInventoryListFilter filter,
        CancellationToken cancellationToken)
    {
        var orgId = context.OrganizationId;
        var branchId = context.BranchId;
        var primaryBranchId = context.PrimaryBranchId;
        var localScope = CatalogProductScopes.ToCode(CatalogProductScope.BranchLocal);

        var branchDefault = await _db.InventoryBranchReorderDefaults.AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.OrganizationId == orgId && d.BranchId == branchId,
                cancellationToken)
            .ConfigureAwait(false);
        var defaultLevel = branchDefault?.ReorderLevel;
        var defaultQuantity = branchDefault?.ReorderQuantity;
        var hasBranchDefault = defaultLevel is not null || defaultQuantity is not null;

        var products = _db.CatalogProducts.AsNoTracking()
            .Where(p => p.OrganizationId == orgId);

        if (!context.OrganizationGovernance)
        {
            products = products.Where(p => p.Scope != localScope || p.OriginBranchId == branchId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ProductStatus)
            && Enum.TryParse<CatalogProductStatus>(filter.ProductStatus.Trim(), ignoreCase: true, out var status))
        {
            var statusName = status.ToString();
            products = products.Where(p => p.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            products = products.Where(p =>
                p.Name.ToLower().Contains(term)
                || (p.Sku != null && p.Sku.ToLower().Contains(term))
                || (p.Barcode != null && p.Barcode.Contains(term)));
        }

        if (filter.CategoryId is Guid categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        var explicitBalances = _db.InventoryBranchBalances.AsNoTracking()
            .Where(b => b.OrganizationId == orgId && b.BranchId == branchId);

        var branchReorder = _db.InventoryBranchReorderSettings.AsNoTracking()
            .Where(r => r.OrganizationId == orgId && r.BranchId == branchId);

        var query =
            from p in products
            join a in _db.InventoryAccounts.AsNoTracking()
                on new { p.OrganizationId, ProductId = p.Id }
                equals new { a.OrganizationId, a.ProductId }
                into accountJoin
            from a in accountJoin.DefaultIfEmpty()
            join explicitBal in explicitBalances on p.Id equals explicitBal.ProductId into explicitJoin
            from explicitBal in explicitJoin.DefaultIfEmpty()
            join reorder in branchReorder on p.Id equals reorder.ProductId into reorderJoin
            from reorder in reorderJoin.DefaultIfEmpty()
            join cat in _db.ProductCategories.AsNoTracking() on p.CategoryId equals cat.Id into catJoin
            from cat in catJoin.DefaultIfEmpty()
            let orgOnHand = a != null ? a.OnHandQuantity : 0m
            let otherSum = _db.InventoryBranchBalances
                .Where(b => b.OrganizationId == orgId && b.BranchId != branchId && b.ProductId == p.Id)
                .Select(b => (decimal?)b.OnHandQuantity)
                .Sum() ?? 0m
            let unallocated = orgOnHand - otherSum < 0m ? 0m : orgOnHand - otherSum
            let branchOnHand = explicitBal != null
                ? explicitBal.OnHandQuantity
                : (primaryBranchId != null && primaryBranchId == branchId ? unallocated : 0m)
            let monitoringMode = reorder != null
                ? (reorder.ReorderLevel == null
                    ? InventoryReorderMonitoringModes.NotMonitored
                    : InventoryReorderMonitoringModes.Custom)
                : InventoryReorderMonitoringModes.BranchDefault
            let reorderLevel = reorder != null
                ? reorder.ReorderLevel
                : (hasBranchDefault
                    ? defaultLevel
                    : (primaryBranchId != null && primaryBranchId == branchId ? a.ReorderLevel : null))
            let reorderQuantity = reorder != null
                ? reorder.ReorderQuantity
                : (hasBranchDefault
                    ? defaultQuantity
                    : (primaryBranchId != null && primaryBranchId == branchId ? a.ReorderQuantity : null))
            let isTracked = a != null && a.IsTracked
            select new BranchInventoryListQueryRow(
                p.Id,
                p.OrganizationId,
                p.Name,
                p.UnitOfMeasure,
                p.Status,
                isTracked,
                branchOnHand,
                orgOnHand,
                reorderLevel,
                reorderQuantity,
                a != null ? a.CreatedAtUtc : p.CreatedAtUtc,
                a != null ? a.UpdatedAtUtc : p.UpdatedAtUtc,
                p.TracksExpiration,
                p.ExpirationWarningDays,
                p.Sku,
                p.Barcode,
                p.CategoryId,
                cat != null ? cat.Name : null,
                monitoringMode);

        if (filter.TrackedOnly == true)
        {
            query = query.Where(x => x.IsTracked);
        }
        else if (filter.TrackedOnly == false)
        {
            query = query.Where(x => !x.IsTracked);
        }

        if (filter.LowStockOnly == true)
        {
            query = query.Where(x =>
                x.IsTracked
                && x.MonitoringMode != InventoryReorderMonitoringModes.NotMonitored
                && x.ReorderLevel != null
                && x.BranchOnHand > 0m
                && x.BranchOnHand <= x.ReorderLevel);
        }

        if (filter.ReorderSuggestedOnly == true)
        {
            query = query.Where(x =>
                x.IsTracked
                && x.MonitoringMode != InventoryReorderMonitoringModes.NotMonitored
                && x.ReorderLevel != null
                && x.ReorderQuantity != null
                && x.ReorderQuantity > 0m
                && x.BranchOnHand <= x.ReorderLevel);
        }

        if (!string.IsNullOrWhiteSpace(filter.MonitoringMode)
            && !string.Equals(filter.MonitoringMode, "All", StringComparison.OrdinalIgnoreCase))
        {
            var mode = filter.MonitoringMode.Trim();
            query = query.Where(x => x.MonitoringMode == mode);
        }

        if (!string.IsNullOrWhiteSpace(filter.StockStatus)
            && !string.Equals(filter.StockStatus, "All", StringComparison.OrdinalIgnoreCase))
        {
            var stockStatus = filter.StockStatus.Trim();
            if (string.Equals(stockStatus, nameof(InventoryStockStatus.OutOfStock), StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    x.IsTracked
                    && x.MonitoringMode != InventoryReorderMonitoringModes.NotMonitored
                    && x.BranchOnHand == 0m);
            }
            else if (string.Equals(stockStatus, nameof(InventoryStockStatus.LowStock), StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    x.IsTracked
                    && x.MonitoringMode != InventoryReorderMonitoringModes.NotMonitored
                    && x.ReorderLevel != null
                    && x.BranchOnHand > 0m
                    && x.BranchOnHand <= x.ReorderLevel);
            }
            else if (string.Equals(stockStatus, nameof(InventoryStockStatus.InStock), StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    x.IsTracked
                    && x.MonitoringMode != InventoryReorderMonitoringModes.NotMonitored
                    && x.BranchOnHand > 0m
                    && (x.ReorderLevel == null || x.BranchOnHand > x.ReorderLevel));
            }
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        return (query, total);
    }

    private sealed record BranchInventoryListQueryRow(
        Guid ProductId,
        Guid OrganizationId,
        string Name,
        string UnitOfMeasure,
        string ProductStatus,
        bool IsTracked,
        decimal BranchOnHand,
        decimal OrgOnHand,
        decimal? ReorderLevel,
        decimal? ReorderQuantity,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        bool TracksExpiration,
        int? ExpirationWarningDays,
        string? Sku,
        string? Barcode,
        Guid? CategoryId,
        string? CategoryName,
        string MonitoringMode);

    private async Task<Dictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> LoadMovementSummariesAsync(
        Guid organizationId,
        IReadOnlyList<CatalogProductId> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Select(p => p.Value).ToList();
        var grouped = await _db.StockMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId && ids.Contains(m.ProductId))
            .GroupBy(m => m.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                LatestAt = g.Max(m => m.RecordedAtUtc),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return grouped.ToDictionary(
            g => g.ProductId,
            g => ((DateTimeOffset?)g.LatestAt, g.Count));
    }

    private async Task<Dictionary<Guid, bool>> LoadOpeningFlagsAsync(
        Guid organizationId,
        IReadOnlyList<CatalogProductId> productIds,
        Guid branchId,
        Guid? primaryBranchId,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Select(p => p.Value).ToList();
        var isPrimary = primaryBranchId is not null && primaryBranchId.Value == branchId;
        var withOpening = await _db.StockMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId
                && ids.Contains(m.ProductId)
                && m.MovementType == nameof(StockMovementType.OpeningStock)
                && (m.BranchId == branchId || (isPrimary && m.BranchId == null)))
            .Select(m => m.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ids.ToDictionary(id => id, id => withOpening.Contains(id));
    }

    public async Task<(IReadOnlyList<ReplenishmentCatalogRow> Items, int TotalCount)> ListReplenishmentCatalogAsync(
        BranchInventoryContext retailContext,
        ReplenishmentCatalogFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var orgId = retailContext.OrganizationId;
        var branchId = retailContext.BranchId;
        var primaryBranchId = retailContext.PrimaryBranchId;
        var warehouseBranchId = filter.SupplyWarehouseBranchId;
        var localScope = CatalogProductScopes.ToCode(CatalogProductScope.BranchLocal);
        var activeStatus = nameof(CatalogProductStatus.Active);

        var products = _db.CatalogProducts.AsNoTracking()
            .Where(p => p.OrganizationId == orgId && p.Status == activeStatus);

        if (!retailContext.OrganizationGovernance)
        {
            products = products.Where(p => p.Scope != localScope || p.OriginBranchId == branchId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            products = products.Where(p =>
                p.Name.ToLower().Contains(term)
                || (p.Sku != null && p.Sku.ToLower().Contains(term))
                || (p.Barcode != null && p.Barcode.Contains(term)));
        }

        if (filter.CategoryId is Guid categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        var explicitBalances = _db.InventoryBranchBalances.AsNoTracking()
            .Where(b => b.OrganizationId == orgId && b.BranchId == branchId);

        var branchReorder = _db.InventoryBranchReorderSettings.AsNoTracking()
            .Where(r => r.OrganizationId == orgId && r.BranchId == branchId);

        var query =
            from p in products
            join a in _db.InventoryAccounts.AsNoTracking()
                on new { p.OrganizationId, ProductId = p.Id }
                equals new { a.OrganizationId, a.ProductId }
                into accountJoin
            from a in accountJoin.DefaultIfEmpty()
            join explicitBal in explicitBalances on p.Id equals explicitBal.ProductId into explicitJoin
            from explicitBal in explicitJoin.DefaultIfEmpty()
            join reorder in branchReorder on p.Id equals reorder.ProductId into reorderJoin
            from reorder in reorderJoin.DefaultIfEmpty()
            join cat in _db.ProductCategories.AsNoTracking() on p.CategoryId equals cat.Id into catJoin
            from cat in catJoin.DefaultIfEmpty()
            let orgOnHand = a != null ? a.OnHandQuantity : 0m
            let otherSum = _db.InventoryBranchBalances
                .Where(b => b.OrganizationId == orgId && b.BranchId != branchId && b.ProductId == p.Id)
                .Select(b => (decimal?)b.OnHandQuantity)
                .Sum() ?? 0m
            let unallocated = orgOnHand - otherSum < 0m ? 0m : orgOnHand - otherSum
            let branchOnHand = explicitBal != null
                ? explicitBal.OnHandQuantity
                : (primaryBranchId != null && primaryBranchId == branchId ? unallocated : 0m)
            let reorderLevel = reorder != null
                ? reorder.ReorderLevel
                : (primaryBranchId != null && primaryBranchId == branchId ? a.ReorderLevel : null)
            let isTracked = a != null && a.IsTracked
            where isTracked
            select new
            {
                ProductId = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Barcode = p.Barcode,
                CategoryId = p.CategoryId,
                CategoryName = cat != null ? cat.Name : null,
                UnitOfMeasure = p.UnitOfMeasure,
                SellingMode = p.SellingMode,
                BranchOnHand = branchOnHand,
                OrgOnHand = orgOnHand,
                ReorderLevel = reorderLevel,
                IsTracked = isTracked,
            };

        if (filter.StockFilter == ReplenishmentStockFilters.Low)
        {
            query = query.Where(x =>
                x.ReorderLevel != null
                && x.BranchOnHand > 0m
                && x.BranchOnHand <= x.ReorderLevel);
        }
        else if (filter.StockFilter == ReplenishmentStockFilters.Out)
        {
            query = query.Where(x => x.BranchOnHand <= 0m);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.ProductId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return ([], total);
        }

        var productIds = rows.Select(r => r.ProductId).ToList();
        // One query: all branch balances for page products (warehouse available + primary unallocated).
        var balances = await _db.InventoryBranchBalances.AsNoTracking()
            .Where(b => b.OrganizationId == orgId && productIds.Contains(b.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var balancesByProduct = balances
            .GroupBy(b => b.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = rows.Select(row =>
        {
            balancesByProduct.TryGetValue(row.ProductId, out var productBalances);
            productBalances ??= [];
            var explicitWarehouse = productBalances.FirstOrDefault(b => b.BranchId == warehouseBranchId);
            decimal warehouseOnHand;
            if (explicitWarehouse is not null)
            {
                warehouseOnHand = explicitWarehouse.OnHandQuantity;
            }
            else
            {
                var otherSum = productBalances
                    .Where(b => b.BranchId != warehouseBranchId)
                    .Sum(b => b.OnHandQuantity);
                var unallocated = Math.Max(0m, row.OrgOnHand - otherSum);
                warehouseOnHand = primaryBranchId is not null && primaryBranchId.Value == warehouseBranchId
                    ? unallocated
                    : 0m;
            }

            var warehouseReserved = explicitWarehouse?.ReservedQuantity ?? 0m;
            var warehouseAvailable = Math.Max(0m, warehouseOnHand - warehouseReserved);
            var isLow = row.IsTracked
                && row.ReorderLevel is not null
                && row.BranchOnHand > 0m
                && row.BranchOnHand <= row.ReorderLevel.Value;

            return new ReplenishmentCatalogRow(
                row.ProductId,
                row.Name,
                row.Sku,
                row.Barcode,
                row.CategoryId,
                row.CategoryName,
                row.UnitOfMeasure,
                row.BranchOnHand,
                warehouseAvailable,
                isLow,
                row.IsTracked,
                row.SellingMode);
        }).ToList();

        return (items, total);
    }
}
