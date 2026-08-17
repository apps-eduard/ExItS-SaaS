using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

public sealed class SetGlobalProductImage
{
    private readonly IGlobalProductRepository _products;
    private readonly IGlobalProductImageRepository _images;
    private readonly IGlobalProductImageProcessor _processor;
    private readonly IGlobalProductImageObjectStore _store;
    private readonly IClock _clock;

    public SetGlobalProductImage(
        IGlobalProductRepository products,
        IGlobalProductImageRepository images,
        IGlobalProductImageProcessor processor,
        IGlobalProductImageObjectStore store,
        IClock clock)
    {
        _products = products;
        _images = images;
        _processor = processor;
        _store = store;
        _clock = clock;
    }

    public async Task<ApplicationResult<GlobalProductImageDto>> ExecuteAsync(
        Guid productId,
        byte[] uploadBytes,
        CancellationToken cancellationToken = default)
    {
        var id = GlobalProductId.From(productId);
        var product = await _products.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<GlobalProductImageDto>.Failure(
                ApplicationErrorCodes.GlobalProductNotFound,
                "Global product was not found.");
        }

        var processed = _processor.Process(uploadBytes);
        if (!processed.IsSuccess)
        {
            return ApplicationResult<GlobalProductImageDto>.Failure(
                processed.ErrorCode!,
                processed.ErrorMessage!);
        }

        var current = await _images.GetByProductIdAsync(id, cancellationToken).ConfigureAwait(false);
        var storageKey = current?.StorageKey ?? Guid.NewGuid();
        var nextVersion = (current?.Version ?? 0) + 1;
        var thumbPath = GlobalProductImageStoragePaths.Thumb(storageKey, nextVersion);
        var mediumPath = GlobalProductImageStoragePaths.Medium(storageKey, nextVersion);

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
        GlobalProductImage saved;
        if (current is null)
        {
            saved = GlobalProductImage.Create(
                id,
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
            await _store.DeleteAsync(GlobalProductImageStoragePaths.Thumb(storageKey, current.Version), cancellationToken)
                .ConfigureAwait(false);
            await _store.DeleteAsync(GlobalProductImageStoragePaths.Medium(storageKey, current.Version), cancellationToken)
                .ConfigureAwait(false);
        }

        return ApplicationResult<GlobalProductImageDto>.Success(Map(saved));
    }

    public static GlobalProductImageDto Map(GlobalProductImage image) =>
        new(
            image.GlobalProductId.Value,
            image.Version,
            image.ThumbWidth,
            image.ThumbHeight,
            image.MediumWidth,
            image.MediumHeight);
}

public sealed class RemoveGlobalProductImage
{
    private readonly IGlobalProductRepository _products;
    private readonly IGlobalProductImageRepository _images;
    private readonly IGlobalProductImageObjectStore _store;

    public RemoveGlobalProductImage(
        IGlobalProductRepository products,
        IGlobalProductImageRepository images,
        IGlobalProductImageObjectStore store)
    {
        _products = products;
        _images = images;
        _store = store;
    }

    public async Task<ApplicationResult> ExecuteAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var id = GlobalProductId.From(productId);
        var product = await _products.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.GlobalProductNotFound,
                "Global product was not found.");
        }

        var current = await _images.GetByProductIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return ApplicationResult.Success();
        }

        await _images.DeleteAsync(current, cancellationToken).ConfigureAwait(false);
        await _store.DeleteAsync(
                GlobalProductImageStoragePaths.Thumb(current.StorageKey, current.Version),
                cancellationToken)
            .ConfigureAwait(false);
        await _store.DeleteAsync(
                GlobalProductImageStoragePaths.Medium(current.StorageKey, current.Version),
                cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult.Success();
    }
}

public sealed class GetGlobalProductImage
{
    private readonly IGlobalProductRepository _products;
    private readonly IGlobalProductImageRepository _images;
    private readonly IGlobalProductImageObjectStore _store;

    public GetGlobalProductImage(
        IGlobalProductRepository products,
        IGlobalProductImageRepository images,
        IGlobalProductImageObjectStore store)
    {
        _products = products;
        _images = images;
        _store = store;
    }

    public async Task<ApplicationResult<GlobalProductImageBytes>> ExecuteAsync(
        Guid productId,
        string variant,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var id = GlobalProductId.From(productId);
        var product = await _products.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product is null || (activeOnly && product.Status != GlobalProductStatus.Active))
        {
            return ApplicationResult<GlobalProductImageBytes>.Failure(
                ApplicationErrorCodes.GlobalProductNotFound,
                activeOnly
                    ? "Active global product was not found."
                    : "Global product was not found.");
        }

        if (!string.Equals(variant, GlobalProductImageVariants.Thumb, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(variant, GlobalProductImageVariants.Medium, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<GlobalProductImageBytes>.Failure(
                ApplicationErrorCodes.GlobalProductImageInvalid,
                "Image variant must be thumb or medium.");
        }

        var image = await _images.GetByProductIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (image is null)
        {
            return ApplicationResult<GlobalProductImageBytes>.Failure(
                ApplicationErrorCodes.GlobalProductImageNotFound,
                "This product has no image.");
        }

        var relative = string.Equals(variant, GlobalProductImageVariants.Medium, StringComparison.OrdinalIgnoreCase)
            ? GlobalProductImageStoragePaths.Medium(image.StorageKey, image.Version)
            : GlobalProductImageStoragePaths.Thumb(image.StorageKey, image.Version);
        var bytes = await _store.ReadAsync(relative, cancellationToken).ConfigureAwait(false);
        if (bytes is null || bytes.Length == 0)
        {
            return ApplicationResult<GlobalProductImageBytes>.Failure(
                ApplicationErrorCodes.GlobalProductImageNotFound,
                "This product has no image.");
        }

        return ApplicationResult<GlobalProductImageBytes>.Success(
            new GlobalProductImageBytes(bytes, GlobalProductImage.WebpContentType, image.Version));
    }
}

public sealed class ListGlobalProductImageMeta
{
    private readonly IGlobalProductRepository _products;
    private readonly IGlobalProductImageRepository _images;

    public ListGlobalProductImageMeta(
        IGlobalProductRepository products,
        IGlobalProductImageRepository images)
    {
        _products = products;
        _images = images;
    }

    public async Task<IReadOnlyList<GlobalProductImageMetaDto>> ExecuteAsync(
        IReadOnlyList<Guid> productIds,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Where(id => id != Guid.Empty).Distinct().Take(50).ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var products = await _products.GetByIdsAsync(ids, cancellationToken).ConfigureAwait(false);
        var visible = activeOnly
            ? products.Where(p => p.Status == GlobalProductStatus.Active).ToList()
            : products.ToList();
        var visibleIds = visible.Select(p => p.Id).ToList();
        var images = visibleIds.Count == 0
            ? []
            : await _images.ListByProductIdsAsync(visibleIds, cancellationToken).ConfigureAwait(false);
        var imageByProduct = images.ToDictionary(i => i.GlobalProductId.Value);

        return visible.Select(p =>
        {
            imageByProduct.TryGetValue(p.Id.Value, out var image);
            return new GlobalProductImageMetaDto(p.Id.Value, image is not null, image?.Version);
        }).ToList();
    }
}
