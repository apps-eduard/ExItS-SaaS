using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class DirectPurchaseReceiptUseCaseTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid RemoteBranch = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly DateTimeOffset Utc = new(2026, 8, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_with_supplier_ad_hoc_and_no_source_succeeds()
    {
        var fx = await SeedAsync();
        var withSupplier = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(Utc.UtcDateTime),
                [new CreateDirectPurchaseReceiptLineRequest(fx.CokeId, 2m, 12.5m)],
                SupplierId: fx.SupplierId),
            Actor,
            RemoteBranch);
        Assert.True(withSupplier.IsSuccess);
        Assert.Equal(fx.SupplierId, withSupplier.Value!.SupplierId);
        Assert.Equal("Acme Trading", withSupplier.Value.SourceNameSnapshot);
        Assert.Equal(25m, withSupplier.Value.TotalCost);

        var adHoc = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(Utc.UtcDateTime),
                [new CreateDirectPurchaseReceiptLineRequest(fx.CokeId, 1m, 10m)],
                SourceName: "Wet market"),
            Actor,
            RemoteBranch);
        Assert.True(adHoc.IsSuccess);
        Assert.Null(adHoc.Value!.SupplierId);
        Assert.Equal("Wet market", adHoc.Value.SourceNameSnapshot);

        var noSource = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(Utc.UtcDateTime),
                [new CreateDirectPurchaseReceiptLineRequest(fx.CokeId, 1m, 10m)]),
            Actor,
            RemoteBranch);
        Assert.True(noSource.IsSuccess);
        Assert.Null(noSource.Value!.SourceNameSnapshot);
    }

    [Fact]
    public async Task Multi_line_increases_stock_atomically_and_records_DirectPurchase_movements()
    {
        var fx = await SeedAsync(cokeOnHand: 10m, spriteOnHand: 5m);
        var result = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(Utc.UtcDateTime),
                [
                    new CreateDirectPurchaseReceiptLineRequest(fx.CokeId, 3m, 10m),
                    new CreateDirectPurchaseReceiptLineRequest(fx.SpriteId, 2m, 8m)
                ]),
            Actor,
            RemoteBranch);
        Assert.True(result.IsSuccess);
        Assert.Equal(13m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(7m, fx.Inventory.GetOnHand(fx.SpriteId));
        Assert.Equal(2, fx.Inventory.Movements.Count(m => m.MovementType == StockMovementType.DirectPurchaseReceipt));
        Assert.All(
            fx.Inventory.Movements.Where(m => m.MovementType == StockMovementType.DirectPurchaseReceipt),
            m =>
            {
                Assert.Equal(StockMovementSourceType.DirectPurchase, m.SourceType);
                Assert.Equal(result.Value!.DirectPurchaseReceiptId, m.SourceId);
                Assert.NotNull(m.UnitCost);
                Assert.True(m.UnitCost > 0m);
            });
        Assert.Contains(
            fx.Inventory.Movements,
            m => m.ProductId.Value == fx.CokeId && m.UnitCost == 10m && m.QuantityEffect == 3m);
        Assert.Contains(
            fx.Inventory.Movements,
            m => m.ProductId.Value == fx.SpriteId && m.UnitCost == 8m && m.QuantityEffect == 2m);
    }

    [Fact]
    public async Task Invalid_line_qty_or_cost_fails_without_stock_change()
    {
        var fx = await SeedAsync(cokeOnHand: 4m);
        var before = fx.Inventory.GetOnHand(fx.CokeId);
        var zeroQty = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(Utc.UtcDateTime),
                [new CreateDirectPurchaseReceiptLineRequest(fx.CokeId, 0m, 10m)]),
            Actor,
            RemoteBranch);
        Assert.Equal(DomainErrorCodes.InvalidDirectPurchaseQuantity, zeroQty.ErrorCode);
        Assert.Equal(before, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.DoesNotContain(fx.Inventory.Movements, m => m.MovementType == StockMovementType.DirectPurchaseReceipt);

        var zeroCost = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(Utc.UtcDateTime),
                [new CreateDirectPurchaseReceiptLineRequest(fx.CokeId, 1m, 0m)]),
            Actor,
            RemoteBranch);
        Assert.Equal(DomainErrorCodes.InvalidDirectPurchaseUnitCost, zeroCost.ErrorCode);
        Assert.Equal(before, fx.Inventory.GetOnHand(fx.CokeId));
    }

    [Fact]
    public async Task Cross_tenant_product_and_supplier_are_rejected()
    {
        var fx = await SeedAsync();
        var otherProduct = CatalogProduct.Create(
            PosOrganizationId.From(OrgB),
            "Other",
            UnitOfMeasure.Piece,
            1m,
            Utc);
        fx.Products.Items.Add(otherProduct);

        var productCross = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(Utc.UtcDateTime),
                [new CreateDirectPurchaseReceiptLineRequest(otherProduct.Id.Value, 1m, 5m)]),
            Actor,
            RemoteBranch);
        Assert.Equal(ApplicationErrorCodes.PurchaseProductNotFound, productCross.ErrorCode);

        var supplierCross = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(Utc.UtcDateTime),
                [new CreateDirectPurchaseReceiptLineRequest(fx.CokeId, 1m, 5m)],
                SupplierId: Guid.NewGuid()),
            Actor,
            RemoteBranch);
        Assert.Equal(ApplicationErrorCodes.SupplierNotFound, supplierCross.ErrorCode);
    }

    [Fact]
    public async Task Idempotency_key_replays_without_double_stock_increase()
    {
        var fx = await SeedAsync(cokeOnHand: 10m);
        var request = new CreateDirectPurchaseReceiptRequest(
            DateOnly.FromDateTime(Utc.UtcDateTime),
            [new CreateDirectPurchaseReceiptLineRequest(fx.CokeId, 4m, 9m)],
            IdempotencyKey: "dpr-key-1");

        var first = await fx.Create.ExecuteAsync(OrgA, request, Actor, RemoteBranch);
        Assert.True(first.IsSuccess);
        Assert.Equal(14m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(1, fx.Inventory.Movements.Count(m => m.MovementType == StockMovementType.DirectPurchaseReceipt));

        var second = await fx.Create.ExecuteAsync(OrgA, request, Actor, RemoteBranch);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.DirectPurchaseReceiptId, second.Value!.DirectPurchaseReceiptId);
        Assert.Equal(14m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(1, fx.Inventory.Movements.Count(m => m.MovementType == StockMovementType.DirectPurchaseReceipt));
    }

    [Fact]
    public async Task Untracked_product_is_rejected()
    {
        var fx = await SeedAsync(trackCoke: false);
        var result = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(Utc.UtcDateTime),
                [new CreateDirectPurchaseReceiptLineRequest(fx.CokeId, 1m, 5m)]),
            Actor,
            RemoteBranch);
        Assert.Equal(DomainErrorCodes.InventoryNotTracked, result.ErrorCode);
    }

    private static async Task<Fixture> SeedAsync(
        decimal cokeOnHand = 0m,
        decimal spriteOnHand = 0m,
        bool trackCoke = true)
    {
        var fx = new Fixture();
        await fx.AddProductAsync(fx.CokeId, "Coke", cokeOnHand, trackCoke);
        if (spriteOnHand > 0m)
        {
            await fx.AddProductAsync(fx.SpriteId, "Sprite", spriteOnHand, track: true);
        }

        fx.Suppliers.Items.Add(Supplier.Create(
            PosOrganizationId.From(OrgA),
            "SUP-000001",
            "Acme Trading",
            Utc,
            id: SupplierId.From(fx.SupplierId)));
        return fx;
    }

    private sealed class Fixture
    {
        public Guid CokeId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public Guid SpriteId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public Guid SupplierId { get; } = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        public InMemoryCatalog Products { get; } = new();
        public InMemoryInventory Inventory { get; } = new();
        public InMemoryReceipts Receipts { get; } = new();
        public InMemorySuppliers Suppliers { get; } = new();
        public InMemoryLots Lots { get; } = new();
        public ImmediateUnitOfWork UnitOfWork { get; } = new();
        public FixedClock Clock { get; } = new(Utc);
        public InMemoryBranchBalances BranchBalances { get; } = new();
        public FixedPrimaryBranches Branches { get; } = new(RemoteBranch);
        public CreateDirectPurchaseReceipt Create { get; }

        public Fixture()
        {
            Create = new CreateDirectPurchaseReceipt(
                Receipts,
                Products,
                Suppliers,
                Inventory,
                BranchBalances,
                new InventoryLotStockService(Lots),
                new BranchInventoryMutationService(),
                UnitOfWork,
                new CreateSupplierPayableFromReceipt(new NoOpSupplierPayableRepository()),
                Clock,
                Branches);
        }

        public Task AddProductAsync(Guid productId, string name, decimal opening, bool track)
        {
            var product = CatalogProduct.Create(
                PosOrganizationId.From(OrgA),
                name,
                UnitOfMeasure.Piece,
                10m,
                Utc,
                id: CatalogProductId.From(productId));
            Products.Items.Add(product);
            if (track)
            {
                var account = InventoryAccount.CreateUntracked(
                    PosOrganizationId.From(OrgA),
                    CatalogProductId.From(productId),
                    Utc);
                var movement = account.Enable(opening, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);
                Inventory.Accounts.Add(account);
                if (movement is not null)
                {
                    Inventory.Movements.Add(movement);
                }
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FixedPrimaryBranches(Guid primaryId) : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(id => id, id => id.ToString("D")));

        public Task<Guid?> GetPrimaryBranchIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(primaryId);
    }

    private sealed class InMemoryBranchBalances : IInventoryBranchBalanceRepository
    {
        public List<InventoryBranchBalance> Items { get; } = [];

        public Task<InventoryBranchBalance?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(b =>
                b.OrganizationId == organizationId && b.BranchId == branchId && b.ProductId == productId));

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryBranchBalance>>(
                Items.Where(b => b.OrganizationId == organizationId && productIds.Contains(b.ProductId)).ToList());

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(b =>
                b.OrganizationId == balance.OrganizationId
                && b.BranchId == balance.BranchId
                && b.ProductId == balance.ProductId);
            Items.Add(balance);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class ImmediateUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class InMemoryCatalog : ICatalogProductRepository
    {
        public List<CatalogProduct> Items { get; } = [];

        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(p => p.OrganizationId == organizationId && p.Id == productId));

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                Items.Where(p => p.OrganizationId == organizationId && productIds.Any(id => id == p.Id)).ToList());

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId,
            Guid platformGlobalProductId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0, 0));

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid? CategoryId, int Count)>>([]);

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            Items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemorySuppliers : ISupplierRepository
    {
        public List<Supplier> Items { get; } = [];

        public Task<Supplier?> GetByIdAsync(PosOrganizationId organizationId, SupplierId supplierId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(s => s.OrganizationId == organizationId && s.Id == supplierId));

        public Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Supplier?> FindActiveByNormalizedNameAsync(PosOrganizationId organizationId, string normalizedName, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedEmailAsync(PosOrganizationId organizationId, string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedMobileAsync(PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedTaxAsync(PosOrganizationId organizationId, string normalizedTax, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<string> AllocateNextSupplierCodeAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult("SUP-0002");

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            Items.Add(supplier);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> supplierIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    }

    private sealed class InMemoryReceipts : IDirectPurchaseReceiptRepository
    {
        private readonly List<DirectPurchaseReceipt> _items = [];
        private long _sequence;

        public Task<DirectPurchaseReceipt?> GetByIdAsync(
            PosOrganizationId organizationId,
            DirectPurchaseReceiptId receiptId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == receiptId));

        public Task<DirectPurchaseReceipt?> FindByIdempotencyKeyAsync(
            PosOrganizationId organizationId,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r =>
                r.OrganizationId == organizationId
                && string.Equals(r.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)));

        public Task<(IReadOnlyList<DirectPurchaseReceipt> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            DirectPurchaseReceiptFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = _items.Where(r => r.OrganizationId == organizationId).AsEnumerable();
            var list = query.Skip(skip).Take(take).ToList();
            return Task.FromResult<(IReadOnlyList<DirectPurchaseReceipt>, int)>((list, query.Count()));
        }

        public Task AddAsync(DirectPurchaseReceipt receipt, CancellationToken cancellationToken = default)
        {
            _items.Add(receipt);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(DirectPurchaseReceipt receipt, CancellationToken cancellationToken = default)
        {
            var idx = _items.FindIndex(r => r.Id == receipt.Id);
            if (idx >= 0)
            {
                _items[idx] = receipt;
            }

            return Task.CompletedTask;
        }

        public Task<string> AllocateNextNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default)
        {
            _sequence++;
            return Task.FromResult(DirectPurchaseReceiptNumbers.Format(businessDateUtc, _sequence));
        }
    }

    private sealed class InMemoryInventory : IInventoryRepository
    {
        public List<InventoryAccount> Accounts { get; } = [];
        public List<StockMovement> Movements { get; } = [];

        public decimal GetOnHand(Guid productId) =>
            Accounts.FirstOrDefault(a => a.ProductId.Value == productId)?.OnHandQuantity ?? 0m;

        public Task<InventoryAccount?> GetByProductIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.FirstOrDefault(a => a.OrganizationId == organizationId && a.ProductId == productId));

        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>(
                Accounts.Where(a => a.OrganizationId == organizationId && productIds.Any(id => id == a.ProductId)).ToList());

        public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
        {
            Accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ExecuteWithProductReservationLocksAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default) =>
            action(Accounts.Where(a => a.OrganizationId == organizationId).ToList(), cancellationToken);

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<StockMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            StockMovementId movementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StockMovement?>(
                Movements.FirstOrDefault(m =>
                    m.OrganizationId == organizationId && m.Id == movementId));

        public Task<bool> HasDirectPurchaseReceiptAsync(
            PosOrganizationId organizationId,
            DirectPurchaseReceiptId receiptId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == receiptId.Value
                && m.MovementType == StockMovementType.DirectPurchaseReceipt));

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, InventoryAccountFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasAnyMovementAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasOpeningStockAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(PosOrganizationId organizationId, CatalogProductId productId, StockMovementFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> SumMovementEffectsAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasStockCountVarianceAsync(PosOrganizationId organizationId, StockCountId stockCountId, CatalogProductId productId, StockMovementType movementType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleDeductionAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasCustomerOrderDeductionAsync(PosOrganizationId organizationId, CustomerOrderId orderId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleVoidRestorationAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPurchaseReceiptAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasStockUseAsync(PosOrganizationId organizationId, StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasStockUseVoidRestorationAsync(PosOrganizationId organizationId, StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasProductionMaterialConsumptionAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasProductionMaterialRestorationAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasProductionOutputAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasProductionOutputReversalAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasWasteLossAsync(PosOrganizationId organizationId, WasteLossId wasteLossId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasWasteLossVoidRestorationAsync(PosOrganizationId organizationId, WasteLossId wasteLossId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

    public Task<bool> HasPurchaseReceiptReversalAsync(
        PosOrganizationId organizationId,
        GoodsReceiptId goodsReceiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> HasDirectPurchaseReceiptReversalAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptId receiptId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

        public Task<decimal?> GetLatestAcquisitionUnitCostAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<decimal?>(null);
        public Task<bool> HasSaleReturnRestockAsync(PosOrganizationId organizationId, SaleReturnId saleReturnId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasInventoryTransferMovementAsync(PosOrganizationId organizationId, InventoryTransferId transferId, CatalogProductId productId, StockMovementType movementType, InventoryLotId? lotId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async Task<IReadOnlyDictionary<Guid, decimal?>> GetLatestAcquisitionUnitCostsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<Guid, decimal?>();
            foreach (var productId in productIds)
            {
                var cost = await GetLatestAcquisitionUnitCostAsync(organizationId, productId, cancellationToken)
                    .ConfigureAwait(false);
                if (cost is not null)
                {
                    result[productId.Value] = cost;
                }
            }

            return result;
        }
    }

    private sealed class InMemoryLots : IInventoryLotRepository
    {
        public List<InventoryLot> Items { get; } = [];
        public List<InventoryLotMovement> Movements { get; } = [];

        public Task<InventoryLot?> GetByIdAsync(PosOrganizationId organizationId, InventoryLotId lotId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(l => l.OrganizationId == organizationId && l.Id == lotId));

        public Task<InventoryLot?> FindAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            DateOnly expirationDate,
            string normalizedLotNumber,
            PosBranchId? branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(l =>
                l.OrganizationId == organizationId
                && l.ProductId == productId
                && l.ExpirationDate == expirationDate
                && l.NormalizedLotNumber == normalizedLotNumber
                && l.BranchId == branchId));

        public Task<IReadOnlyList<InventoryLot>> ListOnHandAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId? branchId,
            bool includeDepleted,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryLot>>(Items.Where(l => l.OrganizationId == organizationId && l.ProductId == productId).ToList());

        public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListPagedAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId? branchId,
            bool includeDepleted,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListExpiringPagedAsync(
            PosOrganizationId organizationId,
            PosBranchId? branchId,
            DateOnly expireOnOrBefore,
            DateOnly? expireOnOrAfter,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<InventoryLot>, int)>(([], 0));

        public Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(
            PosOrganizationId organizationId,
            DateOnly today,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0));

        public Task AdoptOrgLevelLotsForBranchAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken = default)
        {
            Items.Add(lot);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(InventoryLot lot, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddMovementAsync(InventoryLotMovement movement, CancellationToken cancellationToken = default)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<bool> HasMovementAsync(
            PosOrganizationId organizationId,
            Guid sourceId,
            InventoryLotId lotId,
            StockMovementType movementType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.SourceId == sourceId
                && m.LotId == lotId
                && m.MovementType == movementType));

        public Task<IReadOnlyList<InventoryLotMovement>> ListBySourceAsync(
            PosOrganizationId organizationId,
            Guid sourceId,
            StockMovementType movementType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryLotMovement>>(
                Movements.Where(m => m.OrganizationId == organizationId && m.SourceId == sourceId && m.MovementType == movementType).ToList());
    }

    private sealed class NoOpSupplierPayableRepository : ISupplierPayableRepository
    {
        public Task<SupplierPayable?> GetByIdAsync(
            PosOrganizationId organizationId,
            SupplierPayableId payableId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SupplierPayable?>(null);

        public Task<SupplierPayable?> FindBySourceAsync(
            PosOrganizationId organizationId,
            SupplierPayableSourceType sourceType,
            Guid sourceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SupplierPayable?>(null);

        public Task<(IReadOnlyList<SupplierPayable> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierPayableFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<SupplierPayable>, int)>((Array.Empty<SupplierPayable>(), 0));

        public Task<IReadOnlyList<SupplierPayablePayment>> ListPaymentsAsync(
            PosOrganizationId organizationId,
            SupplierPayableId payableId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierPayablePayment>>(Array.Empty<SupplierPayablePayment>());

        public Task AddAsync(SupplierPayable payable, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(SupplierPayable payable, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SupplierPayableSummaryTotals> GetSupplierSummaryAsync(
            PosOrganizationId organizationId,
            SupplierId supplierId,
            DateOnly asOfDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SupplierPayableSummaryTotals(0m, 0m, 0));
    }
}