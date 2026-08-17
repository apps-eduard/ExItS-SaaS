namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Private pending original for offline org-created photos. Never SQLite bytes; never Pictures/DCIM.
/// </summary>
public sealed class PendingProductImageStore(IProductImageCacheRoot root)
{
    public string FilePath(Guid organizationId, Guid productId) =>
        Path.Combine(
            DirectoryPath(),
            $"{organizationId:N}_{productId:N}.bin");

    public async Task SaveAsync(
        Guid organizationId,
        Guid productId,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        if (bytes.Length == 0)
        {
            return;
        }

        Directory.CreateDirectory(DirectoryPath());
        var path = FilePath(organizationId, productId);
        var temp = path + ".tmp";
        await File.WriteAllBytesAsync(temp, bytes, cancellationToken).ConfigureAwait(false);
        File.Move(temp, path, overwrite: true);
    }

    public async Task<byte[]?> TryReadAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var path = FilePath(organizationId, productId);
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public void Delete(Guid organizationId, Guid productId)
    {
        var path = FilePath(organizationId, productId);
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
        }
    }

    private string DirectoryPath()
    {
        var path = Path.Combine(root.GetRootDirectory(), "pending-product-images");
        Directory.CreateDirectory(path);
        return path;
    }
}
