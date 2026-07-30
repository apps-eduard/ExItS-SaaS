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
        Assert.Equal(3, open.Context!.SchemaVersion);
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
        Assert.Contains("local_customer_balance", names);
        Assert.Contains("local_download_checkpoint", names);
        Assert.DoesNotContain("customers", names);
        Assert.DoesNotContain("credit_entries", names);
        Assert.DoesNotContain("repayments", names);
        Assert.DoesNotContain("ledger", names);
        Assert.DoesNotContain("sync_queue", names);
        Assert.DoesNotContain("entitlement_cache", names);
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
