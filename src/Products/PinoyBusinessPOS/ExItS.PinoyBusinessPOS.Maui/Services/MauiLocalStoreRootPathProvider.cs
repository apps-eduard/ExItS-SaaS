using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>MAUI application-sandbox root for foundation SQLite files.</summary>
public sealed class MauiLocalStoreRootPathProvider : ILocalStoreRootPathProvider
{
    public string GetLocalStoreRootDirectory()
    {
        var root = Path.Combine(FileSystem.AppDataDirectory, "local-store");
        Directory.CreateDirectory(root);
        return root;
    }
}
