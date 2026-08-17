using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddProductPlatformBarcodeAndImageVersionMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string Target = "AddProductPlatformBarcodeAndImageVersion";
    private const string Previous = "AddProductImages";

    [Fact]
    public async Task Migration_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), x => x.Contains(Target, StringComparison.Ordinal));
        }

        Assert.True(await ColumnsExistAsync());

        await using (var db = new PosDbContext(options))
        {
            var previous = (await db.Database.GetAppliedMigrationsAsync())
                .Single(x => x.Contains(Previous, StringComparison.Ordinal));
            await db.Database.MigrateAsync(previous);
        }

        Assert.False(await ColumnsExistAsync());

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        Assert.True(await ColumnsExistAsync());
    }

    private async Task<bool> ColumnsExistAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = 'products'
              AND column_name IN ('platform_barcode', 'platform_image_version')
            """, connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 2;
    }
}
