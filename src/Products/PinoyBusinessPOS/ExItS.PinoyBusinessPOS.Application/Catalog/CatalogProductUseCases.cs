using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed class CatalogProductQueryService
{
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductImageRepository _images;

    public CatalogProductQueryService(
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IInventoryRepository inventory,
        ICatalogProductImageRepository images)
    {
        _products = products;
        _units = units;
        _inventory = inventory;
        _images = images;
    }

    public async Task<PosCatalogProductDto?> GetByIdAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var product = await _products
            .GetByIdAsync(orgId, CatalogProductId.From(productId), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return null;
        }

        var account = await _inventory
            .GetByProductIdAsync(orgId, product.Id, cancellationToken)
            .ConfigureAwait(false);
        var units = await _units.ListByProductAsync(orgId, product.Id, cancellationToken).ConfigureAwait(false);
        var image = await _images.GetByProductIdAsync(orgId, product.Id, cancellationToken).ConfigureAwait(false);
        return Map(product, account, units, image);
    }

    public async Task<PagedResult<PosCatalogProductDto>> ListAsync(
        Guid organizationId,
        CatalogProductFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _products
            .ListAsync(orgId, filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var accounts = await _inventory
            .ListByProductIdsAsync(orgId, items.Select(p => p.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);
        var unitsByProduct = await _units
            .ListByProductIdsAsync(orgId, items.Select(p => p.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);
        var images = await _images
            .ListByProductIdsAsync(orgId, items.Select(p => p.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);
        var imagesByProduct = images.ToDictionary(i => i.ProductId.Value);

        return new PagedResult<PosCatalogProductDto>(
            items.Select(p => Map(
                    p,
                    accountsByProduct.GetValueOrDefault(p.Id.Value),
                    unitsByProduct.GetValueOrDefault(p.Id.Value),
                    imagesByProduct.GetValueOrDefault(p.Id.Value)))
                .ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    /// <summary>Exact SKU lookup. Active-only by default; inactive matches stay discoverable on request.</summary>
    public async Task<ApplicationResult<PosCatalogProductDto>> LookupBySkuAsync(
        Guid organizationId,
        string sku,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        string? normalized;
        try
        {
            (_, normalized) = CatalogProduct.NormalizeOptionalSku(sku);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosCatalogProductDto>.Failure(ex.ErrorCode, ex.Message);
        }

        if (normalized is null)
        {
            return ApplicationResult<PosCatalogProductDto>.Failure(
                DomainErrorCodes.InvalidProductSku,
                "SKU is required for lookup.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var product = await _products
            .FindByNormalizedSkuAsync(orgId, normalized, cancellationToken)
            .ConfigureAwait(false);

        return await ResolveAsync(orgId, product, includeInactive, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Exact barcode lookup. Active-only by default; inactive matches stay discoverable on request.</summary>
    public async Task<ApplicationResult<PosCatalogProductDto>> LookupByBarcodeAsync(
        Guid organizationId,
        string barcode,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        string? normalized;
        try
        {
            normalized = CatalogProduct.NormalizeOptionalBarcode(barcode);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosCatalogProductDto>.Failure(ex.ErrorCode, ex.Message);
        }

        if (normalized is null)
        {
            return ApplicationResult<PosCatalogProductDto>.Failure(
                DomainErrorCodes.InvalidProductBarcode,
                "Barcode is required for lookup.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var product = await _products
            .FindByBarcodeAsync(orgId, normalized, cancellationToken)
            .ConfigureAwait(false);

        return await ResolveAsync(orgId, product, includeInactive, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApplicationResult<PosCatalogProductDto>> ResolveAsync(
        PosOrganizationId organizationId,
        CatalogProduct? product,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        if (product is null || (!includeInactive && product.Status != CatalogProductStatus.Active))
        {
            return ApplicationResult<PosCatalogProductDto>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        var account = await _inventory
            .GetByProductIdAsync(organizationId, product.Id, cancellationToken)
            .ConfigureAwait(false);
        var units = await _units.ListByProductAsync(organizationId, product.Id, cancellationToken).ConfigureAwait(false);
        var image = await _images.GetByProductIdAsync(organizationId, product.Id, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PosCatalogProductDto>.Success(Map(product, account, units, image));
    }

    public static PosCatalogProductDto Map(
        CatalogProduct product,
        InventoryAccount? account = null,
        IReadOnlyList<CatalogProductUnit>? units = null,
        CatalogProductImage? image = null)
    {
        var isTracked = account?.IsTracked ?? false;
        var onHand = account?.OnHandQuantity ?? 0m;
        var stockStatus = isTracked && account is not null
            ? InventoryStockStatuses.ToCode(account.StockStatus)
            : InventoryStockStatuses.ToCode(InventoryStockStatus.InStock);

        return new(
            product.Id.Value,
            product.OrganizationId.Value,
            product.Name,
            product.Description,
            product.Sku,
            product.Barcode,
            product.CategoryId?.Value,
            UnitOfMeasures.ToCode(product.UnitOfMeasure),
            SellingModes.ToCode(product.SellingMode),
            product.SellingPrice,
            product.Status.ToString(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            product.PlatformGlobalProductId,
            product.PlatformTemplateId,
            product.CatalogSource.ToString(),
            product.CatalogImportedAt,
            product.CatalogSnapshotVersion,
            product.SourceGlobalCategoryId,
            isTracked,
            onHand,
            stockStatus,
            product.TracksExpiration,
            product.ExpirationWarningDays,
            product.CanBePurchased,
            product.CanBeSold,
            product.CanBeUsedAsIngredient,
            product.IsProduced,
            product.UsagePreset,
            units?.Select(CatalogProductUnitHelpers.MapUnit).ToList(),
            product.CanExposeToConnectedBuyers,
            product.DefaultConnectedPoPrice,
            image is not null,
            image?.Version);
    }
}

public sealed class CreateCatalogProduct
{
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IProductCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ISupplierProductExposureRepository? _exposures;

    public CreateCatalogProduct(
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IProductCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        ISupplierProductExposureRepository? exposures = null)
    {
        _products = products;
        _units = units;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _exposures = exposures;
    }

    public async Task<ApplicationResult<CatalogProduct>> ExecuteAsync(
        Guid organizationId,
        string name,
        string unitOfMeasure,
        decimal sellingPrice,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        Guid? categoryId = null,
        Guid? clientProductId = null,
        string? sellingMode = null,
        bool tracksExpiration = false,
        int? expirationWarningDays = null,
        bool? canBePurchased = null,
        bool? canBeSold = null,
        bool? canBeUsedAsIngredient = null,
        bool? isProduced = null,
        string? usagePreset = null,
        IReadOnlyList<PosCatalogProductUnitInput>? units = null,
        bool canExposeToConnectedBuyers = true,
        decimal? defaultConnectedPoPrice = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var staged = await CatalogProductCreateCore.StageAsync(
                _products,
                _units,
                _categories,
                _clock,
                _exposures,
                organizationId,
                name,
                unitOfMeasure,
                sellingPrice,
                description,
                sku,
                barcode,
                categoryId,
                clientProductId,
                sellingMode,
                tracksExpiration,
                expirationWarningDays,
                canBePurchased,
                canBeSold,
                canBeUsedAsIngredient,
                isProduced,
                usagePreset,
                units,
                canExposeToConnectedBuyers,
                defaultConnectedPoPrice,
                cancellationToken).ConfigureAwait(false);
            if (!staged.IsSuccess)
            {
                return ApplicationResult<CatalogProduct>.Failure(staged.ErrorCode!, staged.ErrorMessage!);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogProduct>.Success(staged.Value!);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogProduct>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogProduct>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateCatalogProduct
{
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IProductCategoryRepository _categories;
    private readonly IInventoryRepository _inventory;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ISupplierProductExposureRepository? _exposures;

    public UpdateCatalogProduct(
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IProductCategoryRepository categories,
        IInventoryRepository inventory,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        ISupplierProductExposureRepository? exposures = null)
    {
        _products = products;
        _units = units;
        _categories = categories;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _exposures = exposures;
    }

    public async Task<ApplicationResult<CatalogProduct>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        string name,
        string unitOfMeasure,
        decimal sellingPrice,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        Guid? categoryId = null,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        string? sellingMode = null,
        bool? tracksExpiration = null,
        int? expirationWarningDays = null,
        bool? canBePurchased = null,
        bool? canBeSold = null,
        bool? canBeUsedAsIngredient = null,
        bool? isProduced = null,
        string? usagePreset = null,
        IReadOnlyList<PosCatalogProductUnitInput>? units = null,
        bool? canExposeToConnectedBuyers = null,
        decimal? defaultConnectedPoPrice = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var product = await _products
            .GetByIdAsync(orgId, CatalogProductId.From(productId), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<CatalogProduct>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        if (CatalogConcurrency.IsStale(expectedUpdatedAtUtc, product.UpdatedAtUtc))
        {
            return ApplicationResult<CatalogProduct>.Failure(
                ApplicationErrorCodes.CatalogConcurrencyConflict,
                "The product was updated concurrently. Reload the latest version and try again.");
        }

        try
        {
            var unit = UnitOfMeasures.Parse(unitOfMeasure);
            // Update: omitted/blank SellingMode preserves the existing value.
            var mode = string.IsNullOrWhiteSpace(sellingMode)
                ? product.SellingMode
                : SellingModes.Parse(sellingMode);

            if (unit != product.UnitOfMeasure)
            {
                var hasMovements = await _inventory
                    .HasAnyMovementAsync(orgId, product.Id, cancellationToken)
                    .ConfigureAwait(false);
                var account = await _inventory
                    .GetByProductIdAsync(orgId, product.Id, cancellationToken)
                    .ConfigureAwait(false);
                var trackedWithOnHand = account is { IsTracked: true, OnHandQuantity: not 0m };
                if (hasMovements || trackedWithOnHand)
                {
                    return ApplicationResult<CatalogProduct>.Failure(
                        DomainErrorCodes.InventoryUomChangeBlocked,
                        "Unit of measure cannot change after inventory activity for this product.");
                }
            }

            ProductCategoryId? category = null;
            if (categoryId is not null)
            {
                var isUnchanged = product.CategoryId is not null && product.CategoryId.Value == categoryId.Value;
                if (!isUnchanged)
                {
                    var assignable = await CatalogAssignment
                        .EnsureAssignableCategoryAsync(_categories, orgId, categoryId.Value, cancellationToken)
                        .ConfigureAwait(false);
                    if (!assignable.IsSuccess)
                    {
                        return ApplicationResult<CatalogProduct>.Failure(assignable.ErrorCode!, assignable.ErrorMessage!);
                    }
                }

                category = ProductCategoryId.From(categoryId.Value);
            }

            var (_, normalizedSku) = CatalogProduct.NormalizeOptionalSku(sku);
            var normalizedBarcode = CatalogProduct.NormalizeOptionalBarcode(barcode);
            var conflict = await CatalogAssignment
                .FindIdentifierConflictAsync(_products, orgId, normalizedSku, normalizedBarcode, product.Id, cancellationToken)
                .ConfigureAwait(false);
            if (conflict is not null)
            {
                return ApplicationResult<CatalogProduct>.Failure(conflict.ErrorCode!, conflict.ErrorMessage!);
            }

            var now = _clock.UtcNow;
            product.UpdateDetails(
                name,
                description,
                sku,
                barcode,
                category,
                unit,
                sellingPrice,
                now,
                mode);

            if (tracksExpiration is not null)
            {
                product.SetExpirationTracking(
                    tracksExpiration.Value,
                    expirationWarningDays,
                    now);
            }

            if (canExposeToConnectedBuyers == true)
            {
                product.EnableConnectedBuyerAvailability(now);
                if (defaultConnectedPoPrice is not null)
                {
                    product.SetDefaultConnectedPoPrice(defaultConnectedPoPrice.Value, now);
                }
            }
            else if (canExposeToConnectedBuyers == false)
            {
                product.DisableConnectedBuyerAvailability(now);
            }
            else if (defaultConnectedPoPrice is not null)
            {
                product.SetDefaultConnectedPoPrice(defaultConnectedPoPrice.Value, now);
            }

            if (canBePurchased is not null
                || canBeSold is not null
                || canBeUsedAsIngredient is not null
                || isProduced is not null
                || !string.IsNullOrWhiteSpace(usagePreset))
            {
                var usage = CatalogProductUnitHelpers.ResolveUsage(
                    canBePurchased ?? product.CanBePurchased,
                    canBeSold ?? product.CanBeSold,
                    canBeUsedAsIngredient ?? product.CanBeUsedAsIngredient,
                    isProduced ?? product.IsProduced,
                    usagePreset ?? product.UsagePreset);
                product.UpdateUsage(usage, now);
            }

            if (units is { Count: > 0 })
            {
                var created = units
                    .Select(u => CatalogProductUnitHelpers.CreateFromInput(orgId, product.Id, u, now))
                    .ToList();
                var purchaseUnits = created.Where(u => u.Kind == ProductUnitKind.Purchase).ToList();
                var sellUnits = created.Where(u => u.Kind == ProductUnitKind.Sell).ToList();
                if (purchaseUnits.Count > 0)
                {
                    await _units.ReplaceActiveUnitsAsync(
                            orgId,
                            product.Id,
                            ProductUnitKind.Purchase,
                            purchaseUnits,
                            now,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                if (sellUnits.Count > 0)
                {
                    await _units.ReplaceActiveUnitsAsync(
                            orgId,
                            product.Id,
                            ProductUnitKind.Sell,
                            sellUnits,
                            now,
                            cancellationToken)
                        .ConfigureAwait(false);
                    var primarySellPrice = CatalogProductUnitHelpers.PrimarySellUnitPrice(sellUnits);
                    if (primarySellPrice is not null)
                    {
                        product.UpdateSellingPrice(primarySellPrice.Value, now);
                    }
                }
            }

            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await ConnectedProductExposureSync.SyncAsync(product, _exposures, now, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogProduct>.Success(product);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogProduct>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogProduct>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class ConnectedProductExposureSync
{
    public static async Task SyncAsync(
        CatalogProduct product,
        ISupplierProductExposureRepository? exposures,
        DateTimeOffset utcNow,
        CancellationToken ct)
    {
        if (exposures is null) return;
        var existing = await exposures.GetByProductAsync(product.OrganizationId, product.Id, ct).ConfigureAwait(false);
        if (product.IsBlockedFromConnectedBuyers || !product.CanExposeToConnectedBuyers)
        {
            if (existing is not null && existing.IsExposed)
            {
                existing.Deactivate(utcNow);
                await exposures.UpdateAsync(existing, ct).ConfigureAwait(false);
            }
            return;
        }

        if (product.DefaultConnectedPoPrice is null)
        {
            // Eligible but no staged PO price yet — do not throw; deactivate any prior exposure.
            if (existing is not null && existing.IsExposed)
            {
                existing.Deactivate(utcNow);
                await exposures.UpdateAsync(existing, ct).ConfigureAwait(false);
            }
            return;
        }

        var price = product.DefaultConnectedPoPrice.Value;
        if (existing is null)
        {
            existing = SupplierProductExposure.Expose(
                product.OrganizationId, product.Id, product.Name, product.UnitOfMeasure.ToString(), price, utcNow, product.Sku);
            await exposures.AddAsync(existing, ct).ConfigureAwait(false);
        }
        else
        {
            existing.UpdateOffer(product.Name, product.UnitOfMeasure.ToString(), price, true, utcNow, product.Sku);
            await exposures.UpdateAsync(existing, ct).ConfigureAwait(false);
        }
    }
}

public sealed class DeactivateCatalogProduct
{
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivateCatalogProduct(
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CatalogProduct>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _products
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                CatalogProductId.From(productId),
                cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<CatalogProduct>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        try
        {
            product.Deactivate(_clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogProduct>.Success(product);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogProduct>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogProduct>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReactivateCatalogProduct
{
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivateCatalogProduct(
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CatalogProduct>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _products
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                CatalogProductId.From(productId),
                cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<CatalogProduct>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        try
        {
            product.Reactivate(_clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogProduct>.Success(product);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogProduct>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogProduct>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Today's Prices: narrow bulk current-price update. Partial success (per-item results).
/// Unchanged prices are successes with <c>Changed=false</c> and do not bump UpdatedAtUtc.
/// Does not mutate historical sale lines.
/// </summary>
public sealed class UpdateCatalogProductPrices
{
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IInventoryRepository _inventory;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateCatalogProductPrices(
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IInventoryRepository inventory,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _units = units;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<UpdatePosCatalogProductPricesResponse>> ExecuteAsync(
        Guid organizationId,
        IReadOnlyList<UpdatePosCatalogProductPriceItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items is null || items.Count == 0)
        {
            return ApplicationResult<UpdatePosCatalogProductPricesResponse>.Failure(
                ApplicationErrorCodes.CatalogPriceBulkEmpty,
                "At least one price update item is required.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var results = new UpdatePosCatalogProductPriceResultItem[items.Count];
        var seen = new HashSet<Guid>();
        var productsById = new Dictionary<Guid, CatalogProduct>();
        var now = _clock.UtcNow;
        var anyChanged = false;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.ProductId == Guid.Empty)
            {
                results[i] = Fail(item.ProductId, ApplicationErrorCodes.ProductNotFound, "Product was not found.");
                continue;
            }

            if (!seen.Add(item.ProductId))
            {
                results[i] = Fail(
                    item.ProductId,
                    ApplicationErrorCodes.CatalogPriceBulkDuplicate,
                    "Duplicate ProductId in the same Today's Prices request.");
                continue;
            }

            var product = await _products
                .GetByIdAsync(orgId, CatalogProductId.From(item.ProductId), cancellationToken)
                .ConfigureAwait(false);
            if (product is null)
            {
                results[i] = Fail(item.ProductId, ApplicationErrorCodes.ProductNotFound, "Product was not found.");
                continue;
            }

            if (CatalogConcurrency.IsStaleOrMissing(item.ExpectedUpdatedAtUtc, product.UpdatedAtUtc))
            {
                results[i] = Fail(
                    item.ProductId,
                    ApplicationErrorCodes.CatalogConcurrencyConflict,
                    item.ExpectedUpdatedAtUtc is null
                        ? "ExpectedUpdatedAtUtc is required for Today's Prices updates."
                        : "The product was updated concurrently. Reload the latest version and try again.");
                continue;
            }

            try
            {
                var changed = product.UpdateSellingPrice(item.SellingPrice, now);
                if (changed)
                {
                    await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
                    var productUnits = await _units
                        .ListByProductAsync(orgId, product.Id, cancellationToken)
                        .ConfigureAwait(false);
                    var primarySell = productUnits
                        .Where(u => u.IsActive && u.Kind == ProductUnitKind.Sell)
                        .OrderBy(u => u.SortOrder)
                        .ThenBy(u => u.DisplayName)
                        .FirstOrDefault();
                    if (primarySell is not null)
                    {
                        primarySell.Update(
                            primarySell.DisplayName,
                            primarySell.ShortLabel,
                            primarySell.MultiplierToBase,
                            now,
                            sellingPrice: item.SellingPrice,
                            allowsCustomQuantity: primarySell.AllowsCustomQuantity,
                            sortOrder: primarySell.SortOrder);
                        await _units.UpdateAsync(primarySell, cancellationToken).ConfigureAwait(false);
                    }

                    anyChanged = true;
                }

                productsById[item.ProductId] = product;
                results[i] = new UpdatePosCatalogProductPriceResultItem(
                    item.ProductId,
                    Succeeded: true,
                    Changed: changed,
                    Product: null);
            }
            catch (DomainException ex)
            {
                results[i] = Fail(item.ProductId, ex.ErrorCode, ex.Message);
            }
            catch (PersistenceConflictException ex)
            {
                results[i] = Fail(item.ProductId, ex.ErrorCode, ex.Message);
            }
        }

        if (anyChanged)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var accounts = await _inventory
            .ListByProductIdsAsync(
                orgId,
                productsById.Values.Select(p => p.Id).ToList(),
                cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

        for (var i = 0; i < results.Length; i++)
        {
            var row = results[i];
            if (!row.Succeeded || !productsById.TryGetValue(row.ProductId, out var product))
            {
                continue;
            }

            accountsByProduct.TryGetValue(row.ProductId, out var account);
            results[i] = row with { Product = CatalogProductQueryService.Map(product, account) };
        }

        var succeeded = results.Count(r => r.Succeeded);
        var failed = results.Length - succeeded;
        var changedCount = results.Count(r => r is { Succeeded: true, Changed: true });
        return ApplicationResult<UpdatePosCatalogProductPricesResponse>.Success(
            new UpdatePosCatalogProductPricesResponse(results, succeeded, failed, changedCount));
    }

    private static UpdatePosCatalogProductPriceResultItem Fail(Guid productId, string code, string message) =>
        new(productId, Succeeded: false, Changed: false, Product: null, code, message);
}

/// <summary>
/// Stages a new catalog product (and default units) without calling SaveChanges.
/// Callers that need multi-aggregate atomicity must persist once after staging.
/// </summary>
internal static class CatalogProductCreateCore
{
    public static async Task<ApplicationResult<CatalogProduct>> StageAsync(
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IProductCategoryRepository categories,
        IClock clock,
        ISupplierProductExposureRepository? exposures,
        Guid organizationId,
        string name,
        string unitOfMeasure,
        decimal sellingPrice,
        string? description,
        string? sku,
        string? barcode,
        Guid? categoryId,
        Guid? clientProductId,
        string? sellingMode,
        bool tracksExpiration,
        int? expirationWarningDays,
        bool? canBePurchased,
        bool? canBeSold,
        bool? canBeUsedAsIngredient,
        bool? isProduced,
        string? usagePreset,
        IReadOnlyList<PosCatalogProductUnitInput>? unitInputs,
        bool canExposeToConnectedBuyers,
        decimal? defaultConnectedPoPrice,
        CancellationToken cancellationToken)
    {
        var orgId = PosOrganizationId.From(organizationId);

        if (clientProductId is not null)
        {
            var existingById = await products
                .GetByIdAsync(orgId, CatalogProductId.From(clientProductId.Value), cancellationToken)
                .ConfigureAwait(false);
            if (existingById is not null)
            {
                return ApplicationResult<CatalogProduct>.Success(existingById);
            }
        }

        var unit = UnitOfMeasures.Parse(unitOfMeasure);
        var mode = SellingModes.Parse(sellingMode);
        var usage = CatalogProductUnitHelpers.ResolveUsage(
            canBePurchased,
            canBeSold,
            canBeUsedAsIngredient,
            isProduced,
            usagePreset);
        ProductCategoryId? category = null;
        if (categoryId is not null)
        {
            var assignable = await CatalogAssignment
                .EnsureAssignableCategoryAsync(categories, orgId, categoryId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (!assignable.IsSuccess)
            {
                return ApplicationResult<CatalogProduct>.Failure(assignable.ErrorCode!, assignable.ErrorMessage!);
            }

            category = ProductCategoryId.From(categoryId.Value);
        }

        var now = clock.UtcNow;
        var product = CatalogProduct.Create(
            orgId,
            name,
            unit,
            sellingPrice,
            now,
            description,
            sku,
            barcode,
            category,
            clientProductId is null ? null : CatalogProductId.From(clientProductId.Value),
            mode,
            tracksExpiration,
            expirationWarningDays,
            usage);
        var conflict = await CatalogAssignment
            .FindIdentifierConflictAsync(
                products,
                orgId,
                product.NormalizedSku,
                product.Barcode,
                selfId: null,
                cancellationToken)
            .ConfigureAwait(false);
        if (conflict is not null)
        {
            return ApplicationResult<CatalogProduct>.Failure(conflict.ErrorCode!, conflict.ErrorMessage!);
        }

        await products.AddAsync(product, cancellationToken).ConfigureAwait(false);

        var seedUnits = unitInputs is { Count: > 0 }
            ? unitInputs.Select(u => CatalogProductUnitHelpers.CreateFromInput(orgId, product.Id, u, now)).ToList()
            : CatalogProductUnitHelpers.CreateDefaultOneToOneUnits(orgId, product, now).ToList();

        var primarySellPrice = CatalogProductUnitHelpers.PrimarySellUnitPrice(seedUnits);
        if (primarySellPrice is not null && primarySellPrice.Value != product.SellingPrice)
        {
            product.UpdateSellingPrice(primarySellPrice.Value, now);
            await products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        }

        if (canExposeToConnectedBuyers)
        {
            product.EnableConnectedBuyerAvailability(now);
        }
        else
        {
            // Explicit false: global-block (Create defaults to eligible).
            product.DisableConnectedBuyerAvailability(now);
        }

        // Product create defaults Default PO to retail. An explicit value wins.
        // Later retail edits do not rewrite a stored Default PO (update path).
        product.SetDefaultConnectedPoPrice(defaultConnectedPoPrice ?? product.SellingPrice, now);
        await products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);

        foreach (var seed in seedUnits)
        {
            await units.AddAsync(seed, cancellationToken).ConfigureAwait(false);
        }

        await ConnectedProductExposureSync.SyncAsync(product, exposures, now, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<CatalogProduct>.Success(product);
    }
}

internal static class CatalogAssignment
{
    public static async Task<ApplicationResult> EnsureAssignableCategoryAsync(
        IProductCategoryRepository categories,
        PosOrganizationId organizationId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var category = await categories
            .GetByIdAsync(organizationId, ProductCategoryId.From(categoryId), cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CategoryNotFound,
                "Category was not found.");
        }

        if (category.Status != ProductCategoryStatus.Active)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CategoryNotAssignable,
                "Only active categories can be assigned to a product.");
        }

        return ApplicationResult.Success();
    }

    public static async Task<ApplicationResult?> FindIdentifierConflictAsync(
        ICatalogProductRepository products,
        PosOrganizationId organizationId,
        string? normalizedSku,
        string? barcode,
        CatalogProductId? selfId,
        CancellationToken cancellationToken)
    {
        if (normalizedSku is not null)
        {
            var bySku = await products
                .FindByNormalizedSkuAsync(organizationId, normalizedSku, cancellationToken)
                .ConfigureAwait(false);
            if (bySku is not null && (selfId is null || bySku.Id != selfId))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.ProductSkuConflict,
                    "This SKU is already used by another product in this organization, including inactive products.");
            }
        }

        if (barcode is not null)
        {
            var byBarcode = await products
                .FindByBarcodeAsync(organizationId, barcode, cancellationToken)
                .ConfigureAwait(false);
            if (byBarcode is not null && (selfId is null || byBarcode.Id != selfId))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.ProductBarcodeConflict,
                    "This barcode is already used by another product in this organization, including inactive products.");
            }
        }

        return null;
    }
}
