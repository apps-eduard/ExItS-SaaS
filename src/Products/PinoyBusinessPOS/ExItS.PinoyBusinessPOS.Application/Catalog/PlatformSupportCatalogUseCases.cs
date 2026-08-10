using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Platform support read of an organization's POS catalog. Organization scope comes only from
/// the trusted path parameter — never from client body or merchant headers.
/// </summary>
public sealed class GetOrganizationCatalogForPlatformSupport
{
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly IProductCategoryRepository _categories;

    public GetOrganizationCatalogForPlatformSupport(
        ICatalogProductRepository products,
        IInventoryRepository inventory,
        IProductCategoryRepository categories)
    {
        _products = products;
        _inventory = inventory;
        _categories = categories;
    }

    public async Task<PlatformSupportOrganizationCatalogSummaryDto> ExecuteAsync(
        Guid organizationId,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var filter = new CatalogProductFilter(Search: search);

        var (items, totalCount) = await _products
            .ListAsync(orgId, filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var (productCount, sourceBreakdown) = await BuildOrgWideBreakdownAsync(orgId, cancellationToken)
            .ConfigureAwait(false);

        var accounts = await _inventory
            .ListByProductIdsAsync(orgId, items.Select(p => p.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

        var categoryIds = items
            .Where(p => p.CategoryId is not null)
            .Select(p => p.CategoryId!)
            .Distinct()
            .ToList();
        var categories = categoryIds.Count == 0
            ? Array.Empty<ProductCategory>()
            : await _categories.ListByIdsAsync(orgId, categoryIds, cancellationToken).ConfigureAwait(false);
        var categoryNames = categories.ToDictionary(c => c.Id.Value, c => c.Name);

        var products = items.Select(p =>
        {
            accountsByProduct.TryGetValue(p.Id.Value, out var account);
            var isTracked = account?.IsTracked ?? false;
            string? categoryName = null;
            if (p.CategoryId is not null)
            {
                categoryNames.TryGetValue(p.CategoryId.Value, out categoryName);
            }

            return new PlatformSupportOrganizationCatalogProductDto(
                p.Id.Value,
                p.Name,
                p.Sku,
                p.Barcode,
                p.CategoryId?.Value,
                categoryName,
                p.SellingPrice,
                isTracked,
                isTracked ? account?.OnHandQuantity ?? 0m : null,
                p.Status.ToString(),
                PlatformSupportCatalogProvenance.ResolveSourceType(p.PlatformTemplateId, p.PlatformGlobalProductId),
                p.PlatformGlobalProductId,
                p.PlatformTemplateId,
                p.CatalogImportedAt,
                p.CatalogSource.ToString());
        }).ToList();

        return new PlatformSupportOrganizationCatalogSummaryDto(
            organizationId,
            productCount,
            sourceBreakdown,
            products,
            Math.Max(page ?? 1, 1),
            take,
            totalCount);
    }

    private async Task<(int ProductCount, IReadOnlyDictionary<string, int> Breakdown)> BuildOrgWideBreakdownAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var breakdown = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [PlatformSupportCatalogProvenance.GlobalTemplate] = 0,
            [PlatformSupportCatalogProvenance.GlobalCatalog] = 0,
            [PlatformSupportCatalogProvenance.MerchantCreated] = 0
        };

        var emptyFilter = new CatalogProductFilter();
        var skip = 0;
        var take = PosPagination.MaxPageSize;
        int totalCount;
        do
        {
            var (chunk, total) = await _products
                .ListAsync(organizationId, emptyFilter, skip, take, cancellationToken)
                .ConfigureAwait(false);
            totalCount = total;
            foreach (var product in chunk)
            {
                var source = PlatformSupportCatalogProvenance.ResolveSourceType(
                    product.PlatformTemplateId,
                    product.PlatformGlobalProductId);
                breakdown[source] = breakdown.GetValueOrDefault(source) + 1;
            }

            skip += chunk.Count;
            if (chunk.Count == 0)
            {
                break;
            }
        }
        while (skip < totalCount);

        return (totalCount, breakdown);
    }
}
