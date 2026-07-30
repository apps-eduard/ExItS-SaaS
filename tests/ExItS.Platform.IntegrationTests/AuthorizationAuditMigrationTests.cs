using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class AuthorizationAuditMigrationTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPlatformAuthorizationAndAudit";
    private const string PreviousMigration = "AddPlatformUsersMembershipsAndProductAccess";

    [Fact]
    public async Task Authorization_and_audit_migration_applies_rolls_back_and_reapplies()
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

        await AssertTablesPresentAsync("audit_records", "platform_role_assignments");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
            Assert.Contains(applied, m => m.Contains(PreviousMigration, StringComparison.Ordinal));
        }

        await AssertTablesAbsentAsync("audit_records", "platform_role_assignments");

        // Existing identity/access and commercial tables remain after rollback of the authorization/audit migration.
        await AssertTablesPresentAsync(
            "platform_users",
            "organization_memberships",
            "product_access_assignments",
            "organizations",
            "subscriptions");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        await AssertTablesPresentAsync("audit_records", "platform_role_assignments");
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
