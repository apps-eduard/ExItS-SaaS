using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class AddGlobalProductSellingModeMigrationTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddGlobalProductSellingMode";
    private const string PreviousMigration = "AddPlanBusinessTypeGrantsAndOrgActivations";

    [Fact]
    public async Task AddGlobalProductSellingMode_applies_rolls_back_and_reapplies()
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

        var columns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'catalog' AND table_name = 'global_products'
            """);
        Assert.Contains("selling_mode", columns);

        var checks = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'catalog'
              AND t.relname = 'global_products'
            """);
        Assert.Contains("ck_global_products_selling_mode", checks);
        Assert.Contains("ck_global_products_selling_mode_unit", checks);

        await using (var context = new PlatformDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var afterRollback = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'catalog' AND table_name = 'global_products'
            """);
        Assert.DoesNotContain("selling_mode", afterRollback);

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }
    }

    private async Task<HashSet<string>> QueryNamesAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
