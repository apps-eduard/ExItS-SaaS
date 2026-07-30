using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace ExItS.BackupRestore;

public sealed record BackupRequest(
    ExItsBackupDatabaseKind DatabaseKind,
    string ConnectionString,
    string OutputDirectory,
    string EnvironmentClassification,
    string? ApplicationGitCommit = null,
    string? MigrationSchemaVersion = null,
    string? PgDumpPath = null,
    /// <summary>When set, run pg_dump inside this Docker container (Testcontainers-friendly).</summary>
    string? DockerContainerId = null);

public sealed record BackupResult(
    BackupManifest Manifest,
    string ArtifactPath,
    string ManifestPath,
    TimeSpan Duration);

public sealed record RestoreRequest(
    ExItsBackupDatabaseKind ExpectedDatabaseKind,
    string ConnectionString,
    string ArtifactPath,
    string ManifestPath,
    bool AllowDestructiveRestore = false,
    string? DestructiveConfirmation = null,
    string? PgRestorePath = null,
    string? DockerContainerId = null);

public sealed record RestoreResult(
    bool Succeeded,
    TimeSpan Duration,
    string Message);

/// <summary>Creates and restores PostgreSQL custom-format dumps using pg_dump/pg_restore.</summary>
public sealed class PostgreSqlBackupService
{
    private const string ContainerDumpPath = "/tmp/exits-backup.dump";

    public async Task<BackupResult> CreateBackupAsync(BackupRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Directory.CreateDirectory(request.OutputDirectory);

        var createdAt = DateTimeOffset.UtcNow;
        var backupSetId = BackupArtifactNaming.BuildBackupSetId(request.DatabaseKind, createdAt);
        var artifactName = BackupArtifactNaming.BuildArtifactFileName(backupSetId);
        var manifestName = BackupArtifactNaming.BuildManifestFileName(backupSetId);
        var artifactPath = Path.Combine(request.OutputDirectory, artifactName);
        var manifestPath = Path.Combine(request.OutputDirectory, manifestName);

        if (File.Exists(artifactPath) || File.Exists(manifestPath))
        {
            throw new InvalidOperationException("Refusing to overwrite an existing backup artifact or manifest.");
        }

        var parsed = ConnectionStringParser.Parse(request.ConnectionString);
        var sw = Stopwatch.StartNew();

        string? serverVersion = null;
        try
        {
            serverVersion = await QueryServerVersionAsync(request.ConnectionString, ct).ConfigureAwait(false);
        }
        catch
        {
            // Version is metadata-only; dump can still proceed.
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(request.DockerContainerId))
            {
                await DockerExecAsync(
                        request.DockerContainerId,
                        [
                            "pg_dump",
                            "-U", parsed.Username,
                            "-d", parsed.Database,
                            "-Fc",
                            "-f", ContainerDumpPath
                        ],
                        parsed.Password,
                        ct)
                    .ConfigureAwait(false);
                await DockerCopyFromAsync(request.DockerContainerId, ContainerDumpPath, artifactPath, ct)
                    .ConfigureAwait(false);
            }
            else
            {
                var pgDump = request.PgDumpPath ?? ResolveTool("pg_dump");
                var args = string.Create(
                    CultureInfo.InvariantCulture,
                    $"--host={parsed.Host} --port={parsed.Port} --username={parsed.Username} --dbname={parsed.Database} --format=custom --file=\"{artifactPath}\" --no-password");
                await RunProcessAsync(pgDump, args, parsed.Password, ct).ConfigureAwait(false);
            }
        }
        catch
        {
            if (File.Exists(artifactPath))
            {
                File.Delete(artifactPath);
            }

            throw;
        }

        if (!File.Exists(artifactPath))
        {
            throw new InvalidOperationException("pg_dump did not produce an artifact.");
        }

        var checksum = BackupChecksum.Sha256HexFile(artifactPath);
        var size = new FileInfo(artifactPath).Length;
        sw.Stop();

