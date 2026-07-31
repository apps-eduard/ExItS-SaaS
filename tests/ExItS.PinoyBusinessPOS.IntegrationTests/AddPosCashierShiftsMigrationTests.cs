using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosCashierShiftsMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosCashierShifts";

    [Fact]
    public async Task AddPosCashierShifts_applies_rolls_back_and_reapplies()
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
              AND table_name IN ('cashier_shifts', 'cashier_shift_movements', 'cashier_shift_number_sequences')
            """);
        Assert.Contains("cashier_shifts", tables);
        Assert.Contains("cashier_shift_movements", tables);
        Assert.Contains("cashier_shift_number_sequences", tables);

        var shiftColumn = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = 'sales'
              AND column_name = 'cashier_shift_id'
            """);
        Assert.Contains("cashier_shift_id", shiftColumn);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync("20260730235210_EnrichPosStockCountDate");
        }

        Assert.DoesNotContain("cashier_shifts", await QueryPosTablesAsync());

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        Assert.Contains("cashier_shifts", await QueryPosTablesAsync());
    }

    private async Task<IReadOnlyList<string>> QueryPosTablesAsync()
    {
        return await QueryNamesAsync(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
            """);
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
