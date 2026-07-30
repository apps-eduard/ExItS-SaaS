using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosRepaymentsMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosRepayments";
    private const string PriorMigration = "20260730081049_AddPosCreditEntries";

    [Fact]
    public async Task AddPosRepayments_applies_rolls_back_and_reapplies()
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

        Assert.Contains("repayments", await QueryPosTablesAsync());
        Assert.Contains("credit_entries", await QueryPosTablesAsync());

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(PriorMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
            Assert.Contains(applied, m => m.Contains("AddPosCreditEntries", StringComparison.Ordinal));
        }

        Assert.DoesNotContain("repayments", await QueryPosTablesAsync());
        Assert.Contains("credit_entries", await QueryPosTablesAsync());

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.Contains("repayments", await QueryPosTablesAsync());

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
              AND table_name IN ('AspNetUsers', 'patients', 'platform_users', 'ledger_entries', 'due_dates', 'statements')
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.False(await reader.ReadAsync());
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
