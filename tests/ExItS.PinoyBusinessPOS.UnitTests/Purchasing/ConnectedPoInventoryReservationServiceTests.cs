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

public sealed class ConnectedPoInventoryReservationServiceTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId SupplierOrg =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid SupplierBranchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Exact_accept_reserves_without_reducing_on_hand()
    {
        var productId = CatalogProductId.New();
        var ctx = Build(productId, trackedOnHand: 10m, qty: 3m);

        ctx.Order.Accept(Now);
        await ctx.Reservations.ReserveConfirmedOnAcceptAsync(ctx.Order, ctx.Relationship, Actor, Now);

        Assert.Equal(10m, ctx.Inventory.Account.OnHandQuantity);
        Assert.Equal(3m, ctx.Inventory.Account.ReservedQuantity);
        Assert.Equal(7m, ctx.Inventory.Account.AvailableQuantity);
        Assert.Equal(ConnectedPoInventoryReservationState.Confirmed, ctx.Order.InventoryReservationState);
        Assert.Single(ctx.Ledger.Rows);
        Assert.Equal(3m, ctx.Ledger.Rows[0].RemainingQuantity);
    }

    [Fact]
    public async Task Shortage_blocks_accept_reservation()
    {
        var productId = CatalogProductId.New();
        var ctx = Build(productId, trackedOnHand: 2m, qty: 5m, productName: "Bath Soap Bar");
        ctx.Order.Accept(Now);

        var ex = await Assert.ThrowsAsync<Domain.Common.DomainException>(() =>
            ctx.Reservations.ReserveConfirmedOnAcceptAsync(ctx.Order, ctx.Relationship, Actor, Now));

        Assert.Equal(ConnectedSupplierErrorCodes.InsufficientSupplierStock, ex.ErrorCode);
        Assert.Equal(0m, ctx.Inventory.Account.ReservedQuantity);
        Assert.Equal(2m, ctx.Inventory.Account.OnHandQuantity);
    }

    [Fact]
    public async Task Competing_orders_cannot_over_reserve()
    {
        var productId = CatalogProductId.New();
        var ctx1 = Build(productId, trackedOnHand: 5m, qty: 4m);
        var ctx2 = BuildShared(ctx1, productId, qty: 3m);

        ctx1.Order.Accept(Now);
        await ctx1.Reservations.ReserveConfirmedOnAcceptAsync(ctx1.Order, ctx1.Relationship, Actor, Now);

        ctx2.Order.Accept(Now.AddMinutes(1));
        var ex = await Assert.ThrowsAsync<Domain.Common.DomainException>(() =>
            ctx2.Reservations.ReserveConfirmedOnAcceptAsync(ctx2.Order, ctx2.Relationship, Actor, Now.AddMinutes(1)));

        Assert.Equal(ConnectedSupplierErrorCodes.InsufficientSupplierStock, ex.ErrorCode);
        Assert.Equal(4m, ctx1.Inventory.Account.ReservedQuantity);
        Assert.Equal(5m, ctx1.Inventory.Account.OnHandQuantity);
    }

    [Fact]
    public async Task Proposal_temporary_hold_and_buyer_confirm()
    {
        var productId = CatalogProductId.New();
        var ctx = Build(productId, trackedOnHand: 10m, qty: 6m);
        ctx.Order.ProposeLineChanges(
            [new ConnectedPoLineProposal(productId, 4m, false, null)],
            Now);

        await ctx.Reservations.ReserveTemporaryOnProposeAsync(ctx.Order, ctx.Relationship, Actor, Now);
        Assert.Equal(ConnectedPoInventoryReservationState.TemporaryProposal, ctx.Order.InventoryReservationState);
        Assert.Equal(4m, ctx.Inventory.Account.ReservedQuantity);
        Assert.Equal(10m, ctx.Inventory.Account.OnHandQuantity);

        ctx.Order.AcceptProposedChanges(Now.AddMinutes(1), Actor);
        await ctx.Reservations.ConfirmTemporaryOnBuyerAcceptAsync(ctx.Order, Actor, Now.AddMinutes(1));
        Assert.Equal(ConnectedPoInventoryReservationState.Confirmed, ctx.Order.InventoryReservationState);
        Assert.Null(ctx.Order.InventoryReservationExpiresAtUtc);
        Assert.Equal(ConnectedPoReservationType.ConfirmedOrder, ctx.Ledger.Rows[0].Type);
    }

    [Fact]
    public async Task Decline_and_withdraw_release_hold()
    {
        var productId = CatalogProductId.New();
        var ctx = Build(productId, trackedOnHand: 8m, qty: 3m);
        ctx.Order.Accept(Now);
        await ctx.Reservations.ReserveConfirmedOnAcceptAsync(ctx.Order, ctx.Relationship, Actor, Now);
        Assert.Equal(3m, ctx.Inventory.Account.ReservedQuantity);

        await ctx.Reservations.ReleaseActiveAsync(ctx.Order, Now.AddMinutes(1));
        Assert.Equal(0m, ctx.Inventory.Account.ReservedQuantity);
        Assert.Equal(8m, ctx.Inventory.Account.OnHandQuantity);
        Assert.Equal(ConnectedPoInventoryReservationState.Released, ctx.Order.InventoryReservationState);
    }

    [Fact]
    public async Task Expiry_releases_and_rejects_proposal()
    {
        var previous = ConnectedPoInventoryReservationOptions.DefaultProposalHoldDuration;
        try
        {
            ConnectedPoInventoryReservationOptions.DefaultProposalHoldDuration = TimeSpan.FromMinutes(1);
            var productId = CatalogProductId.New();
            var ctx = Build(productId, trackedOnHand: 9m, qty: 5m);
            ctx.Order.ProposeLineChanges(
                [new ConnectedPoLineProposal(productId, 3m, false, null)],
                Now);
            await ctx.Reservations.ReserveTemporaryOnProposeAsync(ctx.Order, ctx.Relationship, Actor, Now);

            await ctx.Reservations.ExpireIfNeededAsync(ctx.Order, Now.AddMinutes(2));
            Assert.Equal(ConnectedPurchaseOrderStatus.New, ctx.Order.Status);
            Assert.Equal(ConnectedPoInventoryReservationState.Released, ctx.Order.InventoryReservationState);
            Assert.Equal(0m, ctx.Inventory.Account.ReservedQuantity);
        }
        finally
        {
            ConnectedPoInventoryReservationOptions.DefaultProposalHoldDuration = previous;
        }
    }

    [Fact]
    public async Task Idempotent_confirmed_reserve_retry()
    {
        var productId = CatalogProductId.New();
        var ctx = Build(productId, trackedOnHand: 10m, qty: 2m);
        ctx.Order.Accept(Now);
        await ctx.Reservations.ReserveConfirmedOnAcceptAsync(ctx.Order, ctx.Relationship, Actor, Now);
        await ctx.Reservations.ReserveConfirmedOnAcceptAsync(ctx.Order, ctx.Relationship, Actor, Now.AddSeconds(1));
        Assert.Equal(2m, ctx.Inventory.Account.ReservedQuantity);
        Assert.Single(ctx.Ledger.Rows);
    }

    [Fact]
    public async Task Fulfill_consumes_reservation_and_reduces_on_hand()
    {
        var productId = CatalogProductId.New();
        var ctx = Build(productId, trackedOnHand: 10m, qty: 3m);
        ctx.Order.Accept(Now);
        await ctx.Reservations.ReserveConfirmedOnAcceptAsync(ctx.Order, ctx.Relationship, Actor, Now);

        var fulfill = new ConnectedPurchaseOrderFulfillStock(
            ctx.Inventory,
            ctx.Products,
            ctx.Units,
            ctx.Balances,
            new BranchInventoryMutationService(),
            ctx.Reservations);

        await fulfill.ApplyAsync(ctx.Order, ctx.Relationship, Actor, Now.AddMinutes(1));

        Assert.Equal(7m, ctx.Inventory.Account.OnHandQuantity);
        Assert.Equal(0m, ctx.Inventory.Account.ReservedQuantity);
        Assert.Equal(ConnectedPoInventoryReservationState.Consumed, ctx.Order.InventoryReservationState);
        Assert.Single(ctx.Inventory.Movements);
    }

    private static Ctx Build(
        CatalogProductId productId,
        decimal? trackedOnHand,
        decimal qty,
        string productName = "Item")
    {
        var relationship = ConnectedSupplierRelationship.Request(
            Buyer,
            SupplierOrg,
            Now,
            supplierBranchId: SupplierBranchId,
            supplierBranchName: "Main Branch");
        relationship.Approve(Now.AddMinutes(1));
        var line = ConnectedPurchaseOrderLine.Create(productId, productName, "SKU", qty, 12m, "Piece");
        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-RES",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [line],
            Now.AddMinutes(2));

        var inventory = new ResInventoryStub();
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

        var balances = new ResBalances();
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
        var products = new ResProducts(product);
        var units = new ResUnits();
        var ledger = new ResLedger();
        var reservations = new ConnectedPoInventoryReservationService(
            inventory, balances, products, units, ledger);

        return new Ctx(order, relationship, inventory, balances, products, units, ledger, reservations);
    }

    private static Ctx BuildShared(Ctx shared, CatalogProductId productId, decimal qty)
    {
        var line = ConnectedPurchaseOrderLine.Create(productId, "Item", "SKU", qty, 12m, "Piece");
        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            shared.Relationship,
            PurchaseOrderId.New(),
            "PO-RES-2",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [line],
            Now.AddMinutes(5));
        var reservations = new ConnectedPoInventoryReservationService(
            shared.Inventory, shared.Balances, shared.Products, shared.Units, shared.Ledger);
        return new Ctx(
            order,
            shared.Relationship,
            shared.Inventory,
            shared.Balances,
            shared.Products,
            shared.Units,
            shared.Ledger,
            reservations);
    }

    private sealed record Ctx(
        ConnectedPurchaseOrder Order,
        ConnectedSupplierRelationship Relationship,
        ResInventoryStub Inventory,
        ResBalances Balances,
        ResProducts Products,
        ResUnits Units,
        ResLedger Ledger,
        ConnectedPoInventoryReservationService Reservations);

    private sealed class ResInventoryStub : CostResolverInventoryStub
    {
        private readonly Dictionary<Guid, InventoryAccount> _accounts = new();
        public List<StockMovement> Movements { get; } = [];
        public InventoryAccount Account => _accounts.Values.Single();

        public void Seed(InventoryAccount account) => _accounts[account.ProductId.Value] = account;

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
            Task.FromResult(Movements.Any(m =>
                m.SourceId == connectedPurchaseOrderId.Value
                && m.ProductId == productId
                && m.MovementType == StockMovementType.ConnectedPurchaseFulfillment));
    }

    private sealed class ResBalances : IInventoryBranchBalanceRepository
    {
        private readonly Dictionary<(Guid Branch, Guid Product), InventoryBranchBalance> _rows = new();
        public void Seed(InventoryBranchBalance balance) =>
            _rows[(balance.BranchId.Value, balance.ProductId.Value)] = balance;

        public Task<InventoryBranchBalance?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rows.TryGetValue((branchId.Value, productId.Value), out var row) ? row : null);

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

    private sealed class ResProducts(CatalogProduct product) : ICatalogProductRepository
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

    private sealed class ResUnits : ICatalogProductUnitRepository
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

    private sealed class ResLedger : IConnectedPoInventoryReservationRepository
    {
        public List<ConnectedPoInventoryReservation> Rows { get; } = [];

        public Task<IReadOnlyList<ConnectedPoInventoryReservation>> ListActiveByOrderAsync(
            ConnectedPurchaseOrderId orderId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPoInventoryReservation>>(
                Rows.Where(r => r.ConnectedPurchaseOrderId == orderId
                    && r.Status == ConnectedPoReservationStatus.Active).ToList());

        public Task<IReadOnlyList<ConnectedPoInventoryReservation>> ListByOrderAsync(
            ConnectedPurchaseOrderId orderId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPoInventoryReservation>>(
                Rows.Where(r => r.ConnectedPurchaseOrderId == orderId).ToList());

        public Task<IReadOnlyList<ConnectedPoInventoryReservation>> ListByProductBranchAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId branchId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPoInventoryReservation>>(
                Rows.Where(r =>
                        r.OrganizationId == organizationId
                        && r.ProductId == productId
                        && r.BranchId == branchId
                        && r.Status == ConnectedPoReservationStatus.Active
                        && r.RemainingQuantity > 0m)
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .ToList());

        public Task<IReadOnlyDictionary<Guid, decimal>> SumExpiredStillActiveRemainingByProductAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            IReadOnlyCollection<CatalogProductId> productIds,
            DateTimeOffset utcNow,
            CancellationToken ct = default)
        {
            var ids = productIds.Select(p => p.Value).ToHashSet();
            var map = Rows
                .Where(r =>
                    r.OrganizationId == organizationId
                    && r.BranchId == branchId
                    && ids.Contains(r.ProductId.Value)
                    && r.Status == ConnectedPoReservationStatus.Active
                    && r.RemainingQuantity > 0m
                    && r.ExpiresAtUtc is DateTimeOffset expires
                    && utcNow >= expires)
                .GroupBy(r => r.ProductId.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingQuantity));
            return Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(map);
        }

        public Task AddAsync(ConnectedPoInventoryReservation reservation, CancellationToken ct = default)
        {
            Rows.Add(reservation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ConnectedPoInventoryReservation reservation, CancellationToken ct = default)
        {
            var idx = Rows.FindIndex(r => r.Id == reservation.Id);
            if (idx >= 0)
            {
                Rows[idx] = reservation;
            }

            return Task.CompletedTask;
        }
    }
}
