using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed class GetCustomerStorefront
{
    private readonly ISellerCustomerOrderingCapability _capability;
    private readonly ICatalogProductRepository _products;
    private readonly IProductCategoryRepository _categories;
    private readonly IInventoryRepository _inventory;
    private readonly ICustomerOrderBranchDirectory _branches;
    private readonly ICatalogProductImageRepository _images;
    private readonly IPlatformMerchantCatalogClient? _platform;
    private readonly IInventoryBranchBalanceRepository? _branchBalances;
    private readonly ICatalogProductAvailabilityResolver? _availability;
    private readonly IEffectivePriceResolver? _effectivePrices;

    public GetCustomerStorefront(
        ISellerCustomerOrderingCapability capability,
        ICatalogProductRepository products,
        IProductCategoryRepository categories,
        IInventoryRepository inventory,
        ICustomerOrderBranchDirectory branches,
        ICatalogProductImageRepository images,
        IPlatformMerchantCatalogClient? platform = null,
        IInventoryBranchBalanceRepository? branchBalances = null,
        ICatalogProductAvailabilityResolver? availability = null,
        IEffectivePriceResolver? effectivePrices = null)
    {
        _capability = capability;
        _products = products;
        _categories = categories;
        _inventory = inventory;
        _branches = branches;
        _images = images;
        _platform = platform;
        _branchBalances = branchBalances;
        _availability = availability;
        _effectivePrices = effectivePrices;
    }

    public async Task<ApplicationResult<CustomerStorefrontDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        string? search,
        Guid? categoryId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default,
        Guid? fulfillmentBranchId = null)
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

        var branches = await _branches
            .ListBranchesAsync(sellerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var selectedBranchId = ResolveStorefrontBranchId(branches, fulfillmentBranchId);
        if (fulfillmentBranchId is Guid requested
            && requested != Guid.Empty
            && selectedBranchId is null)
        {
            return ApplicationResult<CustomerStorefrontDto>.Failure(
                ApplicationErrorCodes.CustomerOrderBranchNotFound,
                "The selected fulfillment branch was not found for this merchant.");
        }

        if (_availability is not null && selectedBranchId is Guid offerBranchId && sellable.Count > 0)
        {
            var offerings = await _availability
                .ResolveForBranchAsync(
                    orgId,
                    PosBranchId.From(offerBranchId),
                    sellable,
                    cancellationToken)
                .ConfigureAwait(false);
            sellable = sellable
                .Where(p => offerings.TryGetValue(p.Id.Value, out var offer) && offer.IsOffered)
                .ToList();
        }

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
        var liveVersions = await CatalogPlatformImageMeta
            .TryGetVersionsBestEffortAsync(
                _platform,
                pageItems,
                imageByProduct,
                TimeSpan.FromSeconds(2),
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<InventoryBranchBalance> balances = [];
        if (_branchBalances is not null && productIds.Count > 0 && selectedBranchId is not null)
        {
            balances = await _branchBalances
                .ListByProductIdsAsync(orgId, productIds, cancellationToken)
                .ConfigureAwait(false);
        }

        var primaryBranchId = branches.FirstOrDefault(b => b.IsPrimary)?.BranchId;
        IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult>? effectivePrices = null;
        if (_effectivePrices is not null && selectedBranchId is Guid priceBranchId && pageItems.Count > 0)
        {
            effectivePrices = await _effectivePrices
                .ResolveAsync(
                    orgId,
                    PosBranchId.From(priceBranchId),
                    pageItems,
                    unitsByProduct: null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var products = pageItems.Select(p =>
        {
            accountByProduct.TryGetValue(p.Id.Value, out var account);
            var availability = ResolveAvailability(account, selectedBranchId, primaryBranchId, balances, p.Id);
            imageByProduct.TryGetValue(p.Id.Value, out var image);
            liveVersions.TryGetValue(p.Id.Value, out var liveVersion);
            var resolved = CatalogProductImageResolution.Resolve(p, image, liveVersion);
            var unitPrice = effectivePrices?.TryGetValue(
                    EffectivePriceKeys.ForBaseProduct(p.Id.Value),
                    out var priceResult) == true
                ? priceResult.EffectivePrice
                : p.SellingPrice;

            return new CustomerStorefrontProductDto(
                p.Id.Value,
                p.Name,
                p.Sku,
                p.UnitOfMeasure.ToString(),
                p.CategoryId?.Value,
                unitPrice,
                availability.IsAvailable,
                availability.TracksInventory,
                availability.AvailableQuantity,
                availability.Status,
                resolved.HasImage,
                resolved.ImageVersion,
                resolved.Source);
        }).ToList();

        var (categoryRows, _) = await _categories
            .ListAsync(orgId, ProductCategoryStatus.Active, search: null, skip: 0, take: 100, cancellationToken)
            .ConfigureAwait(false);
        var categories = categoryRows
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new CustomerStorefrontCategoryDto(c.Id.Value, c.Name))
            .ToList();

        var branchDtos = branches
            .Where(b => b.CustomerOrderingEnabled && (b.PickupEnabled || b.DeliveryEnabled))
            .Select(b => new CustomerStorefrontBranchDto(
                b.BranchId,
                b.Name,
                b.PickupEnabled,
                b.DeliveryEnabled && capability.CanCustomerDelivery,
                b.CustomerOrderingOperational,
                b.PickupOperational,
                b.DeliveryOperational && capability.CanCustomerDelivery,
                b.OnlineOrdersPaused,
                b.StoreStatusMessage,
                (b.DeliveryServiceAreas ?? [])
                    .Select(a => new CustomerStorefrontDeliveryServiceAreaDto(
                        a.Id,
                        a.CityMunicipalityName,
                        a.RegionOrProvinceName))
                    .ToList()))
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

    private static Guid? ResolveStorefrontBranchId(
        IReadOnlyList<CustomerOrderBranchSnapshot> branches,
        Guid? requested)
    {
        if (requested is Guid id && id != Guid.Empty)
        {
            return branches.Any(b => b.BranchId == id) ? id : null;
        }

        var eligible = branches
            .Where(b => b.CustomerOrderingEnabled && (b.PickupEnabled || b.DeliveryEnabled))
            .ToList();
        if (eligible.Count == 1)
        {
            return eligible[0].BranchId;
        }

        return branches.FirstOrDefault(b => b.IsPrimary)?.BranchId
            ?? eligible.FirstOrDefault()?.BranchId;
    }

    private static StorefrontAvailabilitySnapshot ResolveAvailability(
        InventoryAccount? account,
        Guid? selectedBranchId,
        Guid? primaryBranchId,
        IReadOnlyList<InventoryBranchBalance> balances,
        CatalogProductId productId)
    {
        if (account is null || !account.IsTracked || selectedBranchId is not Guid branchId)
        {
            return CustomerStorefrontAvailability.FromAccount(account);
        }

        var onHand = BranchStockResolver.ResolveOnHand(
            PosBranchId.From(branchId),
            primaryBranchId,
            account.OnHandQuantity,
            balances,
            productId);
        var reserved = BranchStockResolver.ResolveReserved(
            PosBranchId.From(branchId),
            balances,
            productId);
        var available = BranchStockResolver.ResolveAvailable(onHand, reserved);
        return CustomerStorefrontAvailability.FromTrackedQuantity(available);
    }
}
