using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExItS.BackupRestore;

/// <summary>Logical database identity for independent backup/restore artifacts.</summary>
public enum ExItsBackupDatabaseKind
{
    Platform = 1,
    PinoyBusinessPos = 2
}

/// <summary>Safe backup-set manifest — never includes secrets, payloads, or financial content.</summary>
public sealed class BackupManifest
{
    public string SchemaVersion { get; init; } = "1.0";

    public required string BackupSetId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string EnvironmentClassification { get; init; }

    public string? ApplicationGitCommit { get; init; }

    public string? PostgreSqlServerVersion { get; init; }

    public required ExItsBackupDatabaseKind DatabaseKind { get; init; }

    public required string DatabaseName { get; init; }

    public string? MigrationSchemaVersion { get; init; }

    public required string BackupFormat { get; init; }

    public required string ArtifactFileName { get; init; }

    public required long ArtifactSizeBytes { get; init; }

    public required string Sha256Checksum { get; init; }

    public required string EncryptionStatus { get; init; }

    public required string ToolVersion { get; init; }

    public required string CompletionStatus { get; init; }

    public string PhaseMarker { get; init; } = BackupConstants.PhaseMarker;
}

public static class BackupConstants
{
    public const string PhaseMarker = "P9-WP03-backup-and-restore";

    public const string ToolVersion = "1.0.0";

    public const string ManifestFileSuffix = ".manifest.json";

    public const string DumpFileSuffix = ".dump";

    public const string EncryptedSuffix = ".enc";

    public const string CompletionComplete = "Complete";

    public const string CompletionFailed = "Failed";

    public const string EncryptionNone = "None";

    public const string EncryptionAesGcm = "AES-256-GCM";

    public const string DestructiveConfirmationToken = "DESTROY_AND_RESTORE";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
