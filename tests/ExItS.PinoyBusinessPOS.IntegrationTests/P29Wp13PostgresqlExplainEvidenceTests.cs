using System.Text;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// P29-WP13: capture PostgreSQL EXPLAIN (ANALYZE, BUFFERS) for application-realistic queries
/// against seeded volume. Seq Scan on small/medium sets is acceptable — do not fail solely for it.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class P29Wp13PostgresqlExplainEvidenceTests(PosPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Explain_analyze_representative_queries_and_phase29_indexes()
    {
        await EnsureLatestAsync();

        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var org3 = Guid.NewGuid();
        var orgs = new[] { org1, org2, org3 };
        var actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var buyerUser = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await SeedBulkAsync(connection, orgs, actor, buyerUser, now);

        var plans = new List<(string Label, string Plan)>
        {
            ("sales_history", await ExplainAsync(
                connection,
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT * FROM pos.sales
                WHERE organization_id = @org
                ORDER BY recorded_at_utc DESC
                LIMIT 50
                """,
                ("org", org1))),
            ("payment_attempt_idempotency", await ExplainAsync(
                connection,
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT * FROM pos.payment_attempts
                WHERE organization_id = @org AND idempotency_key = @key
                """,
                ("org", org1),
                ("key", $"idem-{org1:N}-0"))),
            ("payment_attempt_provider_reference", await ExplainAsync(
                connection,
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT * FROM pos.payment_attempts
                WHERE provider = 'Fake' AND provider_reference = @pref
                """,
                ("pref", $"fake-ref-{org1:N}-0"))),
            ("active_attempts_for_sale", await ExplainAsync(
                connection,
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT * FROM pos.payment_attempts
                WHERE organization_id = @org
                  AND sale_id = @sale
                  AND status IN ('Created', 'Pending', 'RequiresCustomerAction', 'Processing', 'PendingManualVerification')
                """,
                ("org", org1),
                ("sale", await FirstSaleIdAsync(connection, org1)))),
            ("inventory_account_org_product", await ExplainAsync(
                connection,
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT * FROM pos.inventory_accounts
                WHERE organization_id = @org AND product_id = @product
                """,
                ("org", org1),
                ("product", await FirstProductIdAsync(connection, org1)))),
            ("customer_orders_buyer_history", await ExplainAsync(
                connection,
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT * FROM pos.customer_orders
                WHERE customer_platform_user_id = @uid
                ORDER BY created_at_utc DESC
                LIMIT 20
                """,
                ("uid", buyerUser))),
            ("dashboard_payment_method_aggregate", await ExplainAsync(
                connection,
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT payment_method, COUNT(*), SUM(total)
                FROM pos.sales
                WHERE organization_id = @org
                  AND status = 'Completed'
                  AND recorded_at_utc >= @from
                GROUP BY payment_method
                """,
                ("org", org1),
                ("from", now.AddDays(-30))))
        };

        foreach (var (label, plan) in plans)
        {
            Assert.False(string.IsNullOrWhiteSpace(plan), $"Plan empty for {label}");
            Assert.Contains("Planning Time", plan, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Execution Time", plan, StringComparison.OrdinalIgnoreCase);
        }

        await using (var idxCmd = connection.CreateCommand())
        {
            idxCmd.CommandText =
                """
                SELECT indexname
                FROM pg_indexes
                WHERE schemaname = 'pos'
                  AND indexname IN (
                    'ix_customer_orders_customer_user_created_at',
                    'ix_customer_orders_customer_buyer_org_created_at',
                    'ux_payment_attempts_org_idempotency',
                    'ux_payment_attempts_provider_reference',
                    'ix_payment_attempts_org_sale_status',
                    'ix_sales_org_recorded_at',
                    'ux_inventory_accounts_org_product')
                ORDER BY indexname
                """;
            var found = new List<string>();
            await using var reader = await idxCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                found.Add(reader.GetString(0));
            }

            Assert.Equal(7, found.Count);
            Assert.Contains("ux_payment_attempts_org_idempotency", found);
            Assert.Contains("ix_customer_orders_customer_user_created_at", found);
        }

        await WritePlanSnippetsAsync(plans);
    }

    private async Task EnsureLatestAsync()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using var context = new PosDbContext(options);
        await context.Database.MigrateAsync();
    }

    private static async Task SeedBulkAsync(
        NpgsqlConnection connection,
        Guid[] orgs,
        Guid actor,
        Guid buyerUser,
        DateTimeOffset now)
    {
        // ~2400 sales + ~1200 attempts + ~600 customer orders across 3 orgs.
        const int salesPerOrg = 800;
        const int ordersPerOrg = 200;

        foreach (var org in orgs)
        {
            var productId = Guid.NewGuid();
            await ExecAsync(
                connection,
                """
                INSERT INTO pos.products (
                    id, organization_id, name, unit_of_measure, selling_mode, selling_price, status,
                    catalog_source, created_at_utc, updated_at_utc)
                VALUES (
                    @pid, @org, 'Explain Seed Product', 'Piece', 'PerItem', 25.00, 'Active',
                    'Manual', @now, @now)
                """,
                ("pid", productId),
                ("org", org),
                ("now", now));

            await ExecAsync(
                connection,
                """
                INSERT INTO pos.inventory_accounts (
                    id, organization_id, product_id, is_tracked, on_hand_quantity, reserved_quantity,
                    created_at_utc, updated_at_utc)
                VALUES (
                    @id, @org, @pid, TRUE, 10000, 0, @now, @now)
                """,
                ("id", Guid.NewGuid()),
                ("org", org),
                ("pid", productId),
                ("now", now));

            var saleSql = new StringBuilder();
            saleSql.Append(
                """
                INSERT INTO pos.sales (
                    id, organization_id, sale_number, status, stock_reservation_state, payment_method,
                    subtotal, tax_amount, total, recorded_at_utc, recorded_by, updated_at_utc, buyer_party_kind,
                    amount_tendered, change_amount)
                VALUES
                """);
            for (var i = 0; i < salesPerOrg; i++)
            {
                if (i > 0)
                {
                    saleSql.Append(',');
                }

                var status = i % 7 == 0 ? "AwaitingPayment" : "Completed";
                var method = (i % 3) switch
                {
                    0 => "Card",
                    1 => "Cash",
                    _ => "GCash"
                };
                // Cash checkouts are always Completed in app — keep tender consistency valid.
                if (method == "Cash")
                {
                    status = "Completed";
                }

                var stockState = status == "AwaitingPayment" ? "Reserved" : "None";
                var amountTendered = method == "Cash" ? "50.00" : "NULL";
                var changeAmount = method == "Cash" ? "25.00" : "NULL";
                saleSql.Append(
                    $"""
                     ('{Guid.NewGuid():D}', '{org:D}', 'S-{org.ToString("N")[..8]}-{i:D4}', '{status}', '{stockState}', '{method}',
                      25.00, 0, 25.00, TIMESTAMPTZ '{now.AddMinutes(-i):O}', '{actor:D}', TIMESTAMPTZ '{now.AddMinutes(-i):O}', 'WalkIn',
                      {amountTendered}, {changeAmount})
                     """);
            }

            await ExecAsync(connection, saleSql.ToString());

            await using (var saleIdsCmd = connection.CreateCommand())
            {
                saleIdsCmd.CommandText =
                    """
                    SELECT id FROM pos.sales
                    WHERE organization_id = @org
                    ORDER BY recorded_at_utc DESC
                    LIMIT 400
                    """;
                saleIdsCmd.Parameters.AddWithValue("org", org);
                var saleIds = new List<Guid>();
                await using (var reader = await saleIdsCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        saleIds.Add(reader.GetGuid(0));
                    }
                }

                var attemptSql = new StringBuilder();
                attemptSql.Append(
                    """
                    INSERT INTO pos.payment_attempts (
                        id, organization_id, sale_id, method, provider, provider_reference,
                        amount, currency, status, idempotency_key, created_by,
                        created_at_utc, updated_at_utc, provider_event_sequence)
                    VALUES
                    """);
                for (var i = 0; i < saleIds.Count; i++)
                {
                    if (i > 0)
                    {
                        attemptSql.Append(',');
                    }

                    var status = (i % 5) switch
                    {
                        0 => "RequiresCustomerAction",
                        1 => "Paid",
                        2 => "Created",
                        3 => "Failed",
                        _ => "Cancelled"
                    };
                    var pref = status is "Created" ? "NULL" : $"'fake-ref-{org:N}-{i}'";
                    attemptSql.Append(
                        $"""
                         ('{Guid.NewGuid():D}', '{org:D}', '{saleIds[i]:D}', 'Card', 'Fake', {pref},
                          25.00, 'PHP', '{status}', 'idem-{org:N}-{i}', '{actor:D}',
                          TIMESTAMPTZ '{now.AddMinutes(-i):O}', TIMESTAMPTZ '{now.AddMinutes(-i):O}', {i})
                         """);
                }

                await ExecAsync(connection, attemptSql.ToString());
            }

            var orderSql = new StringBuilder();
            orderSql.Append(
                """
                INSERT INTO pos.customer_orders (
                    id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                    fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                    customer_party_type, customer_display_name_snapshot, customer_platform_user_id,
                    merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                    created_at_utc, updated_at_utc)
                VALUES
                """);
            var branchId = Guid.NewGuid();
            for (var i = 0; i < ordersPerOrg; i++)
            {
                if (i > 0)
                {
                    orderSql.Append(',');
                }

                orderSql.Append(
                    $"""
                     ('{Guid.NewGuid():D}', '{org:D}', 'CO-{org.ToString("N")[..6]}-{i:D4}', 'Submitted', 'Pending', 'Unpaid',
                      'Pickup', '{branchId:D}', 'Main',
                      'Personal', 'Buyer', '{buyerUser:D}',
                      100.00, 0, 100.00, 'None',
                      TIMESTAMPTZ '{now.AddMinutes(-i):O}', TIMESTAMPTZ '{now.AddMinutes(-i):O}')
                     """);
            }

            await ExecAsync(connection, orderSql.ToString());
        }
    }

    private static async Task<string> ExplainAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        var sb = new StringBuilder();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sb.AppendLine(reader.GetString(0));
        }

        return sb.ToString();
    }

    private static async Task<Guid> FirstSaleIdAsync(NpgsqlConnection connection, Guid org)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM pos.sales WHERE organization_id = @org LIMIT 1";
        cmd.Parameters.AddWithValue("org", org);
        return (Guid)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task<Guid> FirstProductIdAsync(NpgsqlConnection connection, Guid org)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM pos.products WHERE organization_id = @org LIMIT 1";
        cmd.Parameters.AddWithValue("org", org);
        return (Guid)(await cmd.ExecuteScalarAsync())!;
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

    private static async Task WritePlanSnippetsAsync(IReadOnlyList<(string Label, string Plan)> plans)
    {
        var reportsDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "reports"));
        if (!Directory.Exists(reportsDir))
        {
            return;
        }

        var path = Path.Combine(reportsDir, "P29-WP13-explain-plan-snippets.md");
        var sb = new StringBuilder();
        sb.AppendLine("# P29-WP13 — EXPLAIN plan snippets");
        sb.AppendLine();
        sb.AppendLine("Captured by `P29Wp13PostgresqlExplainEvidenceTests` (ANALYZE, BUFFERS).");
        sb.AppendLine("Seq Scan on small/medium sets is acceptable evidence, not a failure.");
        sb.AppendLine();
        foreach (var (label, plan) in plans)
        {
            sb.AppendLine($"## {label}");
            sb.AppendLine();
            sb.AppendLine("```");
            var lines = plan.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines.Take(12))
            {
                sb.AppendLine(line);
            }

            if (lines.Length > 12)
            {
                sb.AppendLine("...");
            }

            sb.AppendLine("```");
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(path, sb.ToString());
    }
}
