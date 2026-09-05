using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosBasicInventoryMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosBasicInventory";
    private const string PreviousMigration = "AddProductBasedUtang";

    [Fact]
    public async Task AddPosBasicInventory_applies_rolls_back_to_utang_and_reapplies()
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
              AND table_name IN ('inventory_accounts', 'stock_movements')
            """);
        Assert.Contains("inventory_accounts", tables);
        Assert.Contains("stock_movements", tables);

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
              AND table_name IN ('inventory_accounts', 'stock_movements')
            """);
        Assert.DoesNotContain("inventory_accounts", afterRollback);
        Assert.DoesNotContain("stock_movements", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Basic_inventory_schema_has_expected_indexes_constraints_and_fks()
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
              AND tablename IN ('inventory_accounts', 'stock_movements')
            """);
        Assert.Contains("ux_inventory_accounts_org_product", indexes);
        Assert.Contains("ux_stock_movements_opening_stock_branch", indexes);
        Assert.Contains("ux_stock_movements_opening_stock_legacy", indexes);
        Assert.Contains("ux_stock_movements_sale_source", indexes);

        var constraints = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname IN ('inventory_accounts', 'stock_movements')
            """);
        Assert.Contains("ck_inventory_accounts_on_hand_non_negative", constraints);
        Assert.Contains("ck_stock_movements_quantity_effect_nonzero", constraints);
        Assert.Contains("ck_stock_movements_movement_type", constraints);
        Assert.Contains("fk_inventory_accounts_products", constraints);
        Assert.Contains("fk_stock_movements_inventory_accounts", constraints);
        Assert.Contains("fk_stock_movements_products", constraints);

        Assert.Equal("r", await QueryDeleteRuleAsync("fk_inventory_accounts_products"));
        Assert.Equal("r", await QueryDeleteRuleAsync("fk_stock_movements_products"));
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
