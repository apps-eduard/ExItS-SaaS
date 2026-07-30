using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExItS.BackupRestore.Tests;

public sealed class BackupManifestAndRetentionTests
{
    [Fact]
    public void Artifact_names_are_unique_per_backup_set()
    {
        var a = BackupArtifactNaming.BuildBackupSetId(ExItsBackupDatabaseKind.Platform, DateTimeOffset.UtcNow);
        var b = BackupArtifactNaming.BuildBackupSetId(ExItsBackupDatabaseKind.Platform, DateTimeOffset.UtcNow);
        Assert.NotEqual(a, b);
        Assert.StartsWith("platform_", a, StringComparison.Ordinal);
        Assert.EndsWith(".dump", BackupArtifactNaming.BuildArtifactFileName(a), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Manifest_round_trips_without_secrets()
    {
        var dir = Path.Combine(Path.GetTempPath(), "exits-backup-ut", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "m.manifest.json");
            var manifest = new BackupManifest
            {
                BackupSetId = "platform_test",
                CreatedAtUtc = DateTimeOffset.Parse("2026-07-31T00:00:00Z"),
                EnvironmentClassification = "Testing",
                DatabaseKind = ExItsBackupDatabaseKind.Platform,
                DatabaseName = "ExItS_Platform",
                BackupFormat = "postgresql-custom",
                ArtifactFileName = "platform_test.dump",
                ArtifactSizeBytes = 12,
                Sha256Checksum = BackupChecksum.Sha256Hex("abc"u8.ToArray()),
                EncryptionStatus = BackupConstants.EncryptionNone,
                ToolVersion = BackupConstants.ToolVersion,
                CompletionStatus = BackupConstants.CompletionComplete
            };

            await BackupManifestStore.WriteAsync(path, manifest);
            var loaded = await BackupManifestStore.ReadAsync(path);
            Assert.Equal(manifest.BackupSetId, loaded.BackupSetId);
            Assert.Equal(BackupConstants.PhaseMarker, loaded.PhaseMarker);

            var json = await File.ReadAllTextAsync(path);
            SecretRedaction.EnsureNoSecrets(json, "manifest");
            Assert.DoesNotContain("Password=", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task Manifest_write_refuses_overwrite()
    {
        var dir = Path.Combine(Path.GetTempPath(), "exits-backup-ut", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "m.manifest.json");
            var manifest = new BackupManifest
            {
                BackupSetId = "pos_test",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                EnvironmentClassification = "Testing",
                DatabaseKind = ExItsBackupDatabaseKind.PinoyBusinessPos,
                DatabaseName = "ExItS_POS",
                BackupFormat = "postgresql-custom",
                ArtifactFileName = "pos_test.dump",
                ArtifactSizeBytes = 1,
                Sha256Checksum = "aa",
                EncryptionStatus = BackupConstants.EncryptionNone,
                ToolVersion = BackupConstants.ToolVersion,
                CompletionStatus = BackupConstants.CompletionComplete
            };
            await BackupManifestStore.WriteAsync(path, manifest);
            await Assert.ThrowsAsync<InvalidOperationException>(() => BackupManifestStore.WriteAsync(path, manifest));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Checksum_mismatch_is_detected()
    {
        var a = BackupChecksum.Sha256Hex("one"u8.ToArray());
        var b = BackupChecksum.Sha256Hex("two"u8.ToArray());
        Assert.False(BackupChecksum.ConstantTimeEquals(a, b));
        Assert.True(BackupChecksum.ConstantTimeEquals(a, a));
    }

    [Fact]
    public void Secret_redaction_masks_password_fragments()
    {
        var redacted = SecretRedaction.Redact("Host=db;Password=super-secret;Username=u");
        Assert.Contains("[REDACTED]", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret", redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void Retention_never_deletes_latest_complete_backup()
    {
        var now = DateTimeOffset.Parse("2026-07-31T12:00:00Z");
        var candidates = new List<RetentionCandidate>
        {
            // 70 days ago on a Wednesday — outside daily/weekly/monthly provisional keep rules.
            new("old", DateTimeOffset.Parse("2026-05-20T12:00:00Z"), "old.dump", "old.json", IsComplete: true),
            new("latest", now.AddHours(-1), "latest.dump", "latest.json", IsComplete: true),
            new("broken", now.AddDays(-1), "broken.dump", "broken.json", IsComplete: false)
        };

        var decisions = RetentionCleaner.Evaluate(candidates, RetentionPolicy.Provisional, now);
        Assert.Contains(decisions, d => d.BackupSetId == "latest" && !d.Delete);
        Assert.Contains(decisions, d => d.BackupSetId == "broken" && d.Delete);
        Assert.Contains(decisions, d => d.BackupSetId == "old" && d.Delete);
    }

    [Fact]
    public async Task Encrypt_artifact_writes_nonce_tag_ciphertext()
    {
        var dir = Path.Combine(Path.GetTempPath(), "exits-backup-ut", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var plain = Path.Combine(dir, "a.dump");
            var enc = Path.Combine(dir, "a.dump.enc");
            await File.WriteAllBytesAsync(plain, Encoding.UTF8.GetBytes("not-a-real-dump"));
            var key = RandomNumberGenerator.GetBytes(32);
            await PostgreSqlBackupService.EncryptArtifactAsync(plain, enc, key);
            Assert.True(new FileInfo(enc).Length > 12 + 16);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                PostgreSqlBackupService.EncryptArtifactAsync(plain, enc, key));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Connection_string_parser_does_not_require_logging_password()
    {
        var parsed = ConnectionStringParser.Parse(
            "Host=127.0.0.1;Port=5432;Database=ExItS_Platform;Username=postgres;Password=dev-only");
        Assert.Equal("ExItS_Platform", parsed.Database);
        Assert.Equal("dev-only", parsed.Password);
    }
}
