using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// STOCK_USE_COST_SOURCE=DEFERRED: UnitCostSnapshot / movement UnitCost is set only when a prior
/// acquisition cost (opening / PO receipt / direct purchase) is known. Never from SellingPrice.
/// Unknown cost remains null.
/// STOCK_USE_CORRECTION_MODEL=REVERSAL: void posts compensating StockUseVoidRestoration + lot restore.
/// </summary>
public sealed class StockUseQueryService
{
    private readonly IStockUseRepository _stockUses;

    public StockUseQueryService(IStockUseRepository stockUses) => _stockUses = stockUses;

    public async Task<StockUseDto?> GetByIdAsync(
        Guid organizationId,
        Guid stockUseId,
        CancellationToken cancellationToken = default)
    {
        var stockUse = await _stockUses
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                StockUseId.From(stockUseId),
                cancellationToken)
            .ConfigureAwait(false);
        return stockUse is null ? null : StockUseMapper.Map(stockUse);
    }

    public async Task<PagedResult<StockUseListItemDto>> ListAsync(
        Guid organizationId,
        StockUseFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _stockUses
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<StockUseListItemDto>(
            items.Select(StockUseMapper.MapListItem).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class CreateStockUse
{
    private readonly IStockUseRepository _stockUses;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public CreateStockUse(
        IStockUseRepository stockUses,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _stockUses = stockUses;
        _products = products;
        _units = units;
        _inventory = inventory;
        _branchBalances = branchBalances;
        _lots = lots;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<StockUseDto>> ExecuteAsync(
        Guid organizationId,
        CreateStockUseRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<StockUseDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a stock use.");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return ApplicationResult<StockUseDto>.Failure(
                DomainErrorCodes.StockUseRequiresLines,
                "At least one stock use line is required.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        if (request.StockUseId is Guid clientId && clientId != Guid.Empty)
                        {
                            var byId = await _stockUses
                                .GetByIdAsync(orgId, StockUseId.From(clientId), ct)
                                .ConfigureAwait(false);
                            if (byId is not null)
                            {
                                return ApplicationResult<StockUseDto>.Success(StockUseMapper.Map(byId));
                            }
                        }

                        if (idempotencyKey is not null)
                        {
                            var existing = await _stockUses
                                .FindByIdempotencyKeyAsync(orgId, idempotencyKey, ct)
                                .ConfigureAwait(false);
                            if (existing is not null)
                            {
                                return ApplicationResult<StockUseDto>.Success(StockUseMapper.Map(existing));
                            }
                        }

                        if (!StockUseReasons.TryParse(request.Reason, out var reason))
                        {
                            return ApplicationResult<StockUseDto>.Failure(
                                DomainErrorCodes.InvalidStockUseReason,
                                $"Stock use reason must be one of: {string.Join(", ", StockUseReasons.Codes)}.");
                        }

                        var mergedLines = MergeLines(request.Lines);
                        var productIds = mergedLines.Select(l => l.ProductId).Distinct().ToList();
                        var catalogIds = productIds.Select(CatalogProductId.From).ToList();
                        var products = await _products
                            .ListByIdsAsync(orgId, catalogIds, ct)
                            .ConfigureAwait(false);
                        var productsById = products.ToDictionary(p => p.Id.Value);

                        foreach (var productId in productIds)
                        {
                            if (!productsById.TryGetValue(productId, out var product))
                            {
                                return ApplicationResult<StockUseDto>.Failure(
                                    ApplicationErrorCodes.SaleProductNotFound,
                                    "One or more products were not found in this organization.");
                            }

                            if (product.Status != CatalogProductStatus.Active)
                            {
                                return ApplicationResult<StockUseDto>.Failure(
                                    ApplicationErrorCodes.SaleProductNotActive,
                                    "Only active catalog products can be consumed via stock use.");
                            }
                        }

                        var unitsById = new Dictionary<Guid, CatalogProductUnit>();
                        var unitIds = mergedLines
                            .Where(l => l.ProductUnitId is Guid uid && uid != Guid.Empty)
                            .Select(l => l.ProductUnitId!.Value)
                            .Distinct()
                            .ToList();
                        foreach (var unitId in unitIds)
                        {
                            var unit = await _units
                                .GetByIdAsync(orgId, ProductUnitId.From(unitId), ct)
                                .ConfigureAwait(false);
                            if (unit is null || !unit.IsActive)
                            {
                                return ApplicationResult<StockUseDto>.Failure(
                                    DomainErrorCodes.InvalidProductUnitId,
                                    "Product unit was not found for this product.");
                            }

                            unitsById[unitId] = unit;
                        }

                        ApplicationResult<StockUseDto>? failure = null;
                        await _inventory
                            .ExecuteWithProductReservationLocksAsync(
                                orgId,
                                catalogIds,
                                async (accounts, lockCt) =>
                                {
                                    var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);
                                    foreach (var productId in productIds)
                                    {
                                        if (!accountsByProduct.TryGetValue(productId, out var account)
                                            || !account.IsTracked)
                                        {
                                            failure = ApplicationResult<StockUseDto>.Failure(
                                                DomainErrorCodes.InventoryNotTracked,
                                                "Inventory must be tracked for all products on a stock use.");
                                            return;
                                        }
                                    }

                                    var drafts = new List<StockUseLineDraft>(mergedLines.Count);
                                    foreach (var line in mergedLines)
                                    {
                                        var product = productsById[line.ProductId];
                                        decimal multiplier = 1m;
                                        ProductUnitId? productUnitId = null;
                                        var unitLabel = UnitOfMeasures.ToCode(product.UnitOfMeasure);

                                        if (line.ProductUnitId is Guid uid && uid != Guid.Empty)
                                        {
                                            var unit = unitsById[uid];
                                            if (unit.ProductId != product.Id)
                                            {
                                                failure = ApplicationResult<StockUseDto>.Failure(
                                                    DomainErrorCodes.InvalidProductUnitId,
                                                    "Product unit does not belong to this product.");
                                                return;
                                            }

                                            multiplier = unit.MultiplierToBase;
                                            productUnitId = unit.Id;
                                            unitLabel = unit.ShortLabel;
                                        }

                                        var baseQty = ProductUnitConversion.ToBaseQuantity(line.Quantity, multiplier);
                                        var account = accountsByProduct[line.ProductId];
                                        if (!product.TracksExpiration && account.AvailableQuantity < baseQty)
                                        {
                                            failure = ApplicationResult<StockUseDto>.Failure(
                                                ApplicationErrorCodes.InsufficientStock,
                                                $"Insufficient stock for '{product.Name}'. Available: {account.AvailableQuantity}, required: {baseQty}.");
                                            return;
                                        }

                                        var unitCost = await _inventory
                                            .GetLatestAcquisitionUnitCostAsync(orgId, product.Id, lockCt)
                                            .ConfigureAwait(false);

                                        drafts.Add(new StockUseLineDraft(
                                            product.Id,
                                            line.Quantity,
                                            multiplier,
                                            product.Name,
                                            unitLabel,
                                            productUnitId,
                                            unitCost));
                                    }

                                    if (failure is not null)
                                    {
                                        return;
                                    }

                                    var utcNow = _clock.UtcNow;
                                    var businessDate = StockUseNumbers.BusinessDateOf(utcNow);
                                    var stockUseNumber = await _stockUses
                                        .AllocateNextNumberAsync(orgId, businessDate, lockCt)
                                        .ConfigureAwait(false);

                                    PosBranchId? branch = request.BranchId is Guid branchGuid && branchGuid != Guid.Empty
                                        ? PosBranchId.From(branchGuid)
                                        : null;

                                    StockUseId? clientStockUseId = request.StockUseId is Guid sid && sid != Guid.Empty
                                        ? StockUseId.From(sid)
                                        : null;

                                    var stockUse = StockUse.Create(
                                        orgId,
                                        stockUseNumber,
                                        reason,
                                        drafts,
                                        actorId,
                                        utcNow,
                                        request.OccurredAtUtc,
                                        branch,
                                        request.ReferenceNumber,
                                        request.Notes,
                                        idempotencyKey,
                                        clientStockUseId);

                                    foreach (var line in stockUse.Lines)
                                    {
                                        var account = accountsByProduct[line.ProductId.Value];
                                        if (await _inventory
                                                .HasStockUseAsync(orgId, stockUse.Id, line.ProductId, lockCt)
                                                .ConfigureAwait(false))
                                        {
                                            continue;
                                        }

                                        var product = productsById[line.ProductId.Value];
                                        if (product.TracksExpiration)
                                        {
                                            var today = InventoryLot.BusinessDateOf(utcNow);
                                            try
                                            {
                                                await _lots
                                                    .ConsumeFefoAsync(
                                                        orgId,
                                                        line.ProductId,
                                                        line.BaseQuantity,
                                                        today,
                                                        actorId,
                                                        utcNow,
                                                        StockMovementType.StockUse,
                                                        StockMovementSourceType.StockUse,
                                                        branch,
                                                        sourceId: stockUse.Id.Value,
                                                        cancellationToken: lockCt)
                                                    .ConfigureAwait(false);
                                            }
                                            catch (DomainException)
                                            {
                                                failure = ApplicationResult<StockUseDto>.Failure(
                                                    ApplicationErrorCodes.InsufficientStock,
                                                    $"Insufficient non-expired stock for '{product.Name}'. Required: {line.BaseQuantity}.");
                                                return;
                                            }
                                        }
                                        else if (account.AvailableQuantity < line.BaseQuantity)
                                        {
                                            failure = ApplicationResult<StockUseDto>.Failure(
                                                ApplicationErrorCodes.InsufficientStock,
                                                $"Insufficient stock for '{product.Name}'. Available: {account.AvailableQuantity}, required: {line.BaseQuantity}.");
                                            return;
                                        }

                                        var movement = StockMovement.StockUse(
                                            orgId,
                                            line.ProductId,
                                            account.Id,
                                            line.BaseQuantity,
                                            product.UnitOfMeasure,
                                            stockUse.Id.Value,
                                            actorId,
                                            utcNow,
                                            reason: $"{StockMovement.StockUseConsumptionReason}: {StockUseReasons.ToCode(reason)}",
                                            sellingMode: product.SellingMode,
                                            branchId: branch?.Value,
                                            unitCost: line.UnitCostSnapshot);

                                        line.AttachInventoryMovement(movement.Id);
                                        var orgOnHandBefore = account.OnHandQuantity;
                                        account.ApplyMovementEffect(movement.QuantityEffect);
                                        account.Touch(utcNow);
                                        await _inventory.UpdateAccountAsync(account, lockCt).ConfigureAwait(false);
                                        await _inventory.AddMovementAsync(movement, lockCt).ConfigureAwait(false);

                                        if (branch is not null)
                                        {
                                            await BranchBalanceMutation
                                                .ApplyAsync(
                                                    _branchBalances,
                                                    _branches,
                                                    orgId,
                                                    branch,
                                                    line.ProductId,
                                                    orgOnHandBefore,
                                                    movement.QuantityEffect,
                                                    utcNow,
                                                    lockCt)
                                                .ConfigureAwait(false);
                                        }
                                    }

                                    if (failure is not null)
                                    {
                                        return;
                                    }

                                    await _stockUses.AddAsync(stockUse, lockCt).ConfigureAwait(false);
                                    await _unitOfWork.SaveChangesAsync(lockCt).ConfigureAwait(false);
                                    failure = ApplicationResult<StockUseDto>.Success(StockUseMapper.Map(stockUse));
                                },
                                ct)
                            .ConfigureAwait(false);

                        return failure ?? ApplicationResult<StockUseDto>.Failure(
                            ApplicationErrorCodes.DomainViolation,
                            "Stock use could not be created.");
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode == DomainErrorCodes.InventoryInsufficientStock
                ? ApplicationErrorCodes.InsufficientStock
                : ex.ErrorCode;
            return ApplicationResult<StockUseDto>.Failure(code, ex.Message);
        }
        catch (Exception ex) when (IsNumberConflict(ex))
        {
            return ApplicationResult<StockUseDto>.Failure(
                ApplicationErrorCodes.StockUseNumberConflict,
                "Stock use number conflict. Retry the request.");
        }
    }

    private static List<CreateStockUseLineRequest> MergeLines(IReadOnlyList<CreateStockUseLineRequest> lines)
    {
        var merged = new Dictionary<(Guid ProductId, Guid? ProductUnitId), decimal>();
        foreach (var line in lines)
        {
            var key = (line.ProductId, line.ProductUnitId);
            merged[key] = merged.TryGetValue(key, out var existing)
                ? existing + line.Quantity
                : line.Quantity;
        }

        return merged
            .Select(kv => new CreateStockUseLineRequest(kv.Key.ProductId, kv.Value, kv.Key.ProductUnitId))
            .ToList();
    }

    private static bool IsNumberConflict(Exception ex)
    {
        var message = ex.ToString();
        return message.Contains("ux_stock_uses_org_stock_use_number", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ux_stock_uses_org_idempotency_key", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class VoidStockUse
{
    private readonly IStockUseRepository _stockUses;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public VoidStockUse(
        IStockUseRepository stockUses,
        ICatalogProductRepository products,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _stockUses = stockUses;
        _products = products;
        _inventory = inventory;
        _branchBalances = branchBalances;
        _lots = lots;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<StockUseDto>> ExecuteAsync(
        Guid organizationId,
        Guid stockUseId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<StockUseDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to void a stock use.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = StockUseId.From(stockUseId);

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        var stockUse = await _stockUses.GetByIdAsync(orgId, id, ct).ConfigureAwait(false);
                        if (stockUse is null)
                        {
                            return ApplicationResult<StockUseDto>.Failure(
                                ApplicationErrorCodes.StockUseNotFound,
                                "Stock use was not found.");
                        }

                        if (stockUse.Status == StockUseStatus.Voided)
                        {
                            return ApplicationResult<StockUseDto>.Success(StockUseMapper.Map(stockUse));
                        }

                        var productIds = stockUse.Lines.Select(l => l.ProductId).Distinct().ToList();
                        var products = await _products
                            .ListByIdsAsync(orgId, productIds, ct)
                            .ConfigureAwait(false);
                        var productsById = products.ToDictionary(p => p.Id.Value);
                        var utcNow = _clock.UtcNow;

                        await _inventory
                            .ExecuteWithProductReservationLocksAsync(
                                orgId,
                                productIds,
                                async (accounts, lockCt) =>
                                {
                                    var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);
                                    await _lots
                                        .RestoreSourceAsync(
                                            orgId,
                                            stockUse.Id.Value,
                                            StockMovementType.StockUse,
                                            StockMovementType.StockUseVoidRestoration,
                                            actorId,
                                            utcNow,
                                            lockCt)
                                        .ConfigureAwait(false);

                                    foreach (var line in stockUse.Lines)
                                    {
                                        if (await _inventory
                                                .HasStockUseVoidRestorationAsync(
                                                    orgId,
                                                    stockUse.Id,
                                                    line.ProductId,
                                                    lockCt)
                                                .ConfigureAwait(false))
                                        {
                                            continue;
                                        }

                                        if (!accountsByProduct.TryGetValue(line.ProductId.Value, out var account))
                                        {
                                            continue;
                                        }

                                        if (!productsById.TryGetValue(line.ProductId.Value, out var product))
                                        {
                                            throw new DomainException(
                                                ApplicationErrorCodes.SaleProductNotFound,
                                                "One or more products on the stock use were not found.");
                                        }

                                        var restoration = StockMovement.StockUseVoidRestoration(
                                            orgId,
                                            line.ProductId,
                                            account.Id,
                                            line.BaseQuantity,
                                            product.UnitOfMeasure,
                                            stockUse.Id.Value,
                                            actorId,
                                            utcNow,
                                            sellingMode: product.SellingMode,
                                            branchId: stockUse.BranchId?.Value);

                                        var orgOnHandBefore = account.OnHandQuantity;
                                        account.ApplyMovementEffect(restoration.QuantityEffect);
                                        account.Touch(utcNow);
                                        await _inventory.UpdateAccountAsync(account, lockCt).ConfigureAwait(false);
                                        await _inventory.AddMovementAsync(restoration, lockCt).ConfigureAwait(false);

                                        if (stockUse.BranchId is PosBranchId branch)
                                        {
                                            await BranchBalanceMutation
                                                .ApplyAsync(
                                                    _branchBalances,
                                                    _branches,
                                                    orgId,
                                                    branch,
                                                    line.ProductId,
                                                    orgOnHandBefore,
                                                    restoration.QuantityEffect,
                                                    utcNow,
                                                    lockCt)
                                                .ConfigureAwait(false);
                                        }
                                    }

                                    stockUse.Void(utcNow, actorId);
                                    await _stockUses.UpdateAsync(stockUse, lockCt).ConfigureAwait(false);
                                    await _unitOfWork.SaveChangesAsync(lockCt).ConfigureAwait(false);
                                },
                                ct)
                            .ConfigureAwait(false);

                        var reloaded = await _stockUses.GetByIdAsync(orgId, id, ct).ConfigureAwait(false)
                            ?? stockUse;
                        return ApplicationResult<StockUseDto>.Success(StockUseMapper.Map(reloaded));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockUseDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
