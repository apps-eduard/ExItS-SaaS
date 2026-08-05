using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed class CatalogProductQueryService
{
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryRepository _inventory;

    public CatalogProductQueryService(ICatalogProductRepository products, IInventoryRepository inventory)
    {
        _products = products;
        _inventory = inventory;
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
        return Map(product, account);
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

        return new PagedResult<PosCatalogProductDto>(
            items.Select(p => Map(p, accountsByProduct.GetValueOrDefault(p.Id.Value))).ToList(),
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
        return ApplicationResult<PosCatalogProductDto>.Success(Map(product, account));
    }

    public static PosCatalogProductDto Map(CatalogProduct product, InventoryAccount? account = null)
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
            stockStatus);
    }
}

public sealed class CreateCatalogProduct
{
    private readonly ICatalogProductRepository _products;
    private readonly IProductCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateCatalogProduct(
        ICatalogProductRepository products,
        IProductCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);

            if (clientProductId is not null)
            {
                var existingById = await _products
                    .GetByIdAsync(orgId, CatalogProductId.From(clientProductId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existingById is not null)
                {
                    return ApplicationResult<CatalogProduct>.Success(existingById);
                }
            }

            var unit = UnitOfMeasures.Parse(unitOfMeasure);
            ProductCategoryId? category = null;
            if (categoryId is not null)
            {
                var assignable = await CatalogAssignment
                    .EnsureAssignableCategoryAsync(_categories, orgId, categoryId.Value, cancellationToken)
                    .ConfigureAwait(false);
                if (!assignable.IsSuccess)
                {
                    return ApplicationResult<CatalogProduct>.Failure(assignable.ErrorCode!, assignable.ErrorMessage!);
                }

                category = ProductCategoryId.From(categoryId.Value);
            }

            var product = CatalogProduct.Create(
                orgId,
                name,
                unit,
                sellingPrice,
                _clock.UtcNow,
                description,
                sku,
                barcode,
                category,
                clientProductId is null ? null : CatalogProductId.From(clientProductId.Value));

            var conflict = await CatalogAssignment
                .FindIdentifierConflictAsync(
                    _products,
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

            await _products.AddAsync(product, cancellationToken).ConfigureAwait(false);
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

public sealed class UpdateCatalogProduct
{
    private readonly ICatalogProductRepository _products;
    private readonly IProductCategoryRepository _categories;
    private readonly IInventoryRepository _inventory;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateCatalogProduct(
        ICatalogProductRepository products,
        IProductCategoryRepository categories,
        IInventoryRepository inventory,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _categories = categories;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
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

            product.UpdateDetails(
                name,
                description,
                sku,
                barcode,
                category,
                unit,
                sellingPrice,
                _clock.UtcNow);

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
