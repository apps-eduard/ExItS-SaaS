using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed class GetCustomerStorefront
{
    private readonly ISellerCustomerOrderingCapability _capability;
    private readonly ICatalogProductRepository _products;
    private readonly IProductCategoryRepository _categories;
    private readonly IInventoryRepository _inventory;
    private readonly ICustomerOrderBranchDirectory _branches;
    private readonly ICatalogProductImageRepository _images;

    public GetCustomerStorefront(
        ISellerCustomerOrderingCapability capability,
        ICatalogProductRepository products,
        IProductCategoryRepository categories,
        IInventoryRepository inventory,
        ICustomerOrderBranchDirectory branches,
        ICatalogProductImageRepository images)
    {
        _capability = capability;
        _products = products;
        _categories = categories;
        _inventory = inventory;
        _branches = branches;
        _images = images;
    }

    public async Task<ApplicationResult<CustomerStorefrontDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        string? search,
        Guid? categoryId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        if (sellerOrganizationId == Guid.Empty)
        {
            return ApplicationResult<CustomerStorefrontDto>.Failure(
                ApplicationErrorCodes.OrganizationRequired,
                "Seller organization id is required.");
        }

        var capability = await _capability
            .ResolveAsync(sellerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (!capability.CanCustomerOrder)
        {
            return ApplicationResult<CustomerStorefrontDto>.Failure(
                ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                "This merchant is not accepting customer orders.");
        }

        var orgId = PosOrganizationId.From(sellerOrganizationId);
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var filter = new CatalogProductFilter(
            Status: CatalogProductStatus.Active,
            CategoryId: categoryId is Guid cid && cid != Guid.Empty
                ? ProductCategoryId.From(cid)
                : null,
            Search: string.IsNullOrWhiteSpace(search) ? null : search.Trim());

        // MVP-scale: load Active matches (capped) then filter sellable rows in memory.
        var (listed, _) = await _products
            .ListAsync(orgId, filter, skip: 0, take: PosPagination.MaxPageSize, cancellationToken)
            .ConfigureAwait(false);

        var sellable = listed
            .Where(p => p.CanBeSold && p.SellingPrice > 0m)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Id.Value)
            .ToList();

        var pageItems = sellable.Skip(skip).Take(take).ToList();
        var productIds = pageItems.Select(p => p.Id).ToList();
        var accounts = productIds.Count == 0
            ? []
            : await _inventory
                .ListByProductIdsAsync(orgId, productIds, cancellationToken)
                .ConfigureAwait(false);
        var accountByProduct = accounts.ToDictionary(a => a.ProductId.Value);
        var imageRows = productIds.Count == 0
            ? []
            : await _images.ListByProductIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
        var imageByProduct = imageRows.ToDictionary(i => i.ProductId.Value);

        var products = pageItems.Select(p =>
        {
            accountByProduct.TryGetValue(p.Id.Value, out var account);
            var availability = CustomerStorefrontAvailability.FromAccount(account);
            imageByProduct.TryGetValue(p.Id.Value, out var image);

            return new CustomerStorefrontProductDto(
                p.Id.Value,
                p.Name,
                p.Sku,
                p.UnitOfMeasure.ToString(),
                p.CategoryId?.Value,
                p.SellingPrice,
                availability.IsAvailable,
                availability.TracksInventory,
                availability.AvailableQuantity,
                availability.Status,
                image is not null,
                image?.Version);
        }).ToList();

        var (categoryRows, _) = await _categories
            .ListAsync(orgId, ProductCategoryStatus.Active, search: null, skip: 0, take: 100, cancellationToken)
            .ConfigureAwait(false);
        var categories = categoryRows
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new CustomerStorefrontCategoryDto(c.Id.Value, c.Name))
            .ToList();

        var branches = await _branches
            .ListBranchesAsync(sellerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var branchDtos = branches
            .Where(b => b.PickupEnabled || b.DeliveryEnabled)
            .Select(b => new CustomerStorefrontBranchDto(
                b.BranchId,
                b.Name,
                b.PickupEnabled,
                b.DeliveryEnabled && capability.CanCustomerDelivery))
            .ToList();

        return ApplicationResult<CustomerStorefrontDto>.Success(new CustomerStorefrontDto(
            sellerOrganizationId,
            string.IsNullOrWhiteSpace(capability.OrganizationDisplayName)
                ? string.Empty
                : capability.OrganizationDisplayName.Trim(),
            capability.CanCustomerOrder,
            capability.CanCustomerDelivery,
            categories,
            products,
            sellable.Count,
            page ?? 1,
            take,
            branchDtos));
    }
}
