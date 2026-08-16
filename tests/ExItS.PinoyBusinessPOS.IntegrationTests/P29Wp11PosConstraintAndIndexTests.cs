using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>P29-WP11: CustomerOrder / line constraint corruption + buyer index verification.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class P29Wp11PosConstraintAndIndexTests(PosPostgreSqlFixture fixture)
{
    [Fact]
    public async Task CustomerOrder_and_line_constraints_reject_corruption_and_accept_valid_rows()
    {
        await EnsureLatestAsync();

        var sellerOrg = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        var productSeller = Guid.NewGuid();
        var productOther = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.products (
                id, organization_id, name, unit_of_measure, selling_mode, selling_price, status,
                catalog_source, created_at_utc, updated_at_utc)
            VALUES
              (@p1, @seller, 'Seller Product', 'Piece', 'PerItem', 10.00, 'Active', 'Manual', @now, @now),
              (@p2, @other, 'Other Product', 'Piece', 'PerItem', 10.00, 'Active', 'Manual', @now, @now);
            """,
            ("p1", productSeller),
            ("p2", productOther),
            ("seller", sellerOrg),
            ("other", otherOrg),
            ("now", now));

        // Valid personal party order
        await ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_orders (
                id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                customer_party_type, customer_display_name_snapshot, customer_platform_user_id,
                merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                created_at_utc, updated_at_utc)
            VALUES (
                @oid, @seller, 'CO-0001', 'Draft', 'Pending', 'Unpaid',
                'Pickup', @branch, 'Main',
                'Personal', 'Buyer', @user,
                100.00, 20.00, 120.00, 'None',
                @now, @now);
            """,
            ("oid", orderId),
            ("seller", sellerOrg),
            ("branch", branchId),
            ("user", userId),
            ("now", now));

        // Valid line (same seller org)
        await ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_order_lines (
                id, order_id, seller_organization_id, product_id, line_number,
                name_snapshot, unit_snapshot, quantity, unit_price, discount, line_total)
            VALUES (
                @lid, @oid, @seller, @pid, 1,
                'Seller Product', 'Piece', 1, 100.00, 0, 100.00);
            """,
            ("lid", Guid.NewGuid()),
            ("oid", orderId),
            ("seller", sellerOrg),
            ("pid", productSeller));

        // Party XOR violation — Personal with buyer org set
        var partyXorEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_orders (
                id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                customer_party_type, customer_display_name_snapshot, customer_platform_user_id,
                customer_buyer_organization_id,
                merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                created_at_utc, updated_at_utc)
            VALUES (
                @oid, @seller, 'CO-XOR1', 'Draft', 'Pending', 'Unpaid',
                'Pickup', @branch, 'Main',
                'Personal', 'Bad', @user, @other,
                10.00, 0, 10.00, 'None',
                @now, @now);
            """,
            ("oid", Guid.NewGuid()),
            ("seller", sellerOrg),
            ("branch", branchId),
            ("user", userId),
            ("other", otherOrg),
            ("now", now)));
        Assert.Equal("23514", partyXorEx.SqlState);

        // Party XOR — Organization with platform user set
        var orgPartyXorEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_orders (
                id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                customer_party_type, customer_display_name_snapshot, customer_platform_user_id,
                customer_buyer_organization_id,
                merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                created_at_utc, updated_at_utc)
            VALUES (
                @oid, @seller, 'CO-XOR2', 'Draft', 'Pending', 'Unpaid',
                'Pickup', @branch, 'Main',
                'Organization', 'Bad Org', @user, @other,
                10.00, 0, 10.00, 'None',
                @now, @now);
            """,
            ("oid", Guid.NewGuid()),
            ("seller", sellerOrg),
            ("branch", branchId),
            ("user", userId),
            ("other", otherOrg),
            ("now", now)));
        Assert.Equal("23514", orgPartyXorEx.SqlState);

        // Missing required party identity
        var missingPartyEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_orders (
                id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                customer_party_type, customer_display_name_snapshot,
                merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                created_at_utc, updated_at_utc)
            VALUES (
                @oid, @seller, 'CO-NOPARTY', 'Draft', 'Pending', 'Unpaid',
                'Pickup', @branch, 'Main',
                'Personal', 'No Party',
                10.00, 0, 10.00, 'None',
                @now, @now);
            """,
            ("oid", Guid.NewGuid()),
            ("seller", sellerOrg),
            ("branch", branchId),
            ("now", now)));
        Assert.Equal("23514", missingPartyEx.SqlState);

        // Money identity violation
        var moneyEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_orders (
                id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                customer_party_type, customer_display_name_snapshot, customer_platform_user_id,
                merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                created_at_utc, updated_at_utc)
            VALUES (
                @oid, @seller, 'CO-MONEY', 'Draft', 'Pending', 'Unpaid',
                'Pickup', @branch, 'Main',
                'Personal', 'Buyer', @user,
                100.00, 20.00, 999.00, 'None',
                @now, @now);
            """,
            ("oid", Guid.NewGuid()),
            ("seller", sellerOrg),
            ("branch", branchId),
            ("user", userId),
            ("now", now)));
        Assert.Equal("23514", moneyEx.SqlState);

        // Destination lat/long pair violation
        var destLatLongEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_orders (
                id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                customer_party_type, customer_display_name_snapshot, customer_platform_user_id,
                merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                delivery_destination_latitude,
                created_at_utc, updated_at_utc)
            VALUES (
                @oid, @seller, 'CO-LL1', 'Draft', 'Pending', 'Unpaid',
                'Delivery', @branch, 'Main',
                'Personal', 'Buyer', @user,
                10.00, 0, 10.00, 'None',
                14.5,
                @now, @now);
            """,
            ("oid", Guid.NewGuid()),
            ("seller", sellerOrg),
            ("branch", branchId),
            ("user", userId),
            ("now", now)));
        Assert.Equal("23514", destLatLongEx.SqlState);

        // Branch snapshot lat/long pair violation
        var branchLatLongEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_orders (
                id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                customer_party_type, customer_display_name_snapshot, customer_platform_user_id,
                merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                delivery_branch_longitude_snapshot,
                created_at_utc, updated_at_utc)
            VALUES (
                @oid, @seller, 'CO-LL2', 'Draft', 'Pending', 'Unpaid',
                'Delivery', @branch, 'Main',
                'Personal', 'Buyer', @user,
                10.00, 0, 10.00, 'None',
                120.9,
                @now, @now);
            """,
            ("oid", Guid.NewGuid()),
            ("seller", sellerOrg),
            ("branch", branchId),
            ("user", userId),
            ("now", now)));
        Assert.Equal("23514", branchLatLongEx.SqlState);

        // Line order-org mismatch (order exists under seller, line claims other org)
        var orderOrgMismatchEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_order_lines (
                id, order_id, seller_organization_id, product_id, line_number,
                name_snapshot, unit_snapshot, quantity, unit_price, discount, line_total)
            VALUES (
                @lid, @oid, @other, @pid, 2,
                'Mismatch', 'Piece', 1, 10.00, 0, 10.00);
            """,
            ("lid", Guid.NewGuid()),
            ("oid", orderId),
            ("other", otherOrg),
            ("pid", productOther)));
        Assert.Equal("23503", orderOrgMismatchEx.SqlState);

        // Line product-org mismatch (product belongs to other org)
        var productOrgMismatchEx = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_order_lines (
                id, order_id, seller_organization_id, product_id, line_number,
                name_snapshot, unit_snapshot, quantity, unit_price, discount, line_total)
            VALUES (
                @lid, @oid, @seller, @pid, 3,
                'Wrong Product Org', 'Piece', 1, 10.00, 0, 10.00);
            """,
            ("lid", Guid.NewGuid()),
            ("oid", orderId),
            ("seller", sellerOrg),
            ("pid", productOther)));
        Assert.Equal("23503", productOrgMismatchEx.SqlState);
    }

    [Fact]
    public async Task Customer_orders_buyer_indexes_exist_for_my_orders_schema_verification()
    {
        await EnsureLatestAsync();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'pos'
              AND tablename = 'customer_orders'
              AND indexname IN (
                'ix_customer_orders_customer_user_created_at',
                'ix_customer_orders_customer_buyer_org_created_at')
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

        Assert.Equal(2, indexes.Count);
        Assert.Contains("ix_customer_orders_customer_user_created_at", indexes);
        Assert.Contains("ix_customer_orders_customer_buyer_org_created_at", indexes);

        // Schema-level EXPLAIN wording only — confirms planner can see the relation; not a latency claim.
        await using var explain = new NpgsqlCommand(
            """
            EXPLAIN
            SELECT id
            FROM pos.customer_orders
            WHERE customer_platform_user_id = @uid
            ORDER BY created_at_utc DESC
            LIMIT 20;
            """,
            connection);
        explain.Parameters.AddWithValue("uid", Guid.NewGuid());
        var plan = (string?)await explain.ExecuteScalarAsync();
        Assert.False(string.IsNullOrWhiteSpace(plan));
    }

    private async Task EnsureLatestAsync()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using var context = new PosDbContext(options);
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
