using ExItS.PinoyBusinessPOS.Application.Catalog;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>App-private media root. Never Pictures/DCIM/Downloads and never the SQLite local-store folder.</summary>
public sealed class MauiProductImageCacheRoot : IProductImageCacheRoot
{
    public string GetRootDirectory()
    {
        var root = Path.Combine(FileSystem.AppDataDirectory, "media");
        Directory.CreateDirectory(root);
        return root;
    }
}
