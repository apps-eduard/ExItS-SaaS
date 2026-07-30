using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>
/// Local schema migrations. v1 foundation metadata; v2 generic encrypted outbox.
/// </summary>
public sealed class LocalDatabaseMigrator(TimeProvider? timeProvider = null) : ILocalDatabaseMigrator
{
    public const int FoundationSchemaVersion = 1;
    public const int QueueSchemaVersion = 2;

    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    public int CurrentSchemaVersion => QueueSchemaVersion;

    public async Task<LocalMigrationResult> MigrateAsync(
        ILocalDatabaseConnection connection,
        LocalContextIdentity identity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(identity);
        ct.ThrowIfCancellationRequested();

        try
        {
            await connection.ExecuteAsync("PRAGMA journal_mode=WAL;", ct).ConfigureAwait(false);

            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS local_schema_info (
                    schema_version INTEGER NOT NULL PRIMARY KEY,
                    applied_at_utc TEXT NOT NULL
                );
                """,
                ct).ConfigureAwait(false);

            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS local_context_info (
                    context_hash TEXT NOT NULL PRIMARY KEY,
                    user_id TEXT NOT NULL,
                    organization_id TEXT NOT NULL,
                    product_code TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL,
                    last_opened_at_utc TEXT NOT NULL
                );
                """,
                ct).ConfigureAwait(false);

            var current = await connection
                .QueryScalarAsync<long>("SELECT COALESCE(MAX(schema_version), 0) FROM local_schema_info;", ct)
                .ConfigureAwait(false);

            var now = _clock.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

            if (current < FoundationSchemaVersion)
            {
                await connection.ExecuteAsync(
                    $"""
                    INSERT INTO local_schema_info (schema_version, applied_at_utc)
                    VALUES ({FoundationSchemaVersion}, '{now}');
                    """,
                    ct).ConfigureAwait(false);
                current = FoundationSchemaVersion;
            }

            if (current < QueueSchemaVersion)
            {
                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS offline_operations (
                        operation_id TEXT NOT NULL PRIMARY KEY,
                        device_id TEXT NOT NULL,
                        user_id TEXT NOT NULL,
                        organization_id TEXT NOT NULL,
                        product_code TEXT NOT NULL,
                        operation_type TEXT NOT NULL,
                        payload_version INTEGER NOT NULL,
                        ciphertext BLOB NOT NULL,
                        nonce BLOB NOT NULL,
                        tag BLOB NOT NULL,
                        payload_hash TEXT NOT NULL,
                        idempotency_key TEXT NOT NULL,
                        created_utc TEXT NOT NULL,
                        next_attempt_utc TEXT NOT NULL,
                        attempt_count INTEGER NOT NULL DEFAULT 0,
                        queue_state TEXT NOT NULL,
                        last_attempt_utc TEXT NULL,
                        failure_code TEXT NULL,
                        failure_summary TEXT NULL,
                        server_reference TEXT NULL,
                        concurrency_token TEXT NULL,
                        claimed_by TEXT NULL,
                        claimed_utc TEXT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE INDEX IF NOT EXISTS ix_offline_ops_fifo
                    ON offline_operations (queue_state, created_utc, operation_id);
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE INDEX IF NOT EXISTS ix_offline_ops_next
                    ON offline_operations (queue_state, next_attempt_utc);
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_sync_meta (
                        key TEXT NOT NULL PRIMARY KEY,
                        value TEXT NOT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    $"""
                    INSERT INTO local_schema_info (schema_version, applied_at_utc)
                    VALUES ({QueueSchemaVersion}, '{now}');
                    """,
                    ct).ConfigureAwait(false);
                current = QueueSchemaVersion;
            }

            var existing = await connection
                .QueryScalarAsync<long>(
                    $"""
                    SELECT COUNT(1) FROM local_context_info
                    WHERE context_hash = '{Escape(identity.ContextHash)}';
                    """,
                    ct)
                .ConfigureAwait(false);

            if (existing == 0)
            {
                await connection.ExecuteAsync(
                    $"""
                    INSERT INTO local_context_info (
                        context_hash, user_id, organization_id, product_code, created_at_utc, last_opened_at_utc)
                    VALUES (
                        '{Escape(identity.ContextHash)}',
                        '{Escape(identity.UserId.ToString("D"))}',
                        '{Escape(identity.OrganizationId.ToString("D"))}',
                        '{Escape(identity.ProductCode)}',
                        '{now}',
                        '{now}');
                    """,
                    ct).ConfigureAwait(false);
            }
            else
            {
                await connection.ExecuteAsync(
                    $"""
                    UPDATE local_context_info
                    SET last_opened_at_utc = '{now}'
                    WHERE context_hash = '{Escape(identity.ContextHash)}';
                    """,
                    ct).ConfigureAwait(false);
            }

            var forbidden = await connection.QueryRowsAsync(
                """
                SELECT name FROM sqlite_master
                WHERE type = 'table'
                  AND name IN (
                    'customers', 'credit_entries', 'repayments', 'ledger', 'due_dates',
                    'statements', 'receipts', 'sync_queue', 'conflicts',
                    'entitlement_cache', 'sync_checkpoints');
                """,
                ct).ConfigureAwait(false);

            if (forbidden.Count > 0)
            {
                return new LocalMigrationResult(false, (int)current, "forbidden_tables_present");
            }

            return new LocalMigrationResult(true, QueueSchemaVersion);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new LocalMigrationResult(false, 0, "migration_failed");
        }
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
