using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosExpensesMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosExpenses";
    private const string PreviousMigration = "AddPosBasicInventory";

    [Fact]
    public async Task AddPosExpenses_applies_rolls_back_to_inventory_and_reapplies()
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
              AND table_name IN ('expense_categories', 'expenses', 'expense_number_sequences')
            """);
        Assert.Contains("expense_categories", tables);
        Assert.Contains("expenses", tables);
        Assert.Contains("expense_number_sequences", tables);

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
              AND table_name IN ('expense_categories', 'expenses', 'expense_number_sequences')
            """);
        Assert.DoesNotContain("expense_categories", afterRollback);
        Assert.DoesNotContain("expenses", afterRollback);
        Assert.DoesNotContain("expense_number_sequences", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Expenses_schema_has_expected_indexes_constraints_and_fks()
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
              AND tablename IN ('expense_categories', 'expenses', 'expense_number_sequences')
            """);
        Assert.Contains("ux_expense_categories_org_active_name", indexes);
        Assert.Contains("ux_expenses_org_expense_number", indexes);
        Assert.Contains("ix_expenses_org_expense_date", indexes);

        var constraints = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname IN ('expense_categories', 'expenses', 'expense_number_sequences')
            """);
        Assert.Contains("ck_expense_categories_status", constraints);
        Assert.Contains("ck_expenses_amount_positive", constraints);
        Assert.Contains("ck_expenses_payment_method", constraints);
        Assert.Contains("ck_expenses_tender_consistency", constraints);
        Assert.Contains("ck_expenses_void_consistency", constraints);
        Assert.Contains("fk_expenses_expense_categories", constraints);
        Assert.Contains("ck_expense_number_sequences_last_value_positive", constraints);

        Assert.Equal("r", await QueryDeleteRuleAsync("fk_expenses_expense_categories"));
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
