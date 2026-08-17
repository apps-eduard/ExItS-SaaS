using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public static class ProductImageVariants
{
    public const string Thumb = "thumb";
    public const string Medium = "medium";
}

public static class ProductImageUploadLimits
{
    public const int MaxBytes = 10 * 1024 * 1024;
}

public sealed record ProcessedProductImage(
    byte[] ThumbWebp,
    int ThumbWidth,
    int ThumbHeight,
    byte[] MediumWebp,
    int MediumWidth,
    int MediumHeight);

public sealed record ProductImageBytes(byte[] Content, string ContentType, int Version);

public interface IProductImageProcessor
{
    ApplicationResult<ProcessedProductImage> Process(byte[] uploadBytes);
}

public interface IProductImageObjectStore
{
    Task WriteAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default);

    Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

public interface ICatalogProductImageRepository
{
    Task<CatalogProductImage?> GetByProductIdAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogProductImage>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CatalogProductId> productIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(CatalogProductImage image, CancellationToken cancellationToken = default);

    Task UpdateAsync(CatalogProductImage image, CancellationToken cancellationToken = default);

    Task DeleteAsync(CatalogProductImage image, CancellationToken cancellationToken = default);
}

public static class ProductImageStoragePaths
{
    public static string Thumb(Guid storageKey, int version) =>
        $"products/{storageKey:D}/thumb-v{version}.webp";

    public static string Medium(Guid storageKey, int version) =>
        $"products/{storageKey:D}/medium-v{version}.webp";

    public static bool TryMapToFullPath(string rootDirectory, string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(rootDirectory) || string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var trimmed = relativePath.Replace('\\', '/').Trim().TrimStart('/');
        if (trimmed.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(trimmed)
            || trimmed.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        var combined = Path.GetFullPath(Path.Combine(rootDirectory, trimmed.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(rootDirectory);
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fullPath = combined;
        return true;
    }
}
