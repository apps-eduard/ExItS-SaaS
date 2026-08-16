using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerOrderDeliveryFeeCalculatorTests
{
    [Fact]
    public void Calculate_AppliesBasePlusDistanceCharge()
    {
        var policy = new CustomerOrderBranchDeliveryPolicySnapshot(0m, 50m, 2m, 10m, 20m, null);
        var quote = CustomerOrderDeliveryFeeCalculator.Calculate(policy, 100m, 5m);
        Assert.Equal(3m, quote.ExtraDistanceKm);
        Assert.Equal(30m, quote.DistanceCharge);
        Assert.Equal(80m, quote.DeliveryFee);
        Assert.False(quote.FreeDeliveryApplied);
    }

    [Fact]
    public void Calculate_AppliesFreeDeliveryThreshold()
    {
        var policy = new CustomerOrderBranchDeliveryPolicySnapshot(0m, 50m, 2m, 10m, 20m, 200m);
        var quote = CustomerOrderDeliveryFeeCalculator.Calculate(policy, 250m, 8m);
        Assert.Equal(0m, quote.DeliveryFee);
        Assert.True(quote.FreeDeliveryApplied);
    }
}

public sealed class CustomerOrderStockServiceTests
{
    [Fact]
    public async Task Reserve_And_Release_TrackedAccount()
    {
        var org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var productId = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var now = DateTimeOffset.Parse("2026-08-16T10:00:00Z");
        var account = InventoryAccount.CreateUntracked(org, productId, now);
        account.Enable(10m, UnitOfMeasure.Piece, Guid.Parse("33333333-3333-3333-3333-333333333333"), now, false);
        var inventory = new FakeInventoryRepository(account);
        var stock = new CustomerOrderStockService(inventory);

        var order = CustomerOrder.CreateSubmitted(
            org,
            "SO-000001",
            CustomerOrderParty.Personal(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Pat"),
            CustomerOrderFulfillmentType.Pickup,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Main",
            [new CustomerOrderLineDraft(productId, "Rice", "SKU", UnitOfMeasure.Piece, 3m, 10m)],
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            now);

        order.Accept(Guid.Parse("66666666-6666-6666-6666-666666666666"), now);
        await stock.ReserveForAcceptAsync(order, Guid.Parse("66666666-6666-6666-6666-666666666666"), now);

        Assert.Equal(3m, inventory.Account.ReservedQuantity);
        Assert.Equal(7m, inventory.Account.AvailableQuantity);
        Assert.Equal(CustomerOrderStockReservationState.Reserved, order.StockReservationState);

        await stock.ReleaseIfReservedAsync(order, now.AddMinutes(1));
        Assert.Equal(0m, inventory.Account.ReservedQuantity);
        Assert.Equal(CustomerOrderStockReservationState.Released, order.StockReservationState);
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        public InventoryAccount Account { get; private set; }
        private readonly List<StockMovement> _movements = [];

        public FakeInventoryRepository(InventoryAccount account) => Account = account;

        public Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<InventoryAccount?>(
                Account.ProductId == productId && Account.OrganizationId == organizationId ? Account : null);

        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>(
                productIds.Any(p => p == Account.ProductId) ? [Account] : []);

        public Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default)
        {
            Account = account;
            return Task.CompletedTask;
        }

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            _movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<bool> HasCustomerOrderDeductionAsync(
            PosOrganizationId organizationId,
            CustomerOrderId orderId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_movements.Any(m =>
                m.SourceId == orderId.Value
                && m.ProductId == productId
                && m.SourceType == StockMovementSourceType.CustomerOrder));

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
        public Task<bool> HasSaleReturnRestockAsync(PosOrganizationId organizationId, SaleReturnId saleReturnId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasInventoryTransferMovementAsync(PosOrganizationId organizationId, InventoryTransferId transferId, CatalogProductId productId, StockMovementType movementType, InventoryLotId? lotId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
