using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Infrastructure.Media;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

public sealed class GlobalProductImageStoragePathTests
{
    [Fact]
    public void Generated_keys_are_safe_and_reject_traversal()
    {
        var key = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Assert.Equal(
            "global-products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/thumb-v2.webp",
            GlobalProductImageStoragePaths.Thumb(key, 2));
        Assert.Equal(
            "global-products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/medium-v2.webp",
            GlobalProductImageStoragePaths.Medium(key, 2));

        var root = Path.Combine(Path.GetTempPath(), "platform-img-root");
        Directory.CreateDirectory(root);
        Assert.True(GlobalProductImageStoragePaths.TryMapToFullPath(
            root,
            GlobalProductImageStoragePaths.Thumb(key, 1),
            out var full));
        Assert.StartsWith(Path.GetFullPath(root), full, StringComparison.OrdinalIgnoreCase);
        Assert.False(GlobalProductImageStoragePaths.TryMapToFullPath(root, "../secret.webp", out _));
        Assert.False(GlobalProductImageStoragePaths.TryMapToFullPath(root, "global-products/../thumb.webp", out _));
        Assert.False(GlobalProductImageStoragePaths.TryMapToFullPath(root, @"C:\windows\x.webp", out _));
    }
}

public sealed class MagickGlobalProductImageProcessorTests
{
    private readonly MagickProductImageProcessor _processor = new();

    [Fact]
    public void Accepts_jpeg_png_webp_and_writes_webp_variants()
    {
        foreach (var source in new[] { MakeJpeg(), MakePng(), MakeWebp() })
        {
            var result = _processor.Process(source);
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.True(LooksLikeWebp(result.Value!.ThumbWebp));
            Assert.True(LooksLikeWebp(result.Value.MediumWebp));
            Assert.True(result.Value.ThumbWidth <= MagickProductImageProcessor.ThumbMaxEdge);
            Assert.True(result.Value.ThumbHeight <= MagickProductImageProcessor.ThumbMaxEdge);
        }
    }

    [Fact]
    public void Rejects_spoofed_text_heic_and_oversize()
    {
        Assert.False(_processor.Process("not-an-image"u8.ToArray()).IsSuccess);
        var heic = new byte[] { 0, 0, 0, 24, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'h', (byte)'e', (byte)'i', (byte)'c' };
        Assert.Equal(DomainErrorCodes.GlobalProductImageUnsupportedType, _processor.Process(heic).ErrorCode);

        var oversize = new byte[GlobalProductImageUploadLimits.MaxBytes + 1];
        oversize[0] = 0xFF;
        oversize[1] = 0xD8;
        oversize[2] = 0xFF;
        Assert.Equal(DomainErrorCodes.GlobalProductImageTooLarge, _processor.Process(oversize).ErrorCode);
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

public sealed class GlobalProductImageUseCaseTests
{
    private static readonly Guid ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Utc = new(2026, 8, 17, 22, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Upload_replace_remove_are_versioned_and_missing_product_is_rejected()
    {
        var products = new FakeProducts();
        var images = new MemoryImageRepository();
        var store = new MemoryObjectStore();
        var set = new SetGlobalProductImage(products, images, new MagickProductImageProcessor(), store, new Support.FixedClock(Utc));
        var get = new GetGlobalProductImage(products, images, store);
        var remove = new RemoveGlobalProductImage(products, images, store);

        var missing = await set.ExecuteAsync(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), MagickGlobalProductImageProcessorTests.MakeJpeg());
        Assert.False(missing.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.GlobalProductNotFound, missing.ErrorCode);

        var first = await set.ExecuteAsync(ProductId, MagickGlobalProductImageProcessorTests.MakeJpeg());
        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.Equal(1, first.Value!.Version);
        Assert.Equal(2, store.Files.Count);

        var second = await set.ExecuteAsync(ProductId, MagickGlobalProductImageProcessorTests.MakeJpeg());
        Assert.True(second.IsSuccess);
        Assert.Equal(2, second.Value!.Version);
        Assert.Equal(2, store.Files.Count);

        var draftRead = await get.ExecuteAsync(ProductId, GlobalProductImageVariants.Thumb, activeOnly: true);
        Assert.False(draftRead.IsSuccess);

        products.Activate();
        var thumb = await get.ExecuteAsync(ProductId, GlobalProductImageVariants.Thumb, activeOnly: true);
        Assert.True(thumb.IsSuccess);
        Assert.Equal(2, thumb.Value!.Version);

        var removed = await remove.ExecuteAsync(ProductId);
        Assert.True(removed.IsSuccess);
        Assert.Empty(store.Files);
    }

    [Fact]
    public async Task Failed_processing_does_not_corrupt_active_image()
    {
        var products = new FakeProducts();
        var images = new MemoryImageRepository();
        var store = new MemoryObjectStore();
        var processor = new SequenceProcessor();
        var set = new SetGlobalProductImage(products, images, processor, store, new Support.FixedClock(Utc));

        var ok = await set.ExecuteAsync(ProductId, MagickGlobalProductImageProcessorTests.MakeJpeg());
        Assert.True(ok.IsSuccess);
        processor.FailNext = true;
        var failed = await set.ExecuteAsync(ProductId, MagickGlobalProductImageProcessorTests.MakeJpeg());
        Assert.False(failed.IsSuccess);
        var current = await images.GetByProductIdAsync(GlobalProductId.From(ProductId));
        Assert.Equal(1, current!.Version);
        Assert.Equal(2, store.Files.Count);
    }

    [Fact]
    public async Task Local_filesystem_store_stays_inside_configured_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "platform-product-images-" + Guid.NewGuid().ToString("N"));
        var store = new LocalFileProductImageStore(
            Options.Create(new PlatformProductImageStorageOptions { RootPath = root }),
            new StubHost());
        Assert.StartsWith(Path.GetFullPath(root), store.RootDirectory, StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.WriteAsync("../x.webp", [1, 2, 3]));
    }

