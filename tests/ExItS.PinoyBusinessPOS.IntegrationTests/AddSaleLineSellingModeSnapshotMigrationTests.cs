using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddSaleLineSellingModeSnapshotMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddSaleLineSellingModeSnapshot";
    private const string PreviousMigration = "AddPosProductSellingMode";

    [Fact]
    public async Task AddSaleLineSellingModeSnapshot_applies_defaults_PerItem_and_rolls_back()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var columns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'sale_lines'
            """);
        Assert.Contains("selling_mode_snapshot", columns);
        Assert.Contains("quantity", columns);

        var quantityType = await QueryScalarAsync(
            """
            SELECT numeric_precision || ',' || numeric_scale
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'sale_lines' AND column_name = 'quantity'
            """);
        Assert.Equal("18,3", quantityType);

        var checks = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos' AND t.relname = 'sale_lines'
            """);
        Assert.Contains("ck_sale_lines_selling_mode", checks);

        await using (var context = new PosDbContext(options))
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
            WHERE table_schema = 'pos' AND table_name = 'sale_lines'
            """);
        Assert.DoesNotContain("selling_mode_snapshot", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
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

    private async Task<string?> QueryScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
