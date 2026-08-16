using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

/// <summary>P29-WP11: Platform constraint corruption + organization_branches index verification.</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class P29Wp11PlatformConstraintAndIndexTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task BranchDeliveryPolicy_constraints_reject_corruption_and_accept_valid_rows()
    {
        await EnsureLatestAsync();

        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var branch1 = Guid.NewGuid();
        var branch2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await ExecAsync(
            connection,
            """
            INSERT INTO platform.organizations (id, display_name, slug, status, created_at_utc, updated_at_utc)
            VALUES
              (@org1, 'P29 Org1', @slug1, 'Active', @now, @now),
              (@org2, 'P29 Org2', @slug2, 'Active', @now, @now);
            """,
            ("org1", org1),
            ("org2", org2),
            ("slug1", "p29-o1-" + Guid.NewGuid().ToString("N")[..8]),
            ("slug2", "p29-o2-" + Guid.NewGuid().ToString("N")[..8]),
            ("now", now));

        await ExecAsync(
            connection,
            """
            INSERT INTO platform.organization_branches
              (id, organization_id, code, name, is_primary, status, pickup_enabled, delivery_enabled, created_at_utc, updated_at_utc)
            VALUES
              (@b1, @org1, 'MAIN', 'Main', TRUE, 'Active', TRUE, TRUE, @now, @now),
              (@b2, @org2, 'MAIN', 'Main', TRUE, 'Active', TRUE, TRUE, @now, @now);
            """,
            ("b1", branch1),
            ("b2", branch2),
            ("org1", org1),
            ("org2", org2),
            ("now", now));

        // Valid policy
        await ExecAsync(
            connection,
            """
            INSERT INTO platform.branch_delivery_policies
              (branch_id, organization_id, minimum_order_amount, base_delivery_fee, included_distance_km,
               additional_fee_per_km, maximum_delivery_distance_km, free_delivery_threshold, created_at_utc, updated_at_utc)
            VALUES
              (@b1, @org1, 0, 50, 5, 10, 20, 500, @now, @now);
            """,
            ("b1", branch1),
            ("org1", org1),
            ("now", now));

        // Cross-org policy (branch org1, policy org2) — composite FK must reject
        var crossOrgEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            INSERT INTO platform.branch_delivery_policies
              (branch_id, organization_id, minimum_order_amount, base_delivery_fee, included_distance_km,
               additional_fee_per_km, maximum_delivery_distance_km, free_delivery_threshold, created_at_utc, updated_at_utc)
            VALUES
              (@b2, @org1, 0, 50, 5, 10, 20, NULL, @now, @now);
            """,
            ("b2", branch2),
            ("org1", org1),
            ("now", now)));
        Assert.Equal("23503", crossOrgEx.SqlState);

        // FreeDeliveryThreshold: NULL and 0 accepted
        await ExecAsync(
            connection,
            """
            INSERT INTO platform.branch_delivery_policies
              (branch_id, organization_id, minimum_order_amount, base_delivery_fee, included_distance_km,
               additional_fee_per_km, maximum_delivery_distance_km, free_delivery_threshold, created_at_utc, updated_at_utc)
            VALUES
              (@b2, @org2, 0, 50, 5, 10, 20, NULL, @now, @now);
            """,
            ("b2", branch2),
            ("org2", org2),
            ("now", now));

        await ExecAsync(
            connection,
            """
            UPDATE platform.branch_delivery_policies
            SET free_delivery_threshold = 0, updated_at_utc = @now
            WHERE branch_id = @b2;
            """,
            ("b2", branch2),
            ("now", now));

        // Negative free delivery threshold rejected
        var negThresholdEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            UPDATE platform.branch_delivery_policies
            SET free_delivery_threshold = -1, updated_at_utc = @now
            WHERE branch_id = @b2;
            """,
            ("b2", branch2),
            ("now", now)));
        Assert.Equal("23514", negThresholdEx.SqlState);

        // Lat/long pair violation on branch
        var latLongEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            UPDATE platform.organization_branches
            SET latitude = 14.5, longitude = NULL
            WHERE id = @b1;
            """,
            ("b1", branch1)));
        Assert.Equal("23514", latLongEx.SqlState);

        // Valid lat/long pair
        await ExecAsync(
            connection,
            """
            UPDATE platform.organization_branches
            SET latitude = 14.5995, longitude = 120.9842
            WHERE id = @b1;
            """,
            ("b1", branch1));
    }

    [Fact]
    public async Task Organization_branches_has_alternate_key_index_without_redundant_ux_index()
    {
        await EnsureLatestAsync();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'platform'
              AND tablename = 'organization_branches'
              AND indexdef ILIKE '%(id, organization_id)%'
            ORDER BY indexname;
            """,
            connection);

        var indexes = new List<string>();
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                indexes.Add(reader.GetString(0));
            }
        }

        Assert.Contains("AK_organization_branches_id_organization_id", indexes);
        Assert.DoesNotContain("ux_organization_branches_id_organization_id", indexes);
        Assert.Single(indexes);
    }

    private async Task EnsureLatestAsync()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using var context = new PlatformDbContext(options);
        await context.Database.MigrateAsync();
    }

    private static async Task ExecAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        await cmd.ExecuteNonQueryAsync();
    }
}
