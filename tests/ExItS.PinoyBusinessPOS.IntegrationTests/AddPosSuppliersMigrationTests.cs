using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosSuppliersMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosSuppliers";
    private const string PreviousMigration = "AddPosPerformanceIndexes";

    [Fact]
    public async Task AddPosSuppliers_applies_rolls_back_to_performance_indexes_and_reapplies()
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
              AND table_name IN ('suppliers', 'supplier_code_sequences')
            """);
        Assert.Contains("suppliers", tables);
        Assert.Contains("supplier_code_sequences", tables);

        await using (var context = new PosDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
            Assert.Contains(applied, m => m.Contains(PreviousMigration, StringComparison.Ordinal));
        }

        var afterRollback = await QueryNamesAsync(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
              AND table_name IN ('suppliers', 'supplier_code_sequences')
            """);
        Assert.DoesNotContain("suppliers", afterRollback);
        Assert.DoesNotContain("supplier_code_sequences", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Suppliers_schema_has_expected_indexes_and_constraints()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        var indexes = await QueryNamesAsync(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos'
              AND tablename IN ('suppliers', 'supplier_code_sequences')
            """);
        Assert.Contains("ux_suppliers_org_supplier_code", indexes);
        Assert.Contains("ix_suppliers_org_normalized_name", indexes);
        Assert.Contains("ix_suppliers_org_normalized_email", indexes);
        Assert.Contains("ix_suppliers_org_normalized_mobile", indexes);
        Assert.Contains("ix_suppliers_org_normalized_tax", indexes);
        Assert.Contains("ix_suppliers_org_status", indexes);

        var constraints = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname IN ('suppliers', 'supplier_code_sequences')
            """);
        Assert.Contains("ck_suppliers_status", constraints);
        Assert.Contains("ck_supplier_code_sequences_next_value_positive", constraints);
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
}
