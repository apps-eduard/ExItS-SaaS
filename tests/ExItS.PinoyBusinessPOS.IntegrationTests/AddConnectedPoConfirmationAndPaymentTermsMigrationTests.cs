using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddConnectedPoConfirmationAndPaymentTermsMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string Target = "AddConnectedPoConfirmationAndPaymentTerms";
    private const string Previous = "AddDirectPurchaseReceipts";

    [Fact]
    public async Task Migration_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), x => x.Contains(Target, StringComparison.Ordinal));
        }

        Assert.True(await ColumnExistsAsync("purchase_orders", "payment_term"));
        Assert.True(await ColumnExistsAsync("connected_purchase_orders", "payment_term"));
        Assert.True(await ColumnExistsAsync("connected_purchase_order_lines", "proposed_qty"));
        Assert.True(await ColumnExistsAsync("connected_purchase_order_lines", "confirmed_qty"));
        Assert.True(await ColumnExistsAsync("connected_purchase_order_lines", "availability"));

        await using (var db = new PosDbContext(options))
        {
            var previous = (await db.Database.GetAppliedMigrationsAsync())
                .Single(x => x.Contains(Previous, StringComparison.Ordinal));
            await db.Database.MigrateAsync(previous);
        }

        Assert.False(await ColumnExistsAsync("purchase_orders", "payment_term"));
        Assert.False(await ColumnExistsAsync("connected_purchase_orders", "payment_term"));
        Assert.False(await ColumnExistsAsync("connected_purchase_order_lines", "proposed_qty"));

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        Assert.True(await ColumnExistsAsync("purchase_orders", "payment_term"));
        Assert.True(await ColumnExistsAsync("connected_purchase_order_lines", "confirmed_qty"));
    }

    private async Task<bool> ColumnExistsAsync(string table, string column)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = 'pos'
                AND table_name = @table
                AND column_name = @column)
            """, connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
