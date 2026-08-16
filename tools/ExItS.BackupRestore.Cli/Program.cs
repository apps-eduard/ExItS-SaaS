using ExItS.BackupRestore;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: backup|verify|restore|encrypt|retention ...");
    return 2;
}

try
{
    return args[0].ToLowerInvariant() switch
    {
        "backup" => await RunBackupAsync(args),
        "verify" => await RunVerifyAsync(args),
        "restore" => await RunRestoreAsync(args),
        "encrypt" => await RunEncryptAsync(args),
        "retention" => await RunRetentionAsync(args),
        _ => Fail("Unknown command.")
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine(SecretRedaction.Redact(ex.Message));
    return 1;
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 2;
}

static string? ResolveConnectionString(ExItsBackupDatabaseKind kind)
{
    var envName = kind == ExItsBackupDatabaseKind.Platform
        ? "EXITS_PLATFORM_DATABASE"
        : "EXITS_POS_DATABASE";
    return Environment.GetEnvironmentVariable(envName);
}

static async Task<int> RunBackupAsync(string[] args)
{
    // backup <Platform|PinoyBusinessPos> <outputDir> <envClass> [--commit sha] [--migration ver] [--docker-container id]
    if (args.Length < 4)
    {
        return Fail("backup <kind> <outputDir> <envClass> [--commit sha] [--migration ver] [--docker-container id]");
    }

    var kind = Enum.Parse<ExItsBackupDatabaseKind>(args[1], ignoreCase: true);
    var cs = ResolveConnectionString(kind);
    if (string.IsNullOrWhiteSpace(cs))
    {
        return Fail("Missing connection string environment variable (EXITS_PLATFORM_DATABASE / EXITS_POS_DATABASE).");
    }

    string? commit = null;
    string? migration = null;
    string? dockerContainer = null;
    for (var i = 4; i < args.Length; i++)
    {
        if (args[i] == "--commit" && i + 1 < args.Length)
        {
            commit = args[++i];
        }
        else if (args[i] == "--migration" && i + 1 < args.Length)
        {
            migration = args[++i];
        }
        else if (args[i] == "--docker-container" && i + 1 < args.Length)
        {
            dockerContainer = args[++i];
        }
    }

    var result = await new PostgreSqlBackupService().CreateBackupAsync(new BackupRequest(
        kind, cs, args[2], args[3], commit, migration, DockerContainerId: dockerContainer));
    Console.WriteLine($"BackupSetId={result.Manifest.BackupSetId}");
    Console.WriteLine($"Artifact={Path.GetFileName(result.ArtifactPath)}");
    Console.WriteLine($"Sha256={result.Manifest.Sha256Checksum}");
    Console.WriteLine($"Bytes={result.Manifest.ArtifactSizeBytes}");
    Console.WriteLine($"DurationMs={(int)result.Duration.TotalMilliseconds}");
    return 0;
}

static async Task<int> RunVerifyAsync(string[] args)
{
    if (args.Length < 3)
    {
        return Fail("verify <artifact> <manifest>");
    }

    await PostgreSqlBackupService.VerifyArtifactAsync(args[1], args[2]);
    Console.WriteLine("VERIFY_OK");
    return 0;
}

static async Task<int> RunRestoreAsync(string[] args)
{
    // restore <kind> <artifact> <manifest> [--destructive] [--docker-container id]
    if (args.Length < 4)
    {
        return Fail("restore <kind> <artifact> <manifest> [--destructive] [--docker-container id]");
    }

    var kind = Enum.Parse<ExItsBackupDatabaseKind>(args[1], ignoreCase: true);
    var cs = ResolveConnectionString(kind);
    if (string.IsNullOrWhiteSpace(cs))
    {
        return Fail("Missing connection string environment variable (EXITS_PLATFORM_DATABASE / EXITS_POS_DATABASE).");
    }

    var destructive = args.Contains("--destructive", StringComparer.OrdinalIgnoreCase);
    string? dockerContainer = null;
    for (var i = 4; i < args.Length; i++)
    {
        if (args[i] == "--docker-container" && i + 1 < args.Length)
        {
            dockerContainer = args[++i];
        }
    }

    var result = await new PostgreSqlBackupService().RestoreAsync(new RestoreRequest(
        kind,
        cs,
        args[2],
        args[3],
        AllowDestructiveRestore: destructive,
        DestructiveConfirmation: destructive ? BackupConstants.DestructiveConfirmationToken : null,
        DockerContainerId: dockerContainer));
    Console.WriteLine($"Succeeded={result.Succeeded}");
    Console.WriteLine($"Message={result.Message}");
    Console.WriteLine($"DurationMs={(int)result.Duration.TotalMilliseconds}");
    return result.Succeeded ? 0 : 2;
}

static async Task<int> RunEncryptAsync(string[] args)
{
    if (args.Length < 3)
    {
        return Fail("encrypt <artifact> <output.enc>  (key from EXITS_BACKUP_KEY_FILE)");
    }

    var keyPath = Environment.GetEnvironmentVariable("EXITS_BACKUP_KEY_FILE");
    if (string.IsNullOrWhiteSpace(keyPath) || !File.Exists(keyPath))
    {
        return Fail("EXITS_BACKUP_KEY_FILE missing.");
    }

    var key = await File.ReadAllBytesAsync(keyPath);
    await PostgreSqlBackupService.EncryptArtifactAsync(args[1], args[2], key);
    Console.WriteLine("ENCRYPT_OK");
    return 0;
}

static async Task<int> RunRetentionAsync(string[] args)
{
    // retention <backupDir> [--execute]
    if (args.Length < 2)
    {
        return Fail("retention <backupDir> [--execute]");
    }

    var backupDir = args[1];
    var execute = args.Contains("--execute", StringComparer.OrdinalIgnoreCase);
    var candidates = new List<RetentionCandidate>();
    foreach (var manifestPath in Directory.EnumerateFiles(backupDir, "*.manifest.json"))
    {
        var manifest = await BackupManifestStore.ReadAsync(manifestPath);
        var artifact = Path.Combine(backupDir, manifest.ArtifactFileName);
        var complete = string.Equals(manifest.CompletionStatus, BackupConstants.CompletionComplete, StringComparison.Ordinal)
                       && File.Exists(artifact);
        candidates.Add(new RetentionCandidate(
            manifest.BackupSetId,
            manifest.CreatedAtUtc,
            artifact,
            manifestPath,
            complete));
    }

    var decisions = RetentionCleaner.Evaluate(candidates, RetentionPolicy.Provisional, DateTimeOffset.UtcNow);
    foreach (var decision in decisions)
    {
        Console.WriteLine($"{decision.BackupSetId} delete={decision.Delete} reason={decision.Reason}");
        if (execute && decision.Delete)
        {
            var match = candidates.First(c => c.BackupSetId == decision.BackupSetId);
            if (File.Exists(match.ArtifactPath))
            {
                File.Delete(match.ArtifactPath);
            }

            if (File.Exists(match.ManifestPath))
            {
                File.Delete(match.ManifestPath);
            }

            Console.WriteLine($"DELETED {decision.BackupSetId}");
        }
    }

    if (!execute)
    {
        Console.WriteLine("DRY_RUN_ONLY — pass --execute to apply (still never deletes latest complete).");
    }

    return 0;
}
