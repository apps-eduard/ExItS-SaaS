using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosProductSellingModeMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosProductSellingMode";
    private const string PreviousMigration = "AddPosCatalogImportMetadata";

    [Fact]
    public async Task AddPosProductSellingMode_applies_defaults_PerItem_and_rolls_back()
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

        var productColumns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'products'
            """);
        Assert.Contains("selling_mode", productColumns);

        var importColumns = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'catalog_import_items'
            """);
        Assert.Contains("selling_mode", importColumns);

        var checks = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname IN ('products', 'catalog_import_items')
            """);
        Assert.Contains("ck_products_selling_mode", checks);
        Assert.Contains("ck_products_selling_mode_unit", checks);
        Assert.Contains("ck_catalog_import_items_selling_mode", checks);

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
            WHERE table_schema = 'pos' AND table_name = 'products'
            """);
        Assert.DoesNotContain("selling_mode", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Existing_product_row_receives_PerItem_default()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            var previous = context.Database.GetMigrations()
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
        }

        var orgId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO pos.products (
                    id, organization_id, name, unit_of_measure, selling_price, status,
                    catalog_source, created_at_utc, updated_at_utc)
                VALUES (
                    @id, @org, 'Legacy Product', 'Piece', 10.00, 'Active',
                    'Manual', @now, @now);
                """,
                connection);
            cmd.Parameters.AddWithValue("id", productId);
            cmd.Parameters.AddWithValue("org", orgId);
            cmd.Parameters.AddWithValue("now", now);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT selling_mode FROM pos.products WHERE id = @id;",
                connection);
            cmd.Parameters.AddWithValue("id", productId);
            var mode = (string?)await cmd.ExecuteScalarAsync();
            Assert.Equal("PerItem", mode);
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
}
