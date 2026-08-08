using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.LocalStore;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class PaymentOfflineStoreTests
{
    [Fact]
    public async Task Offline_repayment_create_updates_pending_repayment_and_projected_balance()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();

        await harness.Store.SetConfirmedOutstandingAsync(customerId, 100m, CancellationToken.None);
        var repaymentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await harness.Store.PersistRepaymentCreateAndEnqueueAsync(
            new LocalRepaymentCreateCommand(
                repaymentId,
                customerId,
                operationId,
                operationId.ToString("N"),
                30m,
                "Partial",
                DependsOnCustomerCreateOperationId: null,
                DependsOnCreditCreateOperationId: null),
            CancellationToken.None);

        var balance = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(100m, balance.ConfirmedOutstanding);
        Assert.Equal(0m, balance.PendingCredit);
        Assert.Equal(30m, balance.PendingRepayment);
        Assert.Equal(70m, balance.ProjectedOutstanding);
    }

    [Fact]
    public async Task Local_overpayment_is_rejected_with_local_overpayment()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        await harness.Store.SetConfirmedOutstandingAsync(customerId, 50m, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.PersistRepaymentCreateAndEnqueueAsync(
                new LocalRepaymentCreateCommand(
                    Guid.NewGuid(),
                    customerId,
                    Guid.NewGuid(),
                    "overpay",
                    51m,
                    null,
                    null,
                    null),
                CancellationToken.None));

        Assert.Equal("local_overpayment", ex.Message);
    }

    [Fact]
    public async Task Exact_balance_repayment_is_allowed()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        await harness.Store.SetConfirmedOutstandingAsync(customerId, 75m, CancellationToken.None);

        var operationId = Guid.NewGuid();
        await harness.Store.PersistRepaymentCreateAndEnqueueAsync(
            new LocalRepaymentCreateCommand(
                Guid.NewGuid(),
                customerId,
                operationId,
                operationId.ToString("N"),
                75m,
                "Settle",
                null,
                null),
            CancellationToken.None);

        var balance = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(75m, balance.PendingRepayment);
        Assert.Equal(0m, balance.ProjectedOutstanding);
    }

    [Fact]
    public async Task Repayment_waits_for_pending_customer_create_dependency()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var customerOpId = Guid.NewGuid();
        var repaymentId = Guid.NewGuid();
        var repaymentOpId = Guid.NewGuid();

        await harness.Store.PersistCustomerCreateAndEnqueueAsync(
            new LocalCustomerCreateCommand(
                customerId,
                customerOpId,
                customerOpId.ToString("N"),
                "Dep Customer",
                "09170000002",
                null,
                null),
            CancellationToken.None);

        await harness.Store.SetConfirmedOutstandingAsync(customerId, 0m, CancellationToken.None);
        await harness.Store.PersistCreditCreateAndEnqueueAsync(
            new LocalCreditCreateCommand(
                Guid.NewGuid(),
                customerId,
                Guid.NewGuid(),
                "credit-for-balance",
                100m,
                "Utang",
                customerOpId),
            CancellationToken.None);

        await harness.Store.PersistRepaymentCreateAndEnqueueAsync(
            new LocalRepaymentCreateCommand(
                repaymentId,
                customerId,
                repaymentOpId,
                repaymentOpId.ToString("N"),
                25m,
                "Repay",
                customerOpId,
                null),
            CancellationToken.None);

        var first = await harness.Queue.TryClaimNextAsync("claim-1");
        Assert.NotNull(first);
        Assert.Equal(customerOpId, first!.OperationId);

        Assert.Null(await harness.Queue.TryClaimNextAsync("claim-2"));

        await harness.Queue.MarkSucceededAsync(customerOpId, customerId.ToString("D"));

        var second = await harness.Queue.TryClaimNextAsync("claim-3");
        Assert.NotNull(second);
        Assert.Equal(OfflineOperationTypes.CreditCreate, second!.OperationType);

        await harness.Queue.MarkSucceededAsync(second.OperationId, second.OperationId.ToString("D"));

        var third = await harness.Queue.TryClaimNextAsync("claim-4");
        Assert.NotNull(third);
        Assert.Equal(repaymentOpId, third!.OperationId);
        Assert.Equal(OfflineOperationTypes.RepaymentCreate, third.OperationType);
    }

    [Fact]
    public async Task Credit_reverse_requires_server_confirmed_and_sets_pending_reversal()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var creditId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await harness.Store.UpsertServerCreditAsync(
            new LocalCreditProjection(
                creditId,
                customerId,
                harness.OrgId,
                80m,
                "Goods",
                "Active",
                now,
                LocalEntitySyncState.ServerConfirmed,
                null,
                null,
                null),
            CancellationToken.None);

        var reverseOpId = Guid.NewGuid();
        await harness.Store.PersistCreditReverseAndEnqueueAsync(
            new LocalCreditReverseCommand(
                creditId,
                customerId,
                reverseOpId,
                reverseOpId.ToString("N"),
                "Mistake"),
            CancellationToken.None);

        var credit = await harness.Store.GetCreditAsync(creditId);
        Assert.NotNull(credit);
        Assert.Equal(LocalEntitySyncState.PendingReversal, credit!.EntityState);
        Assert.Equal(reverseOpId, credit.PendingOperationId);

        var pendingCredit = Guid.NewGuid();
        await harness.Store.PersistCreditCreateAndEnqueueAsync(
            new LocalCreditCreateCommand(
                pendingCredit,
                customerId,
                Guid.NewGuid(),
                "pending-only",
                10m,
                "Pending",
                null),
            CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.PersistCreditReverseAndEnqueueAsync(
                new LocalCreditReverseCommand(
                    pendingCredit,
                    customerId,
                    Guid.NewGuid(),
                    "blocked",
                    "Too early"),
                CancellationToken.None));

        Assert.Equal("credit_not_reversible", ex.Message);
    }

    [Fact]
    public async Task Duplicate_pending_reversal_is_blocked_on_second_enqueue()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var creditId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await harness.Store.UpsertServerCreditAsync(
            new LocalCreditProjection(
                creditId,
                customerId,
                harness.OrgId,
                50m,
                "Goods",
                "Active",
                now,
                LocalEntitySyncState.ServerConfirmed,
                null,
                null,
                null),
            CancellationToken.None);

        var firstOpId = Guid.NewGuid();
        await harness.Store.PersistCreditReverseAndEnqueueAsync(
            new LocalCreditReverseCommand(
                creditId,
                customerId,
                firstOpId,
                firstOpId.ToString("N"),
                "First"),
            CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.PersistCreditReverseAndEnqueueAsync(
                new LocalCreditReverseCommand(
                    creditId,
                    customerId,
                    Guid.NewGuid(),
                    "duplicate",
                    "Second"),
                CancellationToken.None));

        Assert.Equal("credit_not_reversible", ex.Message);
    }

    [Fact]
    public async Task Due_date_set_stores_pending_fields_and_discard_clears_optimistic_due_date()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var creditId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var dueDate = new DateOnly(2026, 8, 15);

        await harness.Store.UpsertServerCreditAsync(
            new LocalCreditProjection(
                creditId,
                customerId,
                harness.OrgId,
                40m,
                "Utang",
                "Active",
                now,
                LocalEntitySyncState.ServerConfirmed,
                null,
                null,
                null,
                CurrentDueDate: null),
            CancellationToken.None);

        var opId = Guid.NewGuid();
        await harness.Store.PersistCreditDueDateAndEnqueueAsync(
            new LocalCreditDueDateCommand(
                creditId,
                customerId,
                opId,
                opId.ToString("N"),
                dueDate,
                "Set due",
                IsClear: false,
                ExpectedConcurrencyToken: now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)),
            CancellationToken.None);

        var pending = await harness.Store.GetCreditAsync(creditId);
        Assert.NotNull(pending);
        Assert.Equal(LocalEntitySyncState.PendingUpdate, pending!.EntityState);
        Assert.Equal(dueDate, pending.PendingDueDate);
        Assert.Equal("Set due", pending.PendingDueDateReason);
        Assert.False(pending.PendingDueDateClear);

        await harness.Store.DiscardLocalPendingCreditDueDateAsync(creditId, CancellationToken.None);

        var afterDiscard = await harness.Store.GetCreditAsync(creditId);
        Assert.NotNull(afterDiscard);
        Assert.Equal(LocalEntitySyncState.ServerConfirmed, afterDiscard!.EntityState);
        Assert.Null(afterDiscard.PendingDueDate);
        Assert.Null(afterDiscard.PendingDueDateReason);
        Assert.False(afterDiscard.PendingDueDateClear);
    }

    [Fact]
    public async Task Rejected_repayment_corrects_pending_repayment_total()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        await harness.Store.SetConfirmedOutstandingAsync(customerId, 100m, CancellationToken.None);

        var repaymentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await harness.Store.PersistRepaymentCreateAndEnqueueAsync(
            new LocalRepaymentCreateCommand(
                repaymentId,
                customerId,
                operationId,
                operationId.ToString("N"),
                40m,
                "Partial",
                null,
                null),
            CancellationToken.None);

        var withPending = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(40m, withPending.PendingRepayment);
        Assert.Equal(60m, withPending.ProjectedOutstanding);

        await harness.Store.MarkRepaymentStateAsync(
            repaymentId,
            LocalEntitySyncState.Rejected,
            safeFailureCode: "validation_failed",
            CancellationToken.None);

        var afterReject = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(0m, afterReject.PendingRepayment);
        Assert.Equal(100m, afterReject.ProjectedOutstanding);
    }

    [Fact]
    public async Task RebuildOptimisticBalancesAsync_restores_pending_from_pending_create_projections()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        await harness.Store.SetConfirmedOutstandingAsync(customerId, 100m, CancellationToken.None);

        await harness.Store.PersistCreditCreateAndEnqueueAsync(
            new LocalCreditCreateCommand(
                Guid.NewGuid(),
                customerId,
                Guid.NewGuid(),
                "pending-credit",
                25m,
                "New utang",
                null),
            CancellationToken.None);

        var repayOpId = Guid.NewGuid();
        await harness.Store.PersistRepaymentCreateAndEnqueueAsync(
            new LocalRepaymentCreateCommand(
                Guid.NewGuid(),
                customerId,
                repayOpId,
                repayOpId.ToString("N"),
                10m,
                "Repay",
                null,
                null),
            CancellationToken.None);

        await CorruptPendingRepayColumnsAsync(harness.DbPath, customerId);

        var corrupted = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(25m, corrupted.PendingCredit);
        Assert.Equal(0m, corrupted.PendingRepayment);

        await harness.Store.RebuildOptimisticBalancesAsync(customerId, CancellationToken.None);

        var rebuilt = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(25m, rebuilt.PendingCredit);
        Assert.Equal(10m, rebuilt.PendingRepayment);
        Assert.Equal(115m, rebuilt.ProjectedOutstanding);
    }

    [Fact]
    public async Task ServerConfirmed_credit_after_pending_create_rebuilds_balance_without_double_count()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var creditId = Guid.NewGuid();
        var creditOpId = Guid.NewGuid();
        const decimal amount = 25m;

        await harness.Store.SetConfirmedOutstandingAsync(customerId, 100m, CancellationToken.None);
        await harness.Store.PersistCreditCreateAndEnqueueAsync(
            new LocalCreditCreateCommand(
                creditId,
                customerId,
                creditOpId,
                creditOpId.ToString("N"),
                amount,
                "Offline utang",
                null),
            CancellationToken.None);

        var withPending = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(100m, withPending.ConfirmedOutstanding);
        Assert.Equal(amount, withPending.PendingCredit);
        Assert.Equal(125m, withPending.ProjectedOutstanding);

        var now = DateTimeOffset.UtcNow;
        await harness.Store.UpsertServerCreditAsync(
            new LocalCreditProjection(
                creditId,
                customerId,
                harness.OrgId,
                amount,
                "Offline utang",
                "Active",
                now,
                LocalEntitySyncState.ServerConfirmed,
                null,
                null,
                null),
            CancellationToken.None);

        await harness.Store.SetConfirmedOutstandingAsync(customerId, 125m, CancellationToken.None);
        await harness.Store.RebuildOptimisticBalancesAsync(customerId, CancellationToken.None);

        var afterConfirm = await harness.Store.GetBalanceAsync(customerId);
        Assert.Equal(125m, afterConfirm.ConfirmedOutstanding);
        Assert.Equal(0m, afterConfirm.PendingCredit);
        Assert.Equal(125m, afterConfirm.ProjectedOutstanding);
    }

    [Fact]
    public async Task PersistCreditDueDate_does_not_store_plaintext_reason_in_sqlite_column()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var creditId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        const string reason = "Customer asked for extension";

        await harness.Store.UpsertServerCreditAsync(
            new LocalCreditProjection(
                creditId,
                customerId,
                harness.OrgId,
                40m,
                "Utang",
                "Active",
                now,
                LocalEntitySyncState.ServerConfirmed,
                null,
                null,
                null,
                CurrentDueDate: null),
            CancellationToken.None);

        var opId = Guid.NewGuid();
        await harness.Store.PersistCreditDueDateAndEnqueueAsync(
            new LocalCreditDueDateCommand(
                creditId,
                customerId,
                opId,
                opId.ToString("N"),
                new DateOnly(2026, 8, 15),
                reason,
                IsClear: false,
                ExpectedConcurrencyToken: now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)),
            CancellationToken.None);

        var credit = await harness.Store.GetCreditAsync(creditId);
        Assert.Equal(reason, credit!.PendingDueDateReason);

        await using var raw = new SqliteConnection($"Data Source={harness.DbPath}");
        await raw.OpenAsync();
        await using var cmd = raw.CreateCommand();
        cmd.CommandText =
            """
            SELECT pending_due_date_reason FROM local_credit_projection
            WHERE credit_entry_id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", creditId.ToString("D"));
        var columnValue = await cmd.ExecuteScalarAsync();
        Assert.True(columnValue is null or DBNull);
        AssertNoPlaintextInTextColumns(raw, "local_credit_projection", reason);
    }

    [Fact]
    public async Task PersistRepaymentReverse_does_not_store_plaintext_reason_in_sqlite_column()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        var repaymentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        const string reason = "Posted to wrong customer";

        await harness.Store.UpsertServerRepaymentAsync(
            new LocalRepaymentProjection(
                repaymentId,
                customerId,
                harness.OrgId,
                50m,
                "Partial",
                "Active",
                now,
                LocalEntitySyncState.ServerConfirmed,
                null,
                null,
                null),
            CancellationToken.None);

        var opId = Guid.NewGuid();
        await harness.Store.PersistRepaymentReverseAndEnqueueAsync(
            new LocalRepaymentReverseCommand(
                repaymentId,
                customerId,
                opId,
                opId.ToString("N"),
                reason),
            CancellationToken.None);

        var repayment = await harness.Store.GetRepaymentAsync(repaymentId);
        Assert.Equal(reason, repayment!.PendingReversalReason);

        await using var raw = new SqliteConnection($"Data Source={harness.DbPath}");
        await raw.OpenAsync();
        await using var cmd = raw.CreateCommand();
        cmd.CommandText =
            """
            SELECT pending_reversal_reason FROM local_repayment_projection
            WHERE repayment_id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", repaymentId.ToString("D"));
        var columnValue = await cmd.ExecuteScalarAsync();
        Assert.True(columnValue is null or DBNull);
        AssertNoPlaintextInTextColumns(raw, "local_repayment_projection", reason);
    }

    [Fact]
    public async Task Schema_v4_has_local_repayment_projection_and_no_repayments_table()
    {
        await using var harness = await Harness.CreateAsync();

        await using var sqlite = new SqliteConnection($"Data Source={harness.DbPath}");
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

            Assert.Contains("local_repayment_projection", names);
            Assert.DoesNotContain("repayments", names);
        }
    }

    [Fact]
    public async Task Encrypted_repayment_amount_and_remarks_not_in_sqlite_text_columns()
    {
        await using var harness = await Harness.CreateAsync();
        var customerId = Guid.NewGuid();
        await harness.Store.SetConfirmedOutstandingAsync(customerId, 200m, CancellationToken.None);

        const string remarks = "Bayad sa utang";
        const decimal amount = 123.45m;
        var operationId = Guid.NewGuid();
        var repaymentId = Guid.NewGuid();

        await harness.Store.PersistRepaymentCreateAndEnqueueAsync(
            new LocalRepaymentCreateCommand(
                repaymentId,
                customerId,
                operationId,
                operationId.ToString("N"),
                amount,
                remarks,
                null,
                null),
            CancellationToken.None);

        await using var raw = new SqliteConnection($"Data Source={harness.DbPath}");
        await raw.OpenAsync();
        AssertNoPlaintextInTextColumns(raw, "local_repayment_projection", remarks, amount.ToString("F2", CultureInfo.InvariantCulture));
        AssertDbFileExcludesSecrets(harness.DbPath, remarks, "123.45");
    }

    [Fact]
    public void OfflineOperationTypes_includes_payment_ops_but_not_statement_or_receipt()
    {
        var type = typeof(OfflineOperationTypes);
        var constants = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => v is not null)
            .Cast<string>()
            .ToList();

        Assert.Contains(OfflineOperationTypes.RepaymentCreate, constants);
        Assert.Contains(OfflineOperationTypes.RepaymentReverse, constants);
        Assert.Contains(OfflineOperationTypes.CreditReverse, constants);
        Assert.Contains(OfflineOperationTypes.CreditDueDateSet, constants);
        Assert.Contains(OfflineOperationTypes.CreditDueDateClear, constants);

        Assert.DoesNotContain(constants, v => v.Contains("statement", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(constants, v => v.Contains("receipt", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task CorruptPendingRepayColumnsAsync(string dbPath, Guid customerId)
    {
        await using var sqlite = new SqliteConnection($"Data Source={dbPath}");
        await sqlite.OpenAsync();
        await using var cmd = sqlite.CreateCommand();
        cmd.CommandText =
            """
            UPDATE local_customer_balance
            SET pending_repay_ciphertext = NULL,
                pending_repay_nonce = NULL,
                pending_repay_tag = NULL
            WHERE customer_id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", customerId.ToString("D"));
        await cmd.ExecuteNonQueryAsync();
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

    private sealed class Harness : IAsyncDisposable
    {
        public required TempRoot Root { get; init; }
        public required LocalContextManager Manager { get; init; }
        public required LocalEncryptedCustomerCreditStore Store { get; init; }
        public required OfflineOperationQueue Queue { get; init; }
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
                Manager = manager,
                Store = store,
                Queue = queue,
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
        private readonly string _path = Path.Combine(Path.GetTempPath(), "exits-payment-offline", Guid.NewGuid().ToString("N"));

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
