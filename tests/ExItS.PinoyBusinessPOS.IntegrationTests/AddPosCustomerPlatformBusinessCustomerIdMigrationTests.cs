using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosCustomerPlatformBusinessCustomerIdMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosCustomerPlatformBusinessCustomerId";
    private const string PreviousMigration = "AddSaleLineSellingModeSnapshot";

    [Fact]
    public async Task AddPosCustomerPlatformBusinessCustomerId_applies_nullable_unique_index_and_rolls_back()
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
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'customers'
            """);
        Assert.Contains("platform_business_customer_id", columns);

        var nullable = await QueryScalarAsync(
            """
            SELECT is_nullable
            FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = 'customers'
              AND column_name = 'platform_business_customer_id'
            """);
        Assert.Equal("YES", nullable);

        var indexes = await QueryNamesAsync(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos' AND tablename = 'customers'
            """);
        Assert.Contains("ux_customers_org_platform_business_customer", indexes);

        var foreignKeys = await QueryNamesAsync(
            """
            SELECT c.conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname = 'customers'
              AND c.contype = 'f'
            """);
        Assert.DoesNotContain(
            foreignKeys,
            name => name.Contains("platform_business_customer", StringComparison.OrdinalIgnoreCase));

        await using (var context = new PosDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var afterRollback = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'customers'
            """);
        Assert.DoesNotContain("platform_business_customer_id", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }
    }

    private async Task<HashSet<string>> QueryNamesAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private async Task<string?> QueryScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
