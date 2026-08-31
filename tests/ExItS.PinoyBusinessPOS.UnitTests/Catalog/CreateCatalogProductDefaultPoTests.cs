using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CreateCatalogProductDefaultPoTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_defaults_default_po_price_to_retail_selling_price()
    {
        var products = new MemoryProducts();
        var useCase = Create(products);

        var result = await useCase.ExecuteAsync(OrgId, "Kopiko", "Piece", 18.50m);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var product = Assert.Single(products.Items);
        Assert.Equal(18.50m, product.SellingPrice);
        Assert.Equal(18.50m, product.DefaultConnectedPoPrice);
        Assert.True(product.CanExposeToConnectedBuyers);
    }

    [Fact]
    public async Task Create_keeps_explicit_default_po_price()
    {
        var products = new MemoryProducts();
        var useCase = Create(products);

        var result = await useCase.ExecuteAsync(
            OrgId,
            "Kopiko",
            "Piece",
            sellingPrice: 18.50m,
            defaultConnectedPoPrice: 12m);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var product = Assert.Single(products.Items);
        Assert.Equal(18.50m, product.SellingPrice);
        Assert.Equal(12m, product.DefaultConnectedPoPrice);
    }

    private static CreateCatalogProduct Create(MemoryProducts products) =>
        new(
            products,
            new MemoryUnits(),
            new MemoryCategories(),
            new MemoryBrands(),
            new ImmediateUnitOfWork(),
            new FixedClock(Now),
            new CatalogProductGovernanceAuthority(),
            FixedCatalogGovernanceActorAccessor.Owner());

    private sealed class ImmediateUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class MemoryBrands : IProductBrandRepository
    {
        public Task<ProductBrand?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductBrandId brandId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductBrand?>(null);

        public Task<ProductBrand?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductBrand?>(null);

        public Task<(IReadOnlyList<ProductBrand> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            ProductBrandStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<ProductBrand>, int)>(([], 0));

        public Task<IReadOnlyList<ProductBrand>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<ProductBrandId> brandIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductBrand>>([]);

        public Task AddAsync(ProductBrand brand, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(ProductBrand brand, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MemoryCategories : IProductCategoryRepository
    {
        public Task<ProductCategory?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductCategoryId categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductCategory?>(null);

        public Task<ProductCategory?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductCategory?>(null);

        public Task<ProductCategory?> FindActiveBySourceGlobalCategoryIdAsync(
            PosOrganizationId organizationId,
            Guid sourceGlobalCategoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductCategory?>(null);

        public Task<(IReadOnlyList<ProductCategory> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            ProductCategoryStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<ProductCategory>, int)>(([], 0));

        public Task<IReadOnlyList<ProductCategory>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<ProductCategoryId> categoryIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductCategory>>([]);

        public Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MemoryUnits : ICatalogProductUnitRepository
    {
        public Task<CatalogProductUnit?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductUnitId unitId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProductUnit?>(null);

        public Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductUnit>>([]);

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>>(
                new Dictionary<Guid, IReadOnlyList<CatalogProductUnit>>());

        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ReplaceActiveUnitsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MemoryProducts : ICatalogProductRepository
    {
        public List<CatalogProduct> Items { get; } = [];

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.Id == productId));

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId,
            string normalizedSku,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.NormalizedSku == normalizedSku));

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId,
            string barcode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.Barcode == barcode));

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                Items.Where(x => x.OrganizationId == organizationId && productIds.Contains(x.Id)).ToList());

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([], 0));

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)>
            CountConnectedBuyerAvailabilityAsync(
                PosOrganizationId organizationId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0, 0));

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>>
            ListConnectedBuyerAvailabilityCategoryFacetsAsync(
                PosOrganizationId organizationId,
                CatalogProductFilter filter,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId,
            Guid platformGlobalProductId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            Items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
