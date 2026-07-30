using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>
/// Foundation schema only: local_schema_info + local_context_info.
/// No business, queue, entitlement, or DeviceId tables.
/// </summary>
public sealed class LocalDatabaseMigrator(TimeProvider? timeProvider = null) : ILocalDatabaseMigrator
{
    public const int FoundationSchemaVersion = 1;

    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    public int CurrentSchemaVersion => FoundationSchemaVersion;

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

            // Guard: foundation schema must not contain deferred business/queue tables.
            var forbidden = await connection.QueryRowsAsync(
                """
                SELECT name FROM sqlite_master
                WHERE type = 'table'
                  AND name IN (
                    'customers', 'credit_entries', 'repayments', 'ledger', 'due_dates',
                    'statements', 'receipts', 'operation_queue', 'sync_queue', 'conflicts',
                    'entitlement_cache', 'sync_checkpoints');
                """,
                ct).ConfigureAwait(false);

            if (forbidden.Count > 0)
            {
                return new LocalMigrationResult(false, (int)current, "forbidden_tables_present");
            }

            return new LocalMigrationResult(true, FoundationSchemaVersion);
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
