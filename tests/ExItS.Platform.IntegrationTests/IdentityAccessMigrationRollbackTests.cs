using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class IdentityAccessMigrationRollbackTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPlatformUsersMembershipsAndProductAccess";
    private const string PreviousMigration = "AddEntitlementSnapshotsAndOverrides";

    [Fact]
    public async Task Identity_access_migration_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        await AssertTablesPresentAsync(
            "platform_users",
            "organization_memberships",
            "product_access_assignments");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
            Assert.Contains(applied, m => m.Contains(PreviousMigration, StringComparison.Ordinal));
        }

        await AssertTablesAbsentAsync(
            "platform_users",
            "organization_memberships",
            "product_access_assignments");

        // Existing Phase 3 commercial tables remain after rollback of identity migration.
        await AssertTablesPresentAsync(
            "organizations",
            "subscriptions",
            "entitlement_snapshots");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        await AssertTablesPresentAsync(
            "platform_users",
            "organization_memberships",
            "product_access_assignments");

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'platform'
              AND table_name IN ('AspNetUsers', 'password_hashes', 'refresh_tokens', 'patients')
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.False(await reader.ReadAsync());
    }

    private async Task AssertTablesPresentAsync(params string[] tables)
    {
        var existing = await QueryPlatformTablesAsync();
        foreach (var table in tables)
        {
            Assert.Contains(table, existing);
        }
    }

    private async Task AssertTablesAbsentAsync(params string[] tables)
    {
        var existing = await QueryPlatformTablesAsync();
        foreach (var table in tables)
        {
            Assert.DoesNotContain(table, existing);
        }
    }

    private async Task<HashSet<string>> QueryPlatformTablesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'platform'
              AND table_type = 'BASE TABLE'
            """,
            connection);

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
