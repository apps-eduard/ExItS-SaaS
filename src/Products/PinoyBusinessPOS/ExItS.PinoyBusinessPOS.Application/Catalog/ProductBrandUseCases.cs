using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed class ProductBrandQueryService
{
    private readonly IProductBrandRepository _brands;

    public ProductBrandQueryService(IProductBrandRepository brands) => _brands = brands;

    public async Task<PosProductBrandDto?> GetByIdAsync(
        Guid organizationId,
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        var brand = await _brands
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                ProductBrandId.From(brandId),
                cancellationToken)
            .ConfigureAwait(false);
        return brand is null ? null : Map(brand);
    }

    public async Task<PagedResult<PosProductBrandDto>> ListAsync(
        Guid organizationId,
        ProductBrandStatus? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _brands
            .ListAsync(PosOrganizationId.From(organizationId), status, search, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosProductBrandDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static PosProductBrandDto Map(ProductBrand brand) =>
        new(
            brand.Id.Value,
            brand.OrganizationId.Value,
            brand.Name,
            brand.Status.ToString(),
            brand.CreatedAtUtc,
            brand.UpdatedAtUtc);
}

public sealed class CreateProductBrand
{
    private readonly IProductBrandRepository _brands;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateProductBrand(
        IProductBrandRepository brands,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _brands = brands;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductBrand>> ExecuteAsync(
        Guid organizationId,
        string name,
        Guid? clientBrandId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);

            if (clientBrandId is not null)
            {
                var existingById = await _brands
                    .GetByIdAsync(orgId, ProductBrandId.From(clientBrandId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existingById is not null)
                {
                    return ApplicationResult<ProductBrand>.Success(existingById);
                }
            }

            var brand = clientBrandId is null
                ? ProductBrand.Create(orgId, name, _clock.UtcNow)
                : ProductBrand.Create(
                    orgId,
                    name,
                    _clock.UtcNow,
                    ProductBrandId.From(clientBrandId.Value));

            var duplicate = await _brands
                .FindActiveByNormalizedNameAsync(orgId, brand.NormalizedName, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate is not null)
            {
                return ApplicationResult<ProductBrand>.Failure(
                    ApplicationErrorCodes.BrandNameConflict,
                    "An active brand with this name already exists in this organization.");
            }

            await _brands.AddAsync(brand, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductBrand>.Success(brand);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductBrand>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ProductBrand>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateProductBrand
{
    private readonly IProductBrandRepository _brands;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateProductBrand(
        IProductBrandRepository brands,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _brands = brands;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductBrand>> ExecuteAsync(
        Guid organizationId,
        Guid brandId,
        string name,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var brand = await _brands
            .GetByIdAsync(orgId, ProductBrandId.From(brandId), cancellationToken)
            .ConfigureAwait(false);
        if (brand is null)
        {
            return ApplicationResult<ProductBrand>.Failure(
                ApplicationErrorCodes.BrandNotFound,
                "Brand was not found.");
        }

        if (CatalogConcurrency.IsStale(expectedUpdatedAtUtc, brand.UpdatedAtUtc))
        {
            return ApplicationResult<ProductBrand>.Failure(
                ApplicationErrorCodes.CatalogConcurrencyConflict,
                "The brand was updated concurrently. Reload the latest version and try again.");
        }

        try
        {
            var normalized = ProductBrand.NormalizeForLookup(name);
            if (!string.Equals(normalized, brand.NormalizedName, StringComparison.Ordinal))
            {
                var duplicate = await _brands
                    .FindActiveByNormalizedNameAsync(orgId, normalized, cancellationToken)
                    .ConfigureAwait(false);
                if (duplicate is not null && duplicate.Id != brand.Id)
                {
                    return ApplicationResult<ProductBrand>.Failure(
                        ApplicationErrorCodes.BrandNameConflict,
                        "An active brand with this name already exists in this organization.");
                }
            }

            brand.Rename(name, _clock.UtcNow);
            await _brands.UpdateAsync(brand, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductBrand>.Success(brand);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductBrand>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ProductBrand>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Deactivates a brand without deleting or reassigning products. Products keep their brand
/// reference; only new assignments require an Active brand.
/// </summary>
public sealed class DeactivateProductBrand
{
    private readonly IProductBrandRepository _brands;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivateProductBrand(
        IProductBrandRepository brands,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _brands = brands;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductBrand>> ExecuteAsync(
        Guid organizationId,
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        var brand = await _brands
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                ProductBrandId.From(brandId),
                cancellationToken)
            .ConfigureAwait(false);
        if (brand is null)
        {
            return ApplicationResult<ProductBrand>.Failure(
                ApplicationErrorCodes.BrandNotFound,
                "Brand was not found.");
        }

        try
        {
            brand.Deactivate(_clock.UtcNow);
            await _brands.UpdateAsync(brand, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductBrand>.Success(brand);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductBrand>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ProductBrand>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReactivateProductBrand
{
    private readonly IProductBrandRepository _brands;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivateProductBrand(
        IProductBrandRepository brands,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _brands = brands;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductBrand>> ExecuteAsync(
        Guid organizationId,
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var brand = await _brands
            .GetByIdAsync(orgId, ProductBrandId.From(brandId), cancellationToken)
            .ConfigureAwait(false);
        if (brand is null)
        {
            return ApplicationResult<ProductBrand>.Failure(
                ApplicationErrorCodes.BrandNotFound,
                "Brand was not found.");
        }

        try
        {
            var duplicate = await _brands
                .FindActiveByNormalizedNameAsync(orgId, brand.NormalizedName, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate is not null && duplicate.Id != brand.Id)
            {
                return ApplicationResult<ProductBrand>.Failure(
                    ApplicationErrorCodes.BrandNameConflict,
                    "An active brand with this name already exists in this organization.");
            }

            brand.Reactivate(_clock.UtcNow);
            await _brands.UpdateAsync(brand, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductBrand>.Success(brand);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductBrand>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ProductBrand>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
