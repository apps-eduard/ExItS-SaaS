using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosAdvancedInventoryMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosAdvancedInventory";

    [Fact]
    public async Task AddPosAdvancedInventory_applies_expected_schema()
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

        var tables = await QueryNamesAsync(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
              AND table_name IN (
                'inventory_reorder_changes',
                'stock_counts',
                'stock_count_lines',
                'stock_count_number_sequences')
            """);
        Assert.Contains("inventory_reorder_changes", tables);
        Assert.Contains("stock_counts", tables);
        Assert.Contains("stock_count_lines", tables);
        Assert.Contains("stock_count_number_sequences", tables);

        var columns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = 'inventory_accounts'
              AND column_name = 'reorder_quantity'
            """);
        Assert.Contains("reorder_quantity", columns);

        var indexes = await QueryNamesAsync(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos'
              AND tablename = 'stock_movements'
            """);
        Assert.Contains("ux_stock_movements_stock_count_source", indexes);
        Assert.Contains("ux_stock_movements_purchase_receipt_source", indexes);
    }

    private async Task<IReadOnlyList<string>> QueryNamesAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
