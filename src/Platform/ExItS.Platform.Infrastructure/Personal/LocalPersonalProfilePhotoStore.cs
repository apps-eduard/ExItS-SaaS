using ExItS.Platform.Application.Personal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Infrastructure.Personal;

public sealed class LocalPersonalProfilePhotoStore : IPersonalProfilePhotoStore
{
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private readonly string _root;

    public LocalPersonalProfilePhotoStore(IConfiguration configuration, IHostEnvironment environment)
    {
        _root = ResolveRoot(configuration, environment);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(
        Guid userId,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        await WriteAtStemAsync(Path.Combine(_root, userId.ToString("N")), contentType, content, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SaveForKeyAsync(
        string key,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        return WriteAtStemAsync(Path.Combine(_root, SanitizeKey(key)), contentType, content, cancellationToken);
    }

    public Task<PersonalProfilePhotoFile?> ReadForKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return ReadAtStemAsync(Path.Combine(_root, SanitizeKey(key)), cancellationToken);
    }

    private static async Task WriteAtStemAsync(
        string stem,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken)
    {
        if (!Extensions.TryGetValue(contentType, out var extension))
        {
            throw new InvalidOperationException("Unsupported profile photo type.");
        }

        foreach (var existing in Extensions.Values)
        {
            var path = stem + existing;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        var destination = stem + extension;
        var temp = destination + ".tmp";
        await File.WriteAllBytesAsync(temp, content, cancellationToken).ConfigureAwait(false);
        File.Move(temp, destination, overwrite: true);
    }

    public Task<PersonalProfilePhotoFile?> ReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return ReadAtStemAsync(Path.Combine(_root, userId.ToString("N")), cancellationToken);
    }

    private static async Task<PersonalProfilePhotoFile?> ReadAtStemAsync(string stem, CancellationToken cancellationToken)
    {
        foreach (var pair in Extensions)
        {
            var path = stem + pair.Value;
            if (!File.Exists(path))
            {
                continue;
            }

            var content = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            return new PersonalProfilePhotoFile(pair.Key, content);
        }

        return null;
    }

    private static string SanitizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)
            || key.Length > 80
            || key.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch != '-'))
        {
            throw new InvalidOperationException("Profile photo key is invalid.");
        }

        return key;
    }

    internal static string ResolveRoot(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["ProfilePhotos:RootPath"];
        var candidates = new[]
        {
            configured,
            Path.Combine(environment.ContentRootPath, "App_Data", "personal-profile-photos"),
            Path.Combine(Path.GetTempPath(), "exits-personal-profile-photos"),
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (TryUseDirectory(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No writable directory is available for profile photos.");
    }

    private static bool TryUseDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, ".write-probe");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
