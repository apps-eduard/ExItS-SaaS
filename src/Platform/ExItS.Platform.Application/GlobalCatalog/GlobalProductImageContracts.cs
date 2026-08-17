using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

public static class GlobalProductImageVariants
{
    public const string Thumb = "thumb";
    public const string Medium = "medium";
}

public static class GlobalProductImageUploadLimits
{
    public const int MaxBytes = 10 * 1024 * 1024;
}

public sealed record ProcessedGlobalProductImage(
    byte[] ThumbWebp,
    int ThumbWidth,
    int ThumbHeight,
    byte[] MediumWebp,
    int MediumWidth,
    int MediumHeight);

public sealed record GlobalProductImageBytes(byte[] Content, string ContentType, int Version);

public sealed record GlobalProductImageDto(
    Guid GlobalProductId,
    int Version,
    int ThumbWidth,
    int ThumbHeight,
    int MediumWidth,
    int MediumHeight);

public sealed record GlobalProductImageMetaDto(
    Guid GlobalProductId,
    bool HasImage,
    int? ImageVersion);

public interface IGlobalProductImageProcessor
{
    ApplicationResult<ProcessedGlobalProductImage> Process(byte[] uploadBytes);
}

public interface IGlobalProductImageObjectStore
{
    Task WriteAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default);

    Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

public interface IGlobalProductImageRepository
{
    Task<GlobalProductImage?> GetByProductIdAsync(
        GlobalProductId productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlobalProductImage>> ListByProductIdsAsync(
        IReadOnlyList<GlobalProductId> productIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(GlobalProductImage image, CancellationToken cancellationToken = default);

    Task UpdateAsync(GlobalProductImage image, CancellationToken cancellationToken = default);

    Task DeleteAsync(GlobalProductImage image, CancellationToken cancellationToken = default);
}

public static class GlobalProductImageStoragePaths
{
    public static string Thumb(Guid storageKey, int version) =>
        $"global-products/{storageKey:D}/thumb-v{version}.webp";

    public static string Medium(Guid storageKey, int version) =>
        $"global-products/{storageKey:D}/medium-v{version}.webp";

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
