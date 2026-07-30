using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed class ProductCategoryQueryService
{
    private readonly IProductCategoryRepository _categories;

    public ProductCategoryQueryService(IProductCategoryRepository categories) => _categories = categories;

    public async Task<PosProductCategoryDto?> GetByIdAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categories
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                ProductCategoryId.From(categoryId),
                cancellationToken)
            .ConfigureAwait(false);
        return category is null ? null : Map(category);
    }

    public async Task<PagedResult<PosProductCategoryDto>> ListAsync(
        Guid organizationId,
        ProductCategoryStatus? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _categories
            .ListAsync(PosOrganizationId.From(organizationId), status, search, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosProductCategoryDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static PosProductCategoryDto Map(ProductCategory category) =>
        new(
            category.Id.Value,
            category.OrganizationId.Value,
            category.Name,
            category.Status.ToString(),
            category.CreatedAtUtc,
            category.UpdatedAtUtc);
}

public sealed class CreateProductCategory
{
    private readonly IProductCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateProductCategory(
        IProductCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductCategory>> ExecuteAsync(
        Guid organizationId,
        string name,
        Guid? clientCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);

            if (clientCategoryId is not null)
            {
                var existingById = await _categories
                    .GetByIdAsync(orgId, ProductCategoryId.From(clientCategoryId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existingById is not null)
                {
                    return ApplicationResult<ProductCategory>.Success(existingById);
                }
            }

            var category = clientCategoryId is null
                ? ProductCategory.Create(orgId, name, _clock.UtcNow)
                : ProductCategory.Create(
                    orgId,
                    name,
                    _clock.UtcNow,
                    ProductCategoryId.From(clientCategoryId.Value));

            var duplicate = await _categories
                .FindActiveByNormalizedNameAsync(orgId, category.NormalizedName, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate is not null)
            {
                return ApplicationResult<ProductCategory>.Failure(
                    ApplicationErrorCodes.CategoryNameConflict,
                    "An active category with this name already exists in this organization.");
            }

            await _categories.AddAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductCategory>.Success(category);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductCategory>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ProductCategory>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateProductCategory
{
    private readonly IProductCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateProductCategory(
        IProductCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductCategory>> ExecuteAsync(
        Guid organizationId,
        Guid categoryId,
        string name,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var category = await _categories
            .GetByIdAsync(orgId, ProductCategoryId.From(categoryId), cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult<ProductCategory>.Failure(
                ApplicationErrorCodes.CategoryNotFound,
                "Category was not found.");
        }

        if (CatalogConcurrency.IsStale(expectedUpdatedAtUtc, category.UpdatedAtUtc))
        {
            return ApplicationResult<ProductCategory>.Failure(
                ApplicationErrorCodes.CatalogConcurrencyConflict,
                "The category was updated concurrently. Reload the latest version and try again.");
        }

        try
        {
            var normalized = ProductCategory.NormalizeForLookup(name);
            if (!string.Equals(normalized, category.NormalizedName, StringComparison.Ordinal))
            {
                var duplicate = await _categories
                    .FindActiveByNormalizedNameAsync(orgId, normalized, cancellationToken)
                    .ConfigureAwait(false);
                if (duplicate is not null && duplicate.Id != category.Id)
                {
                    return ApplicationResult<ProductCategory>.Failure(
                        ApplicationErrorCodes.CategoryNameConflict,
                        "An active category with this name already exists in this organization.");
                }
            }

            category.Rename(name, _clock.UtcNow);
            await _categories.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductCategory>.Success(category);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductCategory>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ProductCategory>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Deactivates a category without deleting or reassigning products. Products keep their category
/// reference; only new assignments require an Active category.
/// </summary>
public sealed class DeactivateProductCategory
{
    private readonly IProductCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivateProductCategory(
        IProductCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductCategory>> ExecuteAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categories
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                ProductCategoryId.From(categoryId),
                cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult<ProductCategory>.Failure(
                ApplicationErrorCodes.CategoryNotFound,
                "Category was not found.");
        }

        try
        {
            category.Deactivate(_clock.UtcNow);
            await _categories.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductCategory>.Success(category);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductCategory>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ProductCategory>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReactivateProductCategory
{
    private readonly IProductCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivateProductCategory(
        IProductCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductCategory>> ExecuteAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var category = await _categories
            .GetByIdAsync(orgId, ProductCategoryId.From(categoryId), cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult<ProductCategory>.Failure(
                ApplicationErrorCodes.CategoryNotFound,
                "Category was not found.");
        }

        try
        {
            var duplicate = await _categories
                .FindActiveByNormalizedNameAsync(orgId, category.NormalizedName, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate is not null && duplicate.Id != category.Id)
            {
                return ApplicationResult<ProductCategory>.Failure(
                    ApplicationErrorCodes.CategoryNameConflict,
                    "An active category with this name already exists in this organization.");
            }

            category.Reactivate(_clock.UtcNow);
            await _categories.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductCategory>.Success(category);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductCategory>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ProductCategory>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class CatalogConcurrency
{
    public static bool IsStale(DateTimeOffset? expectedUpdatedAtUtc, DateTimeOffset actualUpdatedAtUtc)
    {
        if (expectedUpdatedAtUtc is null)
        {
            return false;
        }

        return expectedUpdatedAtUtc.Value.ToUniversalTime().UtcTicks
               != actualUpdatedAtUtc.ToUniversalTime().UtcTicks;
    }
}
