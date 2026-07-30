using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosCreditDueDatesMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosCreditDueDates";
    private const string PriorMigration = "20260730084848_AddPosRepayments";

    [Fact]
    public async Task AddPosCreditDueDates_applies_rolls_back_and_reapplies()
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

        Assert.Contains("credit_due_date_changes", await QueryPosTablesAsync());
        Assert.Contains("repayments", await QueryPosTablesAsync());
        Assert.True(await ColumnExistsAsync("credit_entries", "current_due_date"));

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(PriorMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
            Assert.Contains(applied, m => m.Contains("AddPosRepayments", StringComparison.Ordinal));
        }

        Assert.DoesNotContain("credit_due_date_changes", await QueryPosTablesAsync());
        Assert.False(await ColumnExistsAsync("credit_entries", "current_due_date"));
        Assert.Contains("repayments", await QueryPosTablesAsync());

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.Contains("credit_due_date_changes", await QueryPosTablesAsync());
        Assert.True(await ColumnExistsAsync("credit_entries", "current_due_date"));

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
              AND table_name IN ('AspNetUsers', 'patients', 'platform_users', 'ledger_entries', 'statements', 'receipts', 'installments')
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.False(await reader.ReadAsync());
    }

    private async Task<bool> ColumnExistsAsync(string table, string column)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = @table
              AND column_name = @column
            """,
            connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        var result = await command.ExecuteScalarAsync();
        return result is not null;
    }

    private async Task<HashSet<string>> QueryPosTablesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
              AND table_type = 'BASE TABLE'
            """,
            connection);

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