        var manifest = new BackupManifest
        {
            BackupSetId = backupSetId,
            CreatedAtUtc = createdAt,
            EnvironmentClassification = request.EnvironmentClassification,
            ApplicationGitCommit = request.ApplicationGitCommit,
            PostgreSqlServerVersion = serverVersion,
            DatabaseKind = request.DatabaseKind,
            DatabaseName = parsed.Database,
            MigrationSchemaVersion = request.MigrationSchemaVersion,
            BackupFormat = "postgresql-custom",
            ArtifactFileName = artifactName,
            ArtifactSizeBytes = size,
            Sha256Checksum = checksum,
            EncryptionStatus = BackupConstants.EncryptionNone,
            ToolVersion = BackupConstants.ToolVersion,
            CompletionStatus = BackupConstants.CompletionComplete
        };

        await BackupManifestStore.WriteAsync(manifestPath, manifest, ct).ConfigureAwait(false);
        return new BackupResult(manifest, artifactPath, manifestPath, sw.Elapsed);
    }

    public async Task<RestoreResult> RestoreAsync(RestoreRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sw = Stopwatch.StartNew();

        if (!File.Exists(request.ArtifactPath) || !File.Exists(request.ManifestPath))
        {
            return new RestoreResult(false, sw.Elapsed, "Backup set incomplete: artifact or manifest missing.");
        }

        var manifest = await BackupManifestStore.ReadAsync(request.ManifestPath, ct).ConfigureAwait(false);
        if (manifest.DatabaseKind != request.ExpectedDatabaseKind)
        {
            return new RestoreResult(
                false,
                sw.Elapsed,
                $"Manifest database kind mismatch: expected {request.ExpectedDatabaseKind}, got {manifest.DatabaseKind}.");
        }

        if (!string.Equals(manifest.CompletionStatus, BackupConstants.CompletionComplete, StringComparison.Ordinal))
        {
            return new RestoreResult(false, sw.Elapsed, "Manifest completion status is not Complete.");
        }

        var actualChecksum = BackupChecksum.Sha256HexFile(request.ArtifactPath);
        if (!BackupChecksum.ConstantTimeEquals(manifest.Sha256Checksum, actualChecksum))
        {
            return new RestoreResult(false, sw.Elapsed, "Artifact SHA-256 checksum mismatch.");
        }

        var parsed = ConnectionStringParser.Parse(request.ConnectionString);
        var isEmpty = await IsDatabaseEffectivelyEmptyAsync(request.ConnectionString, ct).ConfigureAwait(false);
        if (!isEmpty)
        {
            if (!request.AllowDestructiveRestore
                || !string.Equals(
                    request.DestructiveConfirmation,
                    BackupConstants.DestructiveConfirmationToken,
                    StringComparison.Ordinal))
            {
                return new RestoreResult(
                    false,
                    sw.Elapsed,
                    "Refusing restore over a non-empty database without AllowDestructiveRestore and confirmation token DESTROY_AND_RESTORE.");
            }
        }
        else if (!string.Equals(parsed.Database, manifest.DatabaseName, StringComparison.OrdinalIgnoreCase)
                 && !request.AllowDestructiveRestore)
        {
            return new RestoreResult(
                false,
                sw.Elapsed,
                "Target database name differs from manifest. Pass AllowDestructiveRestore for approved empty disposable targets.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(request.DockerContainerId))
            {
                await DockerCopyToAsync(request.DockerContainerId, request.ArtifactPath, ContainerDumpPath, ct)
                    .ConfigureAwait(false);
                await DockerExecAsync(
                        request.DockerContainerId,
                        [
                            "pg_restore",
                            "-U", parsed.Username,
                            "-d", parsed.Database,
                            "--clean",
                            "--if-exists",
                            "--no-owner",
                            "--no-acl",
                            "--exit-on-error",
                            ContainerDumpPath
                        ],
                        parsed.Password,
                        ct)
                    .ConfigureAwait(false);
            }
            else
            {
                var pgRestore = request.PgRestorePath ?? ResolveTool("pg_restore");
                var args = string.Create(
                    CultureInfo.InvariantCulture,
                    $"--host={parsed.Host} --port={parsed.Port} --username={parsed.Username} --dbname={parsed.Database} --clean --if-exists --no-owner --no-acl --exit-on-error --no-password \"{request.ArtifactPath}\"");
                await RunProcessAsync(pgRestore, args, parsed.Password, ct).ConfigureAwait(false);
            }

            sw.Stop();
            return new RestoreResult(true, sw.Elapsed, "Restore completed.");
        }
        catch (InvalidOperationException ex)
        {
            sw.Stop();
            return new RestoreResult(false, sw.Elapsed, SecretRedaction.Redact(ex.Message));
        }
    }

    public static async Task VerifyArtifactAsync(string artifactPath, string manifestPath, CancellationToken ct = default)
    {
        var manifest = await BackupManifestStore.ReadAsync(manifestPath, ct).ConfigureAwait(false);
        if (!File.Exists(artifactPath))
        {
            throw new InvalidOperationException("Artifact file missing.");
        }

        var checksum = BackupChecksum.Sha256HexFile(artifactPath);
        if (!BackupChecksum.ConstantTimeEquals(manifest.Sha256Checksum, checksum))
        {
            throw new InvalidOperationException("Checksum verification failed.");
        }

        if (!string.Equals(manifest.CompletionStatus, BackupConstants.CompletionComplete, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Manifest is not Complete.");
        }

        var json = await File.ReadAllTextAsync(manifestPath, ct).ConfigureAwait(false);
        SecretRedaction.EnsureNoSecrets(json, "manifest");
    }

    /// <summary>Optional AES-256-GCM envelope for artifacts leaving the protected host.</summary>
    public static async Task EncryptArtifactAsync(
        string artifactPath,
        string encryptedOutputPath,
        byte[] key32Bytes,
        CancellationToken ct = default)
    {
        if (key32Bytes.Length != 32)
        {
            throw new ArgumentException("AES-256-GCM key must be 32 bytes.", nameof(key32Bytes));
        }

        if (File.Exists(encryptedOutputPath))
        {
            throw new InvalidOperationException("Encrypted output already exists.");
        }

        var plaintext = await File.ReadAllBytesAsync(artifactPath, ct).ConfigureAwait(false);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(key32Bytes, 16))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        await using var output = File.Create(encryptedOutputPath);
        await output.WriteAsync(nonce, ct).ConfigureAwait(false);
        await output.WriteAsync(tag, ct).ConfigureAwait(false);
        await output.WriteAsync(ciphertext, ct).ConfigureAwait(false);
    }

    private static string ResolveTool(string name)
    {
        var envName = "EXITS_" + name.ToUpperInvariant().Replace('-', '_') + "_PATH";
        var fromEnv = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
        {
            return fromEnv;
        }

        return name;
    }

    private static async Task RunProcessAsync(
        string fileName,
        string arguments,
        string password,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["PGPASSWORD"] = password;

        using var process = new Process { StartInfo = psi };
        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        SecretRedaction.EnsureNoSecrets(stdout, "pg stdout");
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} failed ({process.ExitCode}): {SecretRedaction.Redact(stderr)}");
        }
    }

    private static async Task DockerExecAsync(
        string containerId,
        IReadOnlyList<string> command,
        string? password,
        CancellationToken ct)
    {
        var args = new List<string> { "exec" };
        if (!string.IsNullOrEmpty(password))
        {
            args.Add("-e");
            args.Add("PGPASSWORD=" + password);
        }

        args.Add(containerId);
        args.AddRange(command);
        await RunDockerAsync(args, ct).ConfigureAwait(false);
    }

    private static Task DockerCopyFromAsync(string containerId, string remotePath, string localPath, CancellationToken ct) =>
        RunDockerAsync(["cp", $"{containerId}:{remotePath}", localPath], ct);

    private static Task DockerCopyToAsync(string containerId, string localPath, string remotePath, CancellationToken ct) =>
        RunDockerAsync(["cp", localPath, $"{containerId}:{remotePath}"], ct);

    private static async Task RunDockerAsync(IReadOnlyList<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        process.Start();
        var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"docker failed ({process.ExitCode}): {SecretRedaction.Redact(stderr)}");
        }
    }

    private static async Task<string?> QueryServerVersionAsync(string connectionString, CancellationToken ct)
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SHOW server_version;";
        return Convert.ToString(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false));
    }

    private static async Task<bool> IsDatabaseEffectivelyEmptyAsync(string connectionString, CancellationToken ct)
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(*)::int
            FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
              AND table_type = 'BASE TABLE';
            """;
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
        return count == 0;
    }
}
