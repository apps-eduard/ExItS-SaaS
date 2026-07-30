using System.Globalization;
using Npgsql;

namespace ExItS.BackupRestore;

public sealed record RestoreValidationResult(
    bool Passed,
    IReadOnlyList<string> Findings);

/// <summary>Post-restore structural validation — mismatches fail; never silently repair.</summary>
public static class RestoreValidator
{
    public static async Task<RestoreValidationResult> ValidatePlatformAsync(
        string connectionString,
        IReadOnlyDictionary<string, long>? expectedMinCounts = null,
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

        return new RestoreValidationResult(findings.Count == 0, findings);
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

    private static string QuoteIdent(string ident) => "\"" + ident.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
