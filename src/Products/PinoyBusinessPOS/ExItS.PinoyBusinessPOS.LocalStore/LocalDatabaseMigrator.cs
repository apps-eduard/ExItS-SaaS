using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>
/// Local schema migrations. v1 foundation metadata; v2 generic encrypted outbox; v3 encrypted business cache;
/// v4 payment projections; v5 selling-catalog cache + local cash-sale outbox support.
/// </summary>
public sealed class LocalDatabaseMigrator(TimeProvider? timeProvider = null) : ILocalDatabaseMigrator
{
    public const int FoundationSchemaVersion = 1;
    public const int QueueSchemaVersion = 2;
    public const int BusinessCacheSchemaVersion = 3;
    public const int PaymentCacheSchemaVersion = 4;
    public const int CatalogSaleSchemaVersion = 5;

    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    public int CurrentSchemaVersion => CatalogSaleSchemaVersion;

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

            if (current < BusinessCacheSchemaVersion)
            {
                await connection.ExecuteAsync(
                    "ALTER TABLE offline_operations ADD COLUMN depends_on_operation_id TEXT NULL;",
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    "ALTER TABLE offline_operations ADD COLUMN entity_id TEXT NULL;",
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_customer_projection (
                        customer_id TEXT NOT NULL PRIMARY KEY,
                        organization_id TEXT NOT NULL,
                        status TEXT NOT NULL,
                        entity_state TEXT NOT NULL,
                        concurrency_token TEXT NULL,
                        pending_operation_id TEXT NULL,
                        created_utc TEXT NOT NULL,
                        updated_utc TEXT NOT NULL,
                        ciphertext BLOB NOT NULL,
                        nonce BLOB NOT NULL,
                        tag BLOB NOT NULL,
                        conflict_server_json TEXT NULL,
                        safe_failure_code TEXT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_credit_projection (
                        credit_entry_id TEXT NOT NULL PRIMARY KEY,
                        customer_id TEXT NOT NULL,
                        organization_id TEXT NOT NULL,
                        entity_state TEXT NOT NULL,
                        pending_operation_id TEXT NULL,
                        depends_on_operation_id TEXT NULL,
                        created_utc TEXT NOT NULL,
                        ciphertext BLOB NOT NULL,
                        nonce BLOB NOT NULL,
                        tag BLOB NOT NULL,
                        safe_failure_code TEXT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_customer_balance (
                        customer_id TEXT NOT NULL PRIMARY KEY,
                        confirmed_ciphertext BLOB NULL,
                        confirmed_nonce BLOB NULL,
                        confirmed_tag BLOB NULL,
                        pending_ciphertext BLOB NULL,
                        pending_nonce BLOB NULL,
                        pending_tag BLOB NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_download_checkpoint (
                        stream TEXT NOT NULL PRIMARY KEY,
                        checkpoint_utc TEXT NOT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    $"""
                    INSERT INTO local_schema_info (schema_version, applied_at_utc)
                    VALUES ({BusinessCacheSchemaVersion}, '{now}');
                    """,
                    ct).ConfigureAwait(false);
                current = BusinessCacheSchemaVersion;
            }

            if (current < PaymentCacheSchemaVersion)
            {
                await connection.ExecuteAsync(
                    """
                    ALTER TABLE local_customer_balance ADD COLUMN pending_repay_ciphertext BLOB NULL;
                    """,
                    ct).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    """
                    ALTER TABLE local_customer_balance ADD COLUMN pending_repay_nonce BLOB NULL;
                    """,
                    ct).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    """
                    ALTER TABLE local_customer_balance ADD COLUMN pending_repay_tag BLOB NULL;
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_repayment_projection (
                        repayment_id TEXT NOT NULL PRIMARY KEY,
                        customer_id TEXT NOT NULL,
                        organization_id TEXT NOT NULL,
                        entity_state TEXT NOT NULL,
                        pending_operation_id TEXT NULL,
                        depends_on_operation_id TEXT NULL,
                        recorded_utc TEXT NOT NULL,
                        ciphertext BLOB NOT NULL,
                        nonce BLOB NOT NULL,
                        tag BLOB NOT NULL,
                        safe_failure_code TEXT NULL,
                        pending_reversal_reason TEXT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE INDEX IF NOT EXISTS ix_local_repay_customer
                    ON local_repayment_projection(customer_id);
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    ALTER TABLE local_credit_projection ADD COLUMN current_due_date TEXT NULL;
                    """,
                    ct).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    """
                    ALTER TABLE local_credit_projection ADD COLUMN pending_due_date TEXT NULL;
                    """,
                    ct).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    """
                    ALTER TABLE local_credit_projection ADD COLUMN pending_due_date_reason TEXT NULL;
                    """,
                    ct).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    """
                    ALTER TABLE local_credit_projection ADD COLUMN pending_due_date_clear INTEGER NOT NULL DEFAULT 0;
                    """,
                    ct).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    """
                    ALTER TABLE local_credit_projection ADD COLUMN conflict_server_json TEXT NULL;
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    $"""
                    INSERT INTO local_schema_info (schema_version, applied_at_utc)
                    VALUES ({PaymentCacheSchemaVersion}, '{now}');
                    """,
                    ct).ConfigureAwait(false);
                current = PaymentCacheSchemaVersion;
            }

            if (current < CatalogSaleSchemaVersion)
            {
                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_catalog_category (
                        category_id TEXT NOT NULL PRIMARY KEY,
                        organization_id TEXT NOT NULL,
                        name TEXT NOT NULL,
                        status TEXT NOT NULL,
                        updated_utc TEXT NOT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_catalog_product (
                        product_id TEXT NOT NULL PRIMARY KEY,
                        organization_id TEXT NOT NULL,
                        name TEXT NOT NULL,
                        description TEXT NULL,
                        sku TEXT NULL,
                        barcode TEXT NULL,
                        category_id TEXT NULL,
                        unit_of_measure TEXT NOT NULL,
                        selling_price TEXT NOT NULL,
                        status TEXT NOT NULL,
                        is_tracked INTEGER NOT NULL DEFAULT 0,
                        on_hand_quantity TEXT NOT NULL DEFAULT '0',
                        stock_status TEXT NOT NULL DEFAULT 'InStock',
                        updated_utc TEXT NOT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE INDEX IF NOT EXISTS ix_local_catalog_product_name
                    ON local_catalog_product(name);
                    """,
                    ct).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    """
                    CREATE INDEX IF NOT EXISTS ix_local_catalog_product_sku
                    ON local_catalog_product(sku);
                    """,
                    ct).ConfigureAwait(false);
                await connection.ExecuteAsync(
                    """
                    CREATE INDEX IF NOT EXISTS ix_local_catalog_product_barcode
                    ON local_catalog_product(barcode);
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_open_shift_snapshot (
                        organization_id TEXT NOT NULL PRIMARY KEY,
                        shift_json TEXT NOT NULL,
                        captured_utc TEXT NOT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE TABLE IF NOT EXISTS local_cash_sale (
                        sale_id TEXT NOT NULL PRIMARY KEY,
                        organization_id TEXT NOT NULL,
                        sale_number TEXT NOT NULL,
                        shift_id TEXT NULL,
                        payment_method TEXT NOT NULL,
                        subtotal TEXT NOT NULL,
                        total TEXT NOT NULL,
                        amount_tendered TEXT NULL,
                        change_amount TEXT NULL,
                        recorded_at_utc TEXT NOT NULL,
                        recorded_by TEXT NOT NULL,
                        entity_state INTEGER NOT NULL,
                        pending_operation_id TEXT NULL,
                        idempotency_key TEXT NOT NULL,
                        server_reference TEXT NULL,
                        receipt_json TEXT NOT NULL,
                        safe_failure_code TEXT NULL,
                        created_utc TEXT NOT NULL,
                        updated_utc TEXT NOT NULL
                    );
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    """
                    CREATE UNIQUE INDEX IF NOT EXISTS ux_local_cash_sale_idempotency
                    ON local_cash_sale(idempotency_key);
                    """,
                    ct).ConfigureAwait(false);

                await connection.ExecuteAsync(
                    $"""
                    INSERT INTO local_schema_info (schema_version, applied_at_utc)
                    VALUES ({CatalogSaleSchemaVersion}, '{now}');
                    """,
                    ct).ConfigureAwait(false);
                current = CatalogSaleSchemaVersion;
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

            return new LocalMigrationResult(true, CatalogSaleSchemaVersion);
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
