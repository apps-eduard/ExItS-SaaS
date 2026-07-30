using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosSimpleSalesMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosSimpleSales";
    private const string PreviousMigration = "AddPosCatalogAndBarcodes";

    [Fact]
    public async Task AddPosSimpleSales_applies_rolls_back_to_catalog_and_reapplies()
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
        Assert.Contains("sales", tables);
        Assert.Contains("sale_lines", tables);
        Assert.Contains("sale_number_sequences", tables);

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
        Assert.DoesNotContain("sales", afterRollback);
        Assert.DoesNotContain("sale_lines", afterRollback);
        Assert.DoesNotContain("sale_number_sequences", afterRollback);
        Assert.Contains("products", afterRollback);
        Assert.Contains("customers", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var reapplied = await QueryPosTablesAsync();
        Assert.Contains("sales", reapplied);
        Assert.Contains("sale_lines", reapplied);
        Assert.Contains("sale_number_sequences", reapplied);
    }

    [Fact]
    public async Task Sales_schema_has_expected_indexes_constraints_and_no_stock_or_tax_columns()
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
              AND tablename IN ('sales', 'sale_lines', 'sale_number_sequences')
            """);
        Assert.Contains("ux_sales_org_sale_number", indexes);
        Assert.Contains("ix_sales_org_recorded_at", indexes);
        Assert.Contains("ix_sales_org_status", indexes);
        Assert.Contains("ix_sales_org_payment_method", indexes);
        Assert.Contains("ux_sale_lines_sale_line_number", indexes);
        Assert.Contains("pk_sale_number_sequences", indexes);

        var constraints = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname IN ('sales', 'sale_lines', 'sale_number_sequences')
            """);
        Assert.Contains("ck_sales_status", constraints);
        Assert.Contains("ck_sales_payment_method", constraints);
        Assert.Contains("ck_sales_totals_non_negative", constraints);
        Assert.Contains("ck_sales_void_consistency", constraints);
        Assert.Contains("ck_sales_tender_consistency", constraints);
        Assert.Contains("ck_sale_lines_quantity_positive", constraints);
        Assert.Contains("ck_sale_lines_amounts_non_negative", constraints);
        Assert.Contains("ck_sale_lines_unit_of_measure", constraints);
        Assert.Contains("ck_sale_number_sequences_last_value_positive", constraints);

        // product_id restricts (products are never hard-deleted); sale_id cascades to prevent orphans.
        Assert.Contains("fk_sale_lines_products", constraints);
        Assert.Contains("fk_sale_lines_sales", constraints);
        Assert.Equal("r", await QueryDeleteRuleAsync("fk_sale_lines_products"));
        Assert.Equal("c", await QueryDeleteRuleAsync("fk_sale_lines_sales"));

        var saleColumns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'sales'
            """);
        Assert.Contains("sale_number", saleColumns);
        Assert.Contains("amount_tendered", saleColumns);
        Assert.Contains("change_amount", saleColumns);
        Assert.Contains("gcash_reference", saleColumns);
        Assert.Contains("recorded_by", saleColumns);
        Assert.Contains("void_reason", saleColumns);
        Assert.DoesNotContain("tax_amount", saleColumns);
        Assert.DoesNotContain("discount_amount", saleColumns);
        Assert.DoesNotContain("customer_id", saleColumns);
        Assert.DoesNotContain("credit_entry_id", saleColumns);
        Assert.DoesNotContain("refunded_amount", saleColumns);

        var lineColumns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'sale_lines'
            """);
        Assert.Contains("name_snapshot", lineColumns);
        Assert.Contains("unit_of_measure_snapshot", lineColumns);
        Assert.Contains("line_total", lineColumns);
        Assert.DoesNotContain("stock_on_hand", lineColumns);
        Assert.DoesNotContain("tax_rate", lineColumns);
        Assert.DoesNotContain("discount_amount", lineColumns);

        var tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("stock_levels", tables);
        Assert.DoesNotContain("inventory_movements", tables);
        Assert.DoesNotContain("sale_refunds", tables);
        Assert.DoesNotContain("sale_payments", tables);
        Assert.DoesNotContain("carts", tables);
    }

    private Task<HashSet<string>> QueryPosTablesAsync() => QueryNamesAsync(
        """
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = 'pos'
          AND table_type = 'BASE TABLE'
        """);

    private async Task<string?> QueryDeleteRuleAsync(string constraintName)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT confdeltype::text FROM pg_constraint WHERE conname = @name",
            connection);
        command.Parameters.AddWithValue("name", constraintName);
        return (string?)await command.ExecuteScalarAsync();
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
