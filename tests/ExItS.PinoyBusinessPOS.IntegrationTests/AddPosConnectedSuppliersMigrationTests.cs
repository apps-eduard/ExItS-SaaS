using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosConnectedSuppliersMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string Target="AddPosConnectedSuppliers";
    private const string Previous="AddPosStockCountTitle";

    [Fact]
    public async Task Migration_applies_rolls_back_and_reapplies()
    {
        var options=new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using(var db=new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(),x=>x.Contains(Target,StringComparison.Ordinal));
        }
        Assert.Equal(4,await CountTablesAsync());

        await using(var db=new PosDbContext(options))
        {
            var previous=(await db.Database.GetAppliedMigrationsAsync()).Single(x=>x.Contains(Previous,StringComparison.Ordinal));
            await db.Database.MigrateAsync(previous);
            Assert.DoesNotContain(await db.Database.GetAppliedMigrationsAsync(),x=>x.Contains(Target,StringComparison.Ordinal));
        }
        Assert.Equal(0,await CountTablesAsync());

        await using(var db=new PosDbContext(options))
        {
            await db.Database.MigrateAsync();
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(),x=>x.Contains(Target,StringComparison.Ordinal));
        }
        Assert.Equal(4,await CountTablesAsync());
    }

    private async Task<long> CountTablesAsync()
    {
        await using var connection=new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command=new NpgsqlCommand("""
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema='pos' AND table_name IN (
              'connected_supplier_relationships','supplier_product_exposures',
              'buyer_supplier_product_links','connected_purchase_orders')
            """,connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
