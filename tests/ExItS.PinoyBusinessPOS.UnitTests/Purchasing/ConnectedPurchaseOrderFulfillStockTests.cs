using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class ConnectedPurchaseOrderFulfillStockTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId SupplierOrg =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid SupplierBranchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Fulfill_reduces_tracked_branch_stock()
    {
        var productId = CatalogProductId.New();
        var (order, relationship, inventory, balances, products, units, service) =
            Build(productId, trackedOnHand: 10m, fulfillmentQty: 3m);

        await service.ApplyAsync(order, relationship, Actor, Now);

        Assert.Equal(7m, inventory.Account.OnHandQuantity);
        Assert.Single(inventory.Movements);
        Assert.Equal(StockMovementType.ConnectedPurchaseFulfillment, inventory.Movements[0].MovementType);
        Assert.Equal(-3m, inventory.Movements[0].QuantityEffect);
        Assert.Equal(7m, balances.Get(SupplierBranchId, productId)!.OnHandQuantity);
    }

    [Fact]
    public async Task Fulfill_retry_does_not_double_deduct()
    {
        var productId = CatalogProductId.New();
        var (order, relationship, inventory, balances, _, _, service) =
            Build(productId, trackedOnHand: 10m, fulfillmentQty: 3m);

        await service.ApplyAsync(order, relationship, Actor, Now);
        await service.ApplyAsync(order, relationship, Actor, Now.AddMinutes(1));

        Assert.Equal(7m, inventory.Account.OnHandQuantity);
        Assert.Single(inventory.Movements);
        Assert.Equal(7m, balances.Get(SupplierBranchId, productId)!.OnHandQuantity);
    }

    [Fact]
    public async Task Insufficient_supplier_stock_rejects()
    {
        var productId = CatalogProductId.New();
        var (order, relationship, _, _, _, _, service) =
            Build(productId, trackedOnHand: 2m, fulfillmentQty: 5m, productName: "Bath Soap Bar");

        var ex = await Assert.ThrowsAsync<Domain.Common.DomainException>(() =>
            service.ApplyAsync(order, relationship, Actor, Now));

        Assert.Equal(ConnectedSupplierErrorCodes.InsufficientSupplierStock, ex.ErrorCode);
        Assert.Equal("Bath Soap Bar has only 2 available; 5 required.", ex.Message);
    }

    [Fact]
    public async Task Untracked_supplier_product_is_not_auto_tracked_or_deducted()
    {
        var productId = CatalogProductId.New();
        var (order, relationship, inventory, balances, _, _, service) =
            Build(productId, trackedOnHand: null, fulfillmentQty: 4m);

        await service.ApplyAsync(order, relationship, Actor, Now);

        Assert.False(inventory.Account.IsTracked);
        Assert.Empty(inventory.Movements);
        Assert.Null(balances.Get(SupplierBranchId, productId));
    }

    [Fact]
    public void Accept_and_prepare_domain_still_do_not_mutate_inventory()
    {
        var relationship = ConnectedSupplierRelationship.Request(
            Buyer,
            SupplierOrg,
            Now,
            supplierBranchId: SupplierBranchId,
            supplierBranchName: "Main Branch");
        relationship.Approve(Now.AddMinutes(1));
        var line = ConnectedPurchaseOrderLine.Create(CatalogProductId.New(), "Item", "SKU", 2m, 10m, "Piece");
        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-1",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [line],
            Now.AddMinutes(2));

        order.Accept(Now.AddMinutes(3));
        Assert.Equal(ConnectedPurchaseOrderStatus.Accepted, order.Status);

        order.StartPreparing(Now.AddMinutes(4));
        Assert.Equal(ConnectedPurchaseOrderStatus.Preparing, order.Status);
    }

    private static (
        ConnectedPurchaseOrder Order,
        ConnectedSupplierRelationship Relationship,
        FulfillInventoryStub Inventory,
        FulfillBalances Balances,
        FulfillProducts Products,
        FulfillUnits Units,
        ConnectedPurchaseOrderFulfillStock Service)
        Build(CatalogProductId productId, decimal? trackedOnHand, decimal fulfillmentQty, string productName = "Item")
    {
        var relationship = ConnectedSupplierRelationship.Request(
            Buyer,
            SupplierOrg,
            Now,
            supplierBranchId: SupplierBranchId,
            supplierBranchName: "Main Branch");
        relationship.Approve(Now.AddMinutes(1));
        var line = ConnectedPurchaseOrderLine.Create(productId, productName, "SKU", fulfillmentQty, 12m, "Piece");
        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-FULFILL",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [line],
            Now.AddMinutes(2));
        order.Accept(Now.AddMinutes(3));

        var inventory = new FulfillInventoryStub();
        if (trackedOnHand is decimal onHand)
        {
            inventory.Seed(InventoryAccount.Rehydrate(
                InventoryAccountId.From(productId.Value),
                SupplierOrg,
                productId,
                isTracked: true,
                reorderLevel: null,
                reorderQuantity: null,
                onHandQuantity: onHand,
                createdAtUtc: Now,
                updatedAtUtc: Now));
        }
        else
        {
            inventory.Seed(InventoryAccount.CreateUntracked(SupplierOrg, productId, Now));
        }

        var balances = new FulfillBalances();
        if (trackedOnHand is decimal branchOnHand)
        {
            balances.Seed(InventoryBranchBalance.Create(
                SupplierOrg,
                PosBranchId.From(SupplierBranchId),
                productId,
                onHandQuantity: branchOnHand,
                Now));
        }

        var product = CatalogProduct.Create(SupplierOrg, productName, UnitOfMeasure.Piece, 20m, Now, id: productId);
        var products = new FulfillProducts(product);
        var units = new FulfillUnits();
        var service = new ConnectedPurchaseOrderFulfillStock(
            inventory,
            products,
            units,
            balances,
            new BranchInventoryMutationService());

        return (order, relationship, inventory, balances, products, units, service);
    }

    private sealed class FulfillInventoryStub : CostResolverInventoryStub
    {
        private readonly Dictionary<Guid, InventoryAccount> _accounts = new();
        private readonly HashSet<(Guid OrderId, Guid ProductId)> _fulfillments = new();
        public List<StockMovement> Movements { get; } = [];
        public InventoryAccount Account => _accounts.Values.Single();

        public void Seed(InventoryAccount account) => _accounts[account.ProductId.Value] = account;

        public override Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _accounts.TryGetValue(productId.Value, out var a) && a.OrganizationId == organizationId
                    ? a
                    : null);

        public override Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>(
                productIds
                    .Select(id => _accounts.TryGetValue(id.Value, out var a) && a.OrganizationId == organizationId ? a : null)
                    .Where(a => a is not null)
                    .Cast<InventoryAccount>()
                    .ToList());

        public override Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
        {
            _accounts[account.ProductId.Value] = account;
            return Task.CompletedTask;
        }

        public override Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            Movements.Add(movement);
            if (movement.SourceId is Guid sourceId
                && movement.MovementType == StockMovementType.ConnectedPurchaseFulfillment)
            {
                _fulfillments.Add((sourceId, movement.ProductId.Value));
            }

            return Task.CompletedTask;
        }

        public override Task ExecuteWithProductReservationLocksAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default) =>
            action(
                productIds
                    .Select(id => _accounts.TryGetValue(id.Value, out var a) ? a : null)
                    .Where(a => a is not null)
                    .Cast<InventoryAccount>()
                    .ToList(),
                cancellationToken);

        public override Task<bool> HasConnectedPurchaseFulfillmentAsync(
            PosOrganizationId organizationId,
            ConnectedPurchaseOrderId connectedPurchaseOrderId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_fulfillments.Contains((connectedPurchaseOrderId.Value, productId.Value)));
    }

    private sealed class FulfillBalances : IInventoryBranchBalanceRepository
    {
        private readonly Dictionary<(Guid Branch, Guid Product), InventoryBranchBalance> _rows = new();

        public void Seed(InventoryBranchBalance balance) =>
            _rows[(balance.BranchId.Value, balance.ProductId.Value)] = balance;

        public InventoryBranchBalance? Get(Guid branchId, CatalogProductId productId) =>
            _rows.TryGetValue((branchId, productId.Value), out var row) ? row : null;

        public Task<InventoryBranchBalance?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Get(branchId.Value, productId));

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryBranchBalance>>(
                _rows.Values.Where(r => productIds.Any(p => p.Value == r.ProductId.Value)).ToList());

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
        {
            _rows[(balance.BranchId.Value, balance.ProductId.Value)] = balance;
            return Task.CompletedTask;
        }
    }

    private sealed class FulfillProducts(CatalogProduct product) : ICatalogProductRepository
    {
        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(product.Id == productId && product.OrganizationId == organizationId ? product : null);

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
                productIds.Any(id => id == product.Id) ? [product] : []);

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId,
            Guid platformGlobalProductId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task AddAsync(CatalogProduct productToAdd, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(CatalogProduct productToUpdate, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FulfillUnits : ICatalogProductUnitRepository
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
                productIds.ToDictionary(p => p.Value, _ => (IReadOnlyList<CatalogProductUnit>)[]));

        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ReplaceActiveUnitsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
