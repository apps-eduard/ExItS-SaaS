using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosInventoryTransfersMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosInventoryTransfers";

    [Fact]
    public async Task AddPosInventoryTransfers_applies_expected_schema()
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
                'inventory_transfers',
                'inventory_transfer_lines',
                'inventory_transfer_number_sequences',
                'inventory_branch_balances')
            """);
        Assert.Contains("inventory_transfers", tables);
        Assert.Contains("inventory_transfer_lines", tables);
        Assert.Contains("inventory_transfer_number_sequences", tables);
        Assert.Contains("inventory_branch_balances", tables);

        var columns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = 'stock_movements'
              AND column_name = 'branch_id'
            """);
        Assert.Contains("branch_id", columns);

        var indexes = await QueryNamesAsync(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos'
              AND indexname IN (
                'ix_inventory_transfers_org_source',
                'ix_inventory_transfers_org_destination',
                'ix_inventory_transfers_org_status',
                'ux_inventory_transfers_org_transfer_number',
                'ux_inventory_transfer_lines_transfer_product',
                'ux_stock_movements_inventory_transfer_source')
            """);
        Assert.Contains("ix_inventory_transfers_org_source", indexes);
        Assert.Contains("ix_inventory_transfers_org_destination", indexes);
        Assert.Contains("ix_inventory_transfers_org_status", indexes);
        Assert.Contains("ux_inventory_transfers_org_transfer_number", indexes);
        Assert.Contains("ux_inventory_transfer_lines_transfer_product", indexes);
        Assert.Contains("ux_stock_movements_inventory_transfer_source", indexes);
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
