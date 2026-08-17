using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Media;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ProductImageStoragePathTests
{
    [Fact]
    public void Generated_keys_are_safe_and_reject_traversal()
    {
        var key = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Assert.Equal("products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/thumb-v2.webp", ProductImageStoragePaths.Thumb(key, 2));
        Assert.Equal("products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/medium-v2.webp", ProductImageStoragePaths.Medium(key, 2));

        var root = Path.Combine(Path.GetTempPath(), "pos-img-root");
        Directory.CreateDirectory(root);
        Assert.True(ProductImageStoragePaths.TryMapToFullPath(root, ProductImageStoragePaths.Thumb(key, 1), out var full));
        Assert.StartsWith(Path.GetFullPath(root), full, StringComparison.OrdinalIgnoreCase);
        Assert.False(ProductImageStoragePaths.TryMapToFullPath(root, "../secret.webp", out _));
        Assert.False(ProductImageStoragePaths.TryMapToFullPath(root, "products/../thumb.webp", out _));
        Assert.False(ProductImageStoragePaths.TryMapToFullPath(root, @"C:\windows\x.webp", out _));
    }
}

public sealed class MagickProductImageProcessorTests
{
    private readonly MagickProductImageProcessor _processor = new();

    [Fact]
    public void Accepts_jpeg_png_webp_and_writes_webp_variants()
    {
        foreach (var source in new[] { MakeJpeg(), MakePng(), MakeWebp() })
        {
            var result = _processor.Process(source);
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.True(MagickProductImageProcessor.IsAcceptedMagic(result.Value!.ThumbWebp));
            Assert.True(LooksLikeWebp(result.Value.ThumbWebp));
            Assert.True(LooksLikeWebp(result.Value.MediumWebp));
            Assert.True(result.Value.ThumbWidth <= MagickProductImageProcessor.ThumbMaxEdge);
            Assert.True(result.Value.ThumbHeight <= MagickProductImageProcessor.ThumbMaxEdge);
            Assert.True(result.Value.MediumWidth <= MagickProductImageProcessor.MediumMaxEdge);
        }
    }

    [Fact]
    public void Rejects_spoofed_extension_text_heic_oversize_and_extreme_dimensions()
    {
        Assert.False(_processor.Process("not-an-image"u8.ToArray()).IsSuccess);
        Assert.False(MagickProductImageProcessor.IsAcceptedMagic("GIF89a"u8.ToArray()));
        var heic = new byte[] { 0, 0, 0, 24, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'h', (byte)'e', (byte)'i', (byte)'c' };
        Assert.False(MagickProductImageProcessor.IsAcceptedMagic(heic));
        Assert.Equal(DomainErrorCodes.ProductImageUnsupportedType, _processor.Process(heic).ErrorCode);

        var oversize = new byte[ProductImageUploadLimits.MaxBytes + 1];
        oversize[0] = 0xFF;
        oversize[1] = 0xD8;
        oversize[2] = 0xFF;
        Assert.Equal(DomainErrorCodes.ProductImageTooLarge, _processor.Process(oversize).ErrorCode);

        using var huge = new ImageMagick.MagickImage(ImageMagick.MagickColors.Red, 8001, 10);
        huge.Format = ImageMagick.MagickFormat.Jpeg;
        Assert.False(_processor.Process(huge.ToByteArray()).IsSuccess);
    }

    internal static byte[] MakeJpeg()
    {
        using var image = new ImageMagick.MagickImage(ImageMagick.MagickColors.Blue, 40, 30);
        image.Format = ImageMagick.MagickFormat.Jpeg;
        return image.ToByteArray();
    }

    private static byte[] MakePng()
    {
        using var image = new ImageMagick.MagickImage(ImageMagick.MagickColors.Green, 32, 32);
        image.Format = ImageMagick.MagickFormat.Png;
        return image.ToByteArray();
    }

    private static byte[] MakeWebp()
    {
        using var image = new ImageMagick.MagickImage(ImageMagick.MagickColors.Yellow, 24, 24);
        image.Format = ImageMagick.MagickFormat.WebP;
        return image.ToByteArray();
    }

    private static bool LooksLikeWebp(byte[] bytes) =>
        bytes.Length >= 12
        && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
        && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P';
}

public sealed class CatalogProductImageUseCaseTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOrg = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Utc = new(2026, 8, 17, 21, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Upload_replace_remove_are_tenant_scoped_and_versioned()
    {
        var products = new FakeProducts();
        var images = new MemoryImageRepository();
        var store = new MemoryObjectStore();
        var set = new SetCatalogProductImage(products, images, new MagickProductImageProcessor(), store, new FixedClock(Utc));
        var get = new GetCatalogProductImage(products, images, store);
        var remove = new RemoveCatalogProductImage(products, images, store);

        var first = await set.ExecuteAsync(Org, ProductId, MagickProductImageProcessorTests.MakeJpeg());
        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value!.Version);
        Assert.Equal(2, store.Files.Count);

        var second = await set.ExecuteAsync(Org, ProductId, MagickProductImageProcessorTests.MakeJpeg());
        Assert.True(second.IsSuccess);
        Assert.Equal(2, second.Value!.Version);
        Assert.Equal(2, store.Files.Count);

        var other = await set.ExecuteAsync(OtherOrg, ProductId, MagickProductImageProcessorTests.MakeJpeg());
        Assert.False(other.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductNotFound, other.ErrorCode);

        var readOther = await get.ExecuteAsync(OtherOrg, ProductId, ProductImageVariants.Thumb);
        Assert.False(readOther.IsSuccess);

        var thumb = await get.ExecuteAsync(Org, ProductId, ProductImageVariants.Thumb);
        Assert.True(thumb.IsSuccess);
        Assert.StartsWith("RIFF", System.Text.Encoding.ASCII.GetString(thumb.Value!.Content.AsSpan(0, 4)));

        var removed = await remove.ExecuteAsync(Org, ProductId);
        Assert.True(removed.IsSuccess);
        Assert.Empty(store.Files);
        Assert.Null(await images.GetByProductIdAsync(PosOrganizationId.From(Org), CatalogProductId.From(ProductId)));
    }

    [Fact]
    public async Task Failed_processing_does_not_corrupt_active_image()
    {
        var products = new FakeProducts();
        var images = new MemoryImageRepository();
        var store = new MemoryObjectStore();
        var processor = new SequenceProcessor();
        var set = new SetCatalogProductImage(products, images, processor, store, new FixedClock(Utc));

        var ok = await set.ExecuteAsync(Org, ProductId, MagickProductImageProcessorTests.MakeJpeg());
        Assert.True(ok.IsSuccess);
        processor.FailNext = true;
        var failed = await set.ExecuteAsync(Org, ProductId, MagickProductImageProcessorTests.MakeJpeg());
        Assert.False(failed.IsSuccess);
        var current = await images.GetByProductIdAsync(PosOrganizationId.From(Org), CatalogProductId.From(ProductId));
        Assert.Equal(1, current!.Version);
        Assert.Equal(2, store.Files.Count);
    }

    [Fact]
    public async Task Local_filesystem_store_stays_inside_configured_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "pos-product-images-" + Guid.NewGuid().ToString("N"));
        var store = new LocalFileProductImageStore(
            Options.Create(new ProductImageStorageOptions { RootPath = root }),
            new StubHost());
        Assert.StartsWith(Path.GetFullPath(root), store.RootDirectory, StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.WriteAsync("../x.webp", [1, 2, 3]));
    }

    private sealed class SequenceProcessor : IProductImageProcessor
    {
        public bool FailNext { get; set; }

        public ApplicationResult<ProcessedProductImage> Process(byte[] uploadBytes)
        {
            if (FailNext)
            {
                return ApplicationResult<ProcessedProductImage>.Failure(
                    DomainErrorCodes.InvalidProductImage,
                    "boom");
            }

            return new MagickProductImageProcessor().Process(uploadBytes);
        }
    }

    private sealed class FakeProducts : ICatalogProductRepository
    {
        private readonly CatalogProduct _product = CatalogProduct.Create(
            PosOrganizationId.From(Org),
            "Water",
            UnitOfMeasure.Piece,
            12m,
            Utc,
            id: CatalogProductId.From(ProductId));

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                organizationId.Value == Org && productId.Value == ProductId ? _product : null);

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CatalogProduct>>([]);
        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([], 0));
        public Task<IReadOnlyList<Guid>> ListIdsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => Task.FromResult((0, 0, 0));
        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);
        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MemoryImageRepository : ICatalogProductImageRepository
    {
        private CatalogProductImage? _image;

        public Task<CatalogProductImage?> GetByProductIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_image is not null && _image.OrganizationId == organizationId && _image.ProductId == productId ? _image : null);

        public Task<IReadOnlyList<CatalogProductImage>> ListByProductIdsAsync(PosOrganizationId organizationId, IReadOnlyList<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductImage>>(_image is null ? [] : [_image]);

        public Task AddAsync(CatalogProductImage image, CancellationToken cancellationToken = default)
        {
            _image = image;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProductImage image, CancellationToken cancellationToken = default)
        {
            _image = image;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CatalogProductImage image, CancellationToken cancellationToken = default)
        {
            _image = null;
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryObjectStore : IProductImageObjectStore
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

    private sealed class FixedClock(DateTimeOffset utc) : IClock
    {
        public DateTimeOffset UtcNow => utc;
    }

    private sealed class StubHost : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public sealed class ProductImageThumbnailCacheTests
{
    [Fact]
    public async Task Same_version_hits_file_and_version_change_expires_old()
    {
        var root = new TempRoot();
        var cache = new ProductImageThumbnailCache(root, maxBytes: 1024 * 1024);
        var org = Guid.NewGuid();
        var product = Guid.NewGuid();
        var path1 = await cache.PutAsync(org, product, 1, [1, 2, 3]);
        Assert.True(cache.TryGetExisting(org, product, 1, out var hit));
        Assert.Equal(path1, hit);
        await cache.PutAsync(org, product, 2, [4, 5, 6]);
        Assert.False(cache.TryGetExisting(org, product, 1, out _));
        Assert.True(cache.TryGetExisting(org, product, 2, out _));
    }

    private sealed class TempRoot : IProductImageCacheRoot
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "pos-thumb-cache-" + Guid.NewGuid().ToString("N"));
        public string GetRootDirectory() => _root;
    }
}

public sealed class CatalogDtoHasNoImageBytesTests
{
    [Fact]
    public void Catalog_and_storefront_dtos_carry_metadata_not_bytes()
    {
        var catalog = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Application", "Catalog", "CatalogClientDtos.cs"));
        var storefront = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Application", "CustomerOrdering", "CustomerStorefrontDtos.cs"));
        Assert.Contains("HasImage", catalog, StringComparison.Ordinal);
        Assert.Contains("ImageVersion", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("base64", catalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("byte[]", catalog, StringComparison.Ordinal);
        Assert.Contains("HasImage", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("base64", storefront, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("byte[]", storefront, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent!;
        }

        throw new InvalidOperationException("repo root");
    }
}
