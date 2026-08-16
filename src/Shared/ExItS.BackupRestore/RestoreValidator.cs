using System.Globalization;
using Npgsql;

namespace ExItS.BackupRestore;

public sealed record RestoreValidationResult(
    bool Passed,
    IReadOnlyList<string> Findings);

/// <summary>
/// Critical row fingerprint for post-restore identity checks (IDs + key column values).
/// </summary>
public sealed record CriticalRecordFingerprint(
    string Schema,
    string Table,
    Guid Id,
    IReadOnlyDictionary<string, object?> KeyValues,
    string IdColumn = "id");

/// <summary>Post-restore structural validation — mismatches fail; never silently repair.</summary>
public static class RestoreValidator
{
    public static readonly string[] PlatformPhase29Tables =
    [
        "organization_branches",
        "branch_delivery_policies"
    ];

    public static readonly string[] PosPhase29Tables =
    [
        "customer_orders",
        "customer_order_lines",
        "payment_attempts"
    ];

    public static readonly string[] PlatformPhase29ConstraintNames =
    [
        "ck_branch_delivery_policies_free_threshold_nonneg",
        "ck_branch_delivery_policies_min_order_nonneg",
        "ck_organization_branches_lat_long_pair"
    ];

    public static readonly string[] PosPhase29ConstraintNames =
    [
        "ck_sales_stock_reservation",
        "ck_customer_orders_money_identity",
        "ck_customer_orders_party_xor",
        "ck_customer_orders_totals_non_negative",
        "ck_inventory_accounts_reserved_non_negative",
        "ck_inventory_accounts_reserved_not_over_on_hand"
    ];

    public static async Task<RestoreValidationResult> ValidatePlatformAsync(
        string connectionString,
        IReadOnlyDictionary<string, long>? expectedMinCounts = null,
        bool requirePhase29Tables = false,
        bool checkPhase29ConstraintsBestEffort = false,
        CancellationToken ct = default)
    {
        var findings = new List<string>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await EnsureNoHealthCareTablesAsync(connection, findings, ct).ConfigureAwait(false);
        await EnsureSchemaExistsAsync(connection, "platform", findings, ct).ConfigureAwait(false);
        await EnsureMigrationHistoryAsync(connection, "platform", findings, ct).ConfigureAwait(false);

        var required = new[]
        {
            "organizations", "platform_users", "organization_memberships", "products",
            "subscriptions", "saas_payments", "entitlement_snapshots", "audit_records"
        };
        await EnsureTablesExistAsync(connection, required, findings, ct, schema: "platform").ConfigureAwait(false);

        if (requirePhase29Tables)
        {
            await EnsureTablesExistAsync(connection, PlatformPhase29Tables, findings, ct, schema: "platform")
                .ConfigureAwait(false);
        }

        if (checkPhase29ConstraintsBestEffort)
        {
            await EnsureNamedObjectsBestEffortAsync(
                    connection,
                    "platform",
                    PlatformPhase29ConstraintNames,
                    findings,
                    require: false,
                    ct)
                .ConfigureAwait(false);
        }

        if (expectedMinCounts is not null)
        {
            foreach (var (table, min) in expectedMinCounts)
            {
                var count = await CountRowsAsync(connection, table, ct, schema: "platform").ConfigureAwait(false);
                if (count < min)
                {
                    findings.Add($"platform.{table} row count {count} < expected minimum {min}.");
                }
            }
        }

        return new RestoreValidationResult(findings.Count == 0, findings);
    }

