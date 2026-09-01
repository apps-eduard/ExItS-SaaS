using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddCatalogProductNormalizedNameIdentityMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddCatalogProductNormalizedNameIdentity";
    private const string PreviousMigration = "AddCatalogProductGovernanceFoundation";

    [Fact]
    public async Task PNAME_MIG_01_11_12_applies_backfills_index_rolls_back_and_reapplies()
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

        Assert.True(await NormalizedNameColumnExistsAsync());
        Assert.Contains("ux_products_org_normalized_name", await IndexNamesAsync());

        await using (var context = new PosDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.False(await NormalizedNameColumnExistsAsync());
        Assert.DoesNotContain("ux_products_org_normalized_name", await IndexNamesAsync());

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        Assert.True(await NormalizedNameColumnExistsAsync());
        Assert.Contains("ux_products_org_normalized_name", await IndexNamesAsync());
    }

    [Fact]
    public async Task PNAME_MIG_02_to_09_existing_row_backfill_preserves_identity()
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
        const decimal price = 55.25m;

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO pos.products (
                    id, organization_id, name, unit_of_measure, selling_mode, selling_price, status,
                    catalog_source, scope, origin_branch_id, created_at_utc, updated_at_utc)
                VALUES (
                    @id, @org, '  Coke   1L  ', 'Piece', 'PerItem', @price, 'Active',
                    'Manual', 'OrganizationStandard', NULL, @now, @now);
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
                SELECT id, organization_id, name, normalized_name, selling_price, status, scope,
                       origin_branch_id, created_at_utc, updated_at_utc
                FROM pos.products
                WHERE id = @id;
                """,
                connection);
            cmd.Parameters.AddWithValue("id", productId);
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(productId, reader.GetGuid(0));
            Assert.Equal(orgId, reader.GetGuid(1));
            Assert.Equal("  Coke   1L  ", reader.GetString(2)); // display not rewritten for existing rows
            Assert.Equal("COKE 1L", reader.GetString(3));
            Assert.Equal(price, reader.GetDecimal(4));
            Assert.Equal("Active", reader.GetString(5));
            Assert.Equal("OrganizationStandard", reader.GetString(6));
            Assert.True(reader.IsDBNull(7));
            Assert.Equal(now.UtcDateTime, reader.GetFieldValue<DateTimeOffset>(8).UtcDateTime, TimeSpan.FromMilliseconds(1));
            Assert.Equal(now.UtcDateTime, reader.GetFieldValue<DateTimeOffset>(9).UtcDateTime, TimeSpan.FromMilliseconds(1));
        }

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM pos.products WHERE organization_id = @org;",
                connection);
            cmd.Parameters.AddWithValue("org", orgId);
            Assert.Equal(1L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
        }
    }

    [Fact]
    public async Task PNAME_MIG_10_duplicate_preflight_aborts_without_auto_merge()
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
        var now = DateTimeOffset.UtcNow;
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            foreach (var (id, name) in new[] { (id1, "Coke 1L"), (id2, "coke 1l") })
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO pos.products (
                        id, organization_id, name, unit_of_measure, selling_mode, selling_price, status,
                        catalog_source, scope, created_at_utc, updated_at_utc)
                    VALUES (
                        @id, @org, @name, 'Piece', 'PerItem', 50.00, 'Active',
                        'Manual', 'OrganizationStandard', @now, @now);
                    """,
                    connection);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("org", orgId);
                cmd.Parameters.AddWithValue("name", name);
                cmd.Parameters.AddWithValue("now", now);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        await using (var context = new PosDbContext(options))
        {
            var ex = await Assert.ThrowsAnyAsync<Exception>(() => context.Database.MigrateAsync());
            Assert.Contains("MB2-01C-H1", ex.Message, StringComparison.Ordinal);
            Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // Cleanup so shared fixture can remigrate: delete duplicates then migrate to tip.
        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "DELETE FROM pos.products WHERE organization_id = @org;",
                connection);
            cmd.Parameters.AddWithValue("org", orgId);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        Assert.True(await NormalizedNameColumnExistsAsync());
    }

    private async Task<bool> NormalizedNameColumnExistsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_schema = 'pos'
              AND table_name = 'products'
              AND column_name = 'normalized_name'
            """,
            connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private async Task<HashSet<string>> IndexNamesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos' AND tablename = 'products'
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
