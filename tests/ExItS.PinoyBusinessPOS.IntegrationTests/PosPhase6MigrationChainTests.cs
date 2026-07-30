using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// P6-WP06 closeout: full Phase 6 migration chain apply → stepwise rollback → re-apply.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosPhase6MigrationChainTests(PosPostgreSqlFixture fixture)
{
    private static readonly string[] Phase6Migrations =
    [
        "AddPosCustomers",
        "AddPosCreditEntries",
        "AddPosRepayments",
        "AddPosCreditDueDates"
    ];

    [Fact]
    public async Task Phase6_migration_chain_applies_rolls_back_stepwise_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            foreach (var marker in Phase6Migrations)
            {
                Assert.Contains(applied, m => m.Contains(marker, StringComparison.Ordinal));
            }
        }

        var tables = await QueryPosTablesAsync();
        Assert.Contains("customers", tables);
        Assert.Contains("credit_entries", tables);
        Assert.Contains("repayments", tables);
        Assert.Contains("credit_due_date_changes", tables);
        Assert.DoesNotContain("statements", tables);
        Assert.DoesNotContain("receipts", tables);
        Assert.DoesNotContain("ledger_entries", tables);
        Assert.DoesNotContain("platform_users", tables);
        Assert.DoesNotContain("patients", tables);

        Assert.True(await ColumnExistsAsync("credit_entries", "current_due_date"));
        Assert.True(await FilteredUniqueMobileIndexExistsAsync());

        // Roll back through each Phase 6 migration (newest → oldest prior).
        await MigrateToAsync(options, "20260730084848_AddPosRepayments");
        Assert.DoesNotContain("credit_due_date_changes", await QueryPosTablesAsync());

        await MigrateToAsync(options, "20260730081049_AddPosCreditEntries");
        Assert.DoesNotContain("repayments", await QueryPosTablesAsync());

        await MigrateToAsync(options, "20260730073757_AddPosCustomers");
        Assert.DoesNotContain("credit_entries", await QueryPosTablesAsync());
        Assert.Contains("customers", await QueryPosTablesAsync());

        // Re-apply to latest
        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains("AddPosCreditDueDates", StringComparison.Ordinal));
        }

        tables = await QueryPosTablesAsync();
        Assert.Contains("customers", tables);
        Assert.Contains("credit_entries", tables);
        Assert.Contains("repayments", tables);
        Assert.Contains("credit_due_date_changes", tables);
        Assert.True(await ColumnExistsAsync("credit_entries", "current_due_date"));
        Assert.True(await FilteredUniqueMobileIndexExistsAsync());
    }

    private static async Task MigrateToAsync(DbContextOptions<PosDbContext> options, string targetMigration)
    {
        await using var context = new PosDbContext(options);
        await context.Database.MigrateAsync(targetMigration);
    }

    private async Task<IReadOnlyList<string>> QueryPosTablesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
            ORDER BY table_name
            """;
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private async Task<bool> ColumnExistsAsync(string table, string column)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
              SELECT 1
              FROM information_schema.columns
              WHERE table_schema = 'pos'
                AND table_name = @table
                AND column_name = @column
            )
            """;
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<bool> FilteredUniqueMobileIndexExistsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
              SELECT 1
              FROM pg_indexes
              WHERE schemaname = 'pos'
                AND indexname = 'ux_customers_org_active_mobile'
            )
            """;
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
