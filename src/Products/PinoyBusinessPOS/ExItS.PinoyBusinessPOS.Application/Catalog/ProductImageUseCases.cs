using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed class SetCatalogProductImage
{
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductImageRepository _images;
    private readonly IProductImageProcessor _processor;
    private readonly IProductImageObjectStore _store;
    private readonly IClock _clock;

    public SetCatalogProductImage(
        ICatalogProductRepository products,
        ICatalogProductImageRepository images,
        IProductImageProcessor processor,
        IProductImageObjectStore store,
        IClock clock)
    {
        _products = products;
        _images = images;
        _processor = processor;
        _store = store;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosCatalogProductImageDto>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        byte[] uploadBytes,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<PosCatalogProductImageDto>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        var processed = _processor.Process(uploadBytes);
        if (!processed.IsSuccess)
        {
            return ApplicationResult<PosCatalogProductImageDto>.Failure(processed);
        }

        var current = await _images.GetByProductIdAsync(orgId, catalogId, cancellationToken).ConfigureAwait(false);
        var storageKey = current?.StorageKey ?? Guid.NewGuid();
        var nextVersion = (current?.Version ?? 0) + 1;
        var thumbPath = ProductImageStoragePaths.Thumb(storageKey, nextVersion);
        var mediumPath = ProductImageStoragePaths.Medium(storageKey, nextVersion);

        await _store.WriteAsync(thumbPath, processed.Value!.ThumbWebp, cancellationToken).ConfigureAwait(false);
        try
        {
            await _store.WriteAsync(mediumPath, processed.Value.MediumWebp, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await _store.DeleteAsync(thumbPath, cancellationToken).ConfigureAwait(false);
            throw;
        }

        var now = _clock.UtcNow;
        CatalogProductImage saved;
        if (current is null)
        {
            saved = CatalogProductImage.Create(
                orgId,
                catalogId,
                storageKey,
                nextVersion,
                processed.Value.ThumbWidth,
                processed.Value.ThumbHeight,
                processed.Value.MediumWidth,
                processed.Value.MediumHeight,
                now);
            await _images.AddAsync(saved, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            saved = current.Replace(
                nextVersion,
                processed.Value.ThumbWidth,
                processed.Value.ThumbHeight,
                processed.Value.MediumWidth,
                processed.Value.MediumHeight,
                now);
            await _images.UpdateAsync(saved, cancellationToken).ConfigureAwait(false);
            await _store.DeleteAsync(ProductImageStoragePaths.Thumb(storageKey, current.Version), cancellationToken)
                .ConfigureAwait(false);
            await _store.DeleteAsync(ProductImageStoragePaths.Medium(storageKey, current.Version), cancellationToken)
                .ConfigureAwait(false);
        }

        return ApplicationResult<PosCatalogProductImageDto>.Success(Map(saved));
    }

    public static PosCatalogProductImageDto Map(CatalogProductImage image) =>
        new(image.ProductId.Value, image.Version, image.ThumbWidth, image.ThumbHeight, image.MediumWidth, image.MediumHeight);
}

public sealed class RemoveCatalogProductImage
{
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductImageRepository _images;
    private readonly IProductImageObjectStore _store;

    public RemoveCatalogProductImage(
        ICatalogProductRepository products,
        ICatalogProductImageRepository images,
        IProductImageObjectStore store)
    {
        _products = products;
        _images = images;
        _store = store;
    }

    public async Task<ApplicationResult> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult.Failure(ApplicationErrorCodes.ProductNotFound, "Product was not found.");
        }

        var current = await _images.GetByProductIdAsync(orgId, catalogId, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return ApplicationResult.Success();
        }

        await _images.DeleteAsync(current, cancellationToken).ConfigureAwait(false);
        await _store.DeleteAsync(ProductImageStoragePaths.Thumb(current.StorageKey, current.Version), cancellationToken)
            .ConfigureAwait(false);
        await _store.DeleteAsync(ProductImageStoragePaths.Medium(current.StorageKey, current.Version), cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult.Success();
    }
}

public sealed class GetCatalogProductImage
{
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductImageRepository _images;
    private readonly IProductImageObjectStore _store;
    private readonly IPlatformMerchantCatalogClient? _platform;

    public GetCatalogProductImage(
        ICatalogProductRepository products,
        ICatalogProductImageRepository images,
        IProductImageObjectStore store,
        IPlatformMerchantCatalogClient? platform = null)
    {
        _products = products;
        _images = images;
        _store = store;
        _platform = platform;
    }

    public async Task<ApplicationResult<ProductImageBytes>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        string variant,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<ProductImageBytes>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        return await ReadAsync(orgId, catalogId, variant, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<ApplicationResult<ProductImageBytes>> ReadAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        string variant,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(variant, ProductImageVariants.Thumb, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(variant, ProductImageVariants.Medium, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<ProductImageBytes>.Failure(
                ApplicationErrorCodes.ProductImageInvalid,
                "Image variant must be thumb or medium.");
        }

        var image = await _images.GetByProductIdAsync(organizationId, productId, cancellationToken).ConfigureAwait(false);
        if (image is not null)
        {
            var relative = string.Equals(variant, ProductImageVariants.Medium, StringComparison.OrdinalIgnoreCase)
                ? ProductImageStoragePaths.Medium(image.StorageKey, image.Version)
                : ProductImageStoragePaths.Thumb(image.StorageKey, image.Version);
            var bytes = await _store.ReadAsync(relative, cancellationToken).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0)
            {
                return ApplicationResult<ProductImageBytes>.Failure(
                    ApplicationErrorCodes.ProductImageNotFound,
                    "This product has no image.");
            }

            return ApplicationResult<ProductImageBytes>.Success(
                new ProductImageBytes(bytes, CatalogProductImage.WebpContentType, image.Version));
        }

        var product = await _products.GetByIdAsync(organizationId, productId, cancellationToken).ConfigureAwait(false);
        if (product?.PlatformGlobalProductId is Guid globalId && _platform is not null)
        {
            try
            {
                var shared = await _platform
                    .GetProductImageAsync(globalId, variant, platformSessionToken: null, cancellationToken)
                    .ConfigureAwait(false);
                if (shared is not null)
                {
                    return ApplicationResult<ProductImageBytes>.Success(shared);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
            }
        }

        return ApplicationResult<ProductImageBytes>.Failure(
            ApplicationErrorCodes.ProductImageNotFound,
            "This product has no image.");
    }
}

public sealed class GetPlatformCatalogProductImage
{
    private readonly IPlatformMerchantCatalogClient _platform;

    public GetPlatformCatalogProductImage(IPlatformMerchantCatalogClient platform) => _platform = platform;

    public async Task<ApplicationResult<ProductImageBytes>> ExecuteAsync(
        Guid globalProductId,
        string variant,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(variant, ProductImageVariants.Thumb, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(variant, ProductImageVariants.Medium, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<ProductImageBytes>.Failure(
                ApplicationErrorCodes.ProductImageInvalid,
                "Image variant must be thumb or medium.");
        }

        try
        {
            var shared = await _platform
                .GetProductImageAsync(globalProductId, variant, platformSessionToken: null, cancellationToken)
                .ConfigureAwait(false);
            if (shared is not null)
            {
                return ApplicationResult<ProductImageBytes>.Success(shared);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
        }

        return ApplicationResult<ProductImageBytes>.Failure(
            ApplicationErrorCodes.ProductImageNotFound,
            "This product has no image.");
    }
}

public sealed record PosCatalogProductImageDto(
    Guid ProductId,
    int Version,
    int ThumbWidth,
    int ThumbHeight,
    int MediumWidth,
    int MediumHeight);
