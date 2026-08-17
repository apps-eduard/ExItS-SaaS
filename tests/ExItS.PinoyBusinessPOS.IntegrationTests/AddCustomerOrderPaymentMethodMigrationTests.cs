using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddCustomerOrderPaymentMethodMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string Target = "AddCustomerOrderPaymentMethod";
    private const string Previous = "AddConnectedPoConfirmationAndPaymentTerms";

    [Fact]
    public async Task Migration_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), x => x.Contains(Target, StringComparison.Ordinal));
        }

        Assert.True(await ColumnExistsAsync());
        Assert.True(await ConstraintExistsAsync());

        await using (var db = new PosDbContext(options))
        {
            var previous = (await db.Database.GetAppliedMigrationsAsync())
                .Single(x => x.Contains(Previous, StringComparison.Ordinal));
            await db.Database.MigrateAsync(previous);
        }

        Assert.False(await ColumnExistsAsync());

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        Assert.True(await ColumnExistsAsync());
        Assert.True(await ConstraintExistsAsync());
    }

    private async Task<bool> ColumnExistsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = 'pos'
                AND table_name = 'customer_orders'
                AND column_name = 'payment_method')
            """, connection);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<bool> ConstraintExistsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
              SELECT 1 FROM pg_constraint
              WHERE conname = 'ck_customer_orders_payment_method')
            """, connection);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
