using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.LocalStore;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class LocalCashSaleOfflineStoreTests
{
    [Fact]
    public async Task Offline_cash_sale_persists_locally_and_enqueues_once()
    {
        await using var harness = await Harness.CreateAsync();
        var productId = Guid.NewGuid();
        await SeedProductAsync(harness, productId, onHand: 10m, tracked: true);

        var saleId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await harness.Store.PersistCashSaleAndEnqueueAsync(CreateCommand(harness, saleId, operationId, productId, qty: 2m));

        var local = await harness.Store.GetBySaleIdAsync(saleId);
        Assert.NotNull(local);
        Assert.Equal(LocalEntitySyncState.PendingCreate, local!.EntityState);
        Assert.Equal(saleId.ToString("N"), local.IdempotencyKey);

        var claimed = await harness.Queue.TryClaimNextAsync("claim-1");
        Assert.NotNull(claimed);
        Assert.Equal(OfflineOperationTypes.SaleCheckout, claimed!.OperationType);
        Assert.Equal(saleId, claimed.EntityId);
        Assert.Equal(operationId, claimed.OperationId);

        // Second claim should not return a duplicate for the same sale.
        Assert.Null(await harness.Queue.TryClaimNextAsync("claim-2"));

        // Re-persist same saleId is idempotent (no second queue row).
        await harness.Store.PersistCashSaleAndEnqueueAsync(CreateCommand(harness, saleId, Guid.NewGuid(), productId, qty: 2m));
        Assert.Null(await harness.Queue.TryClaimNextAsync("claim-3"));
    }

    [Fact]
    public async Task App_restart_preserves_unsynced_sale()
    {
        await using var harness = await Harness.CreateAsync();
        var productId = Guid.NewGuid();
        await SeedProductAsync(harness, productId, onHand: 5m, tracked: false);

        var saleId = Guid.NewGuid();
        await harness.Store.PersistCashSaleAndEnqueueAsync(
            CreateCommand(harness, saleId, Guid.NewGuid(), productId, qty: 1m));

        // Simulate process restart: new store/queue over same DB file.
        var store2 = CreateStore(harness);
        var queue2 = CreateQueue(harness);
        var again = await store2.GetBySaleIdAsync(saleId);
        Assert.NotNull(again);
        Assert.Equal(LocalEntitySyncState.PendingCreate, again!.EntityState);

        var claimed = await queue2.TryClaimNextAsync("restart-claim");
        Assert.NotNull(claimed);
        Assert.Equal(saleId, claimed!.EntityId);
    }

    [Fact]
    public async Task Queued_sale_survives_sync_failure_marking()
    {
        await using var harness = await Harness.CreateAsync();
        var productId = Guid.NewGuid();
        await SeedProductAsync(harness, productId, onHand: 3m, tracked: true);

        var saleId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await harness.Store.PersistCashSaleAndEnqueueAsync(
            CreateCommand(harness, saleId, operationId, productId, qty: 1m));

        var claimed = await harness.Queue.TryClaimNextAsync("fail-claim");
        Assert.NotNull(claimed);
        await harness.Queue.MarkFailureAsync(
            claimed!.OperationId,
            OfflineFailureClass.Transient,
            "server_unavailable",
            "temporary",
            nextAttemptUtc: DateTimeOffset.UtcNow.AddMinutes(1),
            attemptCount: claimed.AttemptCount + 1);

        var local = await harness.Store.GetBySaleIdAsync(saleId);
        Assert.NotNull(local);
        Assert.Equal(LocalEntitySyncState.PendingCreate, local!.EntityState);

        // Still present in DB as a queue row (not Succeeded / not deleted).
        await using var raw = new SqliteConnection($"Data Source={harness.DbPath}");
        await raw.OpenAsync();
        await using var cmd = raw.CreateCommand();
        cmd.CommandText = "SELECT queue_state FROM offline_operations WHERE operation_id = $id;";
        cmd.Parameters.AddWithValue("$id", operationId.ToString("D"));
        var state = (string?)await cmd.ExecuteScalarAsync();
        Assert.Equal(nameof(OfflineQueueState.RetryableFailure), state);
    }

    [Fact]
    public async Task Sync_success_marks_local_sale_confirmed_once()
    {
        await using var harness = await Harness.CreateAsync();
        var productId = Guid.NewGuid();
        await SeedProductAsync(harness, productId, onHand: 8m, tracked: true);

        var saleId = Guid.NewGuid();
        await harness.Store.PersistCashSaleAndEnqueueAsync(
            CreateCommand(harness, saleId, Guid.NewGuid(), productId, qty: 1m));

        var serverId = Guid.NewGuid().ToString("D");
        await harness.Store.MarkSyncedAsync(saleId, serverId);
        await harness.Store.MarkSyncedAsync(saleId, serverId);

        var local = await harness.Store.GetBySaleIdAsync(saleId);
        Assert.NotNull(local);
        Assert.Equal(LocalEntitySyncState.ServerConfirmed, local!.EntityState);
        Assert.Equal(serverId, local.ServerReference);
    }

    [Fact]
    public async Task Local_inventory_deducts_on_cash_sale_commit()
    {
        await using var harness = await Harness.CreateAsync();
        var productId = Guid.NewGuid();
        await SeedProductAsync(harness, productId, onHand: 10m, tracked: true);

        await harness.Store.PersistCashSaleAndEnqueueAsync(
            CreateCommand(harness, Guid.NewGuid(), Guid.NewGuid(), productId, qty: 3m));

        var products = await harness.Store.SearchProductsAsync(null, null, 10);
        var product = Assert.Single(products);
        Assert.Equal(7m, product.OnHandQuantity);
    }

    [Fact]
    public async Task Offline_cash_sale_enqueues_payload_version_2_with_line_snapshots()
    {
        await using var harness = await Harness.CreateAsync();
        var productId = Guid.NewGuid();
        await SeedProductAsync(harness, productId, onHand: 10m, tracked: true);

        var saleId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await harness.Store.PersistCashSaleAndEnqueueAsync(
            CreateCommand(harness, saleId, operationId, productId, qty: 2m));

        var claimed = await harness.Queue.TryClaimNextAsync("snap-claim");
        Assert.NotNull(claimed);
        Assert.Equal(OfflineOperationTypes.SaleCheckoutPayloadVersions.Current, claimed!.PayloadVersion);

        var loaded = await harness.Queue.TryLoadEncryptedAsync(claimed.OperationId);
        Assert.NotNull(loaded);
        var protector = new AesGcmLocalPayloadProtector(harness.Tokens);
        var plaintext = await protector.DecryptAsync(
            loaded.Value.Encrypted,
            OfflinePayloadBinding.BuildAssociatedData(
                harness.Manager.ActiveContext!.Identity.ContextHash,
                claimed.OperationId,
                OfflineOperationTypes.SaleCheckout));

        var payload = System.Text.Json.JsonSerializer.Deserialize<CheckoutSaleRequest>(
            plaintext,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
        Assert.NotNull(payload);
        var line = Assert.Single(payload!.Lines);
        Assert.Equal(2m, line.Quantity);
        Assert.Equal(10m, line.UnitPriceSnapshot);
        Assert.Equal(nameof(Domain.Catalog.UnitOfMeasure.Piece), line.UnitOfMeasure);
        Assert.Equal(nameof(Domain.Catalog.SellingMode.PerItem), line.SellingMode);
        Assert.Equal(20m, line.LineTotal);

        var local = await harness.Store.GetBySaleIdAsync(saleId);
        Assert.NotNull(local);
        var receiptLine = Assert.Single(local!.Lines);
        Assert.Equal(2m, receiptLine.Quantity);
        Assert.Equal(10m, receiptLine.UnitPrice);
        Assert.Equal(20m, receiptLine.LineTotal);
    }

    [Fact]
    public async Task Offline_weighted_receipt_json_survives_restart_with_decimal_kg()
    {
        await using var harness = await Harness.CreateAsync();
        var productId = Guid.NewGuid();
        await SeedWeightedProductAsync(harness, productId, onHand: 50m);

        var saleId = Guid.NewGuid();
        await harness.Store.PersistCashSaleAndEnqueueAsync(
            CreateWeightedCommand(harness, saleId, Guid.NewGuid(), productId, qtyKg: 1.200m, pricePerKg: 120m));

        var store2 = CreateStore(harness);
        var again = await store2.GetBySaleIdAsync(saleId);
        Assert.NotNull(again);
        var line = Assert.Single(again!.Lines);
        Assert.Equal(1.200m, line.Quantity);
        Assert.Equal(120m, line.UnitPrice);
        Assert.Equal(144.00m, line.LineTotal);
        Assert.Equal(nameof(Domain.Catalog.SellingMode.ByWeight), line.SellingMode);
        Assert.Equal(nameof(Domain.Catalog.UnitOfMeasure.Kilogram), line.UnitOfMeasure);
    }

    private static LocalCashSaleCommitCommand CreateCommand(
        Harness harness,
        Guid saleId,
        Guid operationId,
        Guid productId,
        decimal qty)
    {
        var unitPrice = 10m;
        var lineTotal = PosSaleOptions.RoundMoney(unitPrice * qty);
        var lines = new List<LocalCashSaleLineSnapshot>
        {
            new(productId, "Test Item", "SKU-1", nameof(Domain.Catalog.UnitOfMeasure.Piece), unitPrice, qty, lineTotal, true, "PerItem")
        };
        var request = new CheckoutSaleRequest(
            [
                new CheckoutSaleLineRequest(
                    productId,
                    qty,
                    UnitPriceSnapshot: unitPrice,
                    UnitOfMeasure: nameof(Domain.Catalog.UnitOfMeasure.Piece),
                    SellingMode: "PerItem",
                    LineTotal: lineTotal,
                    NameSnapshot: "Test Item",
                    SkuSnapshot: "SKU-1")
            ],
            PosSaleOptions.CashPaymentMethod,
            AmountTendered: lineTotal,
            SaleId: saleId,
            ShiftId: Guid.NewGuid());

        return new LocalCashSaleCommitCommand(
            saleId,
            operationId,
            saleId.ToString("N"),
            $"OFF-TEST-{saleId.ToString("N")[..8]}",
            request.ShiftId!.Value,
            lineTotal,
            lineTotal,
            lineTotal,
            0m,
            harness.UserId,
            lines,
            request);
    }

    private static LocalCashSaleCommitCommand CreateWeightedCommand(
        Harness harness,
        Guid saleId,
        Guid operationId,
        Guid productId,
        decimal qtyKg,
        decimal pricePerKg)
    {
        var lineTotal = PosSaleOptions.RoundMoney(pricePerKg * qtyKg);
        var lines = new List<LocalCashSaleLineSnapshot>
        {
            new(
                productId,
                "Tomato",
                "TOM-1",
                nameof(Domain.Catalog.UnitOfMeasure.Kilogram),
                pricePerKg,
                qtyKg,
                lineTotal,
                true,
                nameof(Domain.Catalog.SellingMode.ByWeight))
        };
        var request = new CheckoutSaleRequest(
            [
                new CheckoutSaleLineRequest(
                    productId,
                    qtyKg,
                    UnitPriceSnapshot: pricePerKg,
                    UnitOfMeasure: nameof(Domain.Catalog.UnitOfMeasure.Kilogram),
                    SellingMode: nameof(Domain.Catalog.SellingMode.ByWeight),
                    LineTotal: lineTotal,
                    NameSnapshot: "Tomato",
                    SkuSnapshot: "TOM-1")
            ],
            PosSaleOptions.CashPaymentMethod,
            AmountTendered: lineTotal,
            SaleId: saleId,
            ShiftId: Guid.NewGuid());

        return new LocalCashSaleCommitCommand(
            saleId,
            operationId,
            saleId.ToString("N"),
            $"OFF-W-{saleId.ToString("N")[..8]}",
            request.ShiftId!.Value,
            lineTotal,
            lineTotal,
            lineTotal,
            0m,
            harness.UserId,
            lines,
            request);
    }

    private static async Task SeedProductAsync(Harness harness, Guid productId, decimal onHand, bool tracked)
    {
        var product = new PosCatalogProductDto(
            productId,
            harness.OrgId,
            "Test Item",
            null,
            "SKU-1",
            "BAR-1",
            null,
            nameof(Domain.Catalog.UnitOfMeasure.Piece),
            "PerItem",
            10m,
            PosCatalogOptions.ActiveStatus,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            IsTracked: tracked,
            OnHandQuantity: onHand,
            StockStatus: "InStock");
        await harness.Store.UpsertProductsAsync([product]);
    }

    private static async Task SeedWeightedProductAsync(Harness harness, Guid productId, decimal onHand)
    {
        var product = new PosCatalogProductDto(
            productId,
            harness.OrgId,
            "Tomato",
            null,
            "TOM-1",
            "4800001000001",
            null,
            nameof(Domain.Catalog.UnitOfMeasure.Kilogram),
            nameof(Domain.Catalog.SellingMode.ByWeight),
            150m,
            PosCatalogOptions.ActiveStatus,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            IsTracked: true,
            OnHandQuantity: onHand,
            StockStatus: "InStock");
        await harness.Store.UpsertProductsAsync([product]);
    }

    private static LocalSellingCatalogAndCashSaleStore CreateStore(Harness harness) =>
        new(
            harness.Manager,
            harness.Resolver,
            new AesGcmLocalPayloadProtector(harness.Tokens),
            new DeviceIdentityProvider(harness.Tokens),
            harness.CurrentUser);

    private static OfflineOperationQueue CreateQueue(Harness harness) =>
        new(
            harness.Manager,
            harness.Resolver,
            new AesGcmLocalPayloadProtector(harness.Tokens),
            new DeviceIdentityProvider(harness.Tokens),
            harness.CurrentUser);

    private sealed class Harness : IAsyncDisposable
    {
        public required TempRoot Root { get; init; }
        public required LocalDatabasePathResolver Resolver { get; init; }
        public required LocalContextManager Manager { get; init; }
        public required LocalSellingCatalogAndCashSaleStore Store { get; init; }
        public required OfflineOperationQueue Queue { get; init; }
        public required MemorySecureTokenStore Tokens { get; init; }
        public required CurrentUserContext CurrentUser { get; init; }
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
            var store = new LocalSellingCatalogAndCashSaleStore(
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
                CurrentUser = current,
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
        private readonly string _path = Path.Combine(Path.GetTempPath(), "exits-local-cash-sale", Guid.NewGuid().ToString("N"));

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