    [Fact]
    public void Dtos_do_not_include_image_bytes()
    {
        var dto = new GlobalProductDto(
            ProductId,
            "Coke",
            null,
            "COKE",
            "4800010000016",
            "Brand",
            null,
            "Piece",
            "PerItem",
            8m,
            12m,
            null,
            "Active",
            [],
            [],
            [],
            Utc,
            Utc,
            true,
            3);
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("base64", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RIFF", json, StringComparison.Ordinal);
        Assert.Contains("\"HasImage\":true", json, StringComparison.Ordinal);
    }

    private sealed class SequenceProcessor : IGlobalProductImageProcessor
    {
        public bool FailNext { get; set; }

        public ApplicationResult<ProcessedGlobalProductImage> Process(byte[] uploadBytes)
        {
            if (FailNext)
            {
                return ApplicationResult<ProcessedGlobalProductImage>.Failure(
                    DomainErrorCodes.InvalidGlobalProductImage,
                    "forced");
            }

            return new MagickProductImageProcessor().Process(uploadBytes);
        }
    }

    private sealed class FakeProducts : IGlobalProductRepository
    {
        private GlobalProduct _product = GlobalProduct.Rehydrate(
            GlobalProductId.From(ProductId),
            "Coke",
            null,
            "COKE",
            "4800010000016",
            "Brand",
            null,
            ProductUnit.Piece,
            8m,
            12m,
            null,
            GlobalProductStatus.Draft,
            [],
            [],
            Utc,
            Utc);

        public void Activate() => _product.SetStatus(GlobalProductStatus.Active, Utc);

        public Task<GlobalProduct?> GetByIdAsync(GlobalProductId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == _product.Id ? _product : null);

        public Task<bool> ExistsWithBarcodeAsync(string barcode, GlobalProductId? excludingId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> ExistsWithSkuAsync(string sku, GlobalProductId? excludingId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<(IReadOnlyList<GlobalProduct> Items, int TotalCount)> ListAsync(
            GlobalProductStatus? status,
            GlobalCategoryId? categoryId,
            Guid? businessTypeId,
            string? businessTypeCode,
            string? search,
            string? barcode,
            string? sku,
            int skip,
            int take,
            CancellationToken cancellationToken = default,
            IReadOnlyCollection<Guid>? excludeProductIds = null,
            GlobalProductListSortBy sortBy = GlobalProductListSortBy.Name,
            bool sortDescending = false,
            IReadOnlyCollection<Guid>? allowedBusinessTypeIds = null) =>
            Task.FromResult<(IReadOnlyList<GlobalProduct>, int)>(([_product], 1));

        public Task<IReadOnlyList<GlobalProduct>> GetByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GlobalProduct>>(ids.Contains(_product.Id.Value) ? [_product] : []);

        public Task AddAsync(GlobalProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(GlobalProduct product, CancellationToken cancellationToken = default)
        {
            _product = product;
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryImageRepository : IGlobalProductImageRepository
    {
        private readonly Dictionary<Guid, GlobalProductImage> _store = new();

        public Task<GlobalProductImage?> GetByProductIdAsync(
            GlobalProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.GetValueOrDefault(productId.Value));

        public Task<IReadOnlyList<GlobalProductImage>> ListByProductIdsAsync(
            IReadOnlyList<GlobalProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GlobalProductImage>>(
                productIds.Select(id => _store.GetValueOrDefault(id.Value)).Where(i => i is not null).Cast<GlobalProductImage>().ToList());

        public Task AddAsync(GlobalProductImage image, CancellationToken cancellationToken = default)
        {
            _store[image.GlobalProductId.Value] = image;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(GlobalProductImage image, CancellationToken cancellationToken = default)
        {
            _store[image.GlobalProductId.Value] = image;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(GlobalProductImage image, CancellationToken cancellationToken = default)
        {
            _store.Remove(image.GlobalProductId.Value);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryObjectStore : IGlobalProductImageObjectStore
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

    private sealed class StubHost : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public sealed class GlobalProductImageEndpointAuthTests
{
    [Fact]
    public void Platform_mutate_requires_manage_and_merchant_read_is_active_authenticated_only()
    {
        var root = FindRepositoryRoot();
        var admin = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "GlobalCatalog", "GlobalCatalogEndpoints.cs"));
        var merchant = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "GlobalCatalog", "MerchantCatalogDiscoveryEndpoints.cs"));

        Assert.Contains("MapPut(\"/{id:guid}/image\"", admin, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{id:guid}/image\"", admin, StringComparison.Ordinal);
        Assert.Contains("PlatformPermission.ManageGlobalProducts", admin, StringComparison.Ordinal);
        Assert.Contains("PlatformPermission.ViewGlobalCatalog", admin, StringComparison.Ordinal);
        Assert.Contains("activeOnly: false", admin, StringComparison.Ordinal);

        Assert.Contains("/products/image-meta", merchant, StringComparison.Ordinal);
        Assert.Contains("/products/{id:guid}/image/{variant}", merchant, StringComparison.Ordinal);
        Assert.Contains("EnsureAuthenticated(http)", merchant, StringComparison.Ordinal);
        Assert.Contains("activeOnly: true", merchant, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut", merchant, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", merchant, StringComparison.Ordinal);
        Assert.DoesNotContain("ManageGlobalProducts", merchant, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
