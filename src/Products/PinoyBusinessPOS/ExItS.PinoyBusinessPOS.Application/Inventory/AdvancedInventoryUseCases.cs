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

public sealed class SetInventoryReorderConfiguration
{
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryReorderChangeRepository _history;
    private readonly IInventoryBranchReorderRepository _branchReorder;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetInventoryReorderConfiguration(
        IInventoryRepository inventory,
        IInventoryReorderChangeRepository history,
        IInventoryBranchReorderRepository branchReorder,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _inventory = inventory;
        _history = history;
        _branchReorder = branchReorder;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryAccount>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        Guid productId,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        string reason,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to set reorder configuration.");
        }

        if (branchId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A selected branch is required to set branch reorder configuration.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var branch = PosBranchId.From(branchId);
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
            var existing = await _branchReorder
                .GetAsync(orgId, branch, catalogProductId, cancellationToken)
                .ConfigureAwait(false);
            var change = InventoryReorderChange.Create(
                orgId,
                account.Id,
                catalogProductId,
                existing?.ReorderLevel,
                reorderLevel,
                existing?.ReorderQuantity,
                reorderQuantity,
                reason,
                actorId,
                utcNow);

            var setting = existing is null
                ? InventoryBranchReorderSetting.Create(
                    orgId,
                    branch,
                    catalogProductId,
                    reorderLevel,
                    reorderQuantity,
                    product.UnitOfMeasure,
                    actorId,
                    utcNow)
                : existing;
            if (existing is not null)
            {
                existing.SetConfiguration(
                    reorderLevel,
                    reorderQuantity,
                    product.UnitOfMeasure,
                    actorId,
                    utcNow);
            }

            await _history.AddAsync(change, cancellationToken).ConfigureAwait(false);
            await _branchReorder.UpsertAsync(setting, cancellationToken).ConfigureAwait(false);
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

public sealed class InventoryReconciliationQuery
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryBranchBalanceRepository _balances;

    public InventoryReconciliationQuery(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryBranchBalanceRepository balances)
    {
        _inventory = inventory;
        _products = products;
        _balances = balances;
    }

    public async Task<ApplicationResult<PosInventoryReconciliationDto>> GetAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<PosInventoryReconciliationDto>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        var account = await _inventory
            .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        if (account is null || !account.IsTracked)
        {
            return ApplicationResult<PosInventoryReconciliationDto>.Failure(
                DomainErrorCodes.InventoryNotTracked,
                "Inventory is not tracked for this product.");
        }

        var balances = await _balances
            .ListByProductIdsAsync(orgId, [catalogProductId], cancellationToken)
            .ConfigureAwait(false);
        var explicitSum = balances.Sum(b => b.OnHandQuantity);
        var unallocated = Math.Max(0m, account.OnHandQuantity - explicitSum);
        var movementSum = await _inventory
            .SumMovementEffectsAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        var difference = account.OnHandQuantity - movementSum;
        return ApplicationResult<PosInventoryReconciliationDto>.Success(
            new PosInventoryReconciliationDto(
                productId,
                account.OnHandQuantity,
                explicitSum,
                unallocated,
                movementSum,
                difference,
                difference == 0m));
    }
}

public sealed class StockCountQueryService
{
    private readonly IStockCountRepository _counts;
    private readonly ICatalogProductRepository _products;

    public StockCountQueryService(IStockCountRepository counts, ICatalogProductRepository products)
    {
        _counts = counts;
        _products = products;
    }

