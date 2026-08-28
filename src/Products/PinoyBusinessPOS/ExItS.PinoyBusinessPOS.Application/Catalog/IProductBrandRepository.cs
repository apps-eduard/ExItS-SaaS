using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public interface IProductBrandRepository
{
    Task<ProductBrand?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductBrandId brandId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds an Active brand by normalized name. Inactive names are reusable.</summary>
    Task<ProductBrand?> FindActiveByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProductBrand> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ProductBrandStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductBrand>> ListByIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<ProductBrandId> brandIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductBrand brand, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductBrand brand, CancellationToken cancellationToken = default);
}
