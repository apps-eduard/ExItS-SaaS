using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// RMAP-B03 persistence: the additive discount columns, the adjustment audit table, and the
/// reconciliation checks apply, roll back, and re-apply cleanly.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosCommercialSaleDiscountsMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosCommercialSaleDiscounts";
    private const string PreviousMigration = "AddOperationalActorTraceabilityFields";
    private const string Adjustments = "sale_commercial_discount_adjustments";

    [Fact]
    public async Task AddPosCommercialSaleDiscounts_applies_rolls_back_and_reapplies()
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

        var saleColumns = await ColumnsAsync("sales");
        Assert.Contains("gross_subtotal", saleColumns);
        Assert.Contains("line_discount_total", saleColumns);
        Assert.Contains("sale_discount_total", saleColumns);
        Assert.Contains("discount_total", saleColumns);
        // The DTO contract is unchanged: subtotal stays, now carrying the net pre-tax amount.
        Assert.Contains("subtotal", saleColumns);

        var lineColumns = await ColumnsAsync("sale_lines");
        Assert.Contains("gross_line_total", lineColumns);
        Assert.Contains("line_discount_amount", lineColumns);
        Assert.Contains("sale_discount_allocated_amount", lineColumns);
        Assert.Contains("line_total", lineColumns);

        var adjustmentColumns = await ColumnsAsync(Adjustments);
        foreach (var column in new[]
                 {
                     "id", "sale_id", "organization_id", "scope", "method", "source", "requested_value",
                     "calculated_amount", "reason", "sale_line_id", "applied_by", "recorded_at_utc"
                 })
        {
            Assert.Contains(column, adjustmentColumns);
        }

        Assert.Equal("18,2", await NumericTypeAsync("sales", "gross_subtotal"));
        Assert.Equal("18,2", await NumericTypeAsync("sale_lines", "gross_line_total"));
        Assert.Equal("18,2", await NumericTypeAsync(Adjustments, "calculated_amount"));

        var saleChecks = await ConstraintsAsync("sales");
        Assert.Contains("ck_sales_discount_reconciliation", saleChecks);
        Assert.Contains("ck_sales_discount_totals_non_negative", saleChecks);

        var lineChecks = await ConstraintsAsync("sale_lines");
        Assert.Contains("ck_sale_lines_discount_reconciliation", lineChecks);
        Assert.Contains("ck_sale_lines_discount_amounts_non_negative", lineChecks);

        var adjustmentChecks = await ConstraintsAsync(Adjustments);
        foreach (var check in new[]
                 {
                     "ck_sale_commercial_discount_adjustments_scope",
                     "ck_sale_commercial_discount_adjustments_method",
                     "ck_sale_commercial_discount_adjustments_source",
                     "ck_sale_commercial_discount_adjustments_amounts",
                     "ck_sale_commercial_discount_adjustments_line_scope"
                 })
        {
            Assert.Contains(check, adjustmentChecks);
        }

        var indexes = await IndexesAsync(Adjustments);
        Assert.Contains("ix_sale_commercial_discount_adjustments_org_sale", indexes);
        Assert.Contains("ix_sale_commercial_discount_adjustments_sale_line", indexes);

        await using (var context = new PosDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var afterRollback = await ColumnsAsync("sales");
        Assert.DoesNotContain("gross_subtotal", afterRollback);
        Assert.DoesNotContain("discount_total", afterRollback);
        Assert.Contains("subtotal", afterRollback);
        Assert.Empty(await ColumnsAsync(Adjustments));

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.Contains("gross_subtotal", await ColumnsAsync("sales"));
        Assert.Contains("gross_line_total", await ColumnsAsync("sale_lines"));
    }

    private Task<HashSet<string>> ColumnsAsync(string table) => QueryNamesAsync(
        """
        SELECT column_name
        FROM information_schema.columns
        WHERE table_schema = 'pos' AND table_name = @table
        """,
        table);

    private Task<HashSet<string>> ConstraintsAsync(string table) => QueryNamesAsync(
        """
        SELECT conname
        FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid
        JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'pos' AND t.relname = @table
        """,
        table);

    private Task<HashSet<string>> IndexesAsync(string table) => QueryNamesAsync(
        """
        SELECT indexname
        FROM pg_indexes
        WHERE schemaname = 'pos' AND tablename = @table
        """,
        table);

    private async Task<string?> NumericTypeAsync(string table, string column)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT numeric_precision || ',' || numeric_scale
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = @table AND column_name = @column
            """,
            connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return (await command.ExecuteScalarAsync())?.ToString();
    }

    private async Task<HashSet<string>> QueryNamesAsync(string sql, string table)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("table", table);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
