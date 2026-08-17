using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Best-effort thumbnail prefetch for templates the merchant explicitly adopted.
/// Never blocks import. Never downloads the full Platform catalog.
/// </summary>
public sealed class AdoptedTemplateThumbnailPrefetch(
    ProductImageThumbnailCache cache,
    IPosCatalogClient catalog)
{
    public async Task TryPrefetchAsync(
        IEnumerable<PlatformMerchantCatalogTemplateProductDto> products,
        CancellationToken cancellationToken = default)
    {
        foreach (var product in products)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!product.HasImage || product.ImageVersion is not int version || version <= 0)
            {
                continue;
            }

            try
            {
                if (cache.TryGetPlatformExisting(product.GlobalProductId, version, out _))
                {
                    continue;
                }

                var result = await catalog
                    .GetPlatformProductImageAsync(product.GlobalProductId, ProductImageVariants.Thumb, cancellationToken)
                    .ConfigureAwait(false);
                if (!result.IsSuccess || result.Data is null || result.Data.Content.Length == 0)
                {
                    continue;
                }

                await cache
                    .PutPlatformAsync(product.GlobalProductId, version, result.Data.Content, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                // Cache failure must not block template adoption.
            }
        }
    }
}
