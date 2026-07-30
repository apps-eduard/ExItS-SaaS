using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddProductBasedUtangMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddProductBasedUtang";
    private const string PreviousMigration = "AddPosSimpleSales";

    [Fact]
    public async Task AddProductBasedUtang_applies_rolls_back_to_simple_sales_and_reapplies()
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

        var saleColumns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'sales'
            """);
        Assert.Contains("customer_id", saleColumns);
        Assert.Contains("linked_credit_entry_id", saleColumns);

        var creditColumns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'credit_entries'
            """);
        Assert.Contains("source_sale_id", creditColumns);

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
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'sales'
            """);
        Assert.DoesNotContain("customer_id", afterRollback);
        Assert.DoesNotContain("linked_credit_entry_id", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Product_based_utang_schema_has_expected_indexes_constraints_and_fks()
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
              AND tablename IN ('sales', 'credit_entries')
            """);
        Assert.Contains("ux_sales_linked_credit_entry_id", indexes);
        Assert.Contains("ix_sales_customer_id", indexes);
        Assert.Contains("ux_credit_entries_source_sale_id", indexes);

        var constraints = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname IN ('sales', 'credit_entries')
            """);
        Assert.Contains("ck_sales_payment_method", constraints);
        Assert.Contains("ck_sales_tender_consistency", constraints);
        Assert.Contains("fk_sales_customers", constraints);
        Assert.Contains("fk_credit_entries_source_sale", constraints);
        Assert.DoesNotContain("fk_sales_linked_credit_entry", constraints);

        Assert.Equal("r", await QueryDeleteRuleAsync("fk_sales_customers"));
        Assert.Equal("r", await QueryDeleteRuleAsync("fk_credit_entries_source_sale"));

        var paymentMethodSql = await QueryCheckConstraintSqlAsync("ck_sales_payment_method");
        Assert.Contains("Utang", paymentMethodSql, StringComparison.Ordinal);

        var tenderSql = await QueryCheckConstraintSqlAsync("ck_sales_tender_consistency");
        Assert.Contains("linked_credit_entry_id", tenderSql, StringComparison.Ordinal);
        Assert.Contains("Utang", tenderSql, StringComparison.Ordinal);
    }

    private async Task<string> QueryCheckConstraintSqlAsync(string constraintName)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT pg_get_constraintdef(c.oid)
            FROM pg_constraint c
            JOIN pg_namespace n ON n.oid = c.connamespace
            WHERE n.nspname = 'pos' AND c.conname = @name
            """,
            connection);
        command.Parameters.AddWithValue("name", constraintName);
        return (string?)await command.ExecuteScalarAsync() ?? string.Empty;
    }

    private async Task<string?> QueryDeleteRuleAsync(string constraintName)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT confdeltype::text FROM pg_constraint WHERE conname = @name",
            connection);
        command.Parameters.AddWithValue("name", constraintName);
        return (string?)await command.ExecuteScalarAsync();
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
