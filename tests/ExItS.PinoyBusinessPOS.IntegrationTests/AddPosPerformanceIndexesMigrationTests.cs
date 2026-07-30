using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>P9-WP02: justified performance indexes migrate apply / rollback / re-apply.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosPerformanceIndexesMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosPerformanceIndexes";
    private const string PreviousMigration = "AddPosExpenses";

    [Fact]
    public async Task AddPosPerformanceIndexes_applies_rolls_back_and_reapplies()
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

        var indexes = await QueryNamesAsync(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos'
              AND indexname IN (
                'ix_sale_lines_org_product',
                'ix_stock_movements_org_recorded',
                'ix_customers_org_updated')
            """);
        Assert.Contains("ix_sale_lines_org_product", indexes);
        Assert.Contains("ix_stock_movements_org_recorded", indexes);
        Assert.Contains("ix_customers_org_updated", indexes);

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
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos'
              AND indexname IN (
                'ix_sale_lines_org_product',
                'ix_stock_movements_org_recorded',
                'ix_customers_org_updated')
            """);
        Assert.DoesNotContain("ix_sale_lines_org_product", afterRollback);
        Assert.DoesNotContain("ix_stock_movements_org_recorded", afterRollback);
        Assert.DoesNotContain("ix_customers_org_updated", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }
    }

    private async Task<HashSet<string>> QueryNamesAsync(string sql)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
