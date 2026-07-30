using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public interface IProductCategoryRepository
{
    Task<ProductCategory?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductCategoryId categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds an Active category by normalized name. Inactive names are reusable.</summary>
    Task<ProductCategory?> FindActiveByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProductCategory> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ProductCategoryStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductCategory category, CancellationToken cancellationToken = default);
}
