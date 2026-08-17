using ExItS.PinoyBusinessPOS.Domain.Catalog;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

internal static class CatalogPlatformImageMeta
{
    public static async Task<int?> TryGetVersionAsync(
        IPlatformMerchantCatalogClient? platform,
        CatalogProduct product,
        CatalogProductImage? merchantOverride,
        CancellationToken cancellationToken)
    {
        if (merchantOverride is not null || product.PlatformGlobalProductId is null || platform is null)
        {
            return null;
        }

        try
        {
            var items = await platform
                .ListProductImageMetaAsync([product.PlatformGlobalProductId.Value], platformSessionToken: null, cancellationToken)
                .ConfigureAwait(false);
            var match = items.FirstOrDefault(i => i.GlobalProductId == product.PlatformGlobalProductId.Value);
            return match is { HasImage: true, ImageVersion: > 0 } ? match.ImageVersion : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    public static async Task<Dictionary<Guid, int?>> TryGetVersionsAsync(
        IPlatformMerchantCatalogClient? platform,
        IReadOnlyList<CatalogProduct> products,
        IReadOnlyDictionary<Guid, CatalogProductImage> merchantOverrides,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, int?>();
        if (platform is null)
        {
            return result;
        }

        var ids = products
            .Where(p => p.PlatformGlobalProductId is not null && !merchantOverrides.ContainsKey(p.Id.Value))
            .Select(p => p.PlatformGlobalProductId!.Value)
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return result;
        }

        try
        {
            var items = await platform
                .ListProductImageMetaAsync(ids, platformSessionToken: null, cancellationToken)
                .ConfigureAwait(false);
            var byGlobal = items
                .Where(i => i.HasImage && i.ImageVersion is > 0)
                .ToDictionary(i => i.GlobalProductId, i => i.ImageVersion);
            foreach (var product in products)
            {
                if (product.PlatformGlobalProductId is Guid globalId
                    && byGlobal.TryGetValue(globalId, out var version))
                {
                    result[product.Id.Value] = version;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return result;
        }

        return result;
    }
}
