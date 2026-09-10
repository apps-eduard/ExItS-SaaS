using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed class GetInventoryBranchReorderDefault
{
    private readonly IInventoryBranchReorderDefaultRepository _defaults;

    public GetInventoryBranchReorderDefault(IInventoryBranchReorderDefaultRepository defaults) =>
        _defaults = defaults;

    public async Task<PosInventoryBranchReorderDefaultDto> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var setting = await _defaults
            .GetAsync(PosOrganizationId.From(organizationId), PosBranchId.From(branchId), cancellationToken)
            .ConfigureAwait(false);
        return setting is null
            ? new PosInventoryBranchReorderDefaultDto(organizationId, branchId, null, null, null, null)
            : new PosInventoryBranchReorderDefaultDto(
                setting.OrganizationId.Value,
                setting.BranchId.Value,
                setting.ReorderLevel,
                setting.ReorderQuantity,
                setting.UpdatedAtUtc,
                setting.UpdatedBy);
    }
}

public sealed class SetInventoryBranchReorderDefault
{
    private readonly IInventoryBranchReorderDefaultRepository _defaults;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetInventoryBranchReorderDefault(
        IInventoryBranchReorderDefaultRepository defaults,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _defaults = defaults;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosInventoryBranchReorderDefaultDto>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosInventoryBranchReorderDefaultDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to set the branch low stock default.");
        }

