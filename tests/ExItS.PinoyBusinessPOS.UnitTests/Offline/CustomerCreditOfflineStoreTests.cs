using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.LocalStore;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class CustomerCreditOfflineStoreTests
{
    [Fact]
    public async Task PersistCustomerCreate_encrypts_projection_and_queue_payload()
    {
        await using var harness = await Harness.CreateAsync();
        const string displayName = "Maria Santos";
        const string mobile = "09171234567";
        const string address = "123 Mabini St";

        var customerId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await harness.Store.PersistCustomerCreateAndEnqueueAsync(
            new LocalCustomerCreateCommand(
                customerId,
                operationId,
                operationId.ToString("N"),
                displayName,
                mobile,
                address,
                "Neighbor"),
            CancellationToken.None);

        var dbPath = harness.DbPath;
        await using var raw = new SqliteConnection($"Data Source={dbPath}");
        await raw.OpenAsync();

        await using (var cmd = raw.CreateCommand())
        {
            cmd.CommandText =
                """
                SELECT ciphertext, nonce, tag FROM local_customer_projection
                WHERE customer_id = $id;
                """;
            cmd.Parameters.AddWithValue("$id", customerId.ToString("D"));
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.NotEmpty((byte[])reader["ciphertext"]);
            Assert.NotEmpty((byte[])reader["nonce"]);
            Assert.NotEmpty((byte[])reader["tag"]);
        }

        AssertNoPlaintextInTextColumns(raw, "local_customer_projection", displayName, mobile, address);

        await using (var cmd = raw.CreateCommand())
        {
            cmd.CommandText =
                """
                SELECT ciphertext, nonce, tag FROM offline_operations
                WHERE operation_id = $id;
                """;
            cmd.Parameters.AddWithValue("$id", operationId.ToString("D"));
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.NotEmpty((byte[])reader["ciphertext"]);
        }

        var keyValue = await harness.Tokens.GetAsync(SecureTokenKeys.LocalPayloadEncryptionKey);
        Assert.NotNull(keyValue);
        AssertDbFileExcludesSecrets(dbPath, keyValue!, mobile, address);
    }

    [Fact]
    public async Task PersistCustomerCreate_does_not_store_encryption_key_in_sqlite()
    {
        await using var harness = await Harness.CreateAsync();
        var operationId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await harness.Store.PersistCustomerCreateAndEnqueueAsync(
            new LocalCustomerCreateCommand(
                customerId,
                operationId,
                operationId.ToString("N"),
                "Key Probe",
                "09998887777",
                null,
                null),
            CancellationToken.None);

        var keyValue = await harness.Tokens.GetAsync(SecureTokenKeys.LocalPayloadEncryptionKey);
        Assert.NotNull(keyValue);
        AssertDbFileExcludesSecrets(harness.DbPath, keyValue!);
        Assert.DoesNotContain(SecureTokenKeys.LocalPayloadEncryptionKey, ReadDbFileText(harness.DbPath), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Credit_create_waits_for_customer_create_dependency()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var customerOpId = Guid.NewGuid();
        var creditId = Guid.NewGuid();
        var creditOpId = Guid.NewGuid();

        await harness.Store.PersistCustomerCreateAndEnqueueAsync(
            new LocalCustomerCreateCommand(
                customerId,
                customerOpId,
                customerOpId.ToString("N"),
                "Dep Customer",
                "09170000001",
                null,
                null),
            CancellationToken.None);

        await harness.Store.PersistCreditCreateAndEnqueueAsync(
            new LocalCreditCreateCommand(
                creditId,
                customerId,
                creditOpId,
                creditOpId.ToString("N"),
                50m,
                "Utang",
                customerOpId),
            CancellationToken.None);

        var first = await harness.Queue.TryClaimNextAsync("claim-1");
        Assert.NotNull(first);
        Assert.Equal(customerOpId, first!.OperationId);
        Assert.Equal(OfflineOperationTypes.CustomerCreate, first.OperationType);

        Assert.Null(await harness.Queue.TryClaimNextAsync("claim-2"));

        await harness.Queue.MarkSucceededAsync(customerOpId, customerId.ToString("D"));

        var second = await harness.Queue.TryClaimNextAsync("claim-3");
        Assert.NotNull(second);
        Assert.Equal(creditOpId, second!.OperationId);
        Assert.Equal(OfflineOperationTypes.CreditCreate, second.OperationType);
    }

    [Fact]
    public async Task Customer_create_permanent_failure_blocks_dependent_credit()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var customerOpId = Guid.NewGuid();
        var creditId = Guid.NewGuid();
        var creditOpId = Guid.NewGuid();

        await harness.Store.PersistCustomerCreateAndEnqueueAsync(
            new LocalCustomerCreateCommand(
                customerId,
                customerOpId,
                customerOpId.ToString("N"),
                "Fail Customer",
                null,
                null,
                null),
            CancellationToken.None);

        await harness.Store.PersistCreditCreateAndEnqueueAsync(
            new LocalCreditCreateCommand(
                creditId,
                customerId,
                creditOpId,
                creditOpId.ToString("N"),
                25m,
                "Blocked",
                customerOpId),
            CancellationToken.None);

        await harness.Queue.MarkFailureAsync(
            customerOpId,
            OfflineFailureClass.Permanent,
            "customer_rejected",
            null,
            nextAttemptUtc: null,
            attemptCount: 1);

        await harness.Store.MarkDependentsBlockedAsync(customerOpId, "dependency_failed");

        var metadata = await harness.Queue.ListSafeMetadataAsync(10);
        var creditOp = metadata.Single(m => m.OperationId == creditOpId);
        Assert.Equal(OfflineQueueState.Conflict, creditOp.QueueState);
        Assert.Equal("dependency_failed", creditOp.FailureCode);

        var credits = await harness.Store.ListCreditsAsync(customerId);
        var credit = Assert.Single(credits);
        Assert.Equal(LocalEntitySyncState.Conflict, credit.EntityState);
        Assert.Equal("dependency_failed", credit.SafeFailureCode);
    }

    [Fact]
    public async Task Balance_projection_tracks_confirmed_pending_and_rejected_credit()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var creditId = Guid.NewGuid();
        var creditOpId = Guid.NewGuid();

        await harness.Store.SetConfirmedOutstandingAsync(customerId, 100m);
        await harness.Store.PersistCreditCreateAndEnqueueAsync(
            new LocalCreditCreateCommand(
                creditId,
                customerId,
                creditOpId,
                creditOpId.ToString("N"),
                25m,
                "New utang",
                DependsOnCustomerCreateOperationId: null),
            CancellationToken.None);

        var withPending = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(100m, withPending.ConfirmedOutstanding);
        Assert.Equal(25m, withPending.PendingCredit);
        Assert.Equal(0m, withPending.PendingRepayment);
        Assert.Equal(125m, withPending.ProjectedOutstanding);

        await harness.Store.MarkCreditStateAsync(
            creditId,
            LocalEntitySyncState.Rejected,
            safeFailureCode: "validation_failed");

        var afterReject = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(100m, afterReject.ConfirmedOutstanding);
        Assert.Equal(0m, afterReject.PendingCredit);
        Assert.Equal(0m, afterReject.PendingRepayment);
        Assert.Equal(100m, afterReject.ProjectedOutstanding);
    }

    [Fact]
    public async Task Customer_in_one_organization_is_not_visible_in_another()
    {
        using var root = new TempRoot();
        var resolver = new LocalDatabasePathResolver(root);
        var manager = new LocalContextManager(resolver, new LocalDatabaseFactory(), new LocalDatabaseMigrator());
        var tokens = new MemorySecureTokenStore();
        var protector = new AesGcmLocalPayloadProtector(tokens);
        var userId = Guid.NewGuid();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        await OpenContextAsync(manager, tokens, userId, orgA);
        var storeA = CreateStore(manager, resolver, protector, tokens, userId, orgA);
        var customerId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await storeA.PersistCustomerCreateAndEnqueueAsync(
            new LocalCustomerCreateCommand(
                customerId,
                operationId,
                operationId.ToString("N"),
                "Org A Only",
                "09176665555",
                null,
                null),
            CancellationToken.None);

        await OpenContextAsync(manager, tokens, userId, orgB);
        var storeB = CreateStore(manager, resolver, protector, tokens, userId, orgB);

        Assert.Null(await storeB.GetCustomerAsync(customerId));
        Assert.Empty(await storeB.ListCustomersAsync(null, 0, 20));
        Assert.Equal(0, await storeB.CountCustomersAsync(null));

        await manager.DisposeAsync();
        root.Dispose();
    }

    [Fact]
    public async Task Schema_migrates_from_v2_to_v3_without_legacy_customers_table()
    {
        var root = new TempRoot();
        var resolver = new LocalDatabasePathResolver(root);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var product = PosProductCodes.PinoyBusinessPos;
        var hash = resolver.ComputeContextHash(userId, orgId, product);
        var identity = new LocalContextIdentity(hash, userId, orgId, product);
        var path = resolver.ResolveDatabasePath(userId, orgId, product);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await SeedV2SchemaAsync(path);

        await using var connection = await new LocalDatabaseFactory().OpenAsync(path);
        var migrator = new LocalDatabaseMigrator();
        var result = await migrator.MigrateAsync(connection, identity);
        Assert.True(result.Succeeded);
        Assert.Equal(LocalDatabaseMigrator.PersonalUtangSchemaVersion, result.SchemaVersion);

        await using var sqlite = new SqliteConnection($"Data Source={path}");
        await sqlite.OpenAsync();

        await using (var versionCmd = sqlite.CreateCommand())
        {
            versionCmd.CommandText = "SELECT MAX(schema_version) FROM local_schema_info;";
            var version = Convert.ToInt64(await versionCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
            Assert.Equal(LocalDatabaseMigrator.PersonalUtangSchemaVersion, version);
        }

        await using (var tablesCmd = sqlite.CreateCommand())
        {
            tablesCmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";
            var names = new List<string>();
            await using var reader = await tablesCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            Assert.Contains("local_customer_projection", names);
            Assert.DoesNotContain("customers", names);
        }

        root.Dispose();
    }

    private static async Task SeedV2SchemaAsync(string path)
    {
        await using var sqlite = new SqliteConnection($"Data Source={path}");
        await sqlite.OpenAsync();
        var now = DateTimeOffset.UtcNow.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

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
    }

    private static void AssertNoPlaintextInTextColumns(
        SqliteConnection connection,
        string tableName,
        params string?[] forbiddenPlaintext)
    {
        var secrets = forbiddenPlaintext.Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToArray();
        if (secrets.Length == 0)
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT * FROM {tableName};";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i) || reader.GetFieldType(i) == typeof(byte[]))
                {
                    continue;
                }

                var text = Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture) ?? string.Empty;
                foreach (var secret in secrets)
                {
                    Assert.DoesNotContain(secret, text, StringComparison.Ordinal);
                }
            }
        }
    }

    private static void AssertDbFileExcludesSecrets(string dbPath, params string[] secrets)
    {
        var bytes = ReadDbFileBytes(dbPath);
        var utf8 = Encoding.UTF8.GetString(bytes);
        var latin1 = Encoding.Latin1.GetString(bytes);
        foreach (var secret in secrets.Where(s => !string.IsNullOrEmpty(s)))
        {
            Assert.DoesNotContain(secret, utf8, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, latin1, StringComparison.Ordinal);
        }
    }

    private static byte[] ReadDbFileBytes(string dbPath)
    {
        using var stream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static string ReadDbFileText(string dbPath) =>
        Encoding.UTF8.GetString(ReadDbFileBytes(dbPath));

    private static async Task OpenContextAsync(
        LocalContextManager manager,
        MemorySecureTokenStore tokens,
        Guid userId,
        Guid orgId)
    {
        var protector = new AesGcmLocalPayloadProtector(tokens);
        await protector.EnsureKeyAsync();
        Assert.True((await manager.OpenAsync(userId, orgId, PosProductCodes.PinoyBusinessPos)).Succeeded);
    }

    private static LocalEncryptedCustomerCreditStore CreateStore(
        LocalContextManager manager,
        LocalDatabasePathResolver resolver,
        AesGcmLocalPayloadProtector protector,
        MemorySecureTokenStore tokens,
        Guid userId,
        Guid orgId)
    {
        var current = new CurrentUserContext();
        current.Set(new AuthSession(
            userId, "U", "u", "u@example.com", orgId, "Org",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            true, "allowed"));
        return new LocalEncryptedCustomerCreditStore(
            manager,
            resolver,
            protector,
            new DeviceIdentityProvider(tokens),
            current);
    }

    private sealed class Harness : IAsyncDisposable
    {
        public required TempRoot Root { get; init; }
        public required LocalDatabasePathResolver Resolver { get; init; }
        public required LocalContextManager Manager { get; init; }
        public required LocalEncryptedCustomerCreditStore Store { get; init; }
        public required OfflineOperationQueue Queue { get; init; }
        public required MemorySecureTokenStore Tokens { get; init; }
        public required Guid UserId { get; init; }
        public required Guid OrgId { get; init; }
        public string DbPath { get; init; } = string.Empty;

        public static async Task<Harness> CreateAsync()
        {
            var root = new TempRoot();
            var resolver = new LocalDatabasePathResolver(root);
            var manager = new LocalContextManager(resolver, new LocalDatabaseFactory(), new LocalDatabaseMigrator());
            var tokens = new MemorySecureTokenStore();
            var protector = new AesGcmLocalPayloadProtector(tokens);
            await protector.EnsureKeyAsync();
            var userId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var current = new CurrentUserContext();
            current.Set(new AuthSession(
                userId, "U", "u", "u@example.com", orgId, "Org",
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
                true, "allowed"));

            Assert.True((await manager.OpenAsync(userId, orgId, PosProductCodes.PinoyBusinessPos)).Succeeded);
            var store = new LocalEncryptedCustomerCreditStore(
                manager,
                resolver,
                protector,
                new DeviceIdentityProvider(tokens),
                current);
            var queue = new OfflineOperationQueue(
                manager,
                resolver,
                protector,
                new DeviceIdentityProvider(tokens),
                current);

            return new Harness
            {
                Root = root,
                Resolver = resolver,
                Manager = manager,
                Store = store,
                Queue = queue,
                Tokens = tokens,
                UserId = userId,
                OrgId = orgId,
                DbPath = resolver.ResolveDatabasePath(userId, orgId, PosProductCodes.PinoyBusinessPos)
            };
        }

        public async ValueTask DisposeAsync()
        {
            await Manager.DisposeAsync();
            Root.Dispose();
        }
    }

    private sealed class TempRoot : ILocalStoreRootPathProvider, IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "exits-customer-credit-offline", Guid.NewGuid().ToString("N"));

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

    private sealed class MemorySecureTokenStore : ISecureTokenStore
    {
        private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);

        public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
            Task.FromResult(_map.TryGetValue(key, out var v) ? v : null);

        public Task SetAsync(string key, string value, CancellationToken ct = default)
        {
            _map[key] = value;
            return Task.CompletedTask;
        }

        public Task ClearAsync(string key, CancellationToken ct = default)
        {
            _map.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAllSessionKeysAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
