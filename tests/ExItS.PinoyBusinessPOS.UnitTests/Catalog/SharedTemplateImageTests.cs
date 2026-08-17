using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogProductImageResolutionTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly DateTimeOffset Utc = DateTimeOffset.Parse("2026-08-17T22:00:00Z");

    [Fact]
    public void Merchant_override_wins_then_platform_then_placeholder()
    {
        var imported = CatalogProduct.CreateImportedSnapshot(
            Org,
            "Coke",
            UnitOfMeasure.Piece,
            12m,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CatalogSource.Template,
            Utc,
            platformImageVersion: 1);
        var overrideImage = CatalogProductImage.Create(
            Org,
            imported.Id,
            Guid.NewGuid(),
            3,
            100,
            100,
            400,
            400,
            Utc);

        var withOverride = CatalogProductImageResolution.Resolve(imported, overrideImage, livePlatformImageVersion: 2);
        Assert.Equal(CatalogProductImageSources.MerchantOverride, withOverride.Source);
        Assert.Equal(3, withOverride.ImageVersion);
        Assert.True(withOverride.HasMerchantOverride);

        var shared = CatalogProductImageResolution.Resolve(imported, null, livePlatformImageVersion: 2);
        Assert.Equal(CatalogProductImageSources.PlatformTemplate, shared.Source);
        Assert.Equal(2, shared.ImageVersion);
        Assert.False(shared.HasMerchantOverride);

        var none = CatalogProductImageResolution.Resolve(
            CatalogProduct.Create(Org, "Custom", UnitOfMeasure.Piece, 10m, Utc),
            null);
        Assert.Equal(CatalogProductImageSources.None, none.Source);
        Assert.False(none.HasImage);
    }
}

public sealed class SharedPlatformImageFallbackTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherOrg = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid GlobalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Utc = DateTimeOffset.Parse("2026-08-17T22:00:00Z");

    [Fact]
    public async Task Get_uses_shared_platform_bytes_when_org_has_no_override()
    {
        var product = CatalogProduct.CreateImportedSnapshot(
            PosOrganizationId.From(Org),
            "Coke",
            UnitOfMeasure.Piece,
            12m,
            GlobalId,
            CatalogSource.Template,
            Utc,
            platformImageVersion: 1);
        var products = new ImportedProducts(product);
        var images = new EmptyImages();
        var store = new MemoryStore();
        var shared = new byte[] { 1, 2, 3, 4 };
        var platform = new FakePlatform(shared, version: 4);
        var get = new GetCatalogProductImage(products, images, store, platform);

        var result = await get.ExecuteAsync(Org, product.Id.Value, ProductImageVariants.Thumb);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(shared, result.Value!.Content);
        Assert.Equal(4, result.Value.Version);
        Assert.Empty(store.Files);

        var other = await get.ExecuteAsync(OtherOrg, product.Id.Value, ProductImageVariants.Thumb);
        Assert.False(other.IsSuccess);
    }

    [Fact]
    public async Task Live_platform_version_flows_to_non_overridden_org_only()
    {
        var product = CatalogProduct.CreateImportedSnapshot(
            PosOrganizationId.From(Org),
            "Coke",
            UnitOfMeasure.Piece,
            12m,
            GlobalId,
            CatalogSource.Template,
            Utc,
            platformImageVersion: 1);
        var overrideImage = CatalogProductImage.Create(
            PosOrganizationId.From(Org),
            product.Id,
            Guid.NewGuid(),
            7,
            80,
            80,
            400,
            400,
            Utc);
        Assert.Equal(1, CatalogProductImageResolution.Resolve(product, null, livePlatformImageVersion: 1).ImageVersion);
        Assert.Equal(2, CatalogProductImageResolution.Resolve(product, null, livePlatformImageVersion: 2).ImageVersion);
        Assert.Equal(7, CatalogProductImageResolution.Resolve(product, overrideImage, livePlatformImageVersion: 2).ImageVersion);
    }

    private sealed class ImportedProducts(CatalogProduct product) : ICatalogProductRepository
    {
        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(organizationId == product.OrganizationId && productId == product.Id ? product : null);

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CatalogProduct>>([]);
        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([], 0));
        public Task<IReadOnlyList<Guid>> ListIdsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => Task.FromResult((0, 0, 0));
        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);
        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        public Task AddAsync(CatalogProduct catalogProduct, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(CatalogProduct catalogProduct, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyImages : ICatalogProductImageRepository
    {
        public Task<CatalogProductImage?> GetByProductIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProductImage?>(null);
        public Task<IReadOnlyList<CatalogProductImage>> ListByProductIdsAsync(PosOrganizationId organizationId, IReadOnlyList<CatalogProductId> productIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CatalogProductImage>>([]);
        public Task AddAsync(CatalogProductImage image, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(CatalogProductImage image, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(CatalogProductImage image, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MemoryStore : IProductImageObjectStore
    {
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Task WriteAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default)
        {
            Files[relativePath] = content;
            return Task.CompletedTask;
        }
        public Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(Files.TryGetValue(relativePath, out var bytes) ? bytes : null);
        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            Files.Remove(relativePath);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlatform(byte[] bytes, int version) : IPlatformMerchantCatalogClient
    {
        public int GetCalls { get; private set; }

        public Task<PlatformMerchantCatalogTemplateDto?> GetPublishedTemplateAsync(Guid templateId, string? platformSessionToken, CancellationToken cancellationToken = default) => Task.FromResult<PlatformMerchantCatalogTemplateDto?>(null);
        public Task<PlatformMerchantGlobalProductDto?> GetActiveProductAsync(Guid productId, string? platformSessionToken, CancellationToken cancellationToken = default) => Task.FromResult<PlatformMerchantGlobalProductDto?>(null);
        public Task<IReadOnlyList<PlatformMerchantGlobalProductDto>> GetActiveProductsAsync(IReadOnlyList<Guid> productIds, string? platformSessionToken, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlatformMerchantGlobalProductDto>>([]);
        public Task<PagedResult<PlatformMerchantGlobalProductDto>> SearchActiveProductsAsync(string? search, Guid? categoryId, string? businessTypeCode, string? barcode, string? sku, int? page, int? pageSize, string? platformSessionToken, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<PlatformMerchantGlobalProductDto>([], 0, 1, 50));
        public Task<PagedResult<PlatformMerchantGlobalCategoryDto>> ListActiveCategoriesAsync(string? search, string? businessTypeCode, Guid? parentId, int? page, int? pageSize, string? platformSessionToken, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<PlatformMerchantGlobalCategoryDto>([], 0, 1, 50));
        public Task<IReadOnlyList<PlatformGlobalProductImageMetaDto>> ListProductImageMetaAsync(IReadOnlyList<Guid> productIds, string? platformSessionToken, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlatformGlobalProductImageMetaDto>>([new(GlobalId, true, version)]);
        public Task<ProductImageBytes?> GetProductImageAsync(Guid productId, string variant, string? platformSessionToken, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult<ProductImageBytes?>(productId == GlobalId ? new ProductImageBytes(bytes, "image/webp", version) : null);
        }
    }
}

public sealed class ProductImageThumbnailCachePlatformTests
{
    [Fact]
    public async Task Platform_same_version_hits_file_and_version_change_expires_old()
    {
        var root = new TempRoot();
        var cache = new ProductImageThumbnailCache(root, maxBytes: 1024 * 1024);
        var globalId = Guid.NewGuid();
        var path1 = await cache.PutPlatformAsync(globalId, 1, [1, 2, 3]);
        Assert.True(cache.TryGetPlatformExisting(globalId, 1, out var hit));
        Assert.Equal(path1, hit);
        await cache.PutPlatformAsync(globalId, 2, [4, 5, 6]);
        Assert.False(cache.TryGetPlatformExisting(globalId, 1, out _));
        Assert.True(cache.TryGetPlatformExisting(globalId, 2, out _));
    }

    [Fact]
    public async Task Pending_photo_is_a_private_file_not_sqlite_bytes()
    {
        var root = new TempRoot();
        var store = new PendingProductImageStore(root);
        var org = Guid.NewGuid();
        var product = Guid.NewGuid();
        await store.SaveAsync(org, product, [9, 8, 7]);
        var path = store.FilePath(org, product);
        Assert.DoesNotContain("sqlite", path, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new byte[] { 9, 8, 7 }, await store.TryReadAsync(org, product));
        store.Delete(org, product);
        Assert.Null(await store.TryReadAsync(org, product));
    }

    private sealed class TempRoot : IProductImageCacheRoot
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "pos-thumb-cache-" + Guid.NewGuid().ToString("N"));
        public string GetRootDirectory() => _root;
    }
}
