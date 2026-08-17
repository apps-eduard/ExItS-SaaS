using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddProductImagesMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string Target = "AddProductImages";
    private const string Previous = "AddCustomerOrderPaymentMethod";

    [Fact]
    public async Task Migration_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), x => x.Contains(Target, StringComparison.Ordinal));
        }

        Assert.True(await TableExistsAsync());

        await using (var db = new PosDbContext(options))
        {
            var previous = (await db.Database.GetAppliedMigrationsAsync())
                .Single(x => x.Contains(Previous, StringComparison.Ordinal));
            await db.Database.MigrateAsync(previous);
        }

        Assert.False(await TableExistsAsync());

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        Assert.True(await TableExistsAsync());
    }

    private async Task<bool> TableExistsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
              SELECT 1 FROM information_schema.tables
              WHERE table_schema = 'pos'
                AND table_name = 'product_images')
            """, connection);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
