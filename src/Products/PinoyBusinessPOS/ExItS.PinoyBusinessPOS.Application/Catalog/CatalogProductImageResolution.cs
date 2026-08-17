using ExItS.PinoyBusinessPOS.Domain.Catalog;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public static class CatalogProductImageSources
{
    public const string None = "None";
    public const string MerchantOverride = "MerchantOverride";
    public const string PlatformTemplate = "PlatformTemplate";
}

public readonly record struct CatalogProductImageResolution(
    bool HasImage,
    int? ImageVersion,
    string Source,
    bool HasMerchantOverride)
{
    public static CatalogProductImageResolution Resolve(
        CatalogProduct product,
        CatalogProductImage? merchantOverride,
        int? livePlatformImageVersion = null)
    {
        if (merchantOverride is not null)
        {
            return new(true, merchantOverride.Version, CatalogProductImageSources.MerchantOverride, true);
        }

        var platformVersion = livePlatformImageVersion ?? product.PlatformImageVersion;
        if (product.PlatformGlobalProductId is not null && platformVersion is > 0)
        {
            return new(true, platformVersion, CatalogProductImageSources.PlatformTemplate, false);
        }

        return new(false, null, CatalogProductImageSources.None, false);
    }
}