    public async Task<PosStockCountDto?> GetByIdAsync(
        Guid organizationId,
        Guid stockCountId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var count = await _counts
            .GetByIdAsync(orgId, StockCountId.From(stockCountId), cancellationToken)
            .ConfigureAwait(false);
        return count is null ? null : await MapAsync(orgId, count, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<PosStockCountDto>> ListAsync(
        Guid organizationId,
        StockCountFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var orgId = PosOrganizationId.From(organizationId);
        var (items, total) = await _counts.ListAsync(orgId, filter, skip, take, cancellationToken).ConfigureAwait(false);
        var dtos = new List<PosStockCountDto>(items.Count);
        foreach (var item in items)
        {
            dtos.Add(await MapAsync(orgId, item, cancellationToken).ConfigureAwait(false));
        }

        return new PagedResult<PosStockCountDto>(dtos, total, Math.Max(page ?? 1, 1), take);
    }

    internal async Task<PosStockCountDto> MapAsync(
        PosOrganizationId orgId,
        StockCount count,
        CancellationToken cancellationToken)
    {
        var productIds = count.Lines.Select(l => l.ProductId).ToList();
        var products = await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
        var byId = products.ToDictionary(p => p.Id.Value);
        var lineDtos = count.Lines
            .OrderBy(l => l.LineNumber)
            .Select(line =>
            {
                byId.TryGetValue(line.ProductId.Value, out var product);
                return new PosStockCountLineDto(
                    line.Id.Value,
                    line.ProductId.Value,
                    product?.Name ?? "—",
                    product is null ? "Piece" : UnitOfMeasures.ToCode(product.UnitOfMeasure),
                    line.LineNumber,
                    line.SystemOnHandSnapshot,
                    line.CountedQuantity,
                    line.Variance);
            })
            .ToList();

        return new PosStockCountDto(
            count.Id.Value,
            count.OrganizationId.Value,
            count.CountNumber,
            count.Title,
            StockCountStatuses.ToCode(count.Status),
            count.CountDate,
            count.Notes,
            count.StartedAtUtc,
            count.StartedBy,
            count.CompletedAtUtc,
            count.CompletedBy,
            count.CancelledAtUtc,
            count.CancelledBy,
            count.CreatedAtUtc,
            count.UpdatedAtUtc,
            lineDtos);
    }
}

public sealed class CreateStockCount
{
    private readonly IStockCountRepository _counts;
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateStockCount(
        IStockCountRepository counts,
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _counts = counts;
        _inventory = inventory;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockCount>> ExecuteAsync(
        Guid organizationId,
        CreateStockCountRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<StockCount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a stock count.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        try
        {
            var drafts = await StockCountLineDraftBuilder
                .BuildAsync(orgId, request.Lines, _inventory, _products, cancellationToken)
                .ConfigureAwait(false);
            var count = StockCount.CreateDraft(
                orgId,
                drafts,
                _clock.UtcNow,
                request.Title,
                actorId,
                request.CountDate,
                request.Notes);
            await _counts.AddAsync(count, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<StockCount>.Success(count);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockCount>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class StockCountLineDraftBuilder
{
    public static async Task<IReadOnlyList<StockCountLineDraft>> BuildAsync(
        PosOrganizationId orgId,
        IReadOnlyList<CreateStockCountLineRequest> lines,
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        CancellationToken cancellationToken)
    {
        var drafts = new List<StockCountLineDraft>(lines.Count);
        foreach (var line in lines)
        {
            var productId = CatalogProductId.From(line.ProductId);
            var product = await products.GetByIdAsync(orgId, productId, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                throw new DomainException(
                    DomainErrorCodes.StockCountProductNotFound,
                    "Count line product was not found.");
            }

            var account = await inventory.GetByProductIdAsync(orgId, productId, cancellationToken).ConfigureAwait(false);
            if (account is null || !account.IsTracked)
            {
                throw new DomainException(
                    DomainErrorCodes.StockCountProductNotTracked,
                    "All count lines must reference tracked products.");
            }

            drafts.Add(new StockCountLineDraft(productId, line.CountedQuantity));
        }

        return drafts;
    }
}

public sealed class UpdateStockCountDraft
{
    private readonly IStockCountRepository _counts;
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateStockCountDraft(
        IStockCountRepository counts,
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _counts = counts;
        _inventory = inventory;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockCount>> ExecuteAsync(
        Guid organizationId,
        Guid stockCountId,
        UpdateStockCountRequest request,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var count = await _counts
            .GetByIdAsync(orgId, StockCountId.From(stockCountId), cancellationToken)
            .ConfigureAwait(false);
        if (count is null)
        {
            return ApplicationResult<StockCount>.Failure(
                ApplicationErrorCodes.StockCountNotFound,
                "Stock count was not found.");
        }

        try
        {
            var drafts = await StockCountLineDraftBuilder
                .BuildAsync(orgId, request.Lines, _inventory, _products, cancellationToken)
                .ConfigureAwait(false);
            count.UpdateDraft(drafts, _clock.UtcNow, request.CountDate, request.Notes, request.Title);
            await _counts.UpdateAsync(count, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<StockCount>.Success(count);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockCount>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateStockCountInProgress
{
    private readonly IStockCountRepository _counts;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateStockCountInProgress(
        IStockCountRepository counts,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _counts = counts;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockCount>> ExecuteAsync(
        Guid organizationId,
        Guid stockCountId,
        UpdateStockCountRequest request,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var count = await _counts
            .GetByIdAsync(orgId, StockCountId.From(stockCountId), cancellationToken)
            .ConfigureAwait(false);
        if (count is null)
        {
            return ApplicationResult<StockCount>.Failure(
                ApplicationErrorCodes.StockCountNotFound,
                "Stock count was not found.");
        }

        try
        {
            var productIds = request.Lines.Select(l => CatalogProductId.From(l.ProductId)).ToList();
            var products = await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
            var unitByProduct = products.ToDictionary(p => p.Id.Value, p => p.UnitOfMeasure);
            var sellingModeByProduct = products.ToDictionary(p => p.Id.Value, p => p.SellingMode);
            var drafts = request.Lines
                .Select(l => new StockCountLineDraft(CatalogProductId.From(l.ProductId), l.CountedQuantity))
                .ToList();
            count.UpdateInProgressLines(drafts, unitByProduct, _clock.UtcNow, sellingModeByProduct);
            await _counts.UpdateAsync(count, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<StockCount>.Success(count);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockCount>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class StartStockCount
{
    private readonly IStockCountRepository _counts;
    private readonly IInventoryRepository _inventory;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public StartStockCount(
        IStockCountRepository counts,
        IInventoryRepository inventory,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _counts = counts;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockCount>> ExecuteAsync(
        Guid organizationId,
        Guid stockCountId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<StockCount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to start a stock count.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = StockCountId.From(stockCountId);
        var existing = await _counts.GetByIdAsync(orgId, id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return ApplicationResult<StockCount>.Failure(
                ApplicationErrorCodes.StockCountNotFound,
                "Stock count was not found.");
        }

        if (existing.Status == StockCountStatus.InProgress)
        {
            return ApplicationResult<StockCount>.Success(existing);
        }

        try
        {
            var businessDate = StockCountNumbers.BusinessDateOf(_clock.UtcNow);
            var productIds = existing.Lines.Select(l => l.ProductId).ToList();
            var accounts = await _inventory
                .ListByProductIdsAsync(orgId, productIds, cancellationToken)
                .ConfigureAwait(false);
            var onHandByProduct = accounts.ToDictionary(a => a.ProductId.Value, a => a.OnHandQuantity);

            var started = await _counts.StartAsync(
                    orgId,
                    id,
                    businessDate,
                    number =>
                    {
                        existing.Start(number, onHandByProduct, actorId, _clock.UtcNow);
                        return existing;
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<StockCount>.Success(started);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockCount>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<StockCount>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CompleteStockCount
{
    private readonly IStockCountRepository _counts;
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IClock _clock;

    public CompleteStockCount(
        IStockCountRepository counts,
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IClock clock)
    {
        _counts = counts;
        _inventory = inventory;
        _products = products;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockCount>> ExecuteAsync(
        Guid organizationId,
        Guid stockCountId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<StockCount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to complete a stock count.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = StockCountId.From(stockCountId);
        var existing = await _counts.GetByIdAsync(orgId, id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return ApplicationResult<StockCount>.Failure(
                ApplicationErrorCodes.StockCountNotFound,
                "Stock count was not found.");
        }

        if (existing.Status == StockCountStatus.Completed)
        {
            return ApplicationResult<StockCount>.Success(existing);
        }

        try
        {
            var utcNow = _clock.UtcNow;
            var productIds = existing.Lines.Select(l => l.ProductId).ToList();
            var products = await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
            var productById = products.ToDictionary(p => p.Id.Value);

            var completed = await _counts.CompleteAsync(
                    orgId,
                    id,
                    async (count, ct) =>
                    {
                        count.MarkCompleted(actorId, utcNow);
                        var accounts = await _inventory
                            .ListByProductIdsAsync(orgId, productIds, ct)
                            .ConfigureAwait(false);
                        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

                        foreach (var line in count.Lines.OrderBy(l => l.LineNumber))
                        {
                            var variance = line.Variance ?? 0m;
                            if (variance == 0m)
                            {
                                continue;
                            }

                            if (!accountsByProduct.TryGetValue(line.ProductId.Value, out var account))
                            {
                                continue;
                            }

                            if (!productById.TryGetValue(line.ProductId.Value, out var product))
                            {
                                continue;
                            }

                            StockMovement movement;
                            StockMovementType movementType;
                            if (variance > 0m)
                            {
                                movementType = StockMovementType.StockCountVarianceIncrease;
                                if (await _inventory
                                        .HasStockCountVarianceAsync(orgId, count.Id, line.ProductId, movementType, ct)
                                        .ConfigureAwait(false))
                                {
                                    continue;
                                }

                                movement = StockMovement.StockCountVarianceIncrease(
                                    orgId,
                                    line.ProductId,
                                    account.Id,
                                    variance,
                                    product.UnitOfMeasure,
                                    count.Id.Value,
                                    actorId,
                                    utcNow,
                                    sellingMode: product.SellingMode);
                            }
                            else
                            {
                                movementType = StockMovementType.StockCountVarianceDecrease;
                                if (await _inventory
                                        .HasStockCountVarianceAsync(orgId, count.Id, line.ProductId, movementType, ct)
                                        .ConfigureAwait(false))
                                {
                                    continue;
                                }

                                movement = StockMovement.StockCountVarianceDecrease(
                                    orgId,
                                    line.ProductId,
                                    account.Id,
                                    Math.Abs(variance),
                                    product.UnitOfMeasure,
                                    count.Id.Value,
                                    actorId,
                                    utcNow,
                                    sellingMode: product.SellingMode);
                            }

                            account.ApplyMovementEffect(movement.QuantityEffect);
                            account.Touch(utcNow);
                            await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                            await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                        }
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<StockCount>.Success(completed);
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode == DomainErrorCodes.InventoryInsufficientStock
                ? ApplicationErrorCodes.InsufficientStock
                : ex.ErrorCode;
            return ApplicationResult<StockCount>.Failure(code, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<StockCount>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CancelStockCount
{
    private readonly IStockCountRepository _counts;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelStockCount(IStockCountRepository counts, IPosUnitOfWork unitOfWork, IClock clock)
    {
        _counts = counts;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StockCount>> ExecuteAsync(
        Guid organizationId,
        Guid stockCountId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<StockCount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to cancel a stock count.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var count = await _counts
            .GetByIdAsync(orgId, StockCountId.From(stockCountId), cancellationToken)
            .ConfigureAwait(false);
        if (count is null)
        {
            return ApplicationResult<StockCount>.Failure(
                ApplicationErrorCodes.StockCountNotFound,
                "Stock count was not found.");
        }

        if (count.Status == StockCountStatus.Cancelled)
        {
            return ApplicationResult<StockCount>.Success(count);
        }

        try
        {
            count.Cancel(actorId, _clock.UtcNow);
            await _counts.UpdateAsync(count, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<StockCount>.Success(count);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StockCount>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
