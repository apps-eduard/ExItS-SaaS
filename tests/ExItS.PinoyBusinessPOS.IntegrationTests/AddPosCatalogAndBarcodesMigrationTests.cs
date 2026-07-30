using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosCatalogAndBarcodesMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosCatalogAndBarcodes";
    private const string PreviousMigration = "AddPosIdempotencyRecords";

    [Fact]
    public async Task AddPosCatalogAndBarcodes_applies_rolls_back_to_idempotency_and_reapplies()
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

        var tables = await QueryPosTablesAsync();
        Assert.Contains("products", tables);
        Assert.Contains("product_categories", tables);

        await using (var context = new PosDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
            Assert.Contains(applied, m => m.Contains(PreviousMigration, StringComparison.Ordinal));
        }

        var afterRollback = await QueryPosTablesAsync();
        Assert.DoesNotContain("products", afterRollback);
        Assert.DoesNotContain("product_categories", afterRollback);
        Assert.Contains("customers", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var reapplied = await QueryPosTablesAsync();
        Assert.Contains("products", reapplied);
        Assert.Contains("product_categories", reapplied);
    }

    [Fact]
    public async Task Catalog_schema_has_expected_constraints_indexes_and_no_stock_columns()
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
              AND tablename IN ('products', 'product_categories')
            """);
        Assert.Contains("ux_products_org_normalized_sku", indexes);
        Assert.Contains("ux_products_org_barcode", indexes);
        Assert.Contains("ux_product_categories_org_active_name", indexes);

        var checks = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname IN ('products', 'product_categories')
            """);
        Assert.Contains("ck_products_selling_price_non_negative", checks);
        Assert.Contains("ck_products_status", checks);
        Assert.Contains("ck_products_unit_of_measure", checks);
        Assert.Contains("ck_products_barcode_digits", checks);
        Assert.Contains("ck_product_categories_status", checks);
        Assert.Contains("fk_products_product_categories", checks);

        var columns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'products'
            """);
        Assert.Contains("normalized_sku", columns);
        Assert.Contains("barcode", columns);
        Assert.Contains("selling_price", columns);
        Assert.Contains("unit_of_measure", columns);
        Assert.DoesNotContain("stock_on_hand", columns);
        Assert.DoesNotContain("quantity", columns);
        Assert.DoesNotContain("cost_price", columns);
        Assert.DoesNotContain("tax_rate", columns);

        var tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("sales", tables);
        Assert.DoesNotContain("sale_lines", tables);
        Assert.DoesNotContain("stock_levels", tables);
        Assert.DoesNotContain("inventory_movements", tables);
        Assert.DoesNotContain("product_barcodes", tables);
    }

    private Task<HashSet<string>> QueryPosTablesAsync() => QueryNamesAsync(
        """
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = 'pos'
          AND table_type = 'BASE TABLE'
        """);

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
