using ExItS.PinoyBusinessPOS.Application.Catalog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Media;

public sealed class ProductImageStorageOptions
{
    public const string SectionName = "PosMedia";

    /// <summary>Absolute directory for WebP variants. Empty uses ContentRoot/App_Data/product-images.</summary>
    public string? RootPath { get; set; }
}

public sealed class LocalFileProductImageStore : IProductImageObjectStore
{
    private readonly string _root;

    public LocalFileProductImageStore(IOptions<ProductImageStorageOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.RootPath;
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "product-images")
            : Path.GetFullPath(configured);
        Directory.CreateDirectory(_root);
    }

    public string RootDirectory => _root;

    public async Task WriteAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default)
    {
        if (!ProductImageStoragePaths.TryMapToFullPath(_root, relativePath, out var fullPath))
        {
            throw new InvalidOperationException("Unsafe product image path.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temp = fullPath + ".tmp";
        await File.WriteAllBytesAsync(temp, content, cancellationToken).ConfigureAwait(false);
        File.Move(temp, fullPath, overwrite: true);
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (!ProductImageStoragePaths.TryMapToFullPath(_root, relativePath, out var fullPath)
            || !File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ProductImageStoragePaths.TryMapToFullPath(_root, relativePath, out var fullPath)
            || !File.Exists(fullPath))
        {
            return Task.CompletedTask;
        }

        File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
