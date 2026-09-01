using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddCatalogProductGovernanceFoundationMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddCatalogProductGovernanceFoundation";
    private const string PreviousMigration = "AddCustomerOrderDeliveryDistanceExceptionSnapshot";

    [Fact]
    public async Task PGDF_MIG_01_12_applies_backfills_rolls_back_and_reapplies()
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

        Assert.True(await ScopeColumnsExistAsync());
        Assert.True(await AvailabilityTableExistsAsync());
        Assert.Contains("ck_products_scope", await ConstraintNamesAsync());
        Assert.Contains("ck_products_branch_local_origin", await ConstraintNamesAsync());
        Assert.Contains("pk_branch_product_availabilities", await ConstraintNamesAsync());

        await using (var context = new PosDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.False(await ScopeColumnsExistAsync());
        Assert.False(await AvailabilityTableExistsAsync());

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.True(await ScopeColumnsExistAsync());
        Assert.True(await AvailabilityTableExistsAsync());
    }

    [Fact]
    public async Task PGDF_MIG_02_to_09_existing_product_backfill_preserves_identity_and_price()
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
        const decimal price = 77.25m;

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO pos.products (
                    id, organization_id, name, unit_of_measure, selling_mode, selling_price, status,
                    catalog_source, created_at_utc, updated_at_utc)
                VALUES (
                    @id, @org, 'Legacy Product', 'Piece', 'PerItem', @price, 'Active',
                    'Manual', @now, @now);
                """,
                connection);
            cmd.Parameters.AddWithValue("id", productId);
            cmd.Parameters.AddWithValue("org", orgId);
            cmd.Parameters.AddWithValue("price", price);
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
                """
                SELECT id, organization_id, selling_price, status, created_at_utc, updated_at_utc,
                       scope, origin_branch_id
                FROM pos.products
                WHERE id = @id;
                """,
                connection);
            cmd.Parameters.AddWithValue("id", productId);
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(productId, reader.GetGuid(0));
            Assert.Equal(orgId, reader.GetGuid(1));
            Assert.Equal(price, reader.GetDecimal(2));
            Assert.Equal("Active", reader.GetString(3));
            // PostgreSQL timestamptz may round sub-microsecond DateTimeOffset ticks.
            Assert.Equal(now.UtcDateTime, reader.GetFieldValue<DateTimeOffset>(4).UtcDateTime, TimeSpan.FromMilliseconds(1));
            Assert.Equal(now.UtcDateTime, reader.GetFieldValue<DateTimeOffset>(5).UtcDateTime, TimeSpan.FromMilliseconds(1));
            Assert.Equal("OrganizationStandard", reader.GetString(6));
            Assert.True(reader.IsDBNull(7));
        }

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM pos.branch_product_availabilities;",
                connection);
            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            Assert.Equal(0L, count);
        }
    }

    [Fact]
    public async Task PGDF_MIG_10_11_db_rejects_invalid_local_and_scope()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using (var cmd = new NpgsqlCommand(
                         """
                         INSERT INTO pos.products (
                             id, organization_id, name, normalized_name, unit_of_measure, selling_mode, selling_price, status,
                             catalog_source, scope, origin_branch_id, created_at_utc, updated_at_utc)
                         VALUES (
                             @id, @org, 'Bad', 'BAD', 'Piece', 'PerItem', 1.00, 'Active',
                             'Manual', 'BranchLocal', NULL, @now, @now);
                         """,
                         connection))
        {
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("org", Guid.NewGuid());
            cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            Assert.Equal("23514", ex.SqlState);
        }

        await using (var cmd = new NpgsqlCommand(
                         """
                         INSERT INTO pos.products (
                             id, organization_id, name, normalized_name, unit_of_measure, selling_mode, selling_price, status,
                             catalog_source, scope, created_at_utc, updated_at_utc)
                         VALUES (
                             @id, @org, 'Bad', 'BAD', 'Piece', 'PerItem', 1.00, 'Active',
                             'Manual', 'Local', @now, @now);
                         """,
                         connection))
        {
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("org", Guid.NewGuid());
            cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            Assert.Equal("23514", ex.SqlState);
        }
    }

    private async Task<bool> ScopeColumnsExistAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = 'products'
              AND column_name IN ('scope', 'origin_branch_id')
            """,
            connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 2;
    }

    private async Task<bool> AvailabilityTableExistsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = 'pos' AND table_name = 'branch_product_availabilities'
            """,
            connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private async Task<HashSet<string>> ConstraintNamesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'pos'
              AND t.relname IN ('products', 'branch_product_availabilities')
            """,
            connection);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
