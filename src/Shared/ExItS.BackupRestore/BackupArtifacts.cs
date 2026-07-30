using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExItS.BackupRestore;

public static class BackupArtifactNaming
{
    private static readonly Regex Unsafe = new(@"[^A-Za-z0-9._-]+", RegexOptions.Compiled);

    public static string BuildBackupSetId(ExItsBackupDatabaseKind kind, DateTimeOffset utc) =>
        $"{NormalizeKind(kind)}_{utc.UtcDateTime:yyyyMMddTHHmmssZ}_{Guid.NewGuid():N}";

    public static string BuildArtifactFileName(string backupSetId) =>
        $"{Sanitize(backupSetId)}{BackupConstants.DumpFileSuffix}";

    public static string BuildManifestFileName(string backupSetId) =>
        $"{Sanitize(backupSetId)}{BackupConstants.ManifestFileSuffix}";

    public static string NormalizeKind(ExItsBackupDatabaseKind kind) => kind switch
    {
        ExItsBackupDatabaseKind.Platform => "platform",
        ExItsBackupDatabaseKind.PinoyBusinessPos => "pos",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static string Sanitize(string value) =>
        Unsafe.Replace(value.Trim(), "_");
}

public static class BackupChecksum
{
    public static string Sha256Hex(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var hash = SHA256.HashData(stream is MemoryStream ms && ms.TryGetBuffer(out var segment)
            ? segment.AsSpan(0, (int)ms.Length)
            : ReadAll(stream));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Sha256Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static string Sha256HexFile(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool ConstantTimeEquals(string expectedHex, string actualHex)
    {
        if (string.IsNullOrWhiteSpace(expectedHex) || string.IsNullOrWhiteSpace(actualHex))
        {
            return false;
        }

        var a = Encoding.ASCII.GetBytes(expectedHex.Trim().ToLowerInvariant());
        var b = Encoding.ASCII.GetBytes(actualHex.Trim().ToLowerInvariant());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}

public static class BackupManifestStore
{
    public static async Task WriteAsync(string path, BackupManifest manifest, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(manifest);
        if (File.Exists(path))
        {
            throw new InvalidOperationException($"Manifest already exists and will not be overwritten: {Path.GetFileName(path)}");
        }

        var json = JsonSerializer.Serialize(manifest, BackupConstants.JsonOptions);
        await File.WriteAllTextAsync(path, json, Encoding.UTF8, ct).ConfigureAwait(false);
    }

    public static async Task<BackupManifest> ReadAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct).ConfigureAwait(false);
        var manifest = JsonSerializer.Deserialize<BackupManifest>(json, BackupConstants.JsonOptions)
            ?? throw new InvalidOperationException("Manifest deserialization returned null.");
        return manifest;
    }
}

public static class SecretRedaction
{
    private static readonly Regex[] Patterns =
    [
        new(@"Password\s*=\s*[^;\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"Pwd\s*=\s*[^;\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"PGPASSWORD\s*=\s*\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"Host\s*=\s*[^;\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"Username\s*=\s*[^;\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var result = text;
        foreach (var pattern in Patterns)
        {
            result = pattern.Replace(result, "[REDACTED]");
        }

        return result;
    }

    public static void EnsureNoSecrets(string text, string context)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("password=", StringComparison.Ordinal)
            || lower.Contains("pgpassword=", StringComparison.Ordinal)
            || lower.Contains("bearer ", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Potential secret leakage detected in {context}.");
        }
    }
}

public static class ConnectionStringParser
{
    public static (string Host, int Port, string Database, string Username, string Password) Parse(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var map = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);

        var host = map.GetValueOrDefault("Host") ?? map.GetValueOrDefault("Server")
            ?? throw new InvalidOperationException("Connection string missing Host.");
        var database = map.GetValueOrDefault("Database")
            ?? throw new InvalidOperationException("Connection string missing Database.");
        var username = map.GetValueOrDefault("Username") ?? map.GetValueOrDefault("User ID")
            ?? throw new InvalidOperationException("Connection string missing Username.");
        var password = map.GetValueOrDefault("Password") ?? string.Empty;
        var port = 5432;
        if (map.TryGetValue("Port", out var portText)
            && int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            port = parsed;
        }

        return (host, port, database, username, password);
    }
}
