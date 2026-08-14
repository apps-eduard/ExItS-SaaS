using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.LocalStore;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class LocalDatabaseIsolationTests
{
    [Fact]
    public void Path_is_deterministic_and_excludes_raw_identifiers()
    {
        var root = new TempRoot();
        var resolver = new LocalDatabasePathResolver(root);
        var user = Guid.NewGuid();
        var org = Guid.NewGuid();

        var a = resolver.ResolveDatabaseFileName(user, org, PosProductCodes.PinoyBusinessPos);
        var b = resolver.ResolveDatabaseFileName(user, org, PosProductCodes.PinoyBusinessPos);

        Assert.Equal(a, b);
        Assert.StartsWith("pos-local-", a, StringComparison.Ordinal);
        Assert.EndsWith(".db", a, StringComparison.Ordinal);
        Assert.DoesNotContain(user.ToString("D"), a, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(user.ToString("N"), a, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(org.ToString("D"), a, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(org.ToString("N"), a, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(root.GetLocalStoreRootDirectory(), resolver.ResolveDatabasePath(user, org, PosProductCodes.PinoyBusinessPos), StringComparison.Ordinal);
    }

    [Fact]
    public void Distinct_database_per_user_org_and_product()
    {
        var resolver = new LocalDatabasePathResolver(new TempRoot());
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();

        var baseFile = resolver.ResolveDatabaseFileName(user1, org1, PosProductCodes.PinoyBusinessPos);
        Assert.NotEqual(baseFile, resolver.ResolveDatabaseFileName(user2, org1, PosProductCodes.PinoyBusinessPos));
        Assert.NotEqual(baseFile, resolver.ResolveDatabaseFileName(user1, org2, PosProductCodes.PinoyBusinessPos));
        Assert.NotEqual(baseFile, resolver.ResolveDatabaseFileName(user1, org1, "other-product"));
    }

    [Fact]
    public async Task Schema_initializes_foundation_tables_only()
    {
        var root = new TempRoot();
        await using var manager = new LocalContextManager(
            new LocalDatabasePathResolver(root),
            new LocalDatabaseFactory(),
            new LocalDatabaseMigrator());

        var user = Guid.NewGuid();
        var org = Guid.NewGuid();
        var open = await manager.OpenAsync(user, org, PosProductCodes.PinoyBusinessPos);
        Assert.True(open.Succeeded);
        Assert.Equal(LocalDatabaseMigrator.ConnectedSuppliersSchemaVersion, open.Context!.SchemaVersion);
        Assert.Equal(LocalContextInitStatus.Ready, open.Context.Status);

        var path = new LocalDatabasePathResolver(root).ResolveDatabasePath(user, org, PosProductCodes.PinoyBusinessPos);
        await using var connection = await new LocalDatabaseFactory().OpenAsync(path);
        var tables = await connection.QueryRowsAsync(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;");
        var names = tables.Select(r => Convert.ToString(r["name"])!).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("local_schema_info", names);
        Assert.Contains("local_context_info", names);
        Assert.Contains("offline_operations", names);
        Assert.Contains("local_sync_meta", names);
        Assert.Contains("local_customer_projection", names);
        Assert.Contains("local_credit_projection", names);
        Assert.Contains("local_repayment_projection", names);
        Assert.Contains("local_customer_balance", names);
        Assert.Contains("local_download_checkpoint", names);
        Assert.Contains("local_connected_supplier", names);
        Assert.Contains("local_linked_supplier_product", names);
        Assert.Contains("local_connected_supplier_sync_state", names);
        Assert.Contains("local_connected_po_draft", names);
        Assert.DoesNotContain("customers", names);
        Assert.DoesNotContain("credit_entries", names);
        Assert.DoesNotContain("repayments", names);
        Assert.DoesNotContain("ledger", names);
        Assert.DoesNotContain("sync_queue", names);
        Assert.DoesNotContain("entitlement_cache", names);
    }

    [Fact]
    public async Task Clean_init_reaches_current_schema()
    {
        var root = new TempRoot();
        await using var manager = new LocalContextManager(
            new LocalDatabasePathResolver(root),
            new LocalDatabaseFactory(),
            new LocalDatabaseMigrator());

        var open = await manager.OpenAsync(Guid.NewGuid(), Guid.NewGuid(), PosProductCodes.PinoyBusinessPos);
        Assert.True(open.Succeeded);
        Assert.Equal(LocalDatabaseMigrator.ConnectedSuppliersSchemaVersion, open.Context!.SchemaVersion);
        root.Dispose();
    }

    [Fact]
    public async Task Incremental_migration_chain_v1_to_v4_records_each_version()
    {
        var root = new TempRoot();
        var resolver = new LocalDatabasePathResolver(root);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var product = PosProductCodes.PinoyBusinessPos;
        var identity = new LocalContextIdentity(
            resolver.ComputeContextHash(userId, orgId, product), userId, orgId, product);
        var path = resolver.ResolveDatabasePath(userId, orgId, product);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await SeedSchemaVersionAsync(path, foundationOnly: true);

        await using var connection = await new LocalDatabaseFactory().OpenAsync(path);
        var migrator = new LocalDatabaseMigrator();
        var result = await migrator.MigrateAsync(connection, identity);
        Assert.True(result.Succeeded);
        Assert.Equal(LocalDatabaseMigrator.ConnectedSuppliersSchemaVersion, result.SchemaVersion);

        var versions = await connection.QueryRowsAsync(
            "SELECT schema_version FROM local_schema_info ORDER BY schema_version;");
        var versionNums = versions.Select(r => Convert.ToInt32(r["schema_version"])).ToArray();
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], versionNums);

        var tables = await connection.QueryRowsAsync(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;");
        var names = tables.Select(r => Convert.ToString(r["name"])!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("offline_operations", names); // v2
        Assert.Contains("local_customer_projection", names); // v3
        Assert.Contains("local_credit_projection", names); // v3
        Assert.Contains("local_customer_balance", names); // v3
        Assert.Contains("local_repayment_projection", names); // v4
        Assert.Contains("local_personal_contact", names); // v6
        Assert.Contains("local_linked_supplier_product", names); // v8
        Assert.DoesNotContain("customers", names);
        root.Dispose();
    }

    [Fact]
    public async Task Migration_from_seeded_v2_adds_v3_and_v4_tables()
    {
        var root = new TempRoot();
        var resolver = new LocalDatabasePathResolver(root);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var product = PosProductCodes.PinoyBusinessPos;
        var identity = new LocalContextIdentity(
            resolver.ComputeContextHash(userId, orgId, product), userId, orgId, product);
        var path = resolver.ResolveDatabasePath(userId, orgId, product);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await SeedSchemaVersionAsync(path, throughQueue: true);

        await using var connection = await new LocalDatabaseFactory().OpenAsync(path);
        var before = await connection.QueryRowsAsync(
            "SELECT name FROM sqlite_master WHERE type='table';");
        var beforeNames = before.Select(r => Convert.ToString(r["name"])!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("offline_operations", beforeNames);
        Assert.DoesNotContain("local_customer_projection", beforeNames);
        Assert.DoesNotContain("local_repayment_projection", beforeNames);

        var result = await new LocalDatabaseMigrator().MigrateAsync(connection, identity);
        Assert.True(result.Succeeded);
        Assert.Equal(LocalDatabaseMigrator.ConnectedSuppliersSchemaVersion, result.SchemaVersion);

        var after = await connection.QueryRowsAsync(
            "SELECT name FROM sqlite_master WHERE type='table';");
        var afterNames = after.Select(r => Convert.ToString(r["name"])!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("local_customer_projection", afterNames);
        Assert.Contains("local_credit_projection", afterNames);
        Assert.Contains("local_repayment_projection", afterNames);
        Assert.Contains("local_personal_contact", afterNames);
        root.Dispose();
    }

    [Fact]
    public async Task Migration_from_seeded_v3_adds_v4_payment_tables()
    {
        var root = new TempRoot();
        var resolver = new LocalDatabasePathResolver(root);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var product = PosProductCodes.PinoyBusinessPos;
        var identity = new LocalContextIdentity(
            resolver.ComputeContextHash(userId, orgId, product), userId, orgId, product);
        var path = resolver.ResolveDatabasePath(userId, orgId, product);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await SeedSchemaVersionAsync(path, throughBusinessCache: true);

        await using var connection = await new LocalDatabaseFactory().OpenAsync(path);
        var before = await connection.QueryRowsAsync(
            "SELECT name FROM sqlite_master WHERE type='table';");
        var beforeNames = before.Select(r => Convert.ToString(r["name"])!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("local_customer_projection", beforeNames);
        Assert.DoesNotContain("local_repayment_projection", beforeNames);

        var result = await new LocalDatabaseMigrator().MigrateAsync(connection, identity);
        Assert.True(result.Succeeded);
        Assert.Equal(LocalDatabaseMigrator.ConnectedSuppliersSchemaVersion, result.SchemaVersion);

        var after = await connection.QueryRowsAsync(
            "SELECT name FROM sqlite_master WHERE type='table';");
        var afterNames = after.Select(r => Convert.ToString(r["name"])!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("local_repayment_projection", afterNames);
        Assert.Contains("local_personal_contact", afterNames);

        var versions = await connection.QueryRowsAsync(
            "SELECT schema_version FROM local_schema_info ORDER BY schema_version;");
        Assert.Contains(versions, r => Convert.ToInt32(r["schema_version"]) == 4);
        Assert.Contains(versions, r => Convert.ToInt32(r["schema_version"]) == 6);
        root.Dispose();
    }

    private static async Task SeedSchemaVersionAsync(
        string path,
        bool foundationOnly = false,
        bool throughQueue = false,
        bool throughBusinessCache = false)
    {
        await using var sqlite = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}");
        await sqlite.OpenAsync();
        var now = DateTimeOffset.UtcNow.UtcDateTime.ToString("O");

        async Task ExecAsync(string sql)
        {
            await using var cmd = sqlite.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        await ExecAsync(
            """
            CREATE TABLE IF NOT EXISTS local_schema_info (
                schema_version INTEGER NOT NULL PRIMARY KEY,
                applied_at_utc TEXT NOT NULL
            );
            """);
        await ExecAsync(
            """
            CREATE TABLE IF NOT EXISTS local_context_info (
                context_hash TEXT NOT NULL PRIMARY KEY,
                user_id TEXT NOT NULL,
                organization_id TEXT NOT NULL,
                product_code TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                last_opened_at_utc TEXT NOT NULL
            );
            """);
        await ExecAsync($"INSERT INTO local_schema_info (schema_version, applied_at_utc) VALUES (1, '{now}');");

        if (foundationOnly)
        {
            return;
        }

        await ExecAsync(
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
            """);
        await ExecAsync(
            """
            CREATE TABLE IF NOT EXISTS local_sync_meta (
                key TEXT NOT NULL PRIMARY KEY,
                value TEXT NOT NULL
            );
            """);
        await ExecAsync($"INSERT INTO local_schema_info (schema_version, applied_at_utc) VALUES (2, '{now}');");

        if (throughQueue)
        {
            return;
        }

        await ExecAsync("ALTER TABLE offline_operations ADD COLUMN depends_on_operation_id TEXT NULL;");
        await ExecAsync("ALTER TABLE offline_operations ADD COLUMN entity_id TEXT NULL;");
        await ExecAsync(
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
            """);
        await ExecAsync(
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
            """);
        await ExecAsync(
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
            """);
        await ExecAsync(
            """
            CREATE TABLE IF NOT EXISTS local_download_checkpoint (
                stream TEXT NOT NULL PRIMARY KEY,
                checkpoint_utc TEXT NOT NULL
            );
            """);
        await ExecAsync($"INSERT INTO local_schema_info (schema_version, applied_at_utc) VALUES (3, '{now}');");

        if (throughBusinessCache)
        {
            return;
        }
    }

    [Fact]
    public async Task Close_clears_active_context_and_reopen_works()
    {
        var root = new TempRoot();
        await using var manager = new LocalContextManager(
            new LocalDatabasePathResolver(root),
            new LocalDatabaseFactory(),
            new LocalDatabaseMigrator());

        var user = Guid.NewGuid();
        var org = Guid.NewGuid();
        Assert.True((await manager.OpenAsync(user, org, PosProductCodes.PinoyBusinessPos)).Succeeded);
        await manager.CloseAsync();
        Assert.Null(manager.ActiveContext);

        var reopen = await manager.OpenAsync(user, org, PosProductCodes.PinoyBusinessPos);
        Assert.True(reopen.Succeeded);
        Assert.NotNull(manager.ActiveContext);
    }

    [Fact]
    public async Task Organization_switch_uses_different_database_file()
    {
        var root = new TempRoot();
        await using var manager = new LocalContextManager(
            new LocalDatabasePathResolver(root),
            new LocalDatabaseFactory(),
            new LocalDatabaseMigrator());

        var user = Guid.NewGuid();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var first = await manager.OpenAsync(user, orgA, PosProductCodes.PinoyBusinessPos);
        var second = await manager.OpenAsync(user, orgB, PosProductCodes.PinoyBusinessPos);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.NotEqual(first.Context!.DatabaseFileName, second.Context!.DatabaseFileName);
        Assert.Equal(second.Context.DatabaseFileName, manager.ActiveContext!.DatabaseFileName);
    }

    [Fact]
    public async Task Concurrent_open_same_context_is_safe()
    {
        var root = new TempRoot();
        await using var manager = new LocalContextManager(
            new LocalDatabasePathResolver(root),
            new LocalDatabaseFactory(),
            new LocalDatabaseMigrator());

        var user = Guid.NewGuid();
        var org = Guid.NewGuid();
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => manager.OpenAsync(user, org, PosProductCodes.PinoyBusinessPos))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Succeeded));
        Assert.Equal(1, results.Select(r => r.Context!.DatabaseFileName).Distinct(StringComparer.Ordinal).Count());
    }

    private sealed class TempRoot : ILocalStoreRootPathProvider, IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "exits-localstore-tests", Guid.NewGuid().ToString("N"));

        public string GetLocalStoreRootDirectory()
        {
            Directory.CreateDirectory(_path);
            return _path;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_path))
                {
                    Directory.Delete(_path, recursive: true);
                }
            }
            catch
            {
                // best effort
            }
        }
    }
}
