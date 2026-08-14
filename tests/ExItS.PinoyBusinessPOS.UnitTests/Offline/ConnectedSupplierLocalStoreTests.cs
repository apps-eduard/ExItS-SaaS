using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.LocalStore;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class ConnectedSupplierLocalStoreTests
{
    [Fact]
    public async Task Search_is_relationship_scoped_and_matches_name_or_sku()
    {
        using var root = new TempRoot();
        var resolver = new LocalDatabasePathResolver(root);
        await using var context = new LocalContextManager(resolver, new LocalDatabaseFactory(), new LocalDatabaseMigrator());
        Assert.True((await context.OpenAsync(Guid.NewGuid(), Guid.NewGuid(), PosProductCodes.PinoyBusinessPos)).Succeeded);
        var store = new LocalConnectedSupplierStore(context, resolver);
        var relationship = Guid.NewGuid();
        var otherRelationship = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await store.UpsertRangeAsync(
        [
            Product(relationship, "Premium Rice", "RICE-25", now),
            Product(otherRelationship, "Rice from another supplier", "OTHER", now)
        ]);

        var byName = await store.SearchLocalAsync(relationship, "premium", 20);
        var bySku = await store.SearchLocalAsync(relationship, "RICE-25", 20);
        var noMatch = await store.SearchLocalAsync(relationship, "coffee", 20);

        Assert.Single(byName);
        Assert.Single(bySku);
        Assert.Empty(noMatch);
        Assert.All(byName.Concat(bySku), x => Assert.Equal(relationship, x.RelationshipId));
    }

    [Fact]
    public async Task Delta_cursor_and_removed_ids_are_persisted_locally()
    {
        using var root = new TempRoot();
        var resolver = new LocalDatabasePathResolver(root);
        await using var context = new LocalContextManager(resolver, new LocalDatabaseFactory(), new LocalDatabaseMigrator());
        Assert.True((await context.OpenAsync(Guid.NewGuid(), Guid.NewGuid(), PosProductCodes.PinoyBusinessPos)).Succeeded);
        var store = new LocalConnectedSupplierStore(context, resolver);
        var relationship = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var product = Product(relationship, "Cooking Oil", "OIL-1L", now);

        await store.UpsertRangeAsync([product]);
        await store.SetSyncVersionAsync(relationship, 42, now);
        await store.RemoveIdsAsync(relationship, [product.LinkId]);

        Assert.Equal(42, await store.GetSyncVersionAsync(relationship));
        Assert.Empty(await store.ListByRelationshipAsync(relationship));
    }

    [Fact]
    public async Task Active_attached_supplier_is_available_after_offline_restart()
    {
        using var root = new TempRoot();
        var resolver = new LocalDatabasePathResolver(root);
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        await using (var context = new LocalContextManager(resolver, new LocalDatabaseFactory(), new LocalDatabaseMigrator()))
        {
            Assert.True((await context.OpenAsync(userId, organizationId, PosProductCodes.PinoyBusinessPos)).Succeeded);
            var store = new LocalConnectedSupplierStore(context, resolver);
            await store.UpsertConnectedSuppliersAsync(
                [new(relationshipId, Guid.NewGuid(), supplierId, "Connected Foods", "Active", DateTimeOffset.UtcNow)]);
        }

        await using var reopened = new LocalContextManager(resolver, new LocalDatabaseFactory(), new LocalDatabaseMigrator());
        Assert.True((await reopened.OpenAsync(userId, organizationId, PosProductCodes.PinoyBusinessPos)).Succeeded);
        var offlineStore = new LocalConnectedSupplierStore(reopened, resolver);
        var suppliers = await offlineStore.ListConnectedSuppliersAsync();

        var supplier = Assert.Single(suppliers);
        Assert.Equal(relationshipId, supplier.RelationshipId);
        Assert.Equal(supplierId, supplier.BuyerSupplierId);
    }

    private static LocalLinkedSupplierProduct Product(Guid relationshipId, string name, string sku, DateTimeOffset now) =>
        new(Guid.NewGuid(), relationshipId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sku, name, "pc",
            100m, true, true, 1, now, now);

    private sealed class TempRoot : ILocalStoreRootPathProvider, IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "exits-connected-supplier-tests", Guid.NewGuid().ToString("N"));
        public string GetLocalStoreRootDirectory()
        {
            Directory.CreateDirectory(_path);
            return _path;
        }
        public void Dispose()
        {
            try { if (Directory.Exists(_path)) Directory.Delete(_path, true); }
            catch { }
        }
    }
}