        if (branchId == Guid.Empty)
        {
            return ApplicationResult<PosInventoryBranchReorderDefaultDto>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A selected branch is required to set the branch low stock default.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var branch = PosBranchId.From(branchId);
            var utcNow = _clock.UtcNow;
            var existing = await _defaults.GetAsync(orgId, branch, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                existing = InventoryBranchReorderDefault.Create(
                    orgId,
                    branch,
                    reorderLevel,
                    reorderQuantity,
                    actorId,
                    utcNow);
            }
            else
            {
                existing.SetConfiguration(reorderLevel, reorderQuantity, actorId, utcNow);
            }

            await _defaults.UpsertAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosInventoryBranchReorderDefaultDto>.Success(
                new PosInventoryBranchReorderDefaultDto(
                    existing.OrganizationId.Value,
                    existing.BranchId.Value,
                    existing.ReorderLevel,
                    existing.ReorderQuantity,
                    existing.UpdatedAtUtc,
                    existing.UpdatedBy));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosInventoryBranchReorderDefaultDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ClearInventoryProductReorderOverride
{
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryReorderChangeRepository _history;
    private readonly IInventoryBranchReorderRepository _branchReorder;
    private readonly IInventoryBranchReorderDefaultRepository _branchDefaults;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClearInventoryProductReorderOverride(
        IInventoryRepository inventory,
        IInventoryReorderChangeRepository history,
        IInventoryBranchReorderRepository branchReorder,
        IInventoryBranchReorderDefaultRepository branchDefaults,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _inventory = inventory;
        _history = history;
        _branchReorder = branchReorder;
        _branchDefaults = branchDefaults;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryAccount>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        Guid productId,
        string reason,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to clear product low stock overrides.");
        }

        if (branchId == Guid.Empty)
        {
            return ApplicationResult<InventoryAccount>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A selected branch is required to clear product low stock overrides.");
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
            if (existing is null)
            {
                return ApplicationResult<InventoryAccount>.Success(account);
            }

            var branchDefault = await _branchDefaults.GetAsync(orgId, branch, cancellationToken).ConfigureAwait(false);
            var auditReason = EnsureBranchScopedReason(reason, branchId);
            var change = InventoryReorderChange.Create(
                orgId,
                account.Id,
                catalogProductId,
                existing.ReorderLevel,
                branchDefault?.ReorderLevel,
                existing.ReorderQuantity,
                branchDefault?.ReorderQuantity,
                auditReason,
                actorId,
                utcNow);

            await _history.AddAsync(change, cancellationToken).ConfigureAwait(false);
            await _branchReorder.DeleteAsync(orgId, branch, catalogProductId, cancellationToken).ConfigureAwait(false);
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

    internal static string EnsureBranchScopedReason(string reason, Guid branchId)
    {
        var prefix = $"Low stock settings (branch {branchId:D}): ";
        var body = string.IsNullOrWhiteSpace(reason) ? "Use branch default" : reason.Trim();
        var combined = prefix + body;
        return combined.Length <= InventoryReorderChange.ReasonMaxLength
            ? combined
            : combined[..InventoryReorderChange.ReasonMaxLength];
    }
}

public sealed class BulkSetInventoryReorderConfiguration
{
    public const int MaxFilteredProducts = 5000;

    private readonly IInventoryRepository _inventory;
    private readonly IInventoryReorderChangeRepository _history;
    private readonly IInventoryBranchReorderRepository _branchReorder;
    private readonly IInventoryBranchReorderDefaultRepository _branchDefaults;
    private readonly IBranchInventoryQueryRepository _branchInventory;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public BulkSetInventoryReorderConfiguration(
        IInventoryRepository inventory,
        IInventoryReorderChangeRepository history,
        IInventoryBranchReorderRepository branchReorder,
        IInventoryBranchReorderDefaultRepository branchDefaults,
        IBranchInventoryQueryRepository branchInventory,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _inventory = inventory;
        _history = history;
        _branchReorder = branchReorder;
        _branchDefaults = branchDefaults;
        _branchInventory = branchInventory;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BulkSetInventoryReorderResponse>> ExecuteAsync(
        BranchInventoryContext context,
        BulkSetInventoryReorderRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<BulkSetInventoryReorderResponse>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required for bulk low stock updates.");
        }

        if (context.BranchId == Guid.Empty)
        {
            return ApplicationResult<BulkSetInventoryReorderResponse>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A selected branch is required for bulk low stock updates.");
        }

        var mode = (request.Mode ?? string.Empty).Trim();
        if (!IsSupportedMode(mode))
        {
            return ApplicationResult<BulkSetInventoryReorderResponse>.Failure(
                ApplicationErrorCodes.InventoryReorderBulkValidation,
                "Mode must be Custom, NotMonitored, or BranchDefault.");
        }

        if (string.Equals(mode, InventoryReorderMonitoringModes.Custom, StringComparison.OrdinalIgnoreCase)
            && request.ReorderLevel is null)
        {
            return ApplicationResult<BulkSetInventoryReorderResponse>.Failure(
                ApplicationErrorCodes.InventoryReorderBulkValidation,
                "Low stock level is required when setting a custom threshold.");
        }

        List<Guid> productIds;
        var resolve = await ResolveProductIdsAsync(context, request, cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess)
        {
            return ApplicationResult<BulkSetInventoryReorderResponse>.Failure(
                resolve.ErrorCode!,
                resolve.ErrorMessage!);
        }

        productIds = resolve.Value!;

        if (productIds.Count == 0)
        {
            return ApplicationResult<BulkSetInventoryReorderResponse>.Failure(
                ApplicationErrorCodes.InventoryReorderBulkEmpty,
                "No products were selected for the bulk low stock update.");
        }

        var orgId = PosOrganizationId.From(context.OrganizationId);
        var branch = PosBranchId.From(context.BranchId);
        var branchDefault = await _branchDefaults.GetAsync(orgId, branch, cancellationToken).ConfigureAwait(false);
        var reason = ClearInventoryProductReorderOverride.EnsureBranchScopedReason(
            string.IsNullOrWhiteSpace(request.Reason) ? $"Bulk {mode}" : request.Reason!,
            context.BranchId);
        var utcNow = _clock.UtcNow;
        var results = new List<BulkSetInventoryReorderResultItem>(productIds.Count);
        var succeeded = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var productId in productIds)
        {
            var itemResult = await ApplyOneAsync(
                    orgId,
                    branch,
                    CatalogProductId.From(productId),
                    mode,
                    request.ReorderLevel,
                    request.ReorderQuantity,
                    branchDefault,
                    reason,
                    actorId,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false);
            results.Add(itemResult);
            if (itemResult.Succeeded)
            {
                if (string.Equals(itemResult.Outcome, "Skipped", StringComparison.OrdinalIgnoreCase))
                {
                    skipped++;
                }
                else
                {
                    succeeded++;
                }
            }
            else
            {
                failed++;
            }
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BulkSetInventoryReorderResponse>.Failure(ex.ErrorCode, ex.Message);
        }

        return ApplicationResult<BulkSetInventoryReorderResponse>.Success(
            new BulkSetInventoryReorderResponse(
                productIds.Count,
                succeeded,
                skipped,
                failed,
                results));
    }

    private async Task<ApplicationResult<List<Guid>>> ResolveProductIdsAsync(
        BranchInventoryContext context,
        BulkSetInventoryReorderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ApplyToFiltered)
        {
            var filter = new BranchInventoryListFilter(
                Search: request.Search,
                TrackedOnly: true,
                StockStatus: request.StockStatus,
                MonitoringMode: request.MonitoringMode,
                CategoryId: request.CategoryId);
            var (ids, total) = await _branchInventory
                .ListProductIdsAsync(context, filter, MaxFilteredProducts + 1, cancellationToken)
                .ConfigureAwait(false);
            if (total > MaxFilteredProducts || ids.Count > MaxFilteredProducts)
            {
                return ApplicationResult<List<Guid>>.Failure(
                    ApplicationErrorCodes.InventoryReorderBulkTooLarge,
                    $"Bulk update is limited to {MaxFilteredProducts} products. Narrow the filters and try again.");
            }

            return ApplicationResult<List<Guid>>.Success(ids.ToList());
        }

        var selected = (request.ProductIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (selected.Count == 0)
        {
            return ApplicationResult<List<Guid>>.Failure(
                ApplicationErrorCodes.InventoryReorderBulkEmpty,
                "Product ids are required when ApplyToFiltered is false.");
        }

        if (selected.Count > MaxFilteredProducts)
        {
            return ApplicationResult<List<Guid>>.Failure(
                ApplicationErrorCodes.InventoryReorderBulkTooLarge,
                $"Bulk update is limited to {MaxFilteredProducts} products.");
        }

        return ApplicationResult<List<Guid>>.Success(selected);
    }

    private async Task<BulkSetInventoryReorderResultItem> ApplyOneAsync(
        PosOrganizationId orgId,
        PosBranchId branch,
        CatalogProductId productId,
        string mode,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        InventoryBranchReorderDefault? branchDefault,
        string reason,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await _products.GetByIdAsync(orgId, productId, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                return Fail(productId.Value, ApplicationErrorCodes.InventoryProductNotFound, "Product was not found.");
            }

            var account = await _inventory.GetByProductIdAsync(orgId, productId, cancellationToken).ConfigureAwait(false);
            if (account is null || !account.IsTracked)
            {
                return Fail(productId.Value, DomainErrorCodes.InventoryNotTracked, "Inventory is not tracked for this product.");
            }

            var existing = await _branchReorder.GetAsync(orgId, branch, productId, cancellationToken).ConfigureAwait(false);

            if (string.Equals(mode, InventoryReorderMonitoringModes.BranchDefault, StringComparison.OrdinalIgnoreCase))
            {
                if (existing is null)
                {
                    return new BulkSetInventoryReorderResultItem(productId.Value, true, "Skipped", null, null);
                }

                var change = InventoryReorderChange.Create(
                    orgId,
                    account.Id,
                    productId,
                    existing.ReorderLevel,
                    branchDefault?.ReorderLevel,
                    existing.ReorderQuantity,
                    branchDefault?.ReorderQuantity,
                    reason,
                    actorId,
                    utcNow);
                await _history.AddAsync(change, cancellationToken).ConfigureAwait(false);
                await _branchReorder.DeleteAsync(orgId, branch, productId, cancellationToken).ConfigureAwait(false);
                return new BulkSetInventoryReorderResultItem(productId.Value, true, "Updated", null, null);
            }

            decimal? nextLevel;
            decimal? nextQty;
            if (string.Equals(mode, InventoryReorderMonitoringModes.NotMonitored, StringComparison.OrdinalIgnoreCase))
            {
                nextLevel = null;
                nextQty = null;
            }
            else
            {
                nextLevel = reorderLevel;
                nextQty = reorderQuantity;
            }

            if (existing is not null
                && existing.ReorderLevel == nextLevel
                && existing.ReorderQuantity == nextQty)
            {
                return new BulkSetInventoryReorderResultItem(productId.Value, true, "Skipped", null, null);
            }

            var changeCustom = InventoryReorderChange.Create(
                orgId,
                account.Id,
                productId,
                existing?.ReorderLevel,
                nextLevel,
                existing?.ReorderQuantity,
                nextQty,
                reason,
                actorId,
                utcNow);

            var setting = existing is null
                ? InventoryBranchReorderSetting.Create(
                    orgId,
                    branch,
                    productId,
                    nextLevel,
                    nextQty,
                    product.UnitOfMeasure,
                    actorId,
                    utcNow)
                : existing;
            if (existing is not null)
            {
                existing.SetConfiguration(nextLevel, nextQty, product.UnitOfMeasure, actorId, utcNow);
            }

            await _history.AddAsync(changeCustom, cancellationToken).ConfigureAwait(false);
            await _branchReorder.UpsertAsync(setting, cancellationToken).ConfigureAwait(false);
            return new BulkSetInventoryReorderResultItem(productId.Value, true, "Updated", null, null);
        }
        catch (DomainException ex)
        {
            return Fail(productId.Value, ex.ErrorCode, ex.Message);
        }
    }

    private static bool IsSupportedMode(string mode) =>
        string.Equals(mode, InventoryReorderMonitoringModes.Custom, StringComparison.OrdinalIgnoreCase)
        || string.Equals(mode, InventoryReorderMonitoringModes.NotMonitored, StringComparison.OrdinalIgnoreCase)
        || string.Equals(mode, InventoryReorderMonitoringModes.BranchDefault, StringComparison.OrdinalIgnoreCase);

    private static BulkSetInventoryReorderResultItem Fail(Guid productId, string code, string message) =>
        new(productId, false, "Failed", code, message);
}
