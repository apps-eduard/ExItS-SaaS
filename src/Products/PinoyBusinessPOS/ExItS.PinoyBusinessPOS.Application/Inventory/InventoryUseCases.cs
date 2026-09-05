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
    private readonly IBranchInventoryQueryRepository _branchInventory;
    private readonly BranchInventoryReadService _branchReads;
    private readonly BranchInventoryContextResolver _branchContext;
    private readonly IClock _clock;

    public InventoryQueryService(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryLotRepository lots,
        IBranchInventoryQueryRepository branchInventory,
        BranchInventoryReadService branchReads,
        BranchInventoryContextResolver branchContext,
        IClock clock)
    {
        _inventory = inventory;
        _products = products;
        _lots = lots;
        _branchInventory = branchInventory;
        _branchReads = branchReads;
        _branchContext = branchContext;
        _clock = clock;
    }

    public async Task<PosInventoryAccountDto?> GetByProductIdAsync(
        Guid organizationId,
        Guid productId,
        BranchInventoryContext context,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || !_branchContext.CanViewProductInManagement(product, context))
        {
            return null;
        }

        var account = await _inventory
            .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        var summary = await _inventory
            .GetMovementSummaryAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        var hasOpeningStock = await _inventory
            .HasOpeningStockForBranchAsync(
                orgId,
                catalogProductId,
                PosBranchId.From(context.BranchId),
                context.PrimaryBranchId is Guid primary
                    ? PosBranchId.From(primary)
                    : null,
                cancellationToken)
            .ConfigureAwait(false);

        decimal? sellable = null;
        decimal? expired = null;
        decimal? near = null;
        if (product.TracksExpiration)
        {
            var today = InventoryLot.BusinessDateOf(_clock.UtcNow);
            var branch = PosBranchId.From(context.BranchId);
            var lots = await _lots
                .ListOnHandAsync(orgId, catalogProductId, branch, includeDepleted: false, cancellationToken)
                .ConfigureAwait(false);
            if (context.PrimaryBranchId is not null
                && context.PrimaryBranchId.Value == context.BranchId)
            {
                var legacyLots = await _lots
                    .ListOrgLevelOnHandAsync(orgId, catalogProductId, includeDepleted: false, cancellationToken)
                    .ConfigureAwait(false);
                lots = InventoryLotCompatibility.UnionByLotId(lots, legacyLots);
            }

            var warning = product.EffectiveExpirationWarningDays;
            sellable = InventoryLotFefo.SellableQuantity(lots, today);
            expired = InventoryLotFefo.ExpiredQuantity(lots, today);
            near = InventoryLotFefo.NearExpiryQuantity(lots, today, warning);
        }

        var shell = account ?? InventoryAccount.Rehydrate(
            InventoryAccountId.From(product.Id.Value),
            orgId,
            catalogProductId,
            isTracked: false,
            reorderLevel: null,
            reorderQuantity: null,
            onHandQuantity: 0m,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);

        var branchRead = await _branchReads
            .ResolveSingleAsync(context, shell, cancellationToken)
            .ConfigureAwait(false);
        if (branchRead is null)
        {
            return null;
        }

        return Map(
            product,
            shell,
            summary.LatestAt,
            summary.Count,
            sellable,
            expired,
            near,
            hasOpeningStock,
            branchRead);
    }

    public async Task<PagedResult<PosInventoryAccountDto>> ListAsync(
        BranchInventoryContext context,
        InventoryAccountFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var branchFilter = new BranchInventoryListFilter(
            filter.Search,
            filter.TrackedOnly,
            filter.LowStockOnly,
            filter.ReorderSuggestedOnly,
            filter.ProductStatus);
        var (rows, total) = await _branchInventory
            .ListAsync(context, branchFilter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var dtos = rows.Select(MapFromBranchRow).ToList();
        return new PagedResult<PosInventoryAccountDto>(dtos, total, Math.Max(page ?? 1, 1), take);
    }

    public async Task<PagedResult<PosInventoryAccountDto>> ListLowStockAsync(
        BranchInventoryContext context,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (rows, total) = await _branchInventory
            .ListAsync(
                context,
                new BranchInventoryListFilter(Search: search, TrackedOnly: true, LowStockOnly: true),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosInventoryAccountDto>(
            rows.Select(MapFromBranchRow).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PagedResult<PosInventoryAccountDto>> ListReorderSuggestionsAsync(
        BranchInventoryContext context,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (rows, total) = await _branchInventory
            .ListAsync(
                context,
                new BranchInventoryListFilter(Search: search, TrackedOnly: true, ReorderSuggestedOnly: true),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosInventoryAccountDto>(
            rows.Select(MapFromBranchRow).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PagedResult<PosStockMovementDto>> ListMovementsAsync(
        Guid organizationId,
        Guid productId,
        BranchInventoryContext context,
        StockMovementFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);

        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || !_branchContext.CanViewProductInManagement(product, context))
        {
            return new PagedResult<PosStockMovementDto>([], 0, Math.Max(page ?? 1, 1), take);
        }

        var branchFilter = filter with
        {
            BranchId = context.BranchId,
            PrimaryBranchId = context.PrimaryBranchId
        };
        var (items, total) = await _inventory
            .ListMovementsAsync(orgId, catalogProductId, branchFilter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var lotById = await LoadMovementLotsAsync(orgId, items, cancellationToken).ConfigureAwait(false);

        return new PagedResult<PosStockMovementDto>(
            items.Select(m => MapMovement(m, ResolveMovementLot(m, lotById))).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PosStockMovementDto?> GetMovementByIdAsync(
        Guid organizationId,
        Guid movementId,
        BranchInventoryContext context,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var movement = await _inventory
            .GetMovementByIdAsync(orgId, StockMovementId.From(movementId), cancellationToken)
            .ConfigureAwait(false);
        if (movement is null)
        {
            return null;
        }

        if (!MovementBelongsToBranch(movement, context))
        {
            return null;
        }

        InventoryLot? lot = null;
        if (movement.InventoryLotId is InventoryLotId lotId)
        {
            lot = await _lots.GetByIdAsync(orgId, lotId, cancellationToken).ConfigureAwait(false);
        }

        return MapMovement(movement, lot);
    }

    private static bool MovementBelongsToBranch(StockMovement movement, BranchInventoryContext context)
    {
        if (movement.BranchId is null)
        {
            return context.PrimaryBranchId is not null
                && context.PrimaryBranchId.Value == context.BranchId;
        }

        return movement.BranchId.Value == context.BranchId;
    }

    private async Task<IReadOnlyDictionary<Guid, InventoryLot>> LoadMovementLotsAsync(
        PosOrganizationId orgId,
        IReadOnlyList<StockMovement> items,
        CancellationToken cancellationToken)
    {
        var lotIds = items
            .Where(m => m.InventoryLotId is not null)
            .Select(m => m.InventoryLotId!.Value)
            .Distinct()
            .ToList();
        if (lotIds.Count == 0)
        {
            return new Dictionary<Guid, InventoryLot>();
        }

        var lotById = new Dictionary<Guid, InventoryLot>(lotIds.Count);
        foreach (var lotId in lotIds)
        {
            var lot = await _lots
                .GetByIdAsync(orgId, InventoryLotId.From(lotId), cancellationToken)
                .ConfigureAwait(false);
            if (lot is not null)
            {
                lotById[lotId] = lot;
            }
        }

        return lotById;
    }

    private static InventoryLot? ResolveMovementLot(
        StockMovement movement,
        IReadOnlyDictionary<Guid, InventoryLot> lotById)
    {
        if (movement.InventoryLotId is not InventoryLotId lotId)
        {
            return null;
        }

        return lotById.TryGetValue(lotId.Value, out var lot) ? lot : null;
    }

    private static PosInventoryAccountDto MapFromBranchRow(BranchInventoryListRow row)
    {
        var stockStatus = row.IsTracked
            ? InventoryStockStatuses.ToCode(
                InventoryStockStatuses.Derive(row.IsTracked, row.BranchOnHand, row.ReorderLevel))
            : InventoryStockStatuses.ToCode(InventoryStockStatus.InStock);

        return new PosInventoryAccountDto(
            row.ProductId,
            row.OrganizationId,
            row.Name,
            row.UnitOfMeasure,
            row.ProductStatus,
            row.IsTracked,
            row.BranchOnHand,
            row.ReorderLevel,
            row.ReorderQuantity,
            stockStatus,
            row.IsLowStock,
            row.IsReorderSuggested,
            row.SuggestedOrderQuantity,
            row.LatestMovementAtUtc,
            row.MovementCount,
            row.CreatedAtUtc,
            row.UpdatedAtUtc,
            row.TracksExpiration,
            row.ExpirationWarningDays,
            null,
            null,
            null,
            row.HasOpeningStock,
            row.OrganizationOnHand);
    }

    public static PosInventoryAccountDto Map(
        CatalogProduct product,
        InventoryAccount? account,
        DateTimeOffset? latestMovementAtUtc,
        int movementCount,
        decimal? sellableQuantity = null,
        decimal? expiredQuantity = null,
        decimal? nearExpiryQuantity = null,
        bool hasOpeningStock = false,
        BranchInventoryProductRead? branchRead = null)
    {
        var isTracked = account?.IsTracked ?? false;
        var onHand = branchRead?.BranchOnHand ?? account?.OnHandQuantity ?? 0m;
        var reorder = branchRead?.ReorderLevel ?? account?.ReorderLevel;
        var reorderQty = branchRead?.ReorderQuantity ?? account?.ReorderQuantity;
        var isLow = branchRead?.IsLowStock ?? account?.IsLowStock ?? false;
        var isReorderSuggested = branchRead?.IsReorderSuggested ?? account?.IsReorderSuggested ?? false;
        var suggestedQty = branchRead?.SuggestedOrderQuantity ?? account?.SuggestedOrderQuantity;
        var stockStatus = isTracked
            ? InventoryStockStatuses.ToCode(InventoryStockStatuses.Derive(isTracked, onHand, reorder))
            : InventoryStockStatuses.ToCode(InventoryStockStatus.InStock);

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
            nearExpiryQuantity,
            hasOpeningStock,
            branchRead?.OrganizationOnHand ?? account?.OnHandQuantity);
    }

    public static PosStockMovementDto MapMovement(StockMovement movement, InventoryLot? lot = null)
    {
        decimal? stockValue = movement.UnitCost is { } cost
            ? SaleMoney.RoundMoney(cost * movement.QuantityEffect)
            : null;

        return new PosStockMovementDto(
            movement.Id.Value,
            movement.ProductId.Value,
            movement.InventoryAccountId.Value,
            StockMovementTypes.ToCode(movement.MovementType),
            movement.QuantityEffect,
            movement.Reason,
            StockMovementSourceTypes.ToCode(movement.SourceType),
            movement.SourceId,
            movement.RecordedAtUtc,
            movement.RecordedBy,
            lot?.ExpirationDate,
            lot?.LotNumber,
            movement.UnitCost,
            stockValue,
            movement.BranchId);
    }
}

public sealed class EnableInventoryTracking
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly BranchInventoryMutationService _branchMutations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public EnableInventoryTracking(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        BranchInventoryMutationService branchMutations,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _products = products;
        _branchBalances = branchBalances;
        _lots = lots;
        _branchMutations = branchMutations;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<InventoryAccount>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        Guid actorId,
        Guid branchId,
        decimal? openingQuantity = null,
        decimal? reorderLevel = null,
        DateOnly? expirationDate = null,
        string? lotNumber = null,
        decimal? unitCost = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to enable inventory tracking.");
        }

        if (branchId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A branch is required to enable inventory tracking.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var actingBranch = PosBranchId.From(branchId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        try
        {
            if (openingQuantity is > 0m && unitCost is null)
            {
                return ApplicationResult<InventoryAccount>.Failure(
                    DomainErrorCodes.InventoryOpeningUnitCostRequired,
                    "Unit cost is required when opening stock quantity is greater than zero.");
            }

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

            var primaryBranchGuid = _branches is null
                ? (Guid?)null
                : await _branches.GetPrimaryBranchIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
            var primaryBranch = primaryBranchGuid is Guid primary
                ? PosBranchId.From(primary)
                : null;
            var hadOpening = await _inventory
                .HasOpeningStockForBranchAsync(
                    orgId,
                    catalogProductId,
                    actingBranch,
                    primaryBranch,
                    cancellationToken)
                .ConfigureAwait(false);
            var orgOnHandBefore = account.OnHandQuantity;
            var opening = account.Enable(
                openingQuantity,
                product.UnitOfMeasure,
                actorId,
                utcNow,
                hadOpening,
                product.SellingMode,
                unitCost);

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
                opening = opening.WithBranch(actingBranch.Value);
                await _branchMutations
                    .ApplyBranchDeltaAsync(
                        _branchBalances,
                        _branches,
                        orgId,
                        actingBranch,
                        catalogProductId,
                        orgOnHandBefore,
                        opening.QuantityEffect,
                        utcNow,
                        cancellationToken)
                    .ConfigureAwait(false);

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
                            actingBranch,
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

public sealed class AddOpeningStock
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly BranchInventoryMutationService _branchMutations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public AddOpeningStock(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        BranchInventoryMutationService branchMutations,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _products = products;
        _branchBalances = branchBalances;
        _lots = lots;
        _branchMutations = branchMutations;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<InventoryAccount>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        Guid actorId,
        Guid branchId,
        decimal openingQuantity,
        decimal unitCost,
        DateOnly? expirationDate = null,
        string? lotNumber = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to add opening stock.");
        }

        if (branchId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A branch is required to add opening stock.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var actingBranch = PosBranchId.From(branchId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        try
        {
            if (openingQuantity <= 0m)
            {
                return ApplicationResult<InventoryAccount>.Failure(
                    DomainErrorCodes.InvalidInventoryQuantity,
                    "Opening stock quantity must be greater than zero.");
            }

            if (unitCost <= 0m)
            {
                return ApplicationResult<InventoryAccount>.Failure(
                    DomainErrorCodes.InvalidInventoryOpeningUnitCost,
                    "Unit purchase cost must be greater than zero.");
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

            var primaryBranchGuid = _branches is null
                ? (Guid?)null
                : await _branches.GetPrimaryBranchIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
            var primaryBranch = primaryBranchGuid is Guid primary
                ? PosBranchId.From(primary)
                : null;

            var hadOpening = await _inventory
                .HasOpeningStockForBranchAsync(
                    orgId,
                    catalogProductId,
                    actingBranch,
                    primaryBranch,
                    cancellationToken)
                .ConfigureAwait(false);
            if (hadOpening)
            {
                return ApplicationResult<InventoryAccount>.Failure(
                    DomainErrorCodes.InventoryOpeningDuplicate,
                    "Opening stock has already been recorded for this product at this location.");
            }

            var productBalances = await _branchBalances
                .ListByProductIdsAsync(orgId, [catalogProductId], cancellationToken)
                .ConfigureAwait(false);
            var branchOnHand = BranchStockResolver.ResolveOnHand(
                actingBranch,
                primaryBranchGuid,
                account.OnHandQuantity,
                productBalances,
                catalogProductId);
            var branchReserved = BranchStockResolver.ResolveReserved(
                actingBranch,
                productBalances,
                catalogProductId);
            if (branchOnHand != 0m || branchReserved != 0m)
            {
                return ApplicationResult<InventoryAccount>.Failure(
                    DomainErrorCodes.InventoryOpeningRequiresZeroOnHand,
                    "Opening stock can only be added when this location's on-hand and reserved quantities are zero.");
            }

            var utcNow = _clock.UtcNow;
            var orgOnHandBefore = account.OnHandQuantity;
            var opening = account.RecordOpeningStock(
                openingQuantity,
                product.UnitOfMeasure,
                actorId,
                utcNow,
                hasOpeningStockAlready: false,
                product.SellingMode,
                unitCost);
            opening = opening.WithBranch(actingBranch.Value);

            await _branchMutations
                .ApplyBranchDeltaAsync(
                    _branchBalances,
                    _branches,
                    orgId,
                    actingBranch,
                    catalogProductId,
                    orgOnHandBefore,
                    opening.QuantityEffect,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false);

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
                        actingBranch,
                        lotNumber: lotNumber,
                        stockMovementId: opening.Id.Value,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                opening = opening.WithLot(lot.Id);
            }

            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            await _inventory.AddMovementAsync(opening, cancellationToken).ConfigureAwait(false);
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
    private readonly IOrganizationBranchDirectory? _branches;

    public AdjustInventoryStock(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IInventoryBranchBalanceRepository branchBalances,
        IInventoryLotRepository lotRepository,
        InventoryLotStockService lots,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _products = products;
        _units = units;
        _branchBalances = branchBalances;
        _lotRepository = lotRepository;
        _lots = lots;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branches = branches;
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
        Guid? movementId = null,
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

        if (movementId is Guid clientMovementId && clientMovementId != Guid.Empty)
        {
            var existingMovement = await _inventory
                .GetMovementByIdAsync(orgId, StockMovementId.From(clientMovementId), cancellationToken)
                .ConfigureAwait(false);
            if (existingMovement is not null)
            {
                if (existingMovement.ProductId != catalogProductId)
                {
                    return ApplicationResult<InventoryAccount>.Failure(
                        ApplicationErrorCodes.DomainViolation,
                        "Movement identity does not match this product.");
                }

                var existingAccount = await _inventory
                    .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
                    .ConfigureAwait(false);
                if (existingAccount is null)
                {
                    return ApplicationResult<InventoryAccount>.Failure(
                        ApplicationErrorCodes.InventoryAccountNotFound,
                        "Inventory account was not found.");
                }

                return ApplicationResult<InventoryAccount>.Success(existingAccount);
            }
        }

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

            StockMovementId? clientId = movementId is Guid mid && mid != Guid.Empty
                ? StockMovementId.From(mid)
                : null;

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
                    id: clientId,
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
                    id: clientId,
                    sellingMode: product.SellingMode);
            }
            else
            {
                return ApplicationResult<InventoryAccount>.Failure(
                    DomainErrorCodes.InvalidInventoryMovementType,
                    "Adjustment direction must be In or Out.");
            }

            PosBranchId? branch = branchId is Guid locationId && locationId != Guid.Empty
                ? PosBranchId.From(locationId)
                : null;
            if (branch is null)
            {
                return ApplicationResult<InventoryAccount>.Failure(
                    ApplicationErrorCodes.InventoryBranchRequired,
                    "A branch is required to adjust stock.");
            }

            movement = movement.WithBranch(branch.Value);
            var orgOnHandBefore = account.OnHandQuantity;
            account.ApplyMovementEffect(movement.QuantityEffect);
            account.Touch(utcNow);

            if (reorderLevel is not null)
            {
                account.SetReorderLevel(reorderLevel, product.UnitOfMeasure, utcNow);
            }

            await BranchBalanceMutation
                    .ApplyAsync(
                        _branchBalances,
                        _branches,
                        orgId,
                        branch,
                        catalogProductId,
                        orgOnHandBefore,
                        movement.QuantityEffect,
                        utcNow,
                        cancellationToken)
                    .ConfigureAwait(false);

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
                    InventoryLot? target = null;
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

                        // Bound branch may only adjust lots for that branch (matches ListLots filter).
                        if (branch is not null && found.BranchId != branch)
                        {
                            return ApplicationResult<InventoryAccount>.Failure(
                                DomainErrorCodes.InventoryLotMismatch,
                                "Lot does not belong to this branch.");
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
                        var today = InventoryLot.BusinessDateOf(utcNow);
                        var allocations = await _lots
                            .ConsumeFefoAsync(
                                orgId,
                                catalogProductId,
                                quantity,
                                today,
                                actorId,
                                utcNow,
                                movement.MovementType,
                                StockMovementSourceType.Manual,
                                branch,
                                stockMovementId: movement.Id.Value,
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                        var primaryLot = allocations.FirstOrDefault()?.Lot;
                        if (primaryLot is not null)
                        {
                            movement = movement.WithLot(primaryLot.Id);
                        }
                    }

                    if (target is not null)
                    {
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
        CancellationToken cancellationToken = default,
        Guid? branchId = null);

    Task ReserveForAwaitingPaymentAsync(
        Sale sale,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        Guid? branchId = null);

    Task ReleaseIfReservedAsync(
        Sale sale,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        Guid? branchId = null);

    Task ConsumeReservedForPaidAsync(
        Sale sale,
        IReadOnlyDictionary<Guid, CatalogProduct> productsById,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        Guid? branchId = null);

    Task DeductForSaleAsync(
        PosOrganizationId organizationId,
        Sale sale,
        IReadOnlyDictionary<Guid, CatalogProduct> productsById,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        Guid? branchId = null);

    Task RestoreForSaleVoidAsync(
        PosOrganizationId organizationId,
        Sale sale,
        Guid actorId,
        string reason,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        Guid? branchId = null);
}

public sealed class SaleStockService : ISaleStockService
{
    private readonly IInventoryRepository _inventory;
    private readonly InventoryLotStockService _lots;
    private readonly IInventoryBranchBalanceRepository? _branchBalances;
    private readonly IOrganizationBranchDirectory? _branches;

    public SaleStockService(
        IInventoryRepository inventory,
        InventoryLotStockService lots,
        IInventoryBranchBalanceRepository? branchBalances = null,
        IOrganizationBranchDirectory? branches = null)
    {
        _inventory = inventory;
        _lots = lots;
        _branchBalances = branchBalances;
        _branches = branches;
    }

    public async Task EnsureAvailableForSaleAsync(
        PosOrganizationId organizationId,
        Sale sale,
        CancellationToken cancellationToken = default,
        Guid? branchId = null)
    {
        var productIds = sale.Lines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var byProduct = accounts.ToDictionary(a => a.ProductId.Value);
        var balances = await LoadBalancesAsync(organizationId, productIds, cancellationToken).ConfigureAwait(false);
        var primaryId = await ResolvePrimaryAsync(organizationId.Value, cancellationToken).ConfigureAwait(false);

        foreach (var group in sale.Lines.GroupBy(l => l.ProductId.Value))
        {
            if (!byProduct.TryGetValue(group.Key, out var account) || !account.IsTracked)
            {
                continue;
            }

            var needed = group.Sum(l => l.Quantity);
            var available = account.AvailableQuantity;
            if (branchId is Guid location && location != Guid.Empty)
            {
                var productId = CatalogProductId.From(group.Key);
                var onHand = BranchStockResolver.ResolveOnHand(
                    PosBranchId.From(location),
                    primaryId,
                    account.OnHandQuantity,
                    balances,
                    productId);
                var reserved = BranchStockResolver.ResolveReserved(
                    PosBranchId.From(location),
                    balances,
                    productId);
                available = BranchStockResolver.ResolveAvailable(onHand, reserved);
            }

            if (available < needed)
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
        CancellationToken cancellationToken = default,
        Guid? branchId = null)
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
                    var balances = branchId is Guid
                        ? (await LoadBalancesAsync(sale.OrganizationId, productIds, ct).ConfigureAwait(false)).ToList()
                        : [];
                    var primaryId = await ResolvePrimaryAsync(sale.OrganizationId.Value, ct).ConfigureAwait(false);
                    var physicalBranch = ResolveSalePhysicalBranch(sale, branchId);
                    foreach (var line in sale.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        if (physicalBranch is Guid location)
                        {
                            var onHand = BranchStockResolver.ResolveOnHand(
                                PosBranchId.From(location),
                                primaryId,
                                account.OnHandQuantity,
                                balances,
                                line.ProductId);
                            var reserved = BranchStockResolver.ResolveReserved(
                                PosBranchId.From(location),
                                balances,
                                line.ProductId);
                            if (BranchStockResolver.ResolveAvailable(onHand, reserved) < line.Quantity)
                            {
                                throw new DomainException(
                                    ApplicationErrorCodes.InsufficientStock,
                                    "Insufficient available stock for one or more sale lines.");
                            }
                        }

                        account.Reserve(line.Quantity);
                        account.Touch(utcNow);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await ApplyBranchReservationAsync(
                                sale.OrganizationId,
                                physicalBranch,
                                primaryId,
                                line.ProductId,
                                account.OnHandQuantity,
                                line.Quantity,
                                BranchReservationEffect.Reserve,
                                utcNow,
                                ct)
                            .ConfigureAwait(false);
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);

        sale.MarkStockReserved(utcNow);
    }

    public async Task ReleaseIfReservedAsync(
        Sale sale,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        Guid? branchId = null)
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
                    var primaryId = await ResolvePrimaryAsync(sale.OrganizationId.Value, ct).ConfigureAwait(false);
                    var physicalBranch = ResolveSalePhysicalBranch(sale, branchId);
                    foreach (var line in sale.Lines.OrderBy(l => l.LineNumber))
                    {
                        if (!byProduct.TryGetValue(line.ProductId.Value, out var account) || !account.IsTracked)
                        {
                            continue;
                        }

                        account.Release(line.Quantity);
                        account.Touch(utcNow);
                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await ApplyBranchReservationAsync(
                                sale.OrganizationId,
                                physicalBranch,
                                primaryId,
                                line.ProductId,
                                account.OnHandQuantity,
                                line.Quantity,
                                BranchReservationEffect.Release,
                                utcNow,
                                ct)
                            .ConfigureAwait(false);
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
        CancellationToken cancellationToken = default,
        Guid? branchId = null)
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
                    var primaryId = await ResolvePrimaryAsync(sale.OrganizationId.Value, ct).ConfigureAwait(false);
                    var physicalBranch = ResolveSalePhysicalBranch(sale, branchId);
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
                                    ct,
                                    physicalBranch,
                                    applyBranchOverlay: false,
                                    primaryBranchId: primaryId)
                                .ConfigureAwait(false);
                            await ApplyBranchReservationAsync(
                                    sale.OrganizationId,
                                    physicalBranch,
                                    primaryId,
                                    line.ProductId,
                                    account.OnHandQuantity + line.Quantity,
                                    line.Quantity,
                                    BranchReservationEffect.Consume,
                                    utcNow,
                                    ct)
                                .ConfigureAwait(false);
                            continue;
                        }

                        account.ConsumeReservation(line.Quantity);
                        account.Touch(utcNow);
                        var reservedMovement = StockMovement.SaleDeduction(
                            sale.OrganizationId,
                            line.ProductId,
                            account.Id,
                            line.Quantity,
                            line.UnitOfMeasureSnapshot,
                            sale.Id.Value,
                            actorId,
                            utcNow,
                            sellingMode: line.SellingModeSnapshot);
                        if (physicalBranch is Guid reservedBranch)
                        {
                            reservedMovement = reservedMovement.WithBranch(reservedBranch);
                        }

                        await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                        await _inventory.AddMovementAsync(reservedMovement, ct).ConfigureAwait(false);
                        await ApplyBranchReservationAsync(
                                sale.OrganizationId,
                                physicalBranch,
                                primaryId,
                                line.ProductId,
                                account.OnHandQuantity + line.Quantity,
                                line.Quantity,
                                BranchReservationEffect.Consume,
                                utcNow,
                                ct)
                            .ConfigureAwait(false);
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
        CancellationToken cancellationToken = default,
        Guid? branchId = null)
    {
        var productIds = sale.Lines.Select(l => l.ProductId).Distinct().ToList();
        var accounts = await _inventory
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);
        var physicalBranch = ResolveSalePhysicalBranch(sale, branchId);
        var primaryId = await ResolvePrimaryAsync(organizationId.Value, cancellationToken).ConfigureAwait(false);

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
                    cancellationToken,
                    physicalBranch,
                    applyBranchOverlay: true,
                    primaryBranchId: primaryId)
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
        CancellationToken cancellationToken,
        Guid? branchId = null,
        bool applyBranchOverlay = true,
        Guid? primaryBranchId = null)
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
                        branchId: branchId is Guid location && location != Guid.Empty
                            ? PosBranchId.From(location)
                            : null,
                        sourceId: sale.Id.Value,
                        cancellationToken: cancellationToken,
                        primaryBranchId: primaryBranchId)
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

        var physicalBranchId = ResolveSalePhysicalBranch(sale, branchId);
        var orgOnHandBefore = account.OnHandQuantity;
        var movement = StockMovement.SaleDeduction(
            organizationId,
            line.ProductId,
            account.Id,
            line.Quantity,
            line.UnitOfMeasureSnapshot,
            sale.Id.Value,
            actorId,
            utcNow,
            sellingMode: line.SellingModeSnapshot,
            unitCost: line.UnitCostSnapshot);
        if (physicalBranchId is Guid movementBranch && movementBranch != Guid.Empty)
        {
            movement = movement.WithBranch(movementBranch);
        }
        account.ApplyMovementEffect(movement.QuantityEffect);
        account.Touch(utcNow);
        await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
        await _inventory.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
        if (applyBranchOverlay)
        {
            var overlayPrimary = primaryBranchId
                ?? await ResolvePrimaryAsync(organizationId.Value, cancellationToken).ConfigureAwait(false);
            await ApplyBranchDeltaAsync(
                    organizationId,
                    physicalBranchId,
                    overlayPrimary,
                    line.ProductId,
                    orgOnHandBefore,
                    movement.QuantityEffect,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task RestoreForSaleVoidAsync(
        PosOrganizationId organizationId,
        Sale sale,
        Guid actorId,
        string reason,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        Guid? branchId = null)
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
            var voidPhysicalBranchId = ResolveSalePhysicalBranch(sale, branchId);
            if (voidPhysicalBranchId is Guid voidBranch)
            {
                restoration = restoration.WithBranch(voidBranch);
            }
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
            var voidPrimary = await ResolvePrimaryAsync(organizationId.Value, cancellationToken).ConfigureAwait(false);
            await ApplyBranchDeltaAsync(
                    organizationId,
                    voidPhysicalBranchId,
                    voidPrimary,
                    deduction.ProductId,
                    account.OnHandQuantity - restoration.QuantityEffect,
                    restoration.QuantityEffect,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static Guid? ResolveSalePhysicalBranch(Sale sale, Guid? branchId)
    {
        if (sale.BranchId is PosBranchId persisted && persisted.Value != Guid.Empty)
        {
            return persisted.Value;
        }

        if (branchId is Guid location && location != Guid.Empty)
        {
            return location;
        }

        return null;
    }

    private async Task ApplyBranchReservationAsync(
        PosOrganizationId organizationId,
        Guid? branchId,
        Guid? primaryBranchId,
        CatalogProductId productId,
        decimal organizationOnHandBefore,
        decimal quantity,
        BranchReservationEffect effect,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (_branchBalances is null || branchId is not Guid location || location == Guid.Empty || quantity == 0m)
        {
            return;
        }

        await BranchBalanceMutation
            .ApplyReservationAsync(
                _branchBalances,
                organizationId,
                PosBranchId.From(location),
                primaryBranchId,
                productId,
                organizationOnHandBefore,
                quantity,
                effect,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ApplyBranchDeltaAsync(
        PosOrganizationId organizationId,
        Guid? branchId,
        Guid? primaryBranchId,
        CatalogProductId productId,
        decimal organizationOnHandBeforeDelta,
        decimal signedQuantity,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (_branchBalances is null || branchId is not Guid location || location == Guid.Empty || signedQuantity == 0m)
        {
            return;
        }

        await BranchBalanceMutation
            .ApplyAsync(
                _branchBalances,
                organizationId,
                PosBranchId.From(location),
                primaryBranchId,
                productId,
                organizationOnHandBeforeDelta,
                signedQuantity,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<InventoryBranchBalance>> LoadBalancesAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CatalogProductId> productIds,
        CancellationToken cancellationToken)
    {
        if (_branchBalances is null || productIds.Count == 0)
        {
            return [];
        }

        return await _branchBalances
            .ListByProductIdsAsync(organizationId, productIds, cancellationToken)
            .ConfigureAwait(false);
    }

    private Guid? _resolvedPrimaryOrganization;
    private Guid? _resolvedPrimaryBranchId;
    private bool _primaryResolved;

    private async Task<Guid?> ResolvePrimaryAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (_primaryResolved && _resolvedPrimaryOrganization == organizationId)
        {
            return _resolvedPrimaryBranchId;
        }

        Guid? primary = null;
        if (_branches is not null)
        {
            primary = await _branches.GetPrimaryBranchIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        }

        _resolvedPrimaryOrganization = organizationId;
        _resolvedPrimaryBranchId = primary;
        _primaryResolved = true;
        return primary;
    }
}
