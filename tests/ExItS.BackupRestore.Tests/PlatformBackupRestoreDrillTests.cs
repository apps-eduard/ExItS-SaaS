using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ExItS.BackupRestore.Tests;

public sealed class PlatformBackupRestoreDrillTests
{
    [Fact]
    public async Task Platform_backup_restore_preserves_schema_and_rejects_active_overwrite()
    {
        await using var source = new PostgreSqlBuilder().WithImage("postgres:18").WithDatabase("exits_platform_src").Build();
        await using var target = new PostgreSqlBuilder().WithImage("postgres:18").WithDatabase("exits_platform_src").Build();
        await source.StartAsync();
        await target.StartAsync();

        var sourceCs = source.GetConnectionString();
        var targetCs = target.GetConnectionString();
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(sourceCs).Options;
        await using (var db = new PlatformDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        await SeedOrganizationAsync(sourceCs);

        var outDir = Path.Combine(Path.GetTempPath(), "exits-platform-backup", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        try
        {
            var service = new PostgreSqlBackupService();
            var backup = await service.CreateBackupAsync(new BackupRequest(
                ExItsBackupDatabaseKind.Platform,
                sourceCs,
                outDir,
                "Testing",
                DockerContainerId: source.Id));

            Assert.Equal(ExItsBackupDatabaseKind.Platform, backup.Manifest.DatabaseKind);
            Assert.True(backup.Duration > TimeSpan.Zero);
            await PostgreSqlBackupService.VerifyArtifactAsync(backup.ArtifactPath, backup.ManifestPath);

            var refused = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.Platform,
                sourceCs,
                backup.ArtifactPath,
                backup.ManifestPath));
            Assert.False(refused.Succeeded);

            var restored = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.Platform,
                targetCs,
                backup.ArtifactPath,
                backup.ManifestPath,
                DockerContainerId: target.Id));
            Assert.True(restored.Succeeded, restored.Message);

            var validation = await RestoreValidator.ValidatePlatformAsync(
                targetCs,
                new Dictionary<string, long> { ["organizations"] = 1 });
            Assert.True(validation.Passed, string.Join("; ", validation.Findings));
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }
        }
    }

    private static async Task SeedOrganizationAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO platform.organizations (id, display_name, slug, status, created_at_utc, updated_at_utc)
            VALUES (@id, 'Backup Drill Org', @slug, 'Active', @now, @now);
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("slug", "backup-drill-" + Guid.NewGuid().ToString("N")[..8]);
        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }
}
