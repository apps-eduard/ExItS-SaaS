using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class EnableExpirationTrackingUseCaseTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid BranchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Utc = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly ExpiryA = new(2026, 9, 15);
    private static readonly DateOnly ExpiryB = new(2026, 10, 1);

    [Fact]
    public async Task Zero_stock_enables_tracking_without_lots()
    {
        var fx = Seed(onHand: 0m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(OrgId, fx.ProductId, Actor, expirationWarningDays: 14, existingStockLots: null);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.TracksExpiration);
        Assert.Equal(14, result.Value.ExpirationWarningDays);
        Assert.Equal(0m, result.Value.OnHandQuantity);
        Assert.Empty(result.Value.Lots);
        Assert.Empty(fx.Lots.Items);
        Assert.Empty(fx.Inventory.Movements);
        Assert.Empty(fx.Lots.Movements);
    }

    [Fact]
    public async Task Single_lot_init_keeps_on_hand_unchanged_and_writes_lot_ledger_only()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            expirationWarningDays: 7,
            [new ExistingStockLotInput(10m, ExpiryA, "LOT-1")],
            expectedOnHandQuantity: 10m);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, fx.Inventory.GetOnHand(fx.ProductId));
        Assert.Empty(fx.Inventory.Movements);
        Assert.Single(fx.Lots.Items);
        Assert.Equal(10m, fx.Lots.Items[0].QuantityOnHand);
        Assert.Equal(ExpiryA, fx.Lots.Items[0].ExpirationDate);
        Assert.Single(fx.Lots.Movements);
        Assert.Equal(StockMovementType.ExpirationInitialization, fx.Lots.Movements[0].MovementType);
        Assert.Equal(10m, fx.Lots.Movements[0].QuantityEffect);
        Assert.Equal(StockMovementSourceType.Manual, fx.Lots.Movements[0].SourceType);
        Assert.Null(fx.Lots.Movements[0].StockMovementId);
    }

    [Fact]
    public async Task Multi_lot_allocation_sums_to_on_hand()
    {
        var fx = Seed(onHand: 50m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            null,
            [
                new ExistingStockLotInput(20m, ExpiryA, "A"),
                new ExistingStockLotInput(30m, ExpiryB, "B")
            ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, fx.Inventory.GetOnHand(fx.ProductId));
        Assert.Equal(2, fx.Lots.Items.Count);
        Assert.Equal(50m, fx.Lots.Items.Sum(l => l.QuantityOnHand));
        Assert.Equal(2, fx.Lots.Movements.Count);
        Assert.All(fx.Lots.Movements, m => Assert.Equal(StockMovementType.ExpirationInitialization, m.MovementType));
        Assert.Empty(fx.Inventory.Movements);
    }

    [Fact]
    public async Task Under_allocation_is_rejected()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            null,
            [new ExistingStockLotInput(7m, ExpiryA)]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpirationAllocationMismatch, result.ErrorCode);
        Assert.False(fx.Products.Items[0].TracksExpiration);
        Assert.Empty(fx.Lots.Items);
    }

    [Fact]
    public async Task Over_allocation_is_rejected()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            null,
            [new ExistingStockLotInput(12m, ExpiryA)]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpirationAllocationMismatch, result.ErrorCode);
        Assert.Empty(fx.Lots.Items);
    }

    [Fact]
    public async Task Expected_on_hand_mismatch_is_concurrency_conflict()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            null,
            [new ExistingStockLotInput(10m, ExpiryA)],
            expectedOnHandQuantity: 9m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpirationAllocationStockChanged, result.ErrorCode);
        Assert.Empty(fx.Lots.Items);
    }

    [Fact]
    public async Task Missing_expiry_is_rejected()
    {
        var fx = Seed(onHand: 5m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            null,
            [new ExistingStockLotInput(5m, ExpiryDate: null)]);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InventoryExpirationRequired, result.ErrorCode);
        Assert.Empty(fx.Lots.Items);
    }

    [Fact]
    public async Task Invalid_lot_quantity_is_rejected()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            null,
            [
                new ExistingStockLotInput(10m, ExpiryA),
                new ExistingStockLotInput(0m, ExpiryB)
            ]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpirationLotQuantityInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task Allocate_existing_rejects_zero_quantity_line()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: false);
        var service = new InventoryLotStockService(fx.Lots);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.AllocateExistingOnHandLotsAsync(
                PosOrganizationId.From(OrgId),
                CatalogProductId.From(fx.ProductId),
                [new ExistingStockLotInput(0m, ExpiryA)],
                Actor,
                Utc));
        Assert.Equal(ApplicationErrorCodes.ExpirationLotQuantityInvalid, ex.ErrorCode);
    }

    [Fact]
    public async Task Missing_lots_when_on_hand_positive_requires_initialization()
    {
        var fx = Seed(onHand: 8m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(OrgId, fx.ProductId, Actor, null, existingStockLots: []);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpirationInitializationRequired, result.ErrorCode);
    }

    [Fact]
    public async Task Update_catalog_disable_with_stock_is_rejected()
    {
        var fx = Seed(onHand: 4m, tracksExpiration: true);
        var update = new UpdateCatalogProduct(
            fx.Products,
            new NoOpUnits(),
            new NoOpCategories(),
            new NoOpBrands(),
            fx.Inventory,
            fx.UnitOfWork,
            fx.Clock);
        var product = fx.Products.Items[0];
        var result = await update.ExecuteAsync(
            OrgId,
            product.Id.Value,
            product.Name,
            UnitOfMeasures.ToCode(product.UnitOfMeasure),
            product.SellingPrice,
            tracksExpiration: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpirationDisableRequiresZeroOnHand, result.ErrorCode);
        Assert.True(fx.Products.Items[0].TracksExpiration);
    }

    [Fact]
    public async Task Update_catalog_enable_with_stock_requires_initialization_endpoint()
    {
        var fx = Seed(onHand: 4m, tracksExpiration: false);
        var update = new UpdateCatalogProduct(
            fx.Products,
            new NoOpUnits(),
            new NoOpCategories(),
            new NoOpBrands(),
            fx.Inventory,
            fx.UnitOfWork,
            fx.Clock);
        var product = fx.Products.Items[0];
        var result = await update.ExecuteAsync(
            OrgId,
            product.Id.Value,
            product.Name,
            UnitOfMeasures.ToCode(product.UnitOfMeasure),
            product.SellingPrice,
            tracksExpiration: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpirationInitializationRequired, result.ErrorCode);
        Assert.False(fx.Products.Items[0].TracksExpiration);
    }

    [Fact]
    public async Task Already_enabled_with_matching_lots_is_idempotent_success()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: true);
        var lot = InventoryLot.Create(
            PosOrganizationId.From(OrgId),
            CatalogProductId.From(fx.ProductId),
            ExpiryA,
            10m,
            Utc,
            lotNumber: "EXISTING");
        fx.Lots.Items.Add(lot);

        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            null,
            [new ExistingStockLotInput(10m, ExpiryA)]);

        Assert.True(result.IsSuccess);
        Assert.Single(fx.Lots.Items);
        Assert.Empty(fx.Lots.Movements);
    }

    [Fact]
    public async Task Already_enabled_with_lot_mismatch_is_conflict()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: true);
        fx.Lots.Items.Add(InventoryLot.Create(
            PosOrganizationId.From(OrgId),
            CatalogProductId.From(fx.ProductId),
            ExpiryA,
            3m,
            Utc));

        var result = await fx.Enable.ExecuteAsync(OrgId, fx.ProductId, Actor, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpirationTrackingAlreadyEnabled, result.ErrorCode);
    }

    [Fact]
    public async Task Already_enabled_with_zero_lots_repairs_without_changing_on_hand()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: true);
        Assert.Empty(fx.Lots.Items);

        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            expirationWarningDays: 7,
            [
                new ExistingStockLotInput(6m, ExpiryA, "A101"),
                new ExistingStockLotInput(4m, ExpiryB, "B202")
            ],
            expectedOnHandQuantity: 10m);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.TracksExpiration);
        Assert.Equal(10m, fx.Inventory.GetOnHand(fx.ProductId));
        Assert.Empty(fx.Inventory.Movements);
        Assert.Equal(2, fx.Lots.Items.Count);
        Assert.Equal(10m, fx.Lots.Items.Sum(l => l.QuantityOnHand));
        Assert.Equal(2, fx.Lots.Movements.Count);
        Assert.All(fx.Lots.Movements, m => Assert.Equal(StockMovementType.ExpirationInitialization, m.MovementType));
    }

    [Fact]
    public async Task Already_enabled_with_zero_lots_without_allocation_requires_init()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: true);

        var result = await fx.Enable.ExecuteAsync(OrgId, fx.ProductId, Actor, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpirationInitializationRequired, result.ErrorCode);
        Assert.Empty(fx.Lots.Items);
        Assert.Equal(10m, fx.Inventory.GetOnHand(fx.ProductId));
    }

    [Fact]
    public async Task Does_not_create_product_level_stock_movement()
    {
        var fx = Seed(onHand: 15m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            null,
            [new ExistingStockLotInput(15m, ExpiryA)]);

        Assert.True(result.IsSuccess);
        Assert.Empty(fx.Inventory.Movements);
        Assert.DoesNotContain(
            fx.Lots.Movements,
            m => m.MovementType is StockMovementType.OpeningStock or StockMovementType.ManualIncrease);
        Assert.Equal(15m, fx.Inventory.GetOnHand(fx.ProductId));
    }

    [Fact]
    public async Task Branch_scoped_enable_adopts_org_level_lots_for_operational_branch()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: true);
        fx.Lots.Items.Add(InventoryLot.Create(
            PosOrganizationId.From(OrgId),
            CatalogProductId.From(fx.ProductId),
            ExpiryA,
            10m,
            Utc,
            branchId: null,
            lotNumber: "LEGACY"));

        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            null,
            null,
            branchId: BranchId);

        Assert.True(result.IsSuccess);
        Assert.Single(fx.Lots.Items);
        Assert.Equal(PosBranchId.From(BranchId), fx.Lots.Items[0].BranchId);
        Assert.Equal(10m, fx.Lots.Items[0].QuantityOnHand);
    }

    [Fact]
    public async Task Branch_scoped_allocate_creates_lots_on_branch()
    {
        var fx = Seed(onHand: 10m, tracksExpiration: false);
        var result = await fx.Enable.ExecuteAsync(
            OrgId,
            fx.ProductId,
            Actor,
            expirationWarningDays: 7,
            [new ExistingStockLotInput(10m, ExpiryA, "LOT-1")],
            expectedOnHandQuantity: 10m,
            branchId: BranchId);

        Assert.True(result.IsSuccess);
        Assert.Single(fx.Lots.Items);
        Assert.Equal(PosBranchId.From(BranchId), fx.Lots.Items[0].BranchId);
    }

    private static Fixture Seed(decimal onHand, bool tracksExpiration)
    {
        var org = PosOrganizationId.From(OrgId);
        var product = CatalogProduct.Create(
            org,
            "Milk",
            UnitOfMeasure.Piece,
            50m,
            Utc,
            tracksExpiration: tracksExpiration,
            expirationWarningDays: tracksExpiration ? 7 : null);
        var products = new InMemoryCatalog { Items = { product } };
        var inventory = new InMemoryInventory();
        if (onHand > 0m || !tracksExpiration)
        {
            // Always create a tracked account when seeding stock scenarios; for zero stock
            // still create tracked account so enable-path mirrors production after inventory enable.
            var account = InventoryAccount.Rehydrate(
                InventoryAccountId.New(),
                org,
                product.Id,
                isTracked: true,
                reorderLevel: null,
                reorderQuantity: null,
                onHandQuantity: onHand,
                createdAtUtc: Utc,
                updatedAtUtc: Utc);
            inventory.Accounts.Add(account);
        }

        var lots = new InMemoryLots();
        var lotStock = new InventoryLotStockService(lots);
        var clock = new FixedClock(Utc);
        var uow = new ImmediateUnitOfWork();
        var enable = new EnableExpirationTracking(products, inventory, lots, lotStock, uow, clock);
        return new Fixture(product.Id.Value, products, inventory, lots, enable, uow, clock);
    }

    private sealed record Fixture(
        Guid ProductId,
        InMemoryCatalog Products,
        InMemoryInventory Inventory,
        InMemoryLots Lots,
        EnableExpirationTracking Enable,
        ImmediateUnitOfWork UnitOfWork,
        FixedClock Clock);

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

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(p => p.OrganizationId == organizationId && p.Id == productId));

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            Items.Add(product);
            return Task.CompletedTask;
        }

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId,
            string normalizedSku,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId,
            string barcode,
            CancellationToken cancellationToken = default) =>
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
    }

    private sealed class InMemoryInventory : IInventoryRepository
    {
        public List<InventoryAccount> Accounts { get; } = [];
        public List<StockMovement> Movements { get; } = [];

        public decimal GetOnHand(Guid productId) =>
            Accounts.FirstOrDefault(a => a.ProductId.Value == productId)?.OnHandQuantity ?? 0m;

        public Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.FirstOrDefault(a => a.OrganizationId == organizationId && a.ProductId == productId));

        public Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
        {
            Accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>(
                Accounts.Where(a => a.OrganizationId == organizationId && productIds.Any(id => id == a.ProductId)).ToList());

        public Task ExecuteWithProductReservationLocksAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default) =>
            action(Accounts.Where(a => a.OrganizationId == organizationId).ToList(), cancellationToken);

        public Task<StockMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            StockMovementId movementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.FirstOrDefault(m => m.OrganizationId == organizationId && m.Id == movementId));

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            InventoryAccountFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(
            PosOrganizationId organizationId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasAnyMovementAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m => m.OrganizationId == organizationId && m.ProductId == productId));

        public Task<bool> HasOpeningStockAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            StockMovementFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<decimal> SumMovementEffectsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(
            PosOrganizationId organizationId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasStockCountVarianceAsync(
            PosOrganizationId organizationId,
            StockCountId stockCountId,
            CatalogProductId productId,
            StockMovementType movementType,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasSaleDeductionAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasCustomerOrderDeductionAsync(
            PosOrganizationId organizationId,
            CustomerOrderId orderId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasSaleVoidRestorationAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasPurchaseReceiptAsync(
            PosOrganizationId organizationId,
            GoodsReceiptId goodsReceiptId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasDirectPurchaseReceiptAsync(
            PosOrganizationId organizationId,
            DirectPurchaseReceiptId receiptId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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
        public Task<bool> HasSaleReturnRestockAsync(
            PosOrganizationId organizationId,
            SaleReturnId saleReturnId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasInventoryTransferMovementAsync(
            PosOrganizationId organizationId,
            InventoryTransferId transferId,
            CatalogProductId productId,
            StockMovementType movementType,
            InventoryLotId? lotId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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

        public Task<InventoryLot?> GetByIdAsync(
            PosOrganizationId organizationId,
            InventoryLotId lotId,
            CancellationToken cancellationToken = default) =>
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
            CancellationToken cancellationToken = default)
        {
            var query = Items.Where(l => l.OrganizationId == organizationId && l.ProductId == productId);
            if (branchId is not null)
            {
                query = query.Where(l => l.BranchId == branchId);
            }

            if (!includeDepleted)
            {
                query = query.Where(l => l.QuantityOnHand > 0m);
            }

            return Task.FromResult<IReadOnlyList<InventoryLot>>(query.ToList());
        }

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
            throw new NotSupportedException();

        public Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(
            PosOrganizationId organizationId,
            DateOnly today,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AdoptOrgLevelLotsForBranchAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId branchId,
            CancellationToken cancellationToken = default)
        {
            var hasBranchLots = Items.Any(l =>
                l.OrganizationId == organizationId
                && l.ProductId == productId
                && l.BranchId == branchId
                && l.QuantityOnHand > 0m);
            if (hasBranchLots)
            {
                return Task.CompletedTask;
            }

            for (var i = 0; i < Items.Count; i++)
            {
                var lot = Items[i];
                if (lot.OrganizationId != organizationId
                    || lot.ProductId != productId
                    || lot.BranchId is not null
                    || lot.QuantityOnHand <= 0m)
                {
                    continue;
                }

                Items[i] = InventoryLot.Rehydrate(
                    lot.Id,
                    lot.OrganizationId,
                    lot.ProductId,
                    branchId,
                    lot.LotNumber,
                    lot.NormalizedLotNumber,
                    lot.ExpirationDate,
                    lot.QuantityOnHand,
                    lot.CreatedAtUtc,
                    lot.UpdatedAtUtc);
            }

            return Task.CompletedTask;
        }

        public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken = default)
        {
            Items.Add(lot);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(InventoryLot lot, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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
                Movements.Where(m =>
                        m.OrganizationId == organizationId
                        && m.SourceId == sourceId
                        && m.MovementType == movementType)
                    .ToList());
    }

    private sealed class NoOpUnits : ICatalogProductUnitRepository
    {
        public Task<CatalogProductUnit?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductUnitId unitId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProductUnit?>(null);

        public Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductUnit>>([]);

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>>(
                new Dictionary<Guid, IReadOnlyList<CatalogProductUnit>>());

        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ReplaceActiveUnitsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpCategories : IProductCategoryRepository
    {
        public Task<ProductCategory?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductCategoryId categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductCategory?>(null);

        public Task<ProductCategory?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductCategory?>(null);

        public Task<ProductCategory?> FindActiveBySourceGlobalCategoryIdAsync(
            PosOrganizationId organizationId,
            Guid sourceGlobalCategoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductCategory?>(null);

        public Task<(IReadOnlyList<ProductCategory> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            ProductCategoryStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductCategory>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<ProductCategoryId> categoryIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductCategory>>([]);

        public Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpBrands : IProductBrandRepository
    {
        public Task<ProductBrand?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductBrandId brandId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductBrand?>(null);

        public Task<ProductBrand?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductBrand?>(null);

        public Task<(IReadOnlyList<ProductBrand> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            ProductBrandStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductBrand>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<ProductBrandId> brandIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductBrand>>([]);

        public Task AddAsync(ProductBrand brand, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(ProductBrand brand, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}