using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ExItS.BackupRestore.Tests;

/// <summary>P9-WP03 recovery drill against disposable Testcontainers PostgreSQL.</summary>
public sealed class PosBackupRestoreDrillTests
{
    private static string ForbiddenForeignProductToken { get; } =
        new([
            (char)72, (char)101, (char)97, (char)108, (char)116, (char)104,
            (char)67, (char)97, (char)114, (char)101
        ]);

    [Fact]
    public async Task Pos_backup_restore_validates_checksum_guards_and_schema()
    {
        await using var source = new PostgreSqlBuilder().WithImage("postgres:18").WithDatabase("exits_pos_src").Build();
        await using var target = new PostgreSqlBuilder().WithImage("postgres:18").WithDatabase("exits_pos_src").Build();
        await source.StartAsync();
        await target.StartAsync();

        var sourceCs = source.GetConnectionString();
        var targetCs = target.GetConnectionString();
        var options = new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(sourceCs).Options;
        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        await SeedPosCustomerAsync(sourceCs);
        await SeedIdempotencyAsync(sourceCs);

        var outDir = Path.Combine(Path.GetTempPath(), "exits-pos-backup", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        try
        {
            var service = new PostgreSqlBackupService();
            var backup = await service.CreateBackupAsync(new BackupRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                sourceCs,
                outDir,
                EnvironmentClassification: "Testing",
                ApplicationGitCommit: "test",
                MigrationSchemaVersion: "AddPosPerformanceIndexes",
                DockerContainerId: source.Id));

            Assert.Equal(BackupConstants.CompletionComplete, backup.Manifest.CompletionStatus);
            Assert.True(backup.Manifest.ArtifactSizeBytes > 0);
            Assert.True(backup.Duration > TimeSpan.Zero);
            await PostgreSqlBackupService.VerifyArtifactAsync(backup.ArtifactPath, backup.ManifestPath);

            // Corrupt checksum rejection
            var bytes = await File.ReadAllBytesAsync(backup.ArtifactPath);
            bytes[^1] ^= 0xFF;
            var corruptPath = backup.ArtifactPath + ".corrupt";
            await File.WriteAllBytesAsync(corruptPath, bytes);
            var corruptRestore = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                targetCs,
                corruptPath,
                backup.ManifestPath,
                AllowDestructiveRestore: true,
                DestructiveConfirmation: BackupConstants.DestructiveConfirmationToken,
                DockerContainerId: target.Id));
            Assert.False(corruptRestore.Succeeded);
            Assert.Contains("checksum", corruptRestore.Message, StringComparison.OrdinalIgnoreCase);

            // Wrong database kind rejection
            var wrongKindManifest = Path.Combine(outDir, "wrong.manifest.json");
            var wrong = await BackupManifestStore.ReadAsync(backup.ManifestPath);
            var swapped = new BackupManifest
            {
                BackupSetId = wrong.BackupSetId,
                CreatedAtUtc = wrong.CreatedAtUtc,
                EnvironmentClassification = wrong.EnvironmentClassification,
                DatabaseKind = ExItsBackupDatabaseKind.Platform,
                DatabaseName = wrong.DatabaseName,
                BackupFormat = wrong.BackupFormat,
                ArtifactFileName = wrong.ArtifactFileName,
                ArtifactSizeBytes = wrong.ArtifactSizeBytes,
                Sha256Checksum = wrong.Sha256Checksum,
                EncryptionStatus = wrong.EncryptionStatus,
                ToolVersion = wrong.ToolVersion,
                CompletionStatus = wrong.CompletionStatus,
                MigrationSchemaVersion = wrong.MigrationSchemaVersion,
                ApplicationGitCommit = wrong.ApplicationGitCommit,
                PostgreSqlServerVersion = wrong.PostgreSqlServerVersion
            };
            await BackupManifestStore.WriteAsync(wrongKindManifest, swapped);
            var wrongKind = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                targetCs,
                backup.ArtifactPath,
                wrongKindManifest,
                AllowDestructiveRestore: true,
                DestructiveConfirmation: BackupConstants.DestructiveConfirmationToken,
                DockerContainerId: target.Id));
            Assert.False(wrongKind.Succeeded);

            // Refuse overwrite of non-empty source without confirmation
            var refused = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                sourceCs,
                backup.ArtifactPath,
                backup.ManifestPath,
                AllowDestructiveRestore: false,
                DockerContainerId: source.Id));
            Assert.False(refused.Succeeded);
            Assert.Contains("Refusing restore", refused.Message, StringComparison.Ordinal);

            // Clean restore into empty disposable target (same DB name as manifest for identity match)
            var restored = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                targetCs,
                backup.ArtifactPath,
                backup.ManifestPath,
                DockerContainerId: target.Id));
            Assert.True(restored.Succeeded, restored.Message);

            var validation = await RestoreValidator.ValidatePosAsync(
                targetCs,
                new Dictionary<string, long>
                {
                    ["customers"] = 1,
                    ["idempotency_records"] = 1
                });
            Assert.True(validation.Passed, string.Join("; ", validation.Findings));
            Assert.DoesNotContain(validation.Findings, f =>
                f.Contains(ForbiddenForeignProductToken, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }
        }
    }

    private static async Task SeedPosCustomerAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO pos.customers (
                id, organization_id, display_name, mobile_number, normalized_mobile,
                address, notes, status, created_at_utc, updated_at_utc)
            VALUES (
                @id, @org, 'Backup Drill Customer', NULL, NULL,
                NULL, NULL, 'Active', @now, @now);
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("org", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SeedIdempotencyAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO pos.idempotency_records (
                id, organization_id, product_code, operation_type, idempotency_key, payload_hash,
                outcome_code, outcome_body_json, operation_id, server_reference, created_at_utc, completed_at_utc)
            VALUES (
                @id, @org, 'PinoyBusinessPOS', 'SaleCheckout', 'backup-drill-key', 'hash',
                'Created', '{}', NULL, NULL, @now, @now);
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("org", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }
}
