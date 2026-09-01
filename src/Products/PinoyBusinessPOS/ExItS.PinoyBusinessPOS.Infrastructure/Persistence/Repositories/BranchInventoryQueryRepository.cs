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
        var orgId = context.OrganizationId;
        var branchId = context.BranchId;
        var primaryBranchId = context.PrimaryBranchId;
        var localScope = CatalogProductScopes.ToCode(CatalogProductScope.BranchLocal);

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
            let orgOnHand = a != null ? a.OnHandQuantity : 0m
            let otherSum = _db.InventoryBranchBalances
                .Where(b => b.OrganizationId == orgId && b.BranchId != branchId && b.ProductId == p.Id)
                .Select(b => (decimal?)b.OnHandQuantity)
                .Sum() ?? 0m
            let unallocated = orgOnHand - otherSum < 0m ? 0m : orgOnHand - otherSum
            let branchOnHand = explicitBal != null
                ? explicitBal.OnHandQuantity
                : (primaryBranchId == null || primaryBranchId == branchId ? unallocated : 0m)
            let reorderLevel = reorder != null
                ? reorder.ReorderLevel
                : (primaryBranchId == null || primaryBranchId == branchId ? a.ReorderLevel : null)
            let reorderQuantity = reorder != null
                ? reorder.ReorderQuantity
                : (primaryBranchId == null || primaryBranchId == branchId ? a.ReorderQuantity : null)
            let isTracked = a != null && a.IsTracked
            select new
            {
                ProductId = p.Id,
                OrganizationId = p.OrganizationId,
                Name = p.Name,
                UnitOfMeasure = p.UnitOfMeasure,
                ProductStatus = p.Status,
                TracksExpiration = p.TracksExpiration,
                ExpirationWarningDays = p.ExpirationWarningDays,
                IsTracked = isTracked,
                BranchOnHand = branchOnHand,
                OrgOnHand = orgOnHand,
                ReorderLevel = reorderLevel,
                ReorderQuantity = reorderQuantity,
                CreatedAtUtc = a != null ? a.CreatedAtUtc : p.CreatedAtUtc,
                UpdatedAtUtc = a != null ? a.UpdatedAtUtc : p.UpdatedAtUtc,
            };

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
                && x.ReorderLevel != null
                && x.BranchOnHand > 0m
                && x.BranchOnHand <= x.ReorderLevel);
        }

        if (filter.ReorderSuggestedOnly == true)
        {
            query = query.Where(x =>
                x.IsTracked
                && x.ReorderLevel != null
                && x.ReorderQuantity != null
                && x.ReorderQuantity > 0m
                && x.BranchOnHand <= x.ReorderLevel);
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

        var productIds = rows.Select(r => CatalogProductId.From(r.ProductId)).ToList();
        var summaries = await LoadMovementSummariesAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
        var openingFlags = await LoadOpeningFlagsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);

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
                hasOpening);
        }).ToList();

        return (items, total);
    }

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
        CancellationToken cancellationToken)
    {
        var ids = productIds.Select(p => p.Value).ToList();
        var withOpening = await _db.StockMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId
                && ids.Contains(m.ProductId)
                && m.MovementType == nameof(StockMovementType.OpeningStock))
            .Select(m => m.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ids.ToDictionary(id => id, id => withOpening.Contains(id));
    }
}
