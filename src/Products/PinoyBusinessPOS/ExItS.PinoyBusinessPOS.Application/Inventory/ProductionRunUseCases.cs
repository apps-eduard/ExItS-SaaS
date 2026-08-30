using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// PRODUCTION_COST_SCOPE=MATERIAL_ONLY: unit costs from GetLatestAcquisitionUnitCostAsync only.
/// Never SellingPrice. CostStatus Complete/Partial/Unavailable.
/// PRODUCTION_CORRECTION_MODEL=REVERSAL: void restores materials and reverses output when attributable.
/// </summary>
public sealed class ProductionRunQueryService
{
    private readonly IProductionRunRepository _runs;

    public ProductionRunQueryService(IProductionRunRepository runs) => _runs = runs;

    public async Task<ProductionRunDto?> GetByIdAsync(
        Guid organizationId,
        Guid productionRunId,
        CancellationToken cancellationToken = default)
    {
        var run = await _runs
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                ProductionRunId.From(productionRunId),
                cancellationToken)
            .ConfigureAwait(false);
        return run is null ? null : ProductionMapper.Map(run);
    }

    public async Task<PagedResult<ProductionRunListItemDto>> ListAsync(
        Guid organizationId,
        ProductionRunFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _runs
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<ProductionRunListItemDto>(
            items.Select(ProductionMapper.MapListItem).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class CreateProductionRun
{
    private readonly IProductionRunRepository _runs;
    private readonly IProductionDefinitionRepository _definitions;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public CreateProductionRun(
        IProductionRunRepository runs,
        IProductionDefinitionRepository definitions,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _runs = runs;
        _definitions = definitions;
        _products = products;
        _units = units;
        _inventory = inventory;
        _branchBalances = branchBalances;
        _lots = lots;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<ProductionRunDto>> ExecuteAsync(
        Guid organizationId,
        CreateProductionRunRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ProductionRunDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a production run.");
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
                        if (request.ProductionRunId is Guid clientId && clientId != Guid.Empty)
                        {
                            var byId = await _runs
                                .GetByIdAsync(orgId, ProductionRunId.From(clientId), ct)
                                .ConfigureAwait(false);
                            if (byId is not null)
                            {
                                return ApplicationResult<ProductionRunDto>.Success(ProductionMapper.Map(byId));
                            }
                        }

                        if (idempotencyKey is not null)
                        {
                            var existing = await _runs
                                .FindByIdempotencyKeyAsync(orgId, idempotencyKey, ct)
                                .ConfigureAwait(false);
                            if (existing is not null)
                            {
                                return ApplicationResult<ProductionRunDto>.Success(ProductionMapper.Map(existing));
                            }
                        }

                        var definition = await _definitions
                            .GetByIdAsync(orgId, ProductionDefinitionId.From(request.ProductionDefinitionId), ct)
                            .ConfigureAwait(false);
                        if (definition is null)
                        {
                            return ApplicationResult<ProductionRunDto>.Failure(
                                ApplicationErrorCodes.ProductionDefinitionNotFound,
                                "Production definition was not found.");
                        }

                        if (!definition.IsActive)
                        {
                            return ApplicationResult<ProductionRunDto>.Failure(
                                DomainErrorCodes.ProductionDefinitionInactive,
                                "Cannot produce from an inactive production definition.");
                        }

                        decimal outputMultiplier = definition.OutputMultiplierToBase;
                        ProductUnitId? outputUnitId = definition.OutputProductUnitId;
                        if (request.OutputProductUnitId is Guid ouid && ouid != Guid.Empty)
                        {
                            var unit = await _units
                                .GetByIdAsync(orgId, ProductUnitId.From(ouid), ct)
                                .ConfigureAwait(false);
                            if (unit is null || !unit.IsActive || unit.ProductId != definition.OutputProductId)
                            {
                                return ApplicationResult<ProductionRunDto>.Failure(
                                    DomainErrorCodes.InvalidProductUnitId,
                                    "Output product unit was not found for this product.");
                            }

                            outputMultiplier = unit.MultiplierToBase;
                            outputUnitId = unit.Id;
                        }

                        var outputBase = ProductUnitConversion.ToBaseQuantity(request.OutputQuantity, outputMultiplier);
                        if (definition.OutputBaseQuantity <= 0m)
                        {
                            return ApplicationResult<ProductionRunDto>.Failure(
                                DomainErrorCodes.InvalidProductionQuantity,
                                "Production definition output base quantity is invalid.");
                        }

                        var scale = outputBase / definition.OutputBaseQuantity;
                        var overrides = (request.MaterialOverrides ?? [])
                            .GroupBy(o => o.MaterialProductId)
                            .ToDictionary(g => g.Key, g => g.Last());

                        var materialProductIds = definition.Components.Select(c => c.MaterialProductId).ToList();
                        var allProductIds = materialProductIds.Append(definition.OutputProductId).Distinct().ToList();
                        var products = await _products.ListByIdsAsync(orgId, allProductIds, ct).ConfigureAwait(false);
                        var productsById = products.ToDictionary(p => p.Id.Value);

                        if (!productsById.TryGetValue(definition.OutputProductId.Value, out var outputProduct)
                            || outputProduct.Status != CatalogProductStatus.Active
                            || !outputProduct.IsProduced)
                        {
                            return ApplicationResult<ProductionRunDto>.Failure(
                                DomainErrorCodes.ProductionOutputNotEligible,
                                "Output product is not eligible for production.");
                        }

                        if (outputProduct.TracksExpiration && request.OutputExpirationDate is null)
                        {
                            return ApplicationResult<ProductionRunDto>.Failure(
                                DomainErrorCodes.InventoryExpirationRequired,
                                "Expiration date is required for expiration-tracked produced items.");
                        }

                        ApplicationResult<ProductionRunDto>? failure = null;
                        await _inventory
                            .ExecuteWithProductReservationLocksAsync(
                                orgId,
                                allProductIds,
                                async (accounts, lockCt) =>
                                {
                                    var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);
                                    foreach (var productId in allProductIds)
                                    {
                                        if (!accountsByProduct.TryGetValue(productId.Value, out var account)
                                            || !account.IsTracked)
                                        {
                                            failure = ApplicationResult<ProductionRunDto>.Failure(
                                                DomainErrorCodes.InventoryNotTracked,
                                                "Inventory must be tracked for all production materials and the output product.");
                                            return;
                                        }
                                    }

                                    var materialDrafts = new List<ProductionRunMaterialDraft>(definition.Components.Count);
                                    foreach (var component in definition.Components)
                                    {
                                        if (!productsById.TryGetValue(component.MaterialProductId.Value, out var material)
                                            || !material.CanBeUsedAsIngredient
                                            || material.Status != CatalogProductStatus.Active)
                                        {
                                            failure = ApplicationResult<ProductionRunDto>.Failure(
                                                DomainErrorCodes.ProductionComponentNotEligible,
                                                "One or more materials are not eligible for production.");
                                            return;
                                        }

                                        var expectedEntered = ScaleQuantity(component.QuantityEntered, scale);
                                        var multiplier = component.MultiplierToBase;
                                        var unitId = component.ProductUnitId;
                                        var unitLabel = UnitOfMeasures.ToCode(material.UnitOfMeasure);
                                        if (unitId is ProductUnitId pud)
                                        {
                                            var unit = await _units
                                                .GetByIdAsync(orgId, pud, lockCt)
                                                .ConfigureAwait(false);
                                            if (unit is not null)
                                            {
                                                unitLabel = unit.ShortLabel;
                                            }
                                        }

                                        var actualEntered = expectedEntered;
                                        if (overrides.TryGetValue(component.MaterialProductId.Value, out var ov))
                                        {
                                            actualEntered = ov.ActualQuantity;
                                            if (ov.ProductUnitId is Guid uid && uid != Guid.Empty)
                                            {
                                                var unit = await _units
                                                    .GetByIdAsync(orgId, ProductUnitId.From(uid), lockCt)
                                                    .ConfigureAwait(false);
                                                if (unit is null || !unit.IsActive || unit.ProductId != material.Id)
                                                {
                                                    failure = ApplicationResult<ProductionRunDto>.Failure(
                                                        DomainErrorCodes.InvalidProductUnitId,
                                                        "Product unit was not found for this material.");
                                                    return;
                                                }

                                                multiplier = unit.MultiplierToBase;
                                                unitId = unit.Id;
                                                unitLabel = unit.ShortLabel;
                                            }
                                        }

                                        var actualBase = ProductUnitConversion.ToBaseQuantity(actualEntered, multiplier);
                                        var account = accountsByProduct[component.MaterialProductId.Value];
                                        if (!material.TracksExpiration && account.AvailableQuantity < actualBase)
                                        {
                                            failure = ApplicationResult<ProductionRunDto>.Failure(
                                                ApplicationErrorCodes.InsufficientStock,
                                                $"Insufficient stock for '{material.Name}'. Available: {account.AvailableQuantity}, required: {actualBase}.");
                                            return;
                                        }

                                        var unitCost = await _inventory
                                            .GetLatestAcquisitionUnitCostAsync(orgId, material.Id, lockCt)
                                            .ConfigureAwait(false);

                                        materialDrafts.Add(new ProductionRunMaterialDraft(
                                            material.Id,
                                            expectedEntered,
                                            actualEntered,
                                            multiplier,
                                            material.Name,
                                            unitLabel,
                                            unitId,
                                            unitCost));
                                    }

                                    if (failure is not null)
                                    {
                                        return;
                                    }

                                    var utcNow = _clock.UtcNow;
                                    var businessDate = ProductionNumbers.BusinessDateOf(utcNow);
                                    var productionNumber = await _runs
                                        .AllocateNextNumberAsync(orgId, businessDate, lockCt)
                                        .ConfigureAwait(false);

                                    PosBranchId? branch = request.BranchId is Guid branchGuid && branchGuid != Guid.Empty
                                        ? PosBranchId.From(branchGuid)
                                        : null;

                                    ProductionRunId? clientRunId = request.ProductionRunId is Guid rid && rid != Guid.Empty
                                        ? ProductionRunId.From(rid)
                                        : null;

                                    var outputUnitLabel = UnitOfMeasures.ToCode(outputProduct.UnitOfMeasure);
                                    if (outputUnitId is ProductUnitId outUnit)
                                    {
                                        var unit = await _units.GetByIdAsync(orgId, outUnit, lockCt).ConfigureAwait(false);
                                        if (unit is not null)
                                        {
                                            outputUnitLabel = unit.ShortLabel;
                                        }
                                    }

                                    var run = ProductionRun.Create(
                                        orgId,
                                        productionNumber,
                                        definition.Id,
                                        definition.Revision,
                                        definition.Name,
                                        definition.OutputProductId,
                                        request.OutputQuantity,
                                        outputMultiplier,
                                        outputProduct.Name,
                                        outputUnitLabel,
                                        materialDrafts,
                                        actorId,
                                        utcNow,
                                        request.ProducedAtUtc,
                                        branch,
                                        outputUnitId,
                                        request.OutputExpirationDate,
                                        request.OutputLotNumber,
                                        request.ReferenceNumber,
                                        request.Notes,
                                        idempotencyKey,
                                        clientRunId);

                                    foreach (var line in run.Materials)
                                    {
                                        var account = accountsByProduct[line.MaterialProductId.Value];
                                        if (await _inventory
                                                .HasProductionMaterialConsumptionAsync(
                                                    orgId,
                                                    run.Id,
                                                    line.MaterialProductId,
                                                    lockCt)
                                                .ConfigureAwait(false))
                                        {
                                            continue;
                                        }

                                        var material = productsById[line.MaterialProductId.Value];
                                        if (material.TracksExpiration)
                                        {
                                            var today = InventoryLot.BusinessDateOf(utcNow);
                                            try
                                            {
                                                await _lots
                                                    .ConsumeFefoAsync(
                                                        orgId,
                                                        line.MaterialProductId,
                                                        line.ActualBaseQuantity,
                                                        today,
                                                        actorId,
                                                        utcNow,
                                                        StockMovementType.ProductionMaterialConsumption,
                                                        StockMovementSourceType.Production,
                                                        branch,
                                                        sourceId: run.Id.Value,
                                                        cancellationToken: lockCt)
                                                    .ConfigureAwait(false);
                                            }
                                            catch (DomainException)
                                            {
                                                failure = ApplicationResult<ProductionRunDto>.Failure(
                                                    ApplicationErrorCodes.InsufficientStock,
                                                    $"Insufficient non-expired stock for '{material.Name}'. Required: {line.ActualBaseQuantity}.");
                                                return;
                                            }
                                        }
                                        else if (account.AvailableQuantity < line.ActualBaseQuantity)
                                        {
                                            failure = ApplicationResult<ProductionRunDto>.Failure(
                                                ApplicationErrorCodes.InsufficientStock,
                                                $"Insufficient stock for '{material.Name}'. Available: {account.AvailableQuantity}, required: {line.ActualBaseQuantity}.");
                                            return;
                                        }

                                        var movement = StockMovement.ProductionMaterialConsumption(
                                            orgId,
                                            line.MaterialProductId,
                                            account.Id,
                                            line.ActualBaseQuantity,
                                            material.UnitOfMeasure,
                                            run.Id.Value,
                                            actorId,
                                            utcNow,
                                            sellingMode: material.SellingMode,
                                            branchId: branch?.Value,
                                            unitCost: line.UnitCostSnapshot);

                                        line.AttachInventoryMovement(movement.Id);
                                        var orgOnHandBefore = account.OnHandQuantity;
                                        account.ApplyMovementEffect(movement.QuantityEffect);
                                        account.Touch(utcNow);
                                        await _inventory.UpdateAccountAsync(account, lockCt).ConfigureAwait(false);
                                        await _inventory.AddMovementAsync(movement, lockCt).ConfigureAwait(false);
                                        await ApplyBranchAsync(
                                                orgId,
                                                branch,
                                                line.MaterialProductId,
                                                orgOnHandBefore,
                                                movement.QuantityEffect,
                                                utcNow,
                                                lockCt)
                                            .ConfigureAwait(false);
                                    }

                                    if (failure is not null)
                                    {
                                        return;
                                    }

                                    var outputAccount = accountsByProduct[run.OutputProductId.Value];
                                    if (!await _inventory
                                            .HasProductionOutputAsync(orgId, run.Id, run.OutputProductId, lockCt)
                                            .ConfigureAwait(false))
                                    {
                                        var outputMovement = StockMovement.ProductionOutput(
                                            orgId,
                                            run.OutputProductId,
                                            outputAccount.Id,
                                            run.OutputBaseQuantity,
                                            outputProduct.UnitOfMeasure,
                                            run.Id.Value,
                                            actorId,
                                            utcNow,
                                            sellingMode: outputProduct.SellingMode,
                                            branchId: branch?.Value,
                                            unitCost: run.CostStatus == ProductionCostStatus.Complete
                                                ? run.OutputBaseUnitCost
                                                : null);

                                        run.AttachOutputInventoryMovement(outputMovement.Id);
                                        var outputOrgOnHandBefore = outputAccount.OnHandQuantity;
                                        outputAccount.ApplyMovementEffect(outputMovement.QuantityEffect);
                                        outputAccount.Touch(utcNow);
                                        await _inventory.UpdateAccountAsync(outputAccount, lockCt).ConfigureAwait(false);
                                        await _inventory.AddMovementAsync(outputMovement, lockCt).ConfigureAwait(false);
                                        await ApplyBranchAsync(
                                                orgId,
                                                branch,
                                                run.OutputProductId,
                                                outputOrgOnHandBefore,
                                                outputMovement.QuantityEffect,
                                                utcNow,
                                                lockCt)
                                            .ConfigureAwait(false);

                                        if (outputProduct.TracksExpiration)
                                        {
                                            await _lots
                                                .ReceiveAsync(
                                                    orgId,
                                                    run.OutputProductId,
                                                    run.OutputExpirationDate!.Value,
                                                    run.OutputBaseQuantity,
                                                    actorId,
                                                    utcNow,
                                                    StockMovementType.ProductionOutput,
                                                    StockMovementSourceType.Production,
                                                    branch,
                                                    run.OutputLotNumber,
                                                    sourceId: run.Id.Value,
                                                    stockMovementId: outputMovement.Id.Value,
                                                    cancellationToken: lockCt)
                                                .ConfigureAwait(false);
                                        }
                                    }

                                    await _runs.AddAsync(run, lockCt).ConfigureAwait(false);
                                    await _unitOfWork.SaveChangesAsync(lockCt).ConfigureAwait(false);
                                    failure = ApplicationResult<ProductionRunDto>.Success(ProductionMapper.Map(run));
                                },
                                ct)
                            .ConfigureAwait(false);

                        return failure ?? ApplicationResult<ProductionRunDto>.Failure(
                            ApplicationErrorCodes.DomainViolation,
                            "Production run could not be created.");
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode == DomainErrorCodes.InventoryInsufficientStock
                ? ApplicationErrorCodes.InsufficientStock
                : ex.ErrorCode;
            return ApplicationResult<ProductionRunDto>.Failure(code, ex.Message);
        }
        catch (Exception ex) when (IsNumberConflict(ex))
        {
            return ApplicationResult<ProductionRunDto>.Failure(
                ApplicationErrorCodes.ProductionNumberConflict,
                "Production number conflict. Retry the request.");
        }
    }

    private async Task ApplyBranchAsync(
        PosOrganizationId orgId,
        PosBranchId? branch,
        CatalogProductId productId,
        decimal organizationOnHandBeforeDelta,
        decimal quantityEffect,
        DateTimeOffset utcNow,
        CancellationToken ct)
    {
        if (branch is null)
        {
            return;
        }

        await BranchBalanceMutation
            .ApplyAsync(
                _branchBalances,
                _branches,
                orgId,
                branch,
                productId,
                organizationOnHandBeforeDelta,
                quantityEffect,
                utcNow,
                ct)
            .ConfigureAwait(false);
    }

    internal static decimal ScaleQuantity(decimal value, decimal scale) =>
        Math.Round(value * scale, SaleMoney.MeasuredQuantityDecimals, MidpointRounding.AwayFromZero);

    private static bool IsNumberConflict(Exception ex)
    {
        var message = ex.ToString();
        return message.Contains("ux_production_runs_org_production_number", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ux_production_runs_org_idempotency_key", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class VoidProductionRun
{
    private readonly IProductionRunRepository _runs;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public VoidProductionRun(
        IProductionRunRepository runs,
        ICatalogProductRepository products,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _runs = runs;
        _products = products;
        _inventory = inventory;
        _branchBalances = branchBalances;
        _lots = lots;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<ProductionRunDto>> ExecuteAsync(
        Guid organizationId,
        Guid productionRunId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ProductionRunDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to void a production run.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = ProductionRunId.From(productionRunId);

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        var run = await _runs.GetByIdAsync(orgId, id, ct).ConfigureAwait(false);
                        if (run is null)
                        {
                            return ApplicationResult<ProductionRunDto>.Failure(
                                ApplicationErrorCodes.ProductionRunNotFound,
                                "Production run was not found.");
                        }

                        if (run.Status == ProductionRunStatus.Voided)
                        {
                            return ApplicationResult<ProductionRunDto>.Success(ProductionMapper.Map(run));
                        }

                        var productIds = run.Materials
                            .Select(m => m.MaterialProductId)
                            .Append(run.OutputProductId)
                            .Distinct()
                            .ToList();
                        var products = await _products.ListByIdsAsync(orgId, productIds, ct).ConfigureAwait(false);
                        var productsById = products.ToDictionary(p => p.Id.Value);
                        var utcNow = _clock.UtcNow;

                        ApplicationResult<ProductionRunDto>? failure = null;
                        await _inventory
                            .ExecuteWithProductReservationLocksAsync(
                                orgId,
                                productIds,
                                async (accounts, lockCt) =>
                                {
                                    var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);
                                    if (!productsById.TryGetValue(run.OutputProductId.Value, out var outputProduct))
                                    {
                                        failure = ApplicationResult<ProductionRunDto>.Failure(
                                            ApplicationErrorCodes.SaleProductNotFound,
                                            "Output product on the production run was not found.");
                                        return;
                                    }

                                    if (!accountsByProduct.TryGetValue(run.OutputProductId.Value, out var outputAccount))
                                    {
                                        failure = ApplicationResult<ProductionRunDto>.Failure(
                                            DomainErrorCodes.InventoryNotTracked,
                                            "Output inventory account was not found.");
                                        return;
                                    }

                                    if (outputProduct.TracksExpiration)
                                    {
                                        try
                                        {
                                            await _lots
                                                .ReverseReceiveSourceAsync(
                                                    orgId,
                                                    run.Id.Value,
                                                    StockMovementType.ProductionOutput,
                                                    StockMovementType.ProductionOutputReversal,
                                                    actorId,
                                                    utcNow,
                                                    lockCt)
                                                .ConfigureAwait(false);
                                        }
                                        catch (DomainException ex)
                                        {
                                            failure = ApplicationResult<ProductionRunDto>.Failure(ex.ErrorCode, ex.Message);
                                            return;
                                        }
                                    }
                                    else if (outputAccount.OnHandQuantity < run.OutputBaseQuantity)
                                    {
                                        failure = ApplicationResult<ProductionRunDto>.Failure(
                                            DomainErrorCodes.ProductionVoidOutputInsufficient,
                                            "Cannot void production: attributable output stock has already been consumed.");
                                        return;
                                    }

                                    await _lots
                                        .RestoreSourceAsync(
                                            orgId,
                                            run.Id.Value,
                                            StockMovementType.ProductionMaterialConsumption,
                                            StockMovementType.ProductionMaterialRestoration,
                                            actorId,
                                            utcNow,
                                            lockCt)
                                        .ConfigureAwait(false);

                                    foreach (var line in run.Materials)
                                    {
                                        if (await _inventory
                                                .HasProductionMaterialRestorationAsync(
                                                    orgId,
                                                    run.Id,
                                                    line.MaterialProductId,
                                                    lockCt)
                                                .ConfigureAwait(false))
                                        {
                                            continue;
                                        }

                                        if (!accountsByProduct.TryGetValue(line.MaterialProductId.Value, out var account)
                                            || !productsById.TryGetValue(line.MaterialProductId.Value, out var material))
                                        {
                                            continue;
                                        }

                                        var restoration = StockMovement.ProductionMaterialRestoration(
                                            orgId,
                                            line.MaterialProductId,
                                            account.Id,
                                            line.ActualBaseQuantity,
                                            material.UnitOfMeasure,
                                            run.Id.Value,
                                            actorId,
                                            utcNow,
                                            sellingMode: material.SellingMode,
                                            branchId: run.BranchId?.Value);

                                        var orgOnHandBefore = account.OnHandQuantity;
                                        account.ApplyMovementEffect(restoration.QuantityEffect);
                                        account.Touch(utcNow);
                                        await _inventory.UpdateAccountAsync(account, lockCt).ConfigureAwait(false);
                                        await _inventory.AddMovementAsync(restoration, lockCt).ConfigureAwait(false);
                                        await ApplyBranchAsync(
                                                orgId,
                                                run.BranchId,
                                                line.MaterialProductId,
                                                orgOnHandBefore,
                                                restoration.QuantityEffect,
                                                utcNow,
                                                lockCt)
                                            .ConfigureAwait(false);
                                    }

                                    if (!await _inventory
                                            .HasProductionOutputReversalAsync(orgId, run.Id, run.OutputProductId, lockCt)
                                            .ConfigureAwait(false))
                                    {
                                        var reversal = StockMovement.ProductionOutputReversal(
                                            orgId,
                                            run.OutputProductId,
                                            outputAccount.Id,
                                            run.OutputBaseQuantity,
                                            outputProduct.UnitOfMeasure,
                                            run.Id.Value,
                                            actorId,
                                            utcNow,
                                            sellingMode: outputProduct.SellingMode,
                                            branchId: run.BranchId?.Value);

                                        var outputOrgOnHandBefore = outputAccount.OnHandQuantity;
                                        outputAccount.ApplyMovementEffect(reversal.QuantityEffect);
                                        outputAccount.Touch(utcNow);
                                        await _inventory.UpdateAccountAsync(outputAccount, lockCt).ConfigureAwait(false);
                                        await _inventory.AddMovementAsync(reversal, lockCt).ConfigureAwait(false);
                                        await ApplyBranchAsync(
                                                orgId,
                                                run.BranchId,
                                                run.OutputProductId,
                                                outputOrgOnHandBefore,
                                                reversal.QuantityEffect,
                                                utcNow,
                                                lockCt)
                                            .ConfigureAwait(false);
                                    }

                                    run.Void(utcNow, actorId);
                                    await _runs.UpdateAsync(run, lockCt).ConfigureAwait(false);
                                    await _unitOfWork.SaveChangesAsync(lockCt).ConfigureAwait(false);
                                    failure = ApplicationResult<ProductionRunDto>.Success(ProductionMapper.Map(run));
                                },
                                ct)
                            .ConfigureAwait(false);

                        if (failure is not null)
                        {
                            return failure;
                        }

                        var reloaded = await _runs.GetByIdAsync(orgId, id, ct).ConfigureAwait(false) ?? run;
                        return ApplicationResult<ProductionRunDto>.Success(ProductionMapper.Map(reloaded));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductionRunDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task ApplyBranchAsync(
        PosOrganizationId orgId,
        PosBranchId? branch,
        CatalogProductId productId,
        decimal organizationOnHandBeforeDelta,
        decimal quantityEffect,
        DateTimeOffset utcNow,
        CancellationToken ct)
    {
        if (branch is null)
        {
            return;
        }

        await BranchBalanceMutation
            .ApplyAsync(
                _branchBalances,
                _branches,
                orgId,
                branch,
                productId,
                organizationOnHandBeforeDelta,
                quantityEffect,
                utcNow,
                ct)
            .ConfigureAwait(false);
    }
}
