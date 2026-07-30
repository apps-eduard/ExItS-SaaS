using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryRepository : IInventoryRepository
{
    private readonly PosDbContext _db;

    public InventoryRepository(PosDbContext db) => _db = db;

    public async Task<InventoryAccount?> GetByProductIdAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryAccounts
            .FirstOrDefaultAsync(
                a => a.OrganizationId == organizationId.Value && a.ProductId == productId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : InventoryEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var ids = productIds.Select(p => p.Value).ToList();
        var records = await _db.InventoryAccounts
            .Where(a => a.OrganizationId == organizationId.Value && ids.Contains(a.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryEntityMapper.ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        InventoryAccountFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        // Product-centric list: catalog products left-joined to optional inventory accounts.
        var products = _db.CatalogProducts.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value);

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

        var query =
            from p in products
            join a in _db.InventoryAccounts.AsNoTracking()
                on new { p.OrganizationId, ProductId = p.Id }
                equals new { a.OrganizationId, a.ProductId }
                into accountJoin
            from a in accountJoin.DefaultIfEmpty()
            select new { Product = p, Account = a };

        if (filter.TrackedOnly == true)
        {
            query = query.Where(x => x.Account != null && x.Account.IsTracked);
        }
        else if (filter.TrackedOnly == false)
        {
            query = query.Where(x => x.Account == null || !x.Account.IsTracked);
        }

        if (filter.LowStockOnly == true)
        {
            query = query.Where(x =>
                x.Account != null
                && x.Account.IsTracked
                && x.Account.ReorderLevel != null
                && x.Account.OnHandQuantity <= x.Account.ReorderLevel);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderBy(x => x.Product.Name)
            .ThenBy(x => x.Product.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<InventoryAccount>(rows.Count);
        foreach (var row in rows)
        {
            if (row.Account is not null)
            {
                items.Add(InventoryEntityMapper.ToDomain(row.Account));
            }
            else
            {
                // Synthetic untracked shell for products that have never been enabled.
                items.Add(InventoryAccount.Rehydrate(
                    InventoryAccountId.From(row.Product.Id),
                    PosOrganizationId.From(row.Product.OrganizationId),
                    CatalogProductId.From(row.Product.Id),
                    isTracked: false,
                    reorderLevel: null,
                    onHandQuantity: 0m,
                    row.Product.CreatedAtUtc,
                    row.Product.UpdatedAtUtc));
            }
        }

        return (items, total);
    }

    public async Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(
        PosOrganizationId organizationId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await ListAsync(
                organizationId,
                new InventoryAccountFilter(Search: search, TrackedOnly: true, LowStockOnly: true),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

    public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
    {
        _db.InventoryAccounts.Add(InventoryEntityMapper.ToRecord(account));
        return Task.CompletedTask;
    }

    public async Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
    {
        var record = await _db.InventoryAccounts
            .FirstOrDefaultAsync(
                a => a.Id == account.Id.Value && a.OrganizationId == account.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.InventoryAccountNotFound,
                "Inventory account was not found.");
        }

        InventoryEntityMapper.ApplyToRecord(account, record);
    }

    public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        _db.StockMovements.Add(InventoryEntityMapper.ToRecord(movement));
        return Task.CompletedTask;
    }

    public Task<bool> HasAnyMovementAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value && m.ProductId == productId.Value,
            cancellationToken);

    public Task<bool> HasOpeningStockAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.ProductId == productId.Value
                && m.MovementType == nameof(StockMovementType.OpeningStock),
            cancellationToken);

    public async Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.StockMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId.Value && m.ProductId == productId.Value);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(m => m.RecordedAtUtc)
            .ThenByDescending(m => m.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(InventoryEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.StockMovements
            .Where(m =>
                m.OrganizationId == organizationId.Value
                && m.SourceId == saleId.Value
                && m.SourceType == nameof(StockMovementSourceType.Sale)
                && m.MovementType == nameof(StockMovementType.SaleDeduction))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryEntityMapper.ToDomain).ToList();
    }

    public Task<bool> HasSaleDeductionAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == saleId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.Sale)
                && m.MovementType == nameof(StockMovementType.SaleDeduction),
            cancellationToken);

    public Task<bool> HasSaleVoidRestorationAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == saleId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.Sale)
                && m.MovementType == nameof(StockMovementType.SaleVoidRestoration),
            cancellationToken);

    public async Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.StockMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId.Value && m.ProductId == productId.Value);
        var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        if (count == 0)
        {
            return (null, 0);
        }

        var latest = await query.MaxAsync(m => m.RecordedAtUtc, cancellationToken).ConfigureAwait(false);
        return (latest, count);
    }

    public async Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, (DateTimeOffset? LatestAt, int Count)>();
        }

        var ids = productIds.Select(p => p.Value).ToList();
        var rows = await _db.StockMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId.Value && ids.Contains(m.ProductId))
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count(), LatestAt = (DateTimeOffset?)g.Max(x => x.RecordedAtUtc) })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(r => r.ProductId, r => (r.LatestAt, r.Count));
    }
}
