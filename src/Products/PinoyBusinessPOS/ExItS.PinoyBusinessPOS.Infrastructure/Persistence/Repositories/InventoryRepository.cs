using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class InventoryRepository : IInventoryRepository
{
    /// <summary>
    /// Transaction-scoped advisory lock for customer-order reservation mutations on one
    /// organization+product pair. Distinct namespace from sale/PO/register sequence locks.
    /// </summary>
    private const string LockProductSql = "SELECT pg_advisory_xact_lock({0})";

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

        if (filter.ReorderSuggestedOnly == true)
        {
            query = query.Where(x =>
                x.Account != null
                && x.Account.IsTracked
                && x.Account.ReorderLevel != null
                && x.Account.ReorderQuantity != null
                && x.Account.ReorderQuantity > 0
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
                    reorderQuantity: null,
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

    public async Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.InventoryAccounts.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value)
            .OrderBy(a => a.ProductId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(InventoryEntityMapper.ToDomain).ToList();
    }

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

    public async Task ExecuteWithProductReservationLocksAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        var orderedIds = productIds
            .Select(p => p.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (_db.Database.CurrentTransaction is not null)
        {
            await RunLockedAsync(organizationId, orderedIds, action, cancellationToken).ConfigureAwait(false);
            return;
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                await RunLockedAsync(organizationId, orderedIds, action, cancellationToken).ConfigureAwait(false);
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.InventoryConcurrencyConflict,
                    "Inventory was modified concurrently. Reload and try again.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task RunLockedAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<Guid> orderedProductIds,
        Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        foreach (var productId in orderedProductIds)
        {
            await _db.Database
                .ExecuteSqlRawAsync(
                    LockProductSql,
                    [ProductReservationLockKey(organizationId, productId)],
                    cancellationToken)
                .ConfigureAwait(false);
        }

        IReadOnlyList<InventoryAccount> accounts = [];
        if (orderedProductIds.Count > 0)
        {
            var records = await _db.InventoryAccounts
                .Where(a => a.OrganizationId == organizationId.Value && orderedProductIds.Contains(a.ProductId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            accounts = records.Select(InventoryEntityMapper.ToDomain).ToList();
        }

        await action(accounts, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stable 64-bit advisory-lock key for one organization's inventory account for one product.
    /// </summary>
    private static long ProductReservationLockKey(PosOrganizationId organizationId, Guid productId)
    {
        Span<byte> bytes = stackalloc byte[32];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        productId.TryWriteBytes(bytes[16..]);
        unchecked
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            // Distinct namespace from sale / PO / customer-order number locks.
            return (long)(hash ^ 0x1A7E7E5E11EEDUL);
        }
    }

    public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        _db.StockMovements.Add(InventoryEntityMapper.ToRecord(movement));
        return Task.CompletedTask;
    }

    public async Task<StockMovement?> GetMovementByIdAsync(
        PosOrganizationId organizationId,
        StockMovementId movementId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.StockMovements.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.OrganizationId == organizationId.Value && m.Id == movementId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : InventoryEntityMapper.ToDomain(record);
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

    public async Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(
        PosOrganizationId organizationId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await ListAsync(
                organizationId,
                new InventoryAccountFilter(Search: search, TrackedOnly: true, ReorderSuggestedOnly: true),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

    public async Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        StockMovementFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.StockMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId.Value && m.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(filter.MovementType)
            && StockMovementTypes.TryParse(filter.MovementType, out var movementType))
        {
            var code = StockMovementTypes.ToCode(movementType);
            query = query.Where(m => m.MovementType == code);
        }

        if (!string.IsNullOrWhiteSpace(filter.SourceType)
            && StockMovementSourceTypes.TryParse(filter.SourceType, out var sourceType))
        {
            var code = StockMovementSourceTypes.ToCode(sourceType);
            query = query.Where(m => m.SourceType == code);
        }

        if (filter.FromDateUtc is not null)
        {
            var from = new DateTimeOffset(filter.FromDateUtc.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(m => m.RecordedAtUtc >= from);
        }

        if (filter.ToDateUtc is not null)
        {
            var exclusiveTo = new DateTimeOffset(
                filter.ToDateUtc.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(m => m.RecordedAtUtc < exclusiveTo);
        }

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

    public async Task<decimal> SumMovementEffectsAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var sum = await _db.StockMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId.Value && m.ProductId == productId.Value)
            .SumAsync(m => m.QuantityEffect, cancellationToken)
            .ConfigureAwait(false);
        return sum;
    }

    public Task<bool> HasStockCountVarianceAsync(
        PosOrganizationId organizationId,
        StockCountId stockCountId,
        CatalogProductId productId,
        StockMovementType movementType,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == stockCountId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.StockCount)
                && m.MovementType == StockMovementTypes.ToCode(movementType),
            cancellationToken);

    public async Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        CancellationToken cancellationToken = default)
    {
        var from = new DateTimeOffset(fromDateUtc.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var exclusiveTo = new DateTimeOffset(
            toDateUtc.AddDays(1).ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var records = await _db.StockMovements.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId.Value
                        && m.RecordedAtUtc >= from
                        && m.RecordedAtUtc < exclusiveTo)
            .OrderBy(m => m.RecordedAtUtc)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(InventoryEntityMapper.ToDomain).ToList();
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

    public Task<bool> HasCustomerOrderDeductionAsync(
        PosOrganizationId organizationId,
        CustomerOrderId orderId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == orderId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.CustomerOrder)
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

    public Task<bool> HasPurchaseReceiptAsync(
        PosOrganizationId organizationId,
        GoodsReceiptId goodsReceiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == goodsReceiptId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.PurchaseReceipt)
                && m.MovementType == nameof(StockMovementType.PurchaseReceipt),
            cancellationToken);

    public Task<bool> HasDirectPurchaseReceiptAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptId receiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == receiptId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.DirectPurchase)
                && m.MovementType == nameof(StockMovementType.DirectPurchaseReceipt),
            cancellationToken);

    public Task<bool> HasStockUseAsync(
        PosOrganizationId organizationId,
        StockUseId stockUseId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == stockUseId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.StockUse)
                && m.MovementType == nameof(StockMovementType.StockUse),
            cancellationToken);

    public Task<bool> HasStockUseVoidRestorationAsync(
        PosOrganizationId organizationId,
        StockUseId stockUseId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == stockUseId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.StockUse)
                && m.MovementType == nameof(StockMovementType.StockUseVoidRestoration),
            cancellationToken);

    public Task<bool> HasProductionMaterialConsumptionAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == productionRunId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.Production)
                && m.MovementType == nameof(StockMovementType.ProductionMaterialConsumption),
            cancellationToken);

    public Task<bool> HasProductionMaterialRestorationAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == productionRunId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.Production)
                && m.MovementType == nameof(StockMovementType.ProductionMaterialRestoration),
            cancellationToken);

    public Task<bool> HasProductionOutputAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == productionRunId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.Production)
                && m.MovementType == nameof(StockMovementType.ProductionOutput),
            cancellationToken);

    public Task<bool> HasProductionOutputReversalAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == productionRunId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.Production)
                && m.MovementType == nameof(StockMovementType.ProductionOutputReversal),
            cancellationToken);

    public async Task<decimal?> GetLatestAcquisitionUnitCostAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var opening = nameof(StockMovementType.OpeningStock);
        var purchase = nameof(StockMovementType.PurchaseReceipt);
        var direct = nameof(StockMovementType.DirectPurchaseReceipt);
        var productionOutput = nameof(StockMovementType.ProductionOutput);
        return await _db.StockMovements.AsNoTracking()
            .Where(m =>
                m.OrganizationId == organizationId.Value
                && m.ProductId == productId.Value
                && m.UnitCost != null
                && (m.MovementType == opening
                    || m.MovementType == purchase
                    || m.MovementType == direct
                    || m.MovementType == productionOutput))
            .OrderByDescending(m => m.RecordedAtUtc)
            .Select(m => m.UnitCost)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> HasSaleReturnRestockAsync(
        PosOrganizationId organizationId,
        SaleReturnId saleReturnId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.SourceId == saleReturnId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.SaleReturn)
                && m.MovementType == nameof(StockMovementType.SaleReturnRestock),
            cancellationToken);

    public Task<bool> HasInventoryTransferMovementAsync(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        CatalogProductId productId,
        StockMovementType movementType,
        InventoryLotId? lotId = null,
        CancellationToken cancellationToken = default)
    {
        var type = StockMovementTypes.ToCode(movementType);
        var lot = lotId?.Value;
        return _db.StockMovements.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId.Value
                && m.ProductId == productId.Value
                && m.SourceType == nameof(StockMovementSourceType.InventoryTransfer)
                && m.SourceId == transferId.Value
                && m.MovementType == type
                && m.InventoryLotId == lot,
            cancellationToken);
    }

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
