namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>Private app-storage root for disposable product-image files. Never Pictures/DCIM/Downloads.</summary>
public interface IProductImageCacheRoot
{
    string GetRootDirectory();
}

public static class ProductImageCacheLimits
{
    public const long DefaultMaxBytes = 300L * 1024L * 1024L;
}

/// <summary>
/// File-backed thumbnail cache keyed by seller + product + version.
/// Bytes are files, not SQLite. Cache is disposable and is never business truth.
/// </summary>
public sealed class ProductImageThumbnailCache(IProductImageCacheRoot root, long maxBytes = ProductImageCacheLimits.DefaultMaxBytes)
{
    public string FilePath(Guid sellerOrganizationId, Guid productId, int version) =>
        Path.Combine(
            DirectoryPath(),
            $"{sellerOrganizationId:N}_{productId:N}_v{version}_thumb.webp");

    public bool TryGetExisting(Guid sellerOrganizationId, Guid productId, int version, out string path)
    {
        path = FilePath(sellerOrganizationId, productId, version);
        if (!File.Exists(path))
        {
            return false;
        }

        Touch(path);
        return true;
    }

    public async Task<string?> PutAsync(
        Guid sellerOrganizationId,
        Guid productId,
        int version,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        if (bytes.Length == 0)
        {
            return null;
        }

        Directory.CreateDirectory(DirectoryPath());
        var path = FilePath(sellerOrganizationId, productId, version);
        var temp = path + ".tmp";
        await File.WriteAllBytesAsync(temp, bytes, cancellationToken).ConfigureAwait(false);
        File.Move(temp, path, overwrite: true);
        ExpireOtherVersions(sellerOrganizationId, productId, version);
        CleanupIfNeeded();
        return path;
    }

    public void CleanupIfNeeded()
    {
        var dir = DirectoryPath();
        if (!Directory.Exists(dir))
        {
            return;
        }

        var files = new DirectoryInfo(dir).GetFiles("*_thumb.webp")
            .OrderBy(f => f.LastAccessTimeUtc == default ? f.LastWriteTimeUtc : f.LastAccessTimeUtc)
            .ToList();
        var total = files.Sum(f => f.Length);
        foreach (var file in files)
        {
            if (total <= maxBytes)
            {
                break;
            }

            try
            {
                total -= file.Length;
                file.Delete();
            }
            catch (IOException)
            {
            }
        }
    }

    private void ExpireOtherVersions(Guid sellerOrganizationId, Guid productId, int keepVersion)
    {
        var dir = DirectoryPath();
        if (!Directory.Exists(dir))
        {
            return;
        }

        var prefix = $"{sellerOrganizationId:N}_{productId:N}_v";
        var keep = $"{prefix}{keepVersion}_thumb.webp";
        foreach (var file in Directory.EnumerateFiles(dir, prefix + "*_thumb.webp"))
        {
            if (!string.Equals(Path.GetFileName(file), keep, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    private string DirectoryPath()
    {
        var path = Path.Combine(root.GetRootDirectory(), "product-image-cache");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Touch(string path)
    {
        try
        {
            File.SetLastAccessTimeUtc(path, DateTime.UtcNow);
        }
        catch (IOException)
        {
        }
    }
}
