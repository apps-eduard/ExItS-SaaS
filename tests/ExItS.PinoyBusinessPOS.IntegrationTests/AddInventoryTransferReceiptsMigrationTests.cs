using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddInventoryTransferReceiptsMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddInventoryTransferReceiptsAndClosedQty";

    [Fact]
    public async Task AddInventoryTransferReceiptsAndClosedQty_applies_expected_schema()
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
                'inventory_transfer_receipts',
                'inventory_transfer_receipt_lines')
            """);
        Assert.Contains("inventory_transfer_receipts", tables);
        Assert.Contains("inventory_transfer_receipt_lines", tables);

        var columns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = 'inventory_transfer_lines'
              AND column_name = 'closed_qty'
            """);
        Assert.Contains("closed_qty", columns);
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
