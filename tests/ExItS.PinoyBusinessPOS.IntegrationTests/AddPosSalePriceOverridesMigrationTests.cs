using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// RMAP-B01 persistence: sale_price_override_adjustments applies, rolls back, and re-applies.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosSalePriceOverridesMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosSalePriceOverrides";
    private const string PreviousMigration = "AddOpeningClosingCashCountModes";
    private const string Adjustments = "sale_price_override_adjustments";

    [Fact]
    public async Task AddPosSalePriceOverrides_applies_rolls_back_and_reapplies()
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

        var columns = await ColumnsAsync(Adjustments);
        foreach (var column in new[]
                 {
                     "id", "sale_id", "organization_id", "sale_line_id", "baseline_unit_price",
                     "applied_unit_price", "reason", "applied_by", "recorded_at_utc"
                 })
        {
            Assert.Contains(column, columns);
        }

        Assert.Equal("18,2", await NumericTypeAsync(Adjustments, "baseline_unit_price"));
        Assert.Equal("18,2", await NumericTypeAsync(Adjustments, "applied_unit_price"));

        var checks = await ConstraintsAsync(Adjustments);
        Assert.Contains("ck_sale_price_override_adjustments_prices", checks);

        var indexes = await IndexesAsync(Adjustments);
        Assert.Contains("ix_sale_price_override_adjustments_org_sale", indexes);
        Assert.Contains("ix_sale_price_override_adjustments_sale_line", indexes);

        await using (var context = new PosDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.Empty(await ColumnsAsync(Adjustments));

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.Contains("baseline_unit_price", await ColumnsAsync(Adjustments));
    }

    private async Task<List<string>> ColumnsAsync(string table)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = @table
            ORDER BY ordinal_position;
            """;
        command.Parameters.AddWithValue("table", table);
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private async Task<string> NumericTypeAsync(string table, string column)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT numeric_precision::text || ',' || numeric_scale::text
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = @table AND column_name = @column;
            """;
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<List<string>> ConstraintsAsync(string table)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT con.conname
            FROM pg_constraint con
            INNER JOIN pg_class rel ON rel.oid = con.conrelid
            INNER JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
            WHERE nsp.nspname = 'pos' AND rel.relname = @table AND con.contype = 'c';
            """;
        command.Parameters.AddWithValue("table", table);
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private async Task<List<string>> IndexesAsync(string table)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos' AND tablename = @table;
            """;
        command.Parameters.AddWithValue("table", table);
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
