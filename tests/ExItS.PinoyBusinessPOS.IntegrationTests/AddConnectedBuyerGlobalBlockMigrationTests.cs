using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddConnectedBuyerGlobalBlockMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string Target = "AddConnectedBuyerGlobalBlock";
    private const string Previous = "HardenElectronicSalePaymentReservation";

    [Fact]
    public async Task Migration_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), x => x.Contains(Target, StringComparison.Ordinal));
        }

        Assert.True(await BlockColumnExistsAsync());
        Assert.True(await CanExposeDefaultIsTrueAsync());

        await using (var db = new PosDbContext(options))
        {
            var previous = (await db.Database.GetAppliedMigrationsAsync())
                .Single(x => x.Contains(Previous, StringComparison.Ordinal));
            await db.Database.MigrateAsync(previous);
        }

        Assert.False(await BlockColumnExistsAsync());

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        Assert.True(await BlockColumnExistsAsync());
        Assert.True(await CanExposeDefaultIsTrueAsync());
    }

    private async Task<bool> BlockColumnExistsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = 'pos'
                AND table_name = 'products'
                AND column_name = 'is_blocked_from_connected_buyers')
            """, connection);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<bool> CanExposeDefaultIsTrueAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT column_default
            FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = 'products'
              AND column_name = 'can_expose_to_connected_buyers'
            """, connection);
        var value = await command.ExecuteScalarAsync();
        return value is string text && text.Contains("true", StringComparison.OrdinalIgnoreCase);
    }
}
