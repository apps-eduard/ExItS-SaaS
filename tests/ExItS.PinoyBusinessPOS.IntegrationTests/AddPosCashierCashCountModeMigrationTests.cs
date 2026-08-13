using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosCashierCashCountModeMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosCashierCashCountMode";

    [Fact]
    public async Task AddPosCashierCashCountMode_applies_expected_schema()
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

        var columns = await QueryNamesAsync(
            """
            SELECT table_name || '.' || column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND (
                    (table_name = 'operational_setups' AND column_name = 'cash_count_mode')
                 OR (table_name = 'cashier_shifts' AND column_name IN ('effective_cash_count_mode', 'opening_cash_counted'))
              )
            """);
        Assert.Contains("operational_setups.cash_count_mode", columns);
        Assert.Contains("cashier_shifts.effective_cash_count_mode", columns);
        Assert.Contains("cashier_shifts.opening_cash_counted", columns);
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
