using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class ConnectedPurchaseOrderSupplierStockTests
{
    private static readonly PosOrganizationId SupplierOrg =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid SupplierBranchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Untracked_product_is_not_blocked()
    {
        var productId = CatalogProductId.New();
        var relationship = ActiveRelationship();
        var inventory = new StockInventoryStub();
        inventory.Seed(InventoryAccount.CreateUntracked(SupplierOrg, productId, Now));
        var balances = new InMemoryBalances();

        var result = await ConnectedPurchaseOrderSupplierStock.ValidateDemandsAsync(
            relationship,
            [new(productId.Value, 50m, 1m, "Loose Item")],
            inventory,
            balances,
            branches: null,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Out_of_stock_tracked_product_is_rejected()
    {
        var productId = CatalogProductId.New();
        var relationship = ActiveRelationship();
        var inventory = new StockInventoryStub();
        inventory.Seed(InventoryAccount.Rehydrate(
            InventoryAccountId.From(productId.Value),
            SupplierOrg,
            productId,
            isTracked: true,
            reorderLevel: null,
            reorderQuantity: null,
            onHandQuantity: 0m,
            createdAtUtc: Now,
            updatedAtUtc: Now));
        var balances = new InMemoryBalances();
        balances.Seed(InventoryBranchBalance.Create(
            SupplierOrg,
            PosBranchId.From(SupplierBranchId),
            productId,
            onHandQuantity: 0m,
            Now));

        var result = await ConnectedPurchaseOrderSupplierStock.ValidateDemandsAsync(
            relationship,
            [new(productId.Value, 1m, 1m, "Bath Soap Bar")],
            inventory,
            balances,
            branches: null,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.OutOfStockSupplierProduct, result.ErrorCode);
        Assert.Equal("Bath Soap Bar is out of stock.", result.ErrorMessage);
    }

    [Fact]
    public async Task Qty_exceeding_available_is_rejected_with_details()
    {
        var productId = CatalogProductId.New();
        var relationship = ActiveRelationship();
        var inventory = new StockInventoryStub();
        inventory.Seed(InventoryAccount.Rehydrate(
            InventoryAccountId.From(productId.Value),
            SupplierOrg,
            productId,
            isTracked: true,
            reorderLevel: null,
            reorderQuantity: null,
            onHandQuantity: 10m,
            createdAtUtc: Now,
            updatedAtUtc: Now));
        var balances = new InMemoryBalances();
        balances.Seed(InventoryBranchBalance.Create(
            SupplierOrg,
            PosBranchId.From(SupplierBranchId),
            productId,
            onHandQuantity: 10m,
            Now));

        var result = await ConnectedPurchaseOrderSupplierStock.ValidateDemandsAsync(
            relationship,
            [new(productId.Value, 15m, 1m, "Biscuit Pack")],
            inventory,
            balances,
            branches: null,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.InsufficientSupplierStock, result.ErrorCode);
        Assert.Equal("Biscuit Pack has only 10 available; 15 was requested.", result.ErrorMessage);
        Assert.Equal("10", result.ErrorDetails!["availableQuantity"]);
        Assert.Equal("15", result.ErrorDetails["requestedQuantity"]);
    }

    [Fact]
    public async Task Uses_supplier_branch_stock_not_other_branch()
    {
        var productId = CatalogProductId.New();
        var relationship = ActiveRelationship();
        var inventory = new StockInventoryStub();
        inventory.Seed(InventoryAccount.Rehydrate(
            InventoryAccountId.From(productId.Value),
            SupplierOrg,
            productId,
            isTracked: true,
            reorderLevel: null,
            reorderQuantity: null,
            onHandQuantity: 100m,
            createdAtUtc: Now,
            updatedAtUtc: Now));
        var balances = new InMemoryBalances();
        balances.Seed(InventoryBranchBalance.Create(
            SupplierOrg,
            PosBranchId.From(OtherBranchId),
            productId,
            onHandQuantity: 100m,
            Now));
        balances.Seed(InventoryBranchBalance.Create(
            SupplierOrg,
            PosBranchId.From(SupplierBranchId),
            productId,
            onHandQuantity: 2m,
            Now));

        var ok = await ConnectedPurchaseOrderSupplierStock.ValidateDemandsAsync(
            relationship,
            [new(productId.Value, 2m, 1m, "Item")],
            inventory,
            balances,
            branches: null,
            CancellationToken.None);
        Assert.Null(ok);

        var over = await ConnectedPurchaseOrderSupplierStock.ValidateDemandsAsync(
            relationship,
            [new(productId.Value, 3m, 1m, "Item")],
            inventory,
            balances,
            branches: null,
            CancellationToken.None);
        Assert.NotNull(over);
        Assert.Equal(ConnectedSupplierErrorCodes.InsufficientSupplierStock, over!.ErrorCode);
    }

    [Fact]
    public async Task Package_multiplier_compares_in_base_units()
    {
        var productId = CatalogProductId.New();
        var relationship = ActiveRelationship();
        var inventory = new StockInventoryStub();
        inventory.Seed(InventoryAccount.Rehydrate(
            InventoryAccountId.From(productId.Value),
            SupplierOrg,
            productId,
            isTracked: true,
            reorderLevel: null,
            reorderQuantity: null,
            onHandQuantity: 24m,
            createdAtUtc: Now,
            updatedAtUtc: Now));
        var balances = new InMemoryBalances();
        balances.Seed(InventoryBranchBalance.Create(
            SupplierOrg,
            PosBranchId.From(SupplierBranchId),
            productId,
            onHandQuantity: 24m,
            Now));

        var ok = await ConnectedPurchaseOrderSupplierStock.ValidateDemandsAsync(
            relationship,
            [new(productId.Value, 2m, 12m, "Case Pack")],
            inventory,
            balances,
            branches: null,
            CancellationToken.None);
        Assert.Null(ok);

        var over = await ConnectedPurchaseOrderSupplierStock.ValidateDemandsAsync(
            relationship,
            [new(productId.Value, 3m, 12m, "Case Pack")],
            inventory,
            balances,
            branches: null,
            CancellationToken.None);
        Assert.NotNull(over);
        Assert.Equal(ConnectedSupplierErrorCodes.InsufficientSupplierStock, over!.ErrorCode);
        Assert.Equal("Case Pack has only 2 available; 3 was requested.", over.ErrorMessage);
    }

    [Fact]
    public async Task LoadSnapshots_reports_tracked_availability()
    {
        var productId = CatalogProductId.New();
        var inventory = new StockInventoryStub();
        inventory.Seed(InventoryAccount.Rehydrate(
            InventoryAccountId.From(productId.Value),
            SupplierOrg,
            productId,
            isTracked: true,
            reorderLevel: null,
            reorderQuantity: null,
            onHandQuantity: 7m,
            createdAtUtc: Now,
            updatedAtUtc: Now));
        var balances = new InMemoryBalances();
        balances.Seed(InventoryBranchBalance.Create(
            SupplierOrg,
            PosBranchId.From(SupplierBranchId),
            productId,
            onHandQuantity: 7m,
            Now));

        var snapshots = await ConnectedPurchaseOrderSupplierStock.LoadSnapshotsAsync(
            SupplierOrg,
            SupplierBranchId,
            [productId.Value],
            inventory,
            balances,
            branches: null,
            CancellationToken.None);

        Assert.True(snapshots[productId.Value].IsTracked);
        Assert.Equal(7m, snapshots[productId.Value].AvailableBaseQuantity);
    }

    [Fact]
    public async Task Validation_does_not_mutate_inventory()
    {
        var productId = CatalogProductId.New();
        var relationship = ActiveRelationship();
        var inventory = new StockInventoryStub();
        var account = InventoryAccount.Rehydrate(
            InventoryAccountId.From(productId.Value),
            SupplierOrg,
            productId,
            isTracked: true,
            reorderLevel: null,
            reorderQuantity: null,
            onHandQuantity: 5m,
            createdAtUtc: Now,
            updatedAtUtc: Now);
        inventory.Seed(account);
        var balances = new InMemoryBalances();
        var balance = InventoryBranchBalance.Create(
            SupplierOrg,
            PosBranchId.From(SupplierBranchId),
            productId,
            onHandQuantity: 5m,
            Now);
        balances.Seed(balance);

        _ = await ConnectedPurchaseOrderSupplierStock.ValidateDemandsAsync(
            relationship,
            [new(productId.Value, 2m, 1m, "Item")],
            inventory,
            balances,
            branches: null,
            CancellationToken.None);

        Assert.Equal(5m, account.OnHandQuantity);
        Assert.Equal(0m, account.ReservedQuantity);
        Assert.Equal(5m, balance.OnHandQuantity);
        Assert.Equal(0m, balance.ReservedQuantity);
        Assert.Equal(0, inventory.UpdateCallCount);
    }

    private static ConnectedSupplierRelationship ActiveRelationship()
    {
        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            SupplierOrg,
            Now,
            supplierBranchId: SupplierBranchId,
            supplierBranchName: "Main Branch");
        relationship.Approve(Now.AddMinutes(1));
        return relationship;
    }

    private sealed class StockInventoryStub : CostResolverInventoryStub
    {
        private readonly Dictionary<Guid, InventoryAccount> _accounts = new();
        public int UpdateCallCount { get; private set; }

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
            UpdateCallCount++;
            _accounts[account.ProductId.Value] = account;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryBalances : IInventoryBranchBalanceRepository
    {
        private readonly List<InventoryBranchBalance> _items = [];

        public void Seed(InventoryBranchBalance balance) => _items.Add(balance);

        public Task<InventoryBranchBalance?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(b =>
                b.OrganizationId == organizationId
                && b.BranchId == branchId
                && b.ProductId == productId));

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            var ids = productIds.Select(p => p.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<InventoryBranchBalance>>(
                _items.Where(b => b.OrganizationId == organizationId && ids.Contains(b.ProductId.Value)).ToList());
        }

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(b =>
                b.OrganizationId == balance.OrganizationId
                && b.BranchId == balance.BranchId
                && b.ProductId == balance.ProductId);
            _items.Add(balance);
            return Task.CompletedTask;
        }
    }
}
