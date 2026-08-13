using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosInventoryLotsMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosInventoryLots";

    [Fact]
    public async Task AddPosInventoryLots_applies_expected_schema()
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
              AND table_name IN ('inventory_lots', 'inventory_lot_movements')
            """);
        Assert.Contains("inventory_lots", tables);
        Assert.Contains("inventory_lot_movements", tables);

        var columns = await QueryNamesAsync(
            """
            SELECT table_name || '.' || column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND (
                    (table_name = 'products' AND column_name IN ('tracks_expiration', 'expiration_warning_days'))
                 OR (table_name = 'stock_movements' AND column_name = 'inventory_lot_id')
                 OR (table_name = 'inventory_transfer_lines' AND column_name IN ('source_lot_id', 'lot_number', 'expiration_date'))
              )
            """);
        Assert.Contains("products.tracks_expiration", columns);
        Assert.Contains("products.expiration_warning_days", columns);
        Assert.Contains("stock_movements.inventory_lot_id", columns);
        Assert.Contains("inventory_transfer_lines.source_lot_id", columns);
        Assert.Contains("inventory_transfer_lines.lot_number", columns);
        Assert.Contains("inventory_transfer_lines.expiration_date", columns);

        var indexes = await QueryNamesAsync(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos'
              AND indexname IN (
                'ux_inventory_lots_identity_org',
                'ux_inventory_lots_identity_branch',
                'ix_inventory_lots_org_product_expiry',
                'ux_inventory_lot_movements_source_lot',
                'ux_inventory_transfer_lines_transfer_line_number',
                'ux_stock_movements_inventory_transfer_lot')
            """);
        Assert.Contains("ux_inventory_lots_identity_org", indexes);
        Assert.Contains("ux_inventory_lots_identity_branch", indexes);
        Assert.Contains("ix_inventory_lots_org_product_expiry", indexes);
        Assert.Contains("ux_inventory_lot_movements_source_lot", indexes);
        Assert.Contains("ux_inventory_transfer_lines_transfer_line_number", indexes);
        Assert.Contains("ux_stock_movements_inventory_transfer_lot", indexes);
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
