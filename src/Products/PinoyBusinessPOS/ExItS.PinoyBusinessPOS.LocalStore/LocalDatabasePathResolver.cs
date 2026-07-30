using System.Security.Cryptography;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>
/// Deterministic hashed SQLite paths under the MAUI application sandbox.
/// Filenames never include raw user or organization identifiers.
/// </summary>
public sealed class LocalDatabasePathResolver(ILocalStoreRootPathProvider rootPath) : ILocalDatabasePathResolver
{
    public const string FilePrefix = "pos-local-";
    public const string FileExtension = ".db";

    public string ComputeContextHash(Guid userId, Guid organizationId, string productCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        var bytes = Encoding.UTF8.GetBytes($"{userId:D}|{organizationId:D}|{NormalizeProduct(productCode)}");
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string ResolveDatabaseFileName(Guid userId, Guid organizationId, string productCode)
    {
        var hash = ComputeContextHash(userId, organizationId, productCode);
        // Use a stable 32-hex (128-bit) prefix — enough for isolation, short for diagnostics.
        return $"{FilePrefix}{hash[..32]}{FileExtension}";
    }

    public string ResolveDatabasePath(Guid userId, Guid organizationId, string productCode)
    {
        var root = rootPath.GetLocalStoreRootDirectory();
        Directory.CreateDirectory(root);
        return Path.Combine(root, ResolveDatabaseFileName(userId, organizationId, productCode));
    }

    private static string NormalizeProduct(string productCode) =>
        productCode.Trim().ToLowerInvariant();
}