    public static async Task<RestoreValidationResult> ValidatePosAsync(
        string connectionString,
        IReadOnlyDictionary<string, long>? expectedMinCounts = null,
        bool requirePhase29Tables = false,
        bool checkPhase29ConstraintsBestEffort = false,
        bool validateInventoryReservations = true,
        CancellationToken ct = default)
    {
        var findings = new List<string>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await EnsureNoHealthCareTablesAsync(connection, findings, ct).ConfigureAwait(false);
        await EnsureSchemaExistsAsync(connection, "pos", findings, ct).ConfigureAwait(false);
        await EnsureMigrationHistoryAsync(connection, "pos", findings, ct).ConfigureAwait(false);

        var required = new[]
        {
            "customers", "credit_entries", "repayments", "products", "product_categories",
            "sales", "sale_lines", "inventory_accounts", "stock_movements", "expenses",
            "idempotency_records"
        };
        await EnsureTablesExistAsync(connection, required, findings, ct, schema: "pos").ConfigureAwait(false);

        if (requirePhase29Tables)
        {
            await EnsureTablesExistAsync(connection, PosPhase29Tables, findings, ct, schema: "pos")
                .ConfigureAwait(false);
            await EnsureColumnExistsAsync(
                    connection,
                    "pos",
                    "sales",
                    "stock_reservation_state",
                    findings,
                    require: true,
                    ct)
                .ConfigureAwait(false);
        }

        if (checkPhase29ConstraintsBestEffort)
        {
            await EnsureNamedObjectsBestEffortAsync(
                    connection,
                    "pos",
                    PosPhase29ConstraintNames,
                    findings,
                    require: false,
                    ct)
                .ConfigureAwait(false);
        }

        if (expectedMinCounts is not null)
        {
            foreach (var (table, min) in expectedMinCounts)
            {
                var count = await CountRowsAsync(connection, table, ct, schema: "pos").ConfigureAwait(false);
                if (count < min)
                {
                    findings.Add($"pos.{table} row count {count} < expected minimum {min}.");
                }
            }
        }

        // Basic invariant: no negative tracked inventory on-hand.
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                """
                SELECT COUNT(*)::int
                FROM pos.inventory_accounts
                WHERE is_tracked = TRUE AND on_hand_quantity < 0;
                """;
            try
            {
                var negatives = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
                if (negatives > 0)
                {
                    findings.Add($"Found {negatives} tracked inventory accounts with negative on-hand.");
                }
            }
            catch (PostgresException)
            {
                findings.Add("Unable to evaluate inventory on-hand invariant (schema mismatch).");
            }
        }

        if (validateInventoryReservations)
        {
            await ValidateInventoryReservationInvariantsAsync(connection, findings, ct).ConfigureAwait(false);
        }

