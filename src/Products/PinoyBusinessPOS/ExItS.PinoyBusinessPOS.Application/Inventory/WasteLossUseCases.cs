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
/// Waste/loss write-off document. Does not route through StockUse, Production, or ManualDecrease.
/// Cost: GetLatestAcquisitionUnitCostAsync only (includes ProductionOutput); never SellingPrice.
/// Expiration-tracked lines require explicit InventoryLotId and use ConsumeSpecificAsync only (never FEFO).
/// EXPIRED_STOCK_QUICK_FLOW=DEFERRED.
/// </summary>
public sealed class WasteLossQueryService
{
    private readonly IWasteLossRepository _wasteLosses;

    public WasteLossQueryService(IWasteLossRepository wasteLosses) => _wasteLosses = wasteLosses;

    public async Task<WasteLossDto?> GetByIdAsync(
        Guid organizationId,
        Guid wasteLossId,
        CancellationToken cancellationToken = default)
    {
        var wasteLoss = await _wasteLosses
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                WasteLossId.From(wasteLossId),
                cancellationToken)
            .ConfigureAwait(false);
        return wasteLoss is null ? null : WasteLossMapper.Map(wasteLoss);
    }

    public async Task<PagedResult<WasteLossListItemDto>> ListAsync(
        Guid organizationId,
        WasteLossFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _wasteLosses
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<WasteLossListItemDto>(
            items.Select(WasteLossMapper.MapListItem).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class CreateWasteLoss
{
    private readonly IWasteLossRepository _wasteLosses;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly IInventoryLotRepository _lotRepository;
    private readonly InventoryLotStockService _lots;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public CreateWasteLoss(
        IWasteLossRepository wasteLosses,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        IInventoryLotRepository lotRepository,
        InventoryLotStockService lots,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _wasteLosses = wasteLosses;
        _products = products;
        _units = units;
        _inventory = inventory;
        _branchBalances = branchBalances;
        _lotRepository = lotRepository;
        _lots = lots;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<WasteLossDto>> ExecuteAsync(
        Guid organizationId,
        CreateWasteLossRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<WasteLossDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a waste/loss.");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return ApplicationResult<WasteLossDto>.Failure(
                DomainErrorCodes.WasteLossRequiresLines,
                "At least one waste/loss line is required.");
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
                        if (request.WasteLossId is Guid clientId && clientId != Guid.Empty)
                        {
                            var byId = await _wasteLosses
                                .GetByIdAsync(orgId, WasteLossId.From(clientId), ct)
                                .ConfigureAwait(false);
                            if (byId is not null)
                            {
                                return ApplicationResult<WasteLossDto>.Success(WasteLossMapper.Map(byId));
                            }
                        }

                        if (idempotencyKey is not null)
                        {
                            var existing = await _wasteLosses
                                .FindByIdempotencyKeyAsync(orgId, idempotencyKey, ct)
                                .ConfigureAwait(false);
                            if (existing is not null)
                            {
                                return ApplicationResult<WasteLossDto>.Success(WasteLossMapper.Map(existing));
                            }
                        }

                        if (!WasteLossReasons.TryParse(request.Reason, out var reason))
                        {
                            return ApplicationResult<WasteLossDto>.Failure(
                                DomainErrorCodes.InvalidWasteLossReason,
                                $"Waste/loss reason must be one of: {string.Join(", ", WasteLossReasons.Codes)}.");
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
                                return ApplicationResult<WasteLossDto>.Failure(
                                    ApplicationErrorCodes.SaleProductNotFound,
                                    "One or more products were not found in this organization.");
                            }

                            if (product.Status != CatalogProductStatus.Active)
                            {
                                return ApplicationResult<WasteLossDto>.Failure(
                                    ApplicationErrorCodes.SaleProductNotActive,
                                    "Only active catalog products can be written off via waste/loss.");
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
                                return ApplicationResult<WasteLossDto>.Failure(
                                    DomainErrorCodes.InvalidProductUnitId,
                                    "Product unit was not found for this product.");
                            }

                            unitsById[unitId] = unit;
                        }

                        ApplicationResult<WasteLossDto>? failure = null;
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
                                            failure = ApplicationResult<WasteLossDto>.Failure(
                                                DomainErrorCodes.InventoryNotTracked,
                                                "Inventory must be tracked for all products on a waste/loss.");
                                            return;
                                        }
                                    }

                                    var requiredByProduct = mergedLines
                                        .GroupBy(l => l.ProductId)
                                        .ToDictionary(
                                            g => g.Key,
                                            g => g.Sum(l =>
                                            {
                                                var multiplier = 1m;
                                                if (l.ProductUnitId is Guid uid && uid != Guid.Empty)
                                                {
                                                    multiplier = unitsById[uid].MultiplierToBase;
                                                }

                                                return ProductUnitConversion.ToBaseQuantity(l.Quantity, multiplier);
                                            }));

                                    foreach (var (productId, required) in requiredByProduct)
                                    {
                                        var account = accountsByProduct[productId];
                                        var product = productsById[productId];
                                        if (!product.TracksExpiration && account.AvailableQuantity < required)
                                        {
                                            failure = ApplicationResult<WasteLossDto>.Failure(
                                                ApplicationErrorCodes.InsufficientStock,
                                                $"Insufficient stock for '{product.Name}'. Available: {account.AvailableQuantity}, required: {required}.");
                                            return;
                                        }

                                        if (product.TracksExpiration && account.AvailableQuantity < required)
                                        {
                                            failure = ApplicationResult<WasteLossDto>.Failure(
                                                ApplicationErrorCodes.InsufficientStock,
                                                $"Insufficient stock for '{product.Name}'. Available: {account.AvailableQuantity}, required: {required}.");
                                            return;
                                        }
                                    }

                                    var drafts = new List<WasteLossLineDraft>(mergedLines.Count);
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
                                                failure = ApplicationResult<WasteLossDto>.Failure(
                                                    DomainErrorCodes.InvalidProductUnitId,
                                                    "Product unit does not belong to this product.");
                                                return;
                                            }

                                            multiplier = unit.MultiplierToBase;
                                            productUnitId = unit.Id;
                                            unitLabel = unit.ShortLabel;
                                        }

                                        InventoryLotId? lotId = null;
                                        if (product.TracksExpiration)
                                        {
                                            if (line.InventoryLotId is null || line.InventoryLotId == Guid.Empty)
                                            {
                                                failure = ApplicationResult<WasteLossDto>.Failure(
                                                    DomainErrorCodes.WasteLossLotRequired,
                                                    $"Inventory lot is required for expiration-tracked product '{product.Name}'.");
                                                return;
                                            }

                                            lotId = InventoryLotId.From(line.InventoryLotId.Value);
                                            var lot = await _lotRepository
                                                .GetByIdAsync(orgId, lotId, lockCt)
                                                .ConfigureAwait(false);
                                            if (lot is null || lot.ProductId != product.Id)
                                            {
                                                failure = ApplicationResult<WasteLossDto>.Failure(
                                                    DomainErrorCodes.WasteLossLotMismatch,
                                                    $"Inventory lot does not belong to product '{product.Name}'.");
                                                return;
                                            }
                                        }
                                        else if (line.InventoryLotId is Guid providedLot && providedLot != Guid.Empty)
                                        {
                                            failure = ApplicationResult<WasteLossDto>.Failure(
                                                DomainErrorCodes.WasteLossLotNotAllowed,
                                                $"Inventory lot is not allowed for non-expiration product '{product.Name}'.");
                                            return;
                                        }

                                        var unitCost = await _inventory
                                            .GetLatestAcquisitionUnitCostAsync(orgId, product.Id, lockCt)
                                            .ConfigureAwait(false);

                                        drafts.Add(new WasteLossLineDraft(
                                            product.Id,
                                            line.Quantity,
                                            multiplier,
                                            product.Name,
                                            unitLabel,
                                            productUnitId,
                                            lotId,
                                            unitCost));
                                    }

                                    if (failure is not null)
                                    {
                                        return;
                                    }

                                    var utcNow = _clock.UtcNow;
                                    var businessDate = WasteLossNumbers.BusinessDateOf(utcNow);
                                    var wasteLossNumber = await _wasteLosses
                                        .AllocateNextNumberAsync(orgId, businessDate, lockCt)
                                        .ConfigureAwait(false);

                                    PosBranchId? branch = request.BranchId is Guid branchGuid && branchGuid != Guid.Empty
                                        ? PosBranchId.From(branchGuid)
                                        : null;

                                    WasteLossId? clientWasteLossId = request.WasteLossId is Guid wid && wid != Guid.Empty
                                        ? WasteLossId.From(wid)
                                        : null;

                                    var wasteLoss = WasteLoss.Create(
                                        orgId,
                                        wasteLossNumber,
                                        reason,
                                        drafts,
                                        actorId,
                                        utcNow,
                                        request.OccurredAtUtc,
                                        branch,
                                        request.ReferenceNumber,
                                        request.Notes,
                                        idempotencyKey,
                                        clientWasteLossId);

                                    foreach (var line in wasteLoss.Lines)
                                    {
                                        var product = productsById[line.ProductId.Value];
                                        if (product.TracksExpiration)
                                        {
                                            var lot = await _lotRepository
                                                .GetByIdAsync(orgId, line.InventoryLotId!, lockCt)
                                                .ConfigureAwait(false);
                                            if (lot is null)
                                            {
                                                failure = ApplicationResult<WasteLossDto>.Failure(
                                                    DomainErrorCodes.WasteLossLotMismatch,
                                                    $"Inventory lot was not found for '{product.Name}'.");
                                                return;
                                            }

                                            try
                                            {
                                                await _lots
                                                    .ConsumeSpecificAsync(
                                                        orgId,
                                                        lot,
                                                        line.BaseQuantity,
                                                        actorId,
                                                        utcNow,
                                                        StockMovementType.WasteLoss,
                                                        StockMovementSourceType.WasteLoss,
                                                        sourceId: wasteLoss.Id.Value,
                                                        cancellationToken: lockCt)
                                                    .ConfigureAwait(false);
                                            }
                                            catch (DomainException)
                                            {
                                                failure = ApplicationResult<WasteLossDto>.Failure(
                                                    ApplicationErrorCodes.InsufficientStock,
                                                    $"Insufficient lot stock for '{product.Name}'. Required: {line.BaseQuantity}.");
                                                return;
                                            }
                                        }
                                    }

                                    if (failure is not null)
                                    {
                                        return;
                                    }

                                    foreach (var productGroup in wasteLoss.Lines.GroupBy(l => l.ProductId.Value))
                                    {
                                        var productId = CatalogProductId.From(productGroup.Key);
                                        if (await _inventory
                                                .HasWasteLossAsync(orgId, wasteLoss.Id, productId, lockCt)
                                                .ConfigureAwait(false))
                                        {
                                            continue;
                                        }

                                        var groupLines = productGroup.ToList();
                                        var totalQty = groupLines.Sum(l => l.BaseQuantity);
                                        var account = accountsByProduct[productGroup.Key];
                                        var product = productsById[productGroup.Key];

                                        if (account.AvailableQuantity < totalQty)
                                        {
                                            failure = ApplicationResult<WasteLossDto>.Failure(
                                                ApplicationErrorCodes.InsufficientStock,
                                                $"Insufficient stock for '{product.Name}'. Available: {account.AvailableQuantity}, required: {totalQty}.");
                                            return;
                                        }

                                        var movement = StockMovement.WasteLoss(
                                            orgId,
                                            productId,
                                            account.Id,
                                            totalQty,
                                            product.UnitOfMeasure,
                                            wasteLoss.Id.Value,
                                            actorId,
                                            utcNow,
                                            reason: $"{StockMovement.WasteLossConsumptionReason}: {WasteLossReasons.ToCode(reason)}",
                                            sellingMode: product.SellingMode,
                                            branchId: branch?.Value,
                                            unitCost: groupLines[0].UnitCostSnapshot);

                                        groupLines[0].AttachInventoryMovement(movement.Id);
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
                                                    productId,
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

                                    await _wasteLosses.AddAsync(wasteLoss, lockCt).ConfigureAwait(false);
                                    await _unitOfWork.SaveChangesAsync(lockCt).ConfigureAwait(false);
                                    failure = ApplicationResult<WasteLossDto>.Success(WasteLossMapper.Map(wasteLoss));
                                },
                                ct)
                            .ConfigureAwait(false);

                        return failure ?? ApplicationResult<WasteLossDto>.Failure(
                            ApplicationErrorCodes.DomainViolation,
                            "Waste/loss could not be created.");
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode == DomainErrorCodes.InventoryInsufficientStock
                ? ApplicationErrorCodes.InsufficientStock
                : ex.ErrorCode;
            return ApplicationResult<WasteLossDto>.Failure(code, ex.Message);
        }
        catch (Exception ex) when (IsNumberConflict(ex))
        {
            return ApplicationResult<WasteLossDto>.Failure(
                ApplicationErrorCodes.WasteLossNumberConflict,
                "Waste/loss number conflict. Retry the request.");
        }
    }

    private static List<CreateWasteLossLineRequest> MergeLines(IReadOnlyList<CreateWasteLossLineRequest> lines)
    {
        var merged = new Dictionary<(Guid ProductId, Guid? ProductUnitId, Guid? InventoryLotId), decimal>();
        foreach (var line in lines)
        {
            var key = (line.ProductId, line.ProductUnitId, line.InventoryLotId);
            merged[key] = merged.TryGetValue(key, out var existing)
                ? existing + line.Quantity
                : line.Quantity;
        }

        return merged
            .Select(kv => new CreateWasteLossLineRequest(
                kv.Key.ProductId,
                kv.Value,
                kv.Key.ProductUnitId,
                kv.Key.InventoryLotId))
            .ToList();
    }

    private static bool IsNumberConflict(Exception ex)
    {
        var message = ex.ToString();
        return message.Contains("ux_waste_losses_org_waste_loss_number", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ux_waste_losses_org_idempotency_key", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class VoidWasteLoss
{
    private readonly IWasteLossRepository _wasteLosses;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public VoidWasteLoss(
        IWasteLossRepository wasteLosses,
        ICatalogProductRepository products,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _wasteLosses = wasteLosses;
        _products = products;
        _inventory = inventory;
        _branchBalances = branchBalances;
        _lots = lots;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<WasteLossDto>> ExecuteAsync(
        Guid organizationId,
        Guid wasteLossId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<WasteLossDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to void a waste/loss.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = WasteLossId.From(wasteLossId);

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        var wasteLoss = await _wasteLosses.GetByIdAsync(orgId, id, ct).ConfigureAwait(false);
                        if (wasteLoss is null)
                        {
                            return ApplicationResult<WasteLossDto>.Failure(
                                ApplicationErrorCodes.WasteLossNotFound,
                                "Waste/loss was not found.");
                        }

                        if (wasteLoss.Status == WasteLossStatus.Voided)
                        {
                            return ApplicationResult<WasteLossDto>.Success(WasteLossMapper.Map(wasteLoss));
                        }

                        var productIds = wasteLoss.Lines.Select(l => l.ProductId).Distinct().ToList();
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
                                            wasteLoss.Id.Value,
                                            StockMovementType.WasteLoss,
                                            StockMovementType.WasteLossVoidRestoration,
                                            actorId,
                                            utcNow,
                                            lockCt)
                                        .ConfigureAwait(false);

                                    foreach (var productGroup in wasteLoss.Lines.GroupBy(l => l.ProductId.Value))
                                    {
                                        var productId = CatalogProductId.From(productGroup.Key);
                                        if (await _inventory
                                                .HasWasteLossVoidRestorationAsync(
                                                    orgId,
                                                    wasteLoss.Id,
                                                    productId,
                                                    lockCt)
                                                .ConfigureAwait(false))
                                        {
                                            continue;
                                        }

                                        if (!accountsByProduct.TryGetValue(productGroup.Key, out var account))
                                        {
                                            continue;
                                        }

                                        if (!productsById.TryGetValue(productGroup.Key, out var product))
                                        {
                                            throw new DomainException(
                                                ApplicationErrorCodes.SaleProductNotFound,
                                                "One or more products on the waste/loss were not found.");
                                        }

                                        var totalQty = productGroup.Sum(l => l.BaseQuantity);
                                        var restoration = StockMovement.WasteLossVoidRestoration(
                                            orgId,
                                            productId,
                                            account.Id,
                                            totalQty,
                                            product.UnitOfMeasure,
                                            wasteLoss.Id.Value,
                                            actorId,
                                            utcNow,
                                            sellingMode: product.SellingMode,
                                            branchId: wasteLoss.BranchId?.Value);

                                        var orgOnHandBefore = account.OnHandQuantity;
                                        account.ApplyMovementEffect(restoration.QuantityEffect);
                                        account.Touch(utcNow);
                                        await _inventory.UpdateAccountAsync(account, lockCt).ConfigureAwait(false);
                                        await _inventory.AddMovementAsync(restoration, lockCt).ConfigureAwait(false);

                                        if (wasteLoss.BranchId is PosBranchId branch)
                                        {
                                            await BranchBalanceMutation
                                                .ApplyAsync(
                                                    _branchBalances,
                                                    _branches,
                                                    orgId,
                                                    branch,
                                                    productId,
                                                    orgOnHandBefore,
                                                    restoration.QuantityEffect,
                                                    utcNow,
                                                    lockCt)
                                                .ConfigureAwait(false);
                                        }
                                    }

                                    wasteLoss.Void(utcNow, actorId);
                                    await _wasteLosses.UpdateAsync(wasteLoss, lockCt).ConfigureAwait(false);
                                    await _unitOfWork.SaveChangesAsync(lockCt).ConfigureAwait(false);
                                },
                                ct)
                            .ConfigureAwait(false);

                        var reloaded = await _wasteLosses.GetByIdAsync(orgId, id, ct).ConfigureAwait(false)
                            ?? wasteLoss;
                        return ApplicationResult<WasteLossDto>.Success(WasteLossMapper.Map(reloaded));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<WasteLossDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
