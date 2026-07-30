using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosPurchasingMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosPurchasing";
    private const string PreviousMigration = "AddPosSuppliers";

    [Fact]
    public async Task AddPosPurchasing_applies_rolls_back_to_suppliers_and_reapplies()
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
                'purchase_orders', 'purchase_order_lines', 'purchase_order_number_sequences',
                'goods_receipts', 'goods_receipt_lines', 'grn_number_sequences')
            """);
        foreach (var table in new[]
                 {
                     "purchase_orders", "purchase_order_lines", "purchase_order_number_sequences",
                     "goods_receipts", "goods_receipt_lines", "grn_number_sequences"
                 })
        {
            Assert.Contains(table, tables);
        }

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
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
              AND table_name IN ('purchase_orders', 'goods_receipts')
            """);
        Assert.DoesNotContain("purchase_orders", afterRollback);
        Assert.DoesNotContain("goods_receipts", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }
    }

    [Fact]
    public async Task Purchasing_schema_has_expected_indexes_and_constraints()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        var indexes = await QueryNamesAsync(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos'
              AND tablename IN (
                'purchase_orders', 'purchase_order_lines', 'goods_receipts', 'goods_receipt_lines',
                'stock_movements')
            """);
        Assert.Contains("ux_purchase_orders_org_po_number", indexes);
        Assert.Contains("ux_purchase_order_lines_po_product", indexes);
        Assert.Contains("ux_goods_receipts_org_grn_number", indexes);
        Assert.Contains("ux_stock_movements_purchase_receipt_source", indexes);

        var constraints = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname IN ('purchase_orders', 'purchase_order_lines', 'goods_receipts', 'goods_receipt_lines')
            """);
        Assert.Contains("ck_purchase_orders_status", constraints);
        Assert.Contains("ck_purchase_order_lines_unit_cost_nonnegative", constraints);
        Assert.Contains("ck_goods_receipt_lines_received_qty_positive", constraints);
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
