using System.Security.Cryptography;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.LocalStore;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class OfflineQueueEncryptionAndOrderingTests
{
    [Fact]
    public async Task Enqueue_encrypts_payload_and_keeps_key_out_of_sqlite()
    {
        await using var harness = await Harness.CreateAsync();
        var operationId = Guid.NewGuid();
        var plaintext = Encoding.UTF8.GetBytes("probe-token-1");

        await harness.Queue.EnqueueAsync(new OfflineEnqueueRequest(
            operationId,
            OfflineOperationTypes.DevOfflineProbe,
            1,
            "idem-1",
            plaintext));

        var counts = await harness.Queue.GetCountsAsync();
        Assert.Equal(1, counts.Pending);

        var path = harness.Resolver.ResolveDatabasePath(harness.UserId, harness.OrgId, PosProductCodes.PinoyBusinessPos);
        await using var raw = new SqliteConnection($"Data Source={path}");
        await raw.OpenAsync();
        await using var cmd = raw.CreateCommand();
        cmd.CommandText = "SELECT ciphertext, nonce, tag FROM offline_operations WHERE operation_id = $id;";
        cmd.Parameters.AddWithValue("$id", operationId.ToString("D"));
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var ciphertext = (byte[])reader["ciphertext"];
        Assert.NotEmpty(ciphertext);
        Assert.False(Encoding.UTF8.GetString(ciphertext).Contains("probe-token-1", StringComparison.Ordinal));

        await using var scan = raw.CreateCommand();
        scan.CommandText = "SELECT name FROM sqlite_master;";
        var schemaDump = Convert.ToString(await scan.ExecuteScalarAsync()) ?? string.Empty;
        Assert.DoesNotContain(SecureTokenKeys.LocalPayloadEncryptionKey, schemaDump, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(await harness.Tokens.GetAsync(SecureTokenKeys.LocalPayloadEncryptionKey));
    }

    [Fact]
    public async Task Claim_is_fifo_with_operation_id_tie_breaker()
    {
        await using var harness = await Harness.CreateAsync();
        var clock = harness.Clock;
        var t0 = DateTimeOffset.Parse("2026-07-30T10:00:00Z");
        clock.SetUtcNow(t0);

        var first = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var second = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await harness.Queue.EnqueueAsync(new OfflineEnqueueRequest(first, OfflineOperationTypes.DevOfflineProbe, 1, "a", Encoding.UTF8.GetBytes("a")));
        await harness.Queue.EnqueueAsync(new OfflineEnqueueRequest(second, OfflineOperationTypes.DevOfflineProbe, 1, "b", Encoding.UTF8.GetBytes("b")));

        var claimed1 = await harness.Queue.TryClaimNextAsync("claim-1");
        Assert.Equal(first, claimed1!.OperationId);
        await harness.Queue.MarkSucceededAsync(first, "ref-1");

        var claimed2 = await harness.Queue.TryClaimNextAsync("claim-2");
        Assert.Equal(second, claimed2!.OperationId);
    }

    [Fact]
    public async Task Concurrent_claims_do_not_duplicate()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.Queue.EnqueueAsync(new OfflineEnqueueRequest(
            Guid.NewGuid(), OfflineOperationTypes.DevOfflineProbe, 1, "only", Encoding.UTF8.GetBytes("x")));

        var tasks = Enumerable.Range(0, 8)
            .Select(i => harness.Queue.TryClaimNextAsync($"c-{i}"))
            .ToArray();
        var results = await Task.WhenAll(tasks);
        Assert.Equal(1, results.Count(r => r is not null));
    }

    [Fact]
    public async Task Abandoned_syncing_is_recovered_on_restart()
    {
        await using var harness = await Harness.CreateAsync();
        var id = Guid.NewGuid();
        await harness.Queue.EnqueueAsync(new OfflineEnqueueRequest(
            id, OfflineOperationTypes.DevOfflineProbe, 1, "rec", Encoding.UTF8.GetBytes("x")));
        var claimed = await harness.Queue.TryClaimNextAsync("crash");
        Assert.Equal(OfflineQueueState.Syncing, claimed!.QueueState);

        await harness.Queue.RecoverAbandonedSyncingAsync();
        var counts = await harness.Queue.GetCountsAsync();
        Assert.Equal(1, counts.Pending);
        Assert.Equal(0, counts.Syncing);
    }

    [Fact]
    public async Task Tampered_ciphertext_fails_decrypt()
    {
        await using var harness = await Harness.CreateAsync();
        var id = Guid.NewGuid();
        await harness.Queue.EnqueueAsync(new OfflineEnqueueRequest(
            id, OfflineOperationTypes.DevOfflineProbe, 1, "tamper", Encoding.UTF8.GetBytes("secret")));

        var loaded = await harness.Queue.TryLoadEncryptedAsync(id);
        Assert.NotNull(loaded);
        var bad = loaded.Value.Encrypted with
        {
            Ciphertext = loaded.Value.Encrypted.Ciphertext.Select(b => (byte)(b ^ 0xFF)).ToArray()
        };

        await Assert.ThrowsAnyAsync<CryptographicException>(async () =>
            await harness.Protector.DecryptAsync(
                bad,
                OfflinePayloadBinding.BuildAssociatedData(
                    harness.Manager.ActiveContext!.Identity.ContextHash,
                    id,
                    OfflineOperationTypes.DevOfflineProbe)));
    }

    [Fact]
    public async Task Logout_close_retains_pending_rows()
    {
        await using var harness = await Harness.CreateAsync();
        var id = Guid.NewGuid();
        await harness.Queue.EnqueueAsync(new OfflineEnqueueRequest(
            id, OfflineOperationTypes.DevOfflineProbe, 1, "keep", Encoding.UTF8.GetBytes("x")));
        await harness.Manager.CloseAsync();
        Assert.Null(harness.Manager.ActiveContext);

        Assert.True((await harness.Manager.OpenAsync(harness.UserId, harness.OrgId, PosProductCodes.PinoyBusinessPos)).Succeeded);
        var counts = await harness.Queue.GetCountsAsync();
        Assert.Equal(1, counts.Pending);
        Assert.True(await harness.Queue.HasUnsyncedWorkAsync());
    }

    private sealed class Harness : IAsyncDisposable
    {
        public required TempRoot Root { get; init; }
        public required LocalDatabasePathResolver Resolver { get; init; }
        public required LocalContextManager Manager { get; init; }
        public required OfflineOperationQueue Queue { get; init; }
        public required AesGcmLocalPayloadProtector Protector { get; init; }
        public required MemorySecureTokenStore Tokens { get; init; }
        public required FakeTimeProvider Clock { get; init; }
        public required CurrentUserContext CurrentUser { get; init; }
        public Guid UserId { get; init; }
        public Guid OrgId { get; init; }

        public static async Task<Harness> CreateAsync()
        {
            var root = new TempRoot();
            var resolver = new LocalDatabasePathResolver(root);
            var manager = new LocalContextManager(resolver, new LocalDatabaseFactory(), new LocalDatabaseMigrator());
            var tokens = new MemorySecureTokenStore();
            var protector = new AesGcmLocalPayloadProtector(tokens);
            var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
            var userId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var current = new CurrentUserContext();
            current.Set(new AuthSession(
                userId, "U", "u", "u@example.com", orgId, "Org",
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
                true, "allowed"));

            Assert.True((await manager.OpenAsync(userId, orgId, PosProductCodes.PinoyBusinessPos)).Succeeded);
            var queue = new OfflineOperationQueue(manager, resolver, protector, new DeviceIdentityProvider(tokens), current, clock);
            return new Harness
            {
                Root = root,
                Resolver = resolver,
                Manager = manager,
                Queue = queue,
                Protector = protector,
                Tokens = tokens,
                Clock = clock,
                CurrentUser = current,
                UserId = userId,
                OrgId = orgId
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
        private readonly string _path = Path.Combine(Path.GetTempPath(), "exits-offline-q", Guid.NewGuid().ToString("N"));
        public string GetLocalStoreRootDirectory()
        {
            Directory.CreateDirectory(_path);
            return _path;
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_path)) Directory.Delete(_path, true); } catch { }
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

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void SetUtcNow(DateTimeOffset value) => _now = value;
    }
}
