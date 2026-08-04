using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

public sealed class GlobalCategoryQueryService
{
    private readonly IGlobalCategoryRepository _categories;

    public GlobalCategoryQueryService(IGlobalCategoryRepository categories) => _categories = categories;

    public async Task<GlobalCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(GlobalCategoryId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return category is null ? null : GlobalCatalogDtoMaps.Map(category);
    }

    public async Task<PagedResult<GlobalCategoryDto>> ListAsync(
        GlobalCategoryStatus? status,
        Guid? parentId,
        BusinessType? businessType,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _categories
            .ListAsync(
                status,
                parentId is null ? null : GlobalCategoryId.From(parentId.Value),
                businessType,
                search,
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<GlobalCategoryDto>(
            items.Select(GlobalCatalogDtoMaps.Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class GlobalProductQueryService
{
    private readonly IGlobalProductRepository _products;

    public GlobalProductQueryService(IGlobalProductRepository products) => _products = products;

    public async Task<GlobalProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(GlobalProductId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return product is null ? null : GlobalCatalogDtoMaps.Map(product);
    }

    public async Task<PagedResult<GlobalProductDto>> ListAsync(
        GlobalProductStatus? status,
        Guid? categoryId,
        BusinessType? businessType,
        string? search,
        string? barcode,
        string? sku,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _products
            .ListAsync(
                status,
                categoryId is null ? null : GlobalCategoryId.From(categoryId.Value),
                businessType,
                search,
                barcode,
                sku,
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<GlobalProductDto>(
            items.Select(GlobalCatalogDtoMaps.Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class CreateGlobalCategory
{
    private readonly IGlobalCategoryRepository _categories;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateGlobalCategory(
        IGlobalCategoryRepository categories,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<GlobalCategoryDto>> ExecuteAsync(
        CreateGlobalCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parentId = request.ParentId is null ? null : GlobalCategoryId.From(request.ParentId.Value);
            if (parentId is not null)
            {
                var parent = await _categories.GetByIdAsync(parentId, cancellationToken).ConfigureAwait(false);
                if (parent is null)
                {
                    return ApplicationResult<GlobalCategoryDto>.Failure(
                        ApplicationErrorCodes.GlobalCategoryNotFound,
                        "Parent category was not found.");
                }
            }

            var businessTypes = GlobalCatalogUseCaseHelpers.ParseBusinessTypes(request.BusinessTypes);
            var name = GlobalCatalogRules.NormalizeName(request.Name);
            if (await _categories
                    .ExistsWithNameUnderParentAsync(name, parentId, excludingId: null, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<GlobalCategoryDto>.Failure(
                    ApplicationErrorCodes.DuplicateGlobalCategoryName,
                    "A category with this name already exists under the same parent.");
            }

            var category = GlobalCategory.Create(
                name,
                _clock.UtcNow,
                parentId,
                request.IconReference,
                request.SortOrder,
                businessTypes);

            await _categories.AddAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<GlobalCategoryDto>.Success(GlobalCatalogDtoMaps.Map(category));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateGlobalCategory
{
    private readonly IGlobalCategoryRepository _categories;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateGlobalCategory(
        IGlobalCategoryRepository categories,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<GlobalCategoryDto>> ExecuteAsync(
        Guid id,
        UpdateGlobalCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(GlobalCategoryId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(
                ApplicationErrorCodes.GlobalCategoryNotFound,
                "Category was not found.");
        }

        if (IsConcurrencyMismatch(category.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The category was modified by another request. Refresh and try again.");
        }

        try
        {
            var parentId = request.ParentId is null ? null : GlobalCategoryId.From(request.ParentId.Value);
            if (parentId is not null)
            {
                var parent = await _categories.GetByIdAsync(parentId, cancellationToken).ConfigureAwait(false);
                if (parent is null)
                {
                    return ApplicationResult<GlobalCategoryDto>.Failure(
                        ApplicationErrorCodes.GlobalCategoryNotFound,
                        "Parent category was not found.");
                }
            }

            var now = _clock.UtcNow;
            category.Rename(request.Name, now);
            category.SetParent(parentId, now);
            category.SetSortOrder(request.SortOrder, now);
            category.SetIcon(request.IconReference, now);
            category.AssignBusinessTypes(GlobalCatalogUseCaseHelpers.ParseBusinessTypes(request.BusinessTypes), now);

            if (await _categories
                    .ExistsWithNameUnderParentAsync(category.Name, category.ParentId, category.Id, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<GlobalCategoryDto>.Failure(
                    ApplicationErrorCodes.DuplicateGlobalCategoryName,
                    "A category with this name already exists under the same parent.");
            }

            await _categories.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<GlobalCategoryDto>.Success(GlobalCatalogDtoMaps.Map(category));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static bool IsConcurrencyMismatch(DateTimeOffset current, DateTimeOffset? expected) =>
        expected is not null
        && current.ToUnixTimeMilliseconds() != expected.Value.ToUnixTimeMilliseconds();
}

public sealed class SetGlobalCategoryStatus
{
    private readonly IGlobalCategoryRepository _categories;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetGlobalCategoryStatus(
        IGlobalCategoryRepository categories,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<GlobalCategoryDto>> ExecuteAsync(
        Guid id,
        SetGlobalCategoryStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(GlobalCategoryId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(
                ApplicationErrorCodes.GlobalCategoryNotFound,
                "Category was not found.");
        }

        if (UpdateGlobalCategory.IsConcurrencyMismatch(category.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The category was modified by another request. Refresh and try again.");
        }

        if (!Enum.TryParse<GlobalCategoryStatus>(request.Status, ignoreCase: true, out var status))
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(
                DomainErrorCodes.InvalidGlobalCategoryStatusTransition,
                $"Unrecognized category status '{request.Status}'.");
        }

        try
        {
            category.SetStatus(status, _clock.UtcNow);
            await _categories.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<GlobalCategoryDto>.Success(GlobalCatalogDtoMaps.Map(category));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CreateGlobalProduct
{
    private readonly IGlobalProductRepository _products;
    private readonly IGlobalCategoryRepository _categories;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateGlobalProduct(
        IGlobalProductRepository products,
        IGlobalCategoryRepository categories,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<GlobalProductDto>> ExecuteAsync(
        CreateGlobalProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<ProductUnit>(request.Unit, ignoreCase: true, out var unit))
            {
                return ApplicationResult<GlobalProductDto>.Failure(
                    DomainErrorCodes.InvalidGlobalProductUnit,
                    $"Unrecognized product unit '{request.Unit}'.");
            }

            var categoryId = request.GlobalCategoryId is null
                ? null
                : GlobalCategoryId.From(request.GlobalCategoryId.Value);
            if (categoryId is not null)
            {
                var category = await _categories.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false);
                if (category is null)
                {
                    return ApplicationResult<GlobalProductDto>.Failure(
                        ApplicationErrorCodes.GlobalCategoryNotFound,
                        "Category was not found.");
                }
            }

            var barcode = GlobalCatalogRules.NormalizeBarcode(request.Barcode);
            var sku = GlobalCatalogRules.NormalizeSku(request.Sku);

            if (barcode is not null
                && await _products.ExistsWithBarcodeAsync(barcode, excludingId: null, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<GlobalProductDto>.Failure(
                    ApplicationErrorCodes.DuplicateGlobalProductBarcode,
                    "A product with this barcode already exists.");
            }

            if (sku is not null
                && await _products.ExistsWithSkuAsync(sku, excludingId: null, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<GlobalProductDto>.Failure(
                    ApplicationErrorCodes.DuplicateGlobalProductSku,
                    "A product with this SKU already exists.");
            }

            var product = GlobalProduct.Create(
                request.Name,
                unit,
                _clock.UtcNow,
                request.Description,
                sku,
                barcode,
                categoryId,
                request.SuggestedPrice,
                request.SuggestedCost,
                request.ImageReference,
                request.SearchTags,
                GlobalCatalogUseCaseHelpers.ParseBusinessTypes(request.BusinessTypes));

            await _products.AddAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<GlobalProductDto>.Success(GlobalCatalogDtoMaps.Map(product));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<GlobalProductDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<GlobalProductDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateGlobalProduct
{
    private readonly IGlobalProductRepository _products;
    private readonly IGlobalCategoryRepository _categories;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateGlobalProduct(
        IGlobalProductRepository products,
        IGlobalCategoryRepository categories,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<GlobalProductDto>> ExecuteAsync(
        Guid id,
        UpdateGlobalProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(GlobalProductId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<GlobalProductDto>.Failure(
                ApplicationErrorCodes.GlobalProductNotFound,
                "Product was not found.");
        }

        if (IsConcurrencyMismatch(product.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<GlobalProductDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The product was modified by another request. Refresh and try again.");
        }

        try
        {
            if (!Enum.TryParse<ProductUnit>(request.Unit, ignoreCase: true, out var unit))
            {
                return ApplicationResult<GlobalProductDto>.Failure(
                    DomainErrorCodes.InvalidGlobalProductUnit,
                    $"Unrecognized product unit '{request.Unit}'.");
            }

            var categoryId = request.GlobalCategoryId is null
                ? null
                : GlobalCategoryId.From(request.GlobalCategoryId.Value);
            if (categoryId is not null)
            {
                var category = await _categories.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false);
                if (category is null)
                {
                    return ApplicationResult<GlobalProductDto>.Failure(
                        ApplicationErrorCodes.GlobalCategoryNotFound,
                        "Category was not found.");
                }
            }

            var barcode = GlobalCatalogRules.NormalizeBarcode(request.Barcode);
            var sku = GlobalCatalogRules.NormalizeSku(request.Sku);

            if (barcode is not null
                && await _products.ExistsWithBarcodeAsync(barcode, product.Id, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<GlobalProductDto>.Failure(
                    ApplicationErrorCodes.DuplicateGlobalProductBarcode,
                    "A product with this barcode already exists.");
            }

            if (sku is not null
                && await _products.ExistsWithSkuAsync(sku, product.Id, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<GlobalProductDto>.Failure(
                    ApplicationErrorCodes.DuplicateGlobalProductSku,
                    "A product with this SKU already exists.");
            }

            product.Update(
                request.Name,
                unit,
                _clock.UtcNow,
                request.Description,
                sku,
                barcode,
                categoryId,
                request.SuggestedPrice,
                request.SuggestedCost,
                request.ImageReference,
                request.SearchTags,
                GlobalCatalogUseCaseHelpers.ParseBusinessTypes(request.BusinessTypes));

            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<GlobalProductDto>.Success(GlobalCatalogDtoMaps.Map(product));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<GlobalProductDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<GlobalProductDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static bool IsConcurrencyMismatch(DateTimeOffset current, DateTimeOffset? expected) =>
        expected is not null
        && current.ToUnixTimeMilliseconds() != expected.Value.ToUnixTimeMilliseconds();
}

public sealed class SetGlobalProductStatus
{
    private readonly IGlobalProductRepository _products;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetGlobalProductStatus(
        IGlobalProductRepository products,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<GlobalProductDto>> ExecuteAsync(
        Guid id,
        SetGlobalProductStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(GlobalProductId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<GlobalProductDto>.Failure(
                ApplicationErrorCodes.GlobalProductNotFound,
                "Product was not found.");
        }

        if (UpdateGlobalProduct.IsConcurrencyMismatch(product.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<GlobalProductDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The product was modified by another request. Refresh and try again.");
        }

        if (!Enum.TryParse<GlobalProductStatus>(request.Status, ignoreCase: true, out var status))
        {
            return ApplicationResult<GlobalProductDto>.Failure(
                DomainErrorCodes.InvalidGlobalProductStatusTransition,
                $"Unrecognized product status '{request.Status}'.");
        }

        try
        {
            product.SetStatus(status, _clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<GlobalProductDto>.Success(GlobalCatalogDtoMaps.Map(product));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<GlobalProductDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<GlobalProductDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class GlobalCatalogUseCaseHelpers
{
    public static IReadOnlyList<BusinessType> ParseBusinessTypes(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<BusinessType>();
        }

        var parsed = new List<BusinessType>(values.Count);
        foreach (var value in values)
        {
            if (!Enum.TryParse<BusinessType>(value, ignoreCase: true, out var type))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                    $"Unrecognized business type '{value}'.");
            }

            parsed.Add(type);
        }

        return GlobalCatalogRules.NormalizeBusinessTypes(parsed);
    }
}