        return new RestoreValidationResult(findings.Count == 0, findings);
    }

    /// <summary>
    /// Best-effort: report missing named constraints/indexes without failing when absent (older dumps).
    /// When <paramref name="require"/> is true, missing names become findings.
    /// </summary>
    public static async Task<RestoreValidationResult> EnsureNamedConstraintsBestEffortAsync(
        string connectionString,
        string schema,
        IEnumerable<string> constraintOrIndexNames,
        bool require = false,
        CancellationToken ct = default)
    {
        var findings = new List<string>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await EnsureNamedObjectsBestEffortAsync(connection, schema, constraintOrIndexNames, findings, require, ct)
            .ConfigureAwait(false);
        return new RestoreValidationResult(findings.Count == 0, findings);
    }

    /// <summary>
    /// Inventory reservation invariants: reserved &gt;= 0; reserved &lt;= on_hand for tracked accounts.
    /// </summary>
    public static async Task<RestoreValidationResult> ValidateInventoryReservationsAsync(
        string connectionString,
        CancellationToken ct = default)
    {
        var findings = new List<string>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await ValidateInventoryReservationInvariantsAsync(connection, findings, ct).ConfigureAwait(false);
        return new RestoreValidationResult(findings.Count == 0, findings);
    }

    /// <summary>
    /// Compare expected critical record fingerprints (IDs + key values) after restore.
    /// </summary>
    public static async Task<RestoreValidationResult> CompareCriticalFingerprintsAsync(
        string connectionString,
        IReadOnlyList<CriticalRecordFingerprint> expected,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        var findings = new List<string>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        foreach (var fp in expected)
        {
            if (fp.KeyValues.Count == 0)
            {
                findings.Add($"{fp.Schema}.{fp.Table} fingerprint {fp.Id} has no key values.");
                continue;
            }

            var selectCols = string.Join(", ", fp.KeyValues.Keys.Select(QuoteIdent));
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                $"""
                 SELECT {selectCols}
                 FROM {QuoteIdent(fp.Schema)}.{QuoteIdent(fp.Table)}
                 WHERE {QuoteIdent(fp.IdColumn)} = @id;
                 """;
            cmd.Parameters.AddWithValue("id", fp.Id);

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    findings.Add($"Missing fingerprint row {fp.Schema}.{fp.Table}.{fp.IdColumn}={fp.Id}.");
                    continue;
                }

                var ordinal = 0;
                foreach (var (column, expectedValue) in fp.KeyValues)
                {
                    var actual = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
                    if (!FingerprintValuesEqual(expectedValue, actual))
                    {
                        findings.Add(
                            $"{fp.Schema}.{fp.Table} id={fp.Id} column '{column}' expected '{FormatValue(expectedValue)}' but was '{FormatValue(actual)}'.");
                    }

                    ordinal++;
                }
            }
            catch (PostgresException ex)
            {
                findings.Add($"Unable to read fingerprint {fp.Schema}.{fp.Table} id={fp.Id}: {ex.SqlState}");
            }
        }

        return new RestoreValidationResult(findings.Count == 0, findings);
    }

    private static async Task ValidateInventoryReservationInvariantsAsync(
        NpgsqlConnection connection,
        List<string> findings,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(*)::int
            FROM pos.inventory_accounts
            WHERE is_tracked = TRUE
              AND (reserved_quantity < 0 OR reserved_quantity > on_hand_quantity);
            """;
        try
        {
            var bad = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
            if (bad > 0)
            {
                findings.Add(
                    $"Found {bad} tracked inventory accounts with reserved_quantity < 0 or reserved > on_hand.");
            }
        }
        catch (PostgresException)
        {
            // Older schemas may lack reserved_quantity — skip rather than fail hard.
        }
    }

    private static async Task EnsureNamedObjectsBestEffortAsync(
        NpgsqlConnection connection,
        string schema,
        IEnumerable<string> names,
        List<string> findings,
        bool require,
        CancellationToken ct)
    {
        foreach (var name in names)
        {
            var exists = await NamedConstraintOrIndexExistsAsync(connection, schema, name, ct).ConfigureAwait(false);
            if (!exists)
            {
                if (require)
                {
                    findings.Add($"Required constraint/index missing: {schema}.{name}");
                }
                // best-effort: silence when not required (older dumps)
            }
        }
    }

    private static async Task<bool> NamedConstraintOrIndexExistsAsync(
        NpgsqlConnection connection,
        string schema,
        string name,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT EXISTS (
              SELECT 1
              FROM pg_constraint c
              JOIN pg_namespace n ON n.oid = c.connamespace
              WHERE n.nspname = @schema AND c.conname = @name
            ) OR EXISTS (
              SELECT 1
              FROM pg_indexes
              WHERE schemaname = @schema AND indexname = @name
            );
            """;
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("name", name);
        return Convert.ToBoolean(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
    }

    private static async Task EnsureColumnExistsAsync(
        NpgsqlConnection connection,
        string schema,
        string table,
        string column,
        List<string> findings,
        bool require,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = @schema AND table_name = @table AND column_name = @column;
            """;
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        cmd.Parameters.AddWithValue("column", column);
        var exists = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (exists is null && require)
        {
            findings.Add($"Required column missing: {schema}.{table}.{column}");
        }
    }

    private static async Task EnsureNoHealthCareTablesAsync(
        NpgsqlConnection connection,
        List<string> findings,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT table_schema || '.' || table_name
            FROM information_schema.tables
            WHERE table_type = 'BASE TABLE'
              AND (
                    table_name ILIKE '%patient%'
                 OR table_name ILIKE '%healthcare%'
                 OR table_schema ILIKE '%healthcare%'
              );
            """;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            findings.Add($"Forbidden HealthCare-related table present: {reader.GetString(0)}");
        }
    }

    private static async Task EnsureSchemaExistsAsync(
        NpgsqlConnection connection,
        string schema,
        List<string> findings,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema;";
        cmd.Parameters.AddWithValue("schema", schema);
        var exists = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (exists is null)
        {
            findings.Add($"Required schema '{schema}' is missing.");
        }
    }

    private static async Task EnsureMigrationHistoryAsync(
        NpgsqlConnection connection,
        string preferredSchema,
        List<string> findings,
        CancellationToken ct)
    {
        // EF Core may place history in the product schema or public depending on configuration/history.
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COALESCE(
              (SELECT COUNT(*)::int FROM information_schema.tables
               WHERE table_schema = @schema AND table_name = '__EFMigrationsHistory'),
              0);
            """;
        cmd.Parameters.AddWithValue("schema", preferredSchema);
        var inSchema = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);

        cmd.Parameters.Clear();
        cmd.CommandText =
            """
            SELECT COALESCE(
              (SELECT COUNT(*)::int FROM information_schema.tables
               WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'),
              0);
            """;
        var inPublic = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);

        if (inSchema == 0 && inPublic == 0)
        {
            findings.Add($"__EFMigrationsHistory missing from '{preferredSchema}' and 'public'.");
            return;
        }

        var historySchema = inSchema > 0 ? preferredSchema : "public";
        await using var countCmd = connection.CreateCommand();
        countCmd.CommandText =
            $"""
            SELECT COUNT(*)::int
            FROM "{historySchema.Replace("\"", "\"\"", StringComparison.Ordinal)}"."__EFMigrationsHistory";
            """;
        try
        {
            var historyCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
            if (historyCount == 0)
            {
                findings.Add($"{historySchema}.__EFMigrationsHistory has zero migration rows.");
            }
        }
        catch (PostgresException ex)
        {
            findings.Add($"Unable to read {historySchema}.__EFMigrationsHistory: {ex.SqlState}");
        }
    }

    private static async Task EnsureTablesExistAsync(
        NpgsqlConnection connection,
        IEnumerable<string> tables,
        List<string> findings,
        CancellationToken ct,
        string? schema = null)
    {
        foreach (var table in tables)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT 1
                FROM information_schema.tables
                WHERE table_type = 'BASE TABLE'
                  AND table_name = @table
                  AND (@schema IS NULL OR table_schema = @schema);
                """;
            cmd.Parameters.AddWithValue("table", table);
            cmd.Parameters.AddWithValue("schema", (object?)schema ?? DBNull.Value);
            var exists = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            if (exists is null)
            {
                findings.Add($"Required table missing: {(schema is null ? table : $"{schema}.{table}")}");
            }
        }
    }

    private static async Task<long> CountRowsAsync(
        NpgsqlConnection connection,
        string table,
        CancellationToken ct,
        string? schema = null)
    {
        var qualified = schema is null ? QuoteIdent(table) : $"{QuoteIdent(schema)}.{QuoteIdent(table)}";
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*)::bigint FROM {qualified};";
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
    }

    private static bool FingerprintValuesEqual(object? expected, object? actual)
    {
        if (expected is null && actual is null)
        {
            return true;
        }

        if (expected is null || actual is null)
        {
            return false;
        }

        if (expected is Guid eg && actual is Guid ag)
        {
            return eg == ag;
        }

        if (expected is decimal ed)
        {
            return Convert.ToDecimal(actual, CultureInfo.InvariantCulture) == ed;
        }

        if (expected is int ei)
        {
            return Convert.ToInt32(actual, CultureInfo.InvariantCulture) == ei;
        }

        if (expected is long el)
        {
            return Convert.ToInt64(actual, CultureInfo.InvariantCulture) == el;
        }

        if (expected is bool eb)
        {
            return Convert.ToBoolean(actual, CultureInfo.InvariantCulture) == eb;
        }

        return string.Equals(
            Convert.ToString(expected, CultureInfo.InvariantCulture),
            Convert.ToString(actual, CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    private static string FormatValue(object? value) =>
        value is null ? "<null>" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>";

    private static string QuoteIdent(string ident) => "\"" + ident.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
