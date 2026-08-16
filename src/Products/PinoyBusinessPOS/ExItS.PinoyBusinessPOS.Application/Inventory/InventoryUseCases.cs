using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed class InventoryQueryService
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryLotRepository _lots;
    private readonly IClock _clock;

    public InventoryQueryService(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryLotRepository lots,
        IClock clock)
    {
        _inventory = inventory;
        _products = products;
        _lots = lots;
        _clock = clock;
    }

    public async Task<PosInventoryAccountDto?> GetByProductIdAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return null;
        }

        var account = await _inventory
            .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        var summary = await _inventory
            .GetMovementSummaryAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);

        decimal? sellable = null;
        decimal? expired = null;
        decimal? near = null;
        if (product.TracksExpiration)
        {
            var today = InventoryLot.BusinessDateOf(_clock.UtcNow);
            var lots = await _lots
                .ListOnHandAsync(orgId, catalogProductId, branchId: null, includeDepleted: false, cancellationToken)
                .ConfigureAwait(false);
            var warning = product.EffectiveExpirationWarningDays;
            sellable = InventoryLotFefo.SellableQuantity(lots, today);
            expired = InventoryLotFefo.ExpiredQuantity(lots, today);
            near = InventoryLotFefo.NearExpiryQuantity(lots, today, warning);
        }

        return Map(product, account, summary.LatestAt, summary.Count, sellable, expired, near);
    }

    public async Task<PagedResult<PosInventoryAccountDto>> ListAsync(
        Guid organizationId,
        InventoryAccountFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var orgId = PosOrganizationId.From(organizationId);
        var (accounts, total) = await _inventory
            .ListAsync(orgId, filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return await MapPageAsync(orgId, accounts, total, page, take, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<PosInventoryAccountDto>> ListLowStockAsync(
        Guid organizationId,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var orgId = PosOrganizationId.From(organizationId);
        var (accounts, total) = await _inventory
            .ListLowStockAsync(orgId, search, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return await MapPageAsync(orgId, accounts, total, page, take, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<PosInventoryAccountDto>> ListReorderSuggestionsAsync(
        Guid organizationId,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var orgId = PosOrganizationId.From(organizationId);
        var (accounts, total) = await _inventory
            .ListReorderSuggestionsAsync(orgId, search, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return await MapPageAsync(orgId, accounts, total, page, take, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<PosStockMovementDto>> ListMovementsAsync(
        Guid organizationId,
        Guid productId,
        StockMovementFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);

        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return new PagedResult<PosStockMovementDto>([], 0, Math.Max(page ?? 1, 1), take);
        }

        var (items, total) = await _inventory
            .ListMovementsAsync(orgId, catalogProductId, filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosStockMovementDto>(
            items.Select(MapMovement).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    private async Task<PagedResult<PosInventoryAccountDto>> MapPageAsync(
        PosOrganizationId orgId,
        IReadOnlyList<InventoryAccount> accounts,
        int total,
        int? page,
        int take,
        CancellationToken cancellationToken)
    {
        if (accounts.Count == 0)
        {
            return new PagedResult<PosInventoryAccountDto>([], total, Math.Max(page ?? 1, 1), take);
        }

        var productIds = accounts.Select(a => a.ProductId).ToList();
        var products = await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
        var byId = products.ToDictionary(p => p.Id.Value);
        var summaries = await _inventory
            .GetMovementSummariesAsync(orgId, productIds, cancellationToken)
            .ConfigureAwait(false);

        var dtos = new List<PosInventoryAccountDto>(accounts.Count);
        foreach (var account in accounts)
        {
            if (!byId.TryGetValue(account.ProductId.Value, out var product))
            {
                continue;
            }

            summaries.TryGetValue(account.ProductId.Value, out var summary);
            dtos.Add(Map(product, account, summary.LatestAt, summary.Count));
        }

        return new PagedResult<PosInventoryAccountDto>(dtos, total, Math.Max(page ?? 1, 1), take);
    }

    public static PosInventoryAccountDto Map(
        CatalogProduct product,
        InventoryAccount? account,
        DateTimeOffset? latestMovementAtUtc,
        int movementCount,
        decimal? sellableQuantity = null,
        decimal? expiredQuantity = null,
        decimal? nearExpiryQuantity = null)
    {
        var isTracked = account?.IsTracked ?? false;
        var onHand = account?.OnHandQuantity ?? 0m;
        var reorder = account?.ReorderLevel;
        var reorderQty = account?.ReorderQuantity;
        var isLow = account?.IsLowStock ?? false;
        var isReorderSuggested = account?.IsReorderSuggested ?? false;
        var suggestedQty = account?.SuggestedOrderQuantity;
        var stockStatus = account is null
            ? InventoryStockStatuses.ToCode(InventoryStockStatus.InStock)
            : InventoryStockStatuses.ToCode(account.StockStatus);

        return new PosInventoryAccountDto(
            product.Id.Value,
            product.OrganizationId.Value,
            product.Name,
            UnitOfMeasures.ToCode(product.UnitOfMeasure),
            product.Status.ToString(),
            isTracked,
            onHand,
            reorder,
            reorderQty,
            stockStatus,
            isLow,
            isReorderSuggested,
            suggestedQty,
            latestMovementAtUtc,
            movementCount,
            account?.CreatedAtUtc ?? product.CreatedAtUtc,
            account?.UpdatedAtUtc ?? product.UpdatedAtUtc,
            product.TracksExpiration,
            product.ExpirationWarningDays,
            sellableQuantity,
            expiredQuantity,
            nearExpiryQuantity);
    }

    public static PosStockMovementDto MapMovement(StockMovement movement) =>
        new(
            movement.Id.Value,
            movement.ProductId.Value,
            movement.InventoryAccountId.Value,
            StockMovementTypes.ToCode(movement.MovementType),
            movement.QuantityEffect,
            movement.Reason,
            StockMovementSourceTypes.ToCode(movement.SourceType),
            movement.SourceId,
            movement.RecordedAtUtc,
            movement.RecordedBy);
}

public sealed class EnableInventoryTracking
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly InventoryLotStockService _lots;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EnableInventoryTracking(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        InventoryLotStockService lots,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _inventory = inventory;
        _products = products;
        _lots = lots;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryAccount>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        Guid actorId,
        decimal? openingQuantity = null,
        decimal? reorderLevel = null,
        DateOnly? expirationDate = null,
        string? lotNumber = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to enable inventory tracking.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        try
        {
            var account = await _inventory
                .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
                .ConfigureAwait(false);
            var utcNow = _clock.UtcNow;
            var created = false;
            if (account is null)
            {
                account = InventoryAccount.CreateUntracked(orgId, catalogProductId, utcNow);
                created = true;
            }

            var hadOpening = await _inventory
                .HasOpeningStockAsync(orgId, catalogProductId, cancellationToken)
                .ConfigureAwait(false);
            var opening = account.Enable(
                openingQuantity,
                product.UnitOfMeasure,
                actorId,
                utcNow,
                hadOpening,
                product.SellingMode);

            if (reorderLevel is not null)
            {
                account.SetReorderLevel(reorderLevel, product.UnitOfMeasure, utcNow);
            }

            if (created)
            {
                await _inventory.AddAccountAsync(account, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            }

            if (opening is not null)
            {
                if (product.TracksExpiration)
                {
                    if (expirationDate is null)
                    {
                        return ApplicationResult<InventoryAccount>.Failure(
                            DomainErrorCodes.InventoryExpirationRequired,
                            "Expiration date is required for opening stock on expiration-tracked products.");
                    }

                    var lot = await _lots
                        .ReceiveAsync(
                            orgId,
                            catalogProductId,
                            expirationDate.Value,
                            opening.QuantityEffect,
                            actorId,
                            utcNow,
                            StockMovementType.OpeningStock,
                            StockMovementSourceType.Opening,
                            lotNumber: lotNumber,
                            stockMovementId: opening.Id.Value,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    opening = opening.WithLot(lot.Id);
                }

                await _inventory.AddMovementAsync(opening, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<InventoryAccount>.Success(account);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryAccount>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryAccount>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DisableInventoryTracking
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DisableInventoryTracking(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _inventory = inventory;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryAccount>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        var account = await _inventory
            .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        if (account is null)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryAccountNotFound,
                "Inventory account was not found.");
        }

        try
        {
            account.Disable(_clock.UtcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<InventoryAccount>.Success(account);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryAccount>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryAccount>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AdjustInventoryStock
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly IInventoryLotRepository _lotRepository;
    private readonly InventoryLotStockService _lots;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AdjustInventoryStock(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IInventoryBranchBalanceRepository branchBalances,
        IInventoryLotRepository lotRepository,
        InventoryLotStockService lots,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _inventory = inventory;
        _products = products;
        _units = units;
        _branchBalances = branchBalances;
        _lotRepository = lotRepository;
        _lots = lots;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryAccount>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        string direction,
        decimal quantity,
        string reason,
        Guid actorId,
        decimal? reorderLevel = null,
        Guid? branchId = null,
        DateOnly? expirationDate = null,
        string? lotNumber = null,
        Guid? lotId = null,
        Guid? productUnitId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to adjust stock.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        var account = await _inventory
            .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        if (account is null || !account.IsTracked)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                DomainErrorCodes.InventoryNotTracked,
                "Inventory is not tracked for this product.");
        }

        try
        {
            var utcNow = _clock.UtcNow;
            var baseQuantity = quantity;
            if (productUnitId is not null)
            {
                var unit = await _units
                    .GetByIdAsync(orgId, ProductUnitId.From(productUnitId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (unit is null || unit.ProductId != catalogProductId || !unit.IsActive)
                {
                    return ApplicationResult<InventoryAccount>.Failure(
                        DomainErrorCodes.InvalidProductUnitId,
                        "Product unit was not found for this product.");
                }

                baseQuantity = ProductUnitConversion.ToBaseQuantity(quantity, unit.MultiplierToBase);
            }

            var normalizedDirection = (direction ?? string.Empty).Trim();
            StockMovement movement;
            if (string.Equals(normalizedDirection, "In", StringComparison.OrdinalIgnoreCase))
            {
                movement = StockMovement.ManualIncrease(
                    orgId,
                    catalogProductId,
                    account.Id,
                    baseQuantity,
                    product.UnitOfMeasure,
                    reason,
                    actorId,
                    utcNow,
                    sellingMode: product.SellingMode);
            }
            else if (string.Equals(normalizedDirection, "Out", StringComparison.OrdinalIgnoreCase))
            {
                movement = StockMovement.ManualDecrease(
                    orgId,
                    catalogProductId,
                    account.Id,
                    baseQuantity,
                    product.UnitOfMeasure,
                    reason,
                    actorId,
                    utcNow,
                    sellingMode: product.SellingMode);
            }
            else
            {
                return ApplicationResult<InventoryAccount>.Failure(
                    DomainErrorCodes.InvalidInventoryMovementType,
                    "Adjustment direction must be In or Out.");
            }

            account.ApplyMovementEffect(movement.QuantityEffect);
            account.Touch(utcNow);

            if (reorderLevel is not null)
            {
                account.SetReorderLevel(reorderLevel, product.UnitOfMeasure, utcNow);
            }

            PosBranchId? branch = branchId is Guid locationId && locationId != Guid.Empty
                ? PosBranchId.From(locationId)
                : null;
            if (branch is not null)
            {
                var balance = await _branchBalances
                    .GetAsync(orgId, branch, catalogProductId, cancellationToken)
                    .ConfigureAwait(false)
                    ?? InventoryBranchBalance.Create(orgId, branch, catalogProductId, 0m, utcNow);
                balance.Apply(movement.QuantityEffect, utcNow);
                await _branchBalances.UpsertAsync(balance, cancellationToken).ConfigureAwait(false);
            }

            if (product.TracksExpiration)
            {
                if (string.Equals(normalizedDirection, "In", StringComparison.OrdinalIgnoreCase))
                {
                    if (expirationDate is null)
                    {
                        return ApplicationResult<InventoryAccount>.Failure(
                            DomainErrorCodes.InventoryExpirationRequired,
                            "Expiration date is required when receiving expiration-tracked stock.");
                    }

                    var received = await _lots
                        .ReceiveAsync(
                            orgId,
                            catalogProductId,
                            expirationDate.Value,
                            quantity,
                            actorId,
                            utcNow,
                            movement.MovementType,
                            StockMovementSourceType.Manual,
                            branch,
                            lotNumber,
                            stockMovementId: movement.Id.Value,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    movement = movement.WithLot(received.Id);
                }
                else
                {
                    InventoryLot target;
                    if (lotId is Guid specifiedLot)
                    {
                        var found = await _lotRepository
                            .GetByIdAsync(orgId, InventoryLotId.From(specifiedLot), cancellationToken)
                            .ConfigureAwait(false);
                        if (found is null || found.ProductId != catalogProductId)
                        {
                            return ApplicationResult<InventoryAccount>.Failure(
                                DomainErrorCodes.InventoryLotMismatch,
                                "Lot does not belong to this product.");
                        }

                        target = found;
                    }
                    else if (expirationDate is DateOnly expiry)
                    {
                        var (_, normalizedLot) = InventoryLot.NormalizeLotNumber(lotNumber);
                        var found = await _lotRepository
                            .FindAsync(orgId, catalogProductId, expiry, normalizedLot, branch, cancellationToken)
                            .ConfigureAwait(false);
                        if (found is null)
                        {
                            return ApplicationResult<InventoryAccount>.Failure(
                                DomainErrorCodes.InventoryLotMismatch,
                                "Lot was not found for this product.");
                        }

                        target = found;
                    }
                    else
                    {
                        return ApplicationResult<InventoryAccount>.Failure(
                            DomainErrorCodes.InventoryLotMismatch,
                            "A lot is required when decreasing expiration-tracked stock.");
                    }

                    await _lots
                        .ConsumeSpecificAsync(
                            orgId,
                            target,
                            quantity,
                            actorId,
                            utcNow,
                            movement.MovementType,
                            StockMovementSourceType.Manual,
                            stockMovementId: movement.Id.Value,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    movement = movement.WithLot(target.Id);
                }
            }

            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<InventoryAccount>.Success(account);
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode == DomainErrorCodes.InventoryInsufficientStock
                ? ApplicationErrorCodes.InsufficientStock
                : ex.ErrorCode;
            return ApplicationResult<InventoryAccount>.Failure(code, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryAccount>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Checkout/void stock hooks. Deduction is part of authorized sale checkout — not a separate
/// client inventory grant. Online-only; no offline inventory queue.
/// </summary>
public interface ISaleStockService
{
    Task EnsureAvailableForSaleAsync(
        PosOrganizationId organizationId,
        Sale sale,
        CancellationToken cancellationToken = default);

    Task ReserveForAwaitingPaymentAsync(
        Sale sale,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task ReleaseIfReservedAsync(
        Sale sale,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task ConsumeReservedForPaidAsync(
        Sale sale,
        IReadOnlyDictionary<Guid, CatalogProduct> productsById,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task DeductForSaleAsync(
        PosOrganizationId organizationId,
        Sale sale,
        IReadOnlyDictionary<Guid, CatalogProduct> productsById,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task RestoreForSaleVoidAsync(
        PosOrganizationId organizationId,
        Sale sale,
        Guid actorId,
        string reason,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}

public sealed class SaleStockService : ISaleStockService
{
    private readonly IInventoryRepository _inventory;
    private readonly InventoryLotStockService _lots;

    public SaleStockService(IInventoryRepository inventory, InventoryLotStockService lots)
    {
        _inventory = inventory;
        _lots = lots;
    }

    public async Task EnsureAvailableForSaleAsync(
        PosOrganizationId organizationId,
        Sale sale,
        CancellationToken cancellationToken = default)
    {
        var productIds = sale.Lines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var byProduct = accounts.ToDictionary(a => a.ProductId.Value);

        foreach (var group in sale.Lines.GroupBy(l => l.ProductId.Value))
        {
            if (!byProduct.TryGetValue(group.Key, out var account) || !account.IsTracked)
            {
                continue;
            }

            var needed = group.Sum(l => l.Quantity);
            if (account.AvailableQuantity < needed)
            {
                throw new DomainException(
                    ApplicationErrorCodes.InsufficientStock,
                    "Insufficient available stock for one or more sale lines.");
            }
        }
    }

    public async Task ReserveForAwaitingPaymentAsync(
        Sale sale,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        _ = actorId;
        if (sale.StockReservationState == SaleStockReservationState.Reserved)
        {
            return;
        }

        if (sale.StockReservationState == SaleStockReservationState.Consumed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleStockReservation,
                "Cannot reserve stock for a sale that already consumed its reservation.");
        }

        var productIds = sale.Lines.Select(l => l.ProductId).Distinct().ToList();
        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                sale.OrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    foreach (var line in sale.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        account.Reserve(line.Quantity);
                        account.Touch(utcNow);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);

        sale.MarkStockReserved(utcNow);
    }

    public async Task ReleaseIfReservedAsync(
        Sale sale,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (sale.StockReservationState != SaleStockReservationState.Reserved)
        {
            return;
        }

        var productIds = sale.Lines.Select(l => l.ProductId).Distinct().ToList();
        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                sale.OrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    foreach (var line in sale.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        account.Release(line.Quantity);
                        account.Touch(utcNow);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);

        sale.MarkStockReleased(utcNow);
    }

    public async Task ConsumeReservedForPaidAsync(
        Sale sale,
        IReadOnlyDictionary<Guid, CatalogProduct> productsById,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (sale.StockReservationState == SaleStockReservationState.Consumed)
        {
            return;
        }

        if (sale.StockReservationState != SaleStockReservationState.Reserved)
        {
            return;
        }

        var productIds = sale.Lines.Select(l => l.ProductId).Distinct().ToList();
        await _inventory
            .ExecuteWithProductReservationLocksAsync(
                sale.OrganizationId,
                productIds,
                async (accounts, ct) =>
                {
                    var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
                    foreach (var line in sale.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        if (await _inventory
                                .HasSaleDeductionAsync(sale.OrganizationId, sale.Id, line.ProductId, ct)
                                .ConfigureAwait(false))
                        {
                            continue;
                        }

                        if (!productsById.TryGetValue(line.ProductId.Value, out var product))
                        {
                            throw new DomainException(
                                ApplicationErrorCodes.SaleProductNotFound,
                                "One or more products in the cart were not found in this organization.");
                        }

                        if (product.TracksExpiration)
                        {
                            // ConsumeFefo updates lots only; account on-hand is applied by DeductForSale.
                            // For reserved sales: release hold then reuse FEFO deduct path (avoids double on-hand).
                            account.Release(line.Quantity);
                            account.Touch(utcNow);
                            await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                            await DeductTrackedLineForSaleAsync(
                                    sale.OrganizationId,
                                    sale,
                                    line,
                                    product,
                                    account,
                                    actorId,
                                    utcNow,
                                    ct)
                                .ConfigureAwait(false);
                            continue;
                        }

                        account.ConsumeReservation(line.Quantity);
                        account.Touch(utcNow);
                        var movement = StockMovement.SaleDeduction(
                            sale.OrganizationId,
                            line.ProductId,
                            account.Id,
                            line.Quantity,
                            line.UnitOfMeasureSnapshot,
                            sale.Id.Value,
                            actorId,
                            utcNow,
                            sellingMode: line.SellingModeSnapshot);
                        // ConsumeReservation already applied on-hand effect — do not ApplyMovementEffect again.
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);

        sale.MarkStockConsumed(utcNow);
    }

    public async Task DeductForSaleAsync(
        PosOrganizationId organizationId,
        Sale sale,
        IReadOnlyDictionary<Guid, CatalogProduct> productsById,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var productIds = sale.Lines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

        foreach (var line in sale.Lines.OrderBy(l => l.LineNumber))
        {
            if (!accountsByProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
            {
                continue;
            }

            if (await _inventory
                    .HasSaleDeductionAsync(organizationId, sale.Id, line.ProductId, cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            if (!productsById.TryGetValue(line.ProductId.Value, out var product))
            {
                throw new DomainException(
                    ApplicationErrorCodes.SaleProductNotFound,
                    "One or more products in the cart were not found in this organization.");
            }

            await DeductTrackedLineForSaleAsync(
                    organizationId,
                    sale,
                    line,
                    product,
                    account,
                    actorId,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task DeductTrackedLineForSaleAsync(
        PosOrganizationId organizationId,
        Sale sale,
        SaleLine line,
        CatalogProduct product,
        InventoryAccount account,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (product.TracksExpiration)
        {
            var today = InventoryLot.BusinessDateOf(utcNow);
            try
            {
                await _lots
                    .ConsumeFefoAsync(
                        organizationId,
                        line.ProductId,
                        line.Quantity,
                        today,
                        actorId,
                        utcNow,
                        StockMovementType.SaleDeduction,
                        StockMovementSourceType.Sale,
                        sourceId: sale.Id.Value,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (DomainException)
            {
                throw new DomainException(
                    ApplicationErrorCodes.InsufficientStock,
                    $"Insufficient non-expired stock for '{product.Name}'. Required: {line.Quantity}.");
            }
        }
        else if (account.AvailableQuantity < line.Quantity)
        {
            throw new DomainException(
                ApplicationErrorCodes.InsufficientStock,
                $"Insufficient stock for '{product.Name}'. Available: {account.AvailableQuantity}, required: {line.Quantity}.");
        }

        var movement = StockMovement.SaleDeduction(
            organizationId,
            line.ProductId,
            account.Id,
            line.Quantity,
            line.UnitOfMeasureSnapshot,
            sale.Id.Value,
            actorId,
            utcNow,
            sellingMode: line.SellingModeSnapshot);
        account.ApplyMovementEffect(movement.QuantityEffect);
        account.Touch(utcNow);
        await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
        await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
    }

    public async Task RestoreForSaleVoidAsync(
        PosOrganizationId organizationId,
        Sale sale,
        Guid actorId,
        string reason,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var deductions = await _inventory
            .ListSaleDeductionsAsync(organizationId, sale.Id, cancellationToken)
            .ConfigureAwait(false);
        if (deductions.Count == 0)
        {
            return;
        }

        var productIds = deductions.Select(d => d.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

        foreach (var deduction in deductions)
        {
            if (await _inventory
                    .HasSaleVoidRestorationAsync(organizationId, sale.Id, deduction.ProductId, cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            if (!accountsByProduct.TryGetValue(deduction.ProductId.Value, out var account))
            {
                continue;
            }

            var absolute = Math.Abs(deduction.QuantityEffect);
            // UOM precision already validated on the original deduction; Piece-safe absolute restore.
            var unit = UnitOfMeasure.Piece;
            var sellingMode = SellingMode.PerItem;
            var line = sale.Lines.FirstOrDefault(l => l.ProductId == deduction.ProductId);
            if (line is not null)
            {
                unit = line.UnitOfMeasureSnapshot;
                sellingMode = line.SellingModeSnapshot;
            }

            var restoration = StockMovement.SaleVoidRestoration(
                organizationId,
                deduction.ProductId,
                account.Id,
                absolute,
                unit,
                sale.Id.Value,
                actorId,
                utcNow,
                reason,
                sellingMode: sellingMode);
            account.ApplyMovementEffect(restoration.QuantityEffect);
            account.Touch(utcNow);
            await _lots
                .RestoreSourceAsync(
                    organizationId,
                    sale.Id.Value,
                    StockMovementType.SaleDeduction,
                    StockMovementType.SaleVoidRestoration,
                    actorId,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(restoration, cancellationToken).ConfigureAwait(false);
        }
    }
}
