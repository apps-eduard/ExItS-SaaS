using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Platform;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class MultiBranchCustomerCommerceTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly CatalogProductId ProductId = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly Guid MainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BranchBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-18T08:00:00Z");

    [Fact]
    public async Task Branch_b_cannot_consume_unallocated_main_stock()
    {
        var account = TrackedAccount(100m);
        var inventory = new FakeInventoryRepository(account);
        var balances = new InMemoryBalances();
        var stock = new CustomerOrderStockService(inventory, balances, new PrimaryDirectory(MainId));
        var line = new CustomerOrderLineDraft(ProductId, "Coke", "SKU", UnitOfMeasure.Piece, 30m, 25m);

        var blocked = await stock.EnsureAvailableAsync(Org, [line], BranchBId);
        Assert.False(blocked.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, blocked.ErrorCode);

        var ok = await stock.EnsureAvailableAsync(Org, [line], MainId);
        Assert.True(ok.IsSuccess);
        Assert.Equal(100m, account.OnHandQuantity);
    }

    [Fact]
    public async Task Reserve_at_branch_b_does_not_decrement_main_overlay()
    {
        var account = TrackedAccount(100m);
        var inventory = new FakeInventoryRepository(account);
        var balances = new InMemoryBalances();
        balances.Items.Add(InventoryBranchBalance.Create(Org, PosBranchId.From(MainId), ProductId, 100m, T0));
        var stock = new CustomerOrderStockService(inventory, balances, new PrimaryDirectory(MainId));
        var order = Submitted(BranchBId, "Branch B", 1m);

        order.Accept(Guid.Parse("66666666-6666-6666-6666-666666666666"), T0);
        await Assert.ThrowsAsync<DomainException>(() =>
            stock.ReserveForAcceptAsync(order, Guid.Parse("66666666-6666-6666-6666-666666666666"), T0));

        Assert.Equal(100m, balances.OnHand(MainId, ProductId.Value));
        Assert.Equal(0m, account.ReservedQuantity);
    }

    [Fact]
    public void Catalog_and_customer_link_stay_organization_owned()
    {
        Assert.Null(typeof(CatalogProduct).GetProperty("BranchId"));
        Assert.NotNull(typeof(CatalogProduct).GetProperty("OrganizationId"));
        Assert.NotNull(typeof(CustomerOrder).GetProperty("SellerOrganizationId"));
        Assert.NotNull(typeof(CustomerOrder).GetProperty("FulfillmentBranchId"));
    }

    [Fact]
    public void Create_branch_request_does_not_enable_pickup_or_delivery()
    {
        var request = new CreateBranchRequest("B2", "Branch B");
        Assert.False(request.PickupEnabled);
        Assert.False(request.DeliveryEnabled);
    }

    private static InventoryAccount TrackedAccount(decimal onHand)
    {
        var account = InventoryAccount.CreateUntracked(Org, ProductId, T0);
        account.Enable(onHand, UnitOfMeasure.Piece, Guid.Parse("33333333-3333-3333-3333-333333333333"), T0, false);
        return account;
    }

    private static CustomerOrder Submitted(Guid branchId, string branchName, decimal qty) =>
        CustomerOrder.CreateSubmitted(
            Org,
            "SO-000100",
            CustomerOrderParty.Personal(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Paul"),
            CustomerOrderFulfillmentType.Pickup,
            branchId,
            branchName,
            [new CustomerOrderLineDraft(ProductId, "Coke", "SKU", UnitOfMeasure.Piece, qty, 25m)],
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            T0);

    private sealed class PrimaryDirectory(Guid primaryId) : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<Guid?> GetPrimaryBranchIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(primaryId);
    }

    private sealed class InMemoryBalances : IInventoryBranchBalanceRepository
    {
        public List<InventoryBranchBalance> Items { get; } = [];

        public decimal OnHand(Guid branchId, Guid productId) =>
            Items.FirstOrDefault(b => b.BranchId.Value == branchId && b.ProductId.Value == productId)?.OnHandQuantity ?? 0m;

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
                Items.Where(b => b.OrganizationId == organizationId && productIds.Any(id => id == b.ProductId)).ToList());

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(b => b.BranchId == balance.BranchId && b.ProductId == balance.ProductId);
            Items.Add(balance);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        public InventoryAccount Account { get; private set; }
        public FakeInventoryRepository(InventoryAccount account) => Account = account;

        public Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<InventoryAccount?>(Account.ProductId == productId ? Account : null);

        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>(productIds.Any(p => p == Account.ProductId) ? [Account] : []);

        public Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
        {
            Account = account;
            return Task.CompletedTask;
        }

        public Task ExecuteWithProductReservationLocksAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default) =>
            action(productIds.Any(p => p == Account.ProductId) ? [Account] : [], cancellationToken);

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<StockMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            StockMovementId movementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StockMovement?>(null);

        public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, InventoryAccountFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasAnyMovementAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOpeningStockAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(PosOrganizationId organizationId, CatalogProductId productId, StockMovementFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> SumMovementEffectsAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasStockCountVarianceAsync(PosOrganizationId organizationId, StockCountId stockCountId, CatalogProductId productId, StockMovementType movementType, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSaleDeductionAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSaleVoidRestorationAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasPurchaseReceiptAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasDirectPurchaseReceiptAsync(PosOrganizationId organizationId, DirectPurchaseReceiptId receiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasStockUseAsync(PosOrganizationId organizationId, StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> HasStockUseVoidRestorationAsync(PosOrganizationId organizationId, StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<decimal?> GetLatestAcquisitionUnitCostAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<decimal?>(null);
        public Task<bool> HasSaleReturnRestockAsync(PosOrganizationId organizationId, SaleReturnId saleReturnId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasInventoryTransferMovementAsync(PosOrganizationId organizationId, InventoryTransferId transferId, CatalogProductId productId, StockMovementType movementType, InventoryLotId? lotId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasCustomerOrderDeductionAsync(PosOrganizationId organizationId, CustomerOrderId orderId, CatalogProductId productId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
