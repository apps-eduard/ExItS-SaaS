using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryTransferUseCaseTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab");
    private static readonly Guid BranchA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BranchB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BranchOtherOrg = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ActorA = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ActorB = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly DateTimeOffset Utc = new(2026, 8, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Same_org_transfer_full_receive_updates_ledger_and_not_destination_before_receive()
    {
        var fx = await SeedAsync(cokeOnHand: 100m, spriteOnHand: 0m, extraBranchB: 20m);
        var created = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchB, [new InventoryTransferLineRequest(fx.CokeId, 30m)]),
            ActorA,
            BranchA);
        Assert.True(created.IsSuccess);

        var afterCreate = fx.Inventory.GetOnHand(fx.CokeId);
        Assert.Equal(120m, afterCreate);
        Assert.Equal(20m, fx.Balances.OnHand(BranchB, fx.CokeId));

        var dispatched = await fx.Dispatch.ExecuteAsync(OrgA, created.Value!.Id.Value, ActorA, BranchA);
        Assert.True(dispatched.IsSuccess);
        Assert.Equal(InventoryTransferStatus.InTransit, dispatched.Value!.Status);
        Assert.Equal(90m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(70m, fx.Balances.OnHand(BranchA, fx.CokeId));
        Assert.Equal(20m, fx.Balances.OnHand(BranchB, fx.CokeId));
        Assert.Contains(fx.Inventory.Movements, m => m.MovementType == StockMovementType.TransferOut && m.QuantityEffect == -30m);

        var received = await fx.Receive.ExecuteAsync(
            OrgA,
            created.Value.Id.Value,
            new ReceiveInventoryTransferRequest([new InventoryTransferReceiveLineRequest(fx.CokeId, 30m)]),
            ActorB,
            BranchB);
        Assert.True(received.IsSuccess);
        Assert.Equal(InventoryTransferStatus.Received, received.Value!.Status);
        Assert.Equal(120m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(70m, fx.Balances.OnHand(BranchA, fx.CokeId));
        Assert.Equal(50m, fx.Balances.OnHand(BranchB, fx.CokeId));
        Assert.Contains(fx.Inventory.Movements, m => m.MovementType == StockMovementType.TransferIn && m.QuantityEffect == 30m);
        Assert.Contains(fx.Alerts.Items, a => a.Kind == "dispatched" && a.TargetBranchId == BranchB);
        Assert.Contains(fx.Alerts.Items, a => a.Kind == "received" && a.TargetBranchId == BranchA);
        Assert.Equal(fx.CokeId, fx.Products.Items.Single(p => p.Id.Value == fx.CokeId).Id.Value);
    }

    [Fact]
    public async Task Partial_receive_credits_only_received_qty_and_records_shortage()
    {
        var fx = await SeedAsync(cokeOnHand: 20m, spriteOnHand: 10m, waterOnHand: 30m);
        var created = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(
                BranchA,
                BranchB,
                [
                    new InventoryTransferLineRequest(fx.CokeId, 20m),
                    new InventoryTransferLineRequest(fx.SpriteId, 10m),
                    new InventoryTransferLineRequest(fx.WaterId, 30m)
                ]),
            ActorA,
            BranchA);
        Assert.True((await fx.Dispatch.ExecuteAsync(OrgA, created.Value!.Id.Value, ActorA, BranchA)).IsSuccess);

        var received = await fx.Receive.ExecuteAsync(
            OrgA,
            created.Value.Id.Value,
            new ReceiveInventoryTransferRequest(
            [
                new InventoryTransferReceiveLineRequest(fx.CokeId, 20m),
                new InventoryTransferReceiveLineRequest(fx.SpriteId, 8m, "ShortShipment"),
                new InventoryTransferReceiveLineRequest(fx.WaterId, 30m)
            ]),
            ActorB,
            BranchB);
        Assert.True(received.IsSuccess);
        Assert.Equal(InventoryTransferStatus.PartiallyReceived, received.Value!.Status);
        Assert.Equal(8m, received.Value.Lines.Single(l => l.ProductId.Value == fx.SpriteId).ReceivedQty);
        Assert.Equal(2m, received.Value.Lines.Single(l => l.ProductId.Value == fx.SpriteId).DifferenceQty);
        Assert.Equal(20m, fx.Balances.OnHand(BranchB, fx.CokeId));
        Assert.Equal(8m, fx.Balances.OnHand(BranchB, fx.SpriteId));
        Assert.Equal(30m, fx.Balances.OnHand(BranchB, fx.WaterId));
        Assert.Equal(58m, fx.Inventory.GetOnHand(fx.CokeId) + fx.Inventory.GetOnHand(fx.SpriteId) + fx.Inventory.GetOnHand(fx.WaterId) - (0));
        Assert.Equal(58m, fx.Inventory.GetOnHand(fx.CokeId) + fx.Inventory.GetOnHand(fx.SpriteId) + fx.Inventory.GetOnHand(fx.WaterId));
        Assert.DoesNotContain(
            fx.Inventory.Movements,
            m => m.MovementType == StockMovementType.TransferIn
                && m.ProductId.Value == fx.SpriteId
                && m.QuantityEffect == 10m);
        Assert.Contains(
            fx.Inventory.Movements,
            m => m.MovementType == StockMovementType.TransferIn
                && m.ProductId.Value == fx.SpriteId
                && m.QuantityEffect == 8m);
        Assert.Contains(fx.Alerts.Items, a => a.Kind == "partially-received");
    }

    [Fact]
    public async Task Zero_received_line_adds_no_destination_stock()
    {
        var fx = await SeedAsync(cokeOnHand: 10m);
        var created = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchB, [new InventoryTransferLineRequest(fx.CokeId, 10m)]),
            ActorA,
            BranchA);
        Assert.True((await fx.Dispatch.ExecuteAsync(OrgA, created.Value!.Id.Value, ActorA, BranchA)).IsSuccess);
        var received = await fx.Receive.ExecuteAsync(
            OrgA,
            created.Value.Id.Value,
            new ReceiveInventoryTransferRequest([new InventoryTransferReceiveLineRequest(fx.CokeId, 0m)]),
            ActorB,
            BranchB);
        Assert.True(received.IsSuccess);
        Assert.Equal(0m, fx.Balances.OnHand(BranchB, fx.CokeId));
        Assert.DoesNotContain(fx.Inventory.Movements, m => m.MovementType == StockMovementType.TransferIn);
        Assert.Equal(10m, received.Value!.Lines[0].DifferenceQty);
    }

    [Fact]
    public async Task Cross_org_source_equals_dest_zero_qty_and_insufficient_stock_are_rejected()
    {
        var fx = await SeedAsync(cokeOnHand: 5m);
        var cross = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchOtherOrg, [new InventoryTransferLineRequest(fx.CokeId, 1m)]),
            ActorA,
            BranchA);
        Assert.Equal(ApplicationErrorCodes.InventoryTransferBranchNotFound, cross.ErrorCode);

        var same = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchA, [new InventoryTransferLineRequest(fx.CokeId, 1m)]),
            ActorA,
            BranchA);
        Assert.Equal(Domain.Common.DomainErrorCodes.InventoryTransferSameBranch, same.ErrorCode);

        var zero = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchB, [new InventoryTransferLineRequest(fx.CokeId, 0m)]),
            ActorA,
            BranchA);
        Assert.Equal(Domain.Common.DomainErrorCodes.InvalidInventoryTransferQuantity, zero.ErrorCode);

        var negative = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchB, [new InventoryTransferLineRequest(fx.CokeId, -2m)]),
            ActorA,
            BranchA);
        Assert.Equal(Domain.Common.DomainErrorCodes.InvalidInventoryTransferQuantity, negative.ErrorCode);

        var created = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchB, [new InventoryTransferLineRequest(fx.CokeId, 6m)]),
            ActorA,
            BranchA);
        var dispatch = await fx.Dispatch.ExecuteAsync(OrgA, created.Value!.Id.Value, ActorA, BranchA);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, dispatch.ErrorCode);
    }

    [Fact]
    public async Task Isolation_idempotency_and_cancel_guards()
    {
        var fx = await SeedAsync(cokeOnHand: 30m);
        var created = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchB, [new InventoryTransferLineRequest(fx.CokeId, 10m)]),
            ActorA,
            BranchA);
        var transferId = created.Value!.Id.Value;

        var otherOrg = await fx.Queries.GetByIdAsync(OrgB, transferId);
        Assert.Null(otherOrg);

        var wrongBranchReceive = await fx.Receive.ExecuteAsync(
            OrgA,
            transferId,
            new ReceiveInventoryTransferRequest([new InventoryTransferReceiveLineRequest(fx.CokeId, 10m)]),
            ActorB,
            BranchA);
        Assert.Equal(ApplicationErrorCodes.InventoryTransferBranchForbidden, wrongBranchReceive.ErrorCode);

        Assert.True((await fx.Dispatch.ExecuteAsync(OrgA, transferId, ActorA, BranchA)).IsSuccess);
        var dispatchAgain = await fx.Dispatch.ExecuteAsync(OrgA, transferId, ActorA, BranchA);
        Assert.True(dispatchAgain.IsSuccess);
        Assert.Equal(1, fx.Inventory.Movements.Count(m => m.MovementType == StockMovementType.TransferOut));
        var first = await fx.Receive.ExecuteAsync(
            OrgA,
            transferId,
            new ReceiveInventoryTransferRequest([new InventoryTransferReceiveLineRequest(fx.CokeId, 10m)]),
            ActorB,
            BranchB);
        Assert.True(first.IsSuccess);
        var onHand = fx.Inventory.GetOnHand(fx.CokeId);
        var second = await fx.Receive.ExecuteAsync(
            OrgA,
            transferId,
            new ReceiveInventoryTransferRequest([new InventoryTransferReceiveLineRequest(fx.CokeId, 10m)]),
            ActorB,
            BranchB);
        Assert.Equal(ApplicationErrorCodes.InventoryTransferAlreadyReceived, second.ErrorCode);
        Assert.Equal(onHand, fx.Inventory.GetOnHand(fx.CokeId));

        var cancelled = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchB, [new InventoryTransferLineRequest(fx.CokeId, 5m)]),
            ActorA,
            BranchA);
        Assert.True((await fx.Cancel.ExecuteAsync(OrgA, cancelled.Value!.Id.Value, ActorA, BranchA)).IsSuccess);
        var receiveCancelled = await fx.Receive.ExecuteAsync(
            OrgA,
            cancelled.Value.Id.Value,
            new ReceiveInventoryTransferRequest([new InventoryTransferReceiveLineRequest(fx.CokeId, 5m)]),
            ActorB,
            BranchB);
        Assert.False(receiveCancelled.IsSuccess);
    }

    [Fact]
    public async Task Destination_balance_initializes_when_missing_and_product_is_not_duplicated()
    {
        var fx = await SeedAsync(cokeOnHand: 12m);
        Assert.Single(fx.Products.Items);
        var created = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateInventoryTransferRequest(BranchA, BranchB, [new InventoryTransferLineRequest(fx.CokeId, 12m)]),
            ActorA,
            BranchA);
        Assert.True((await fx.Dispatch.ExecuteAsync(OrgA, created.Value!.Id.Value, ActorA, BranchA)).IsSuccess);
        Assert.True((await fx.Receive.ExecuteAsync(
            OrgA,
            created.Value.Id.Value,
            new ReceiveInventoryTransferRequest([new InventoryTransferReceiveLineRequest(fx.CokeId, 12m)]),
            ActorB,
            BranchB)).IsSuccess);
        Assert.Single(fx.Products.Items);
        Assert.Equal(12m, fx.Balances.OnHand(BranchB, fx.CokeId));
    }

    private static async Task<Fixture> SeedAsync(
        decimal cokeOnHand,
        decimal spriteOnHand = 0m,
        decimal waterOnHand = 0m,
        decimal extraBranchB = 0m)
    {
        var fx = new Fixture();
        await fx.EnableAsync(fx.CokeId, "Coke", cokeOnHand);
        if (spriteOnHand > 0m)
        {
            await fx.EnableAsync(fx.SpriteId, "Sprite", spriteOnHand);
        }

        if (waterOnHand > 0m)
        {
            await fx.EnableAsync(fx.WaterId, "Water", waterOnHand);
        }

        if (extraBranchB > 0m)
        {
            var adjust = new AdjustInventoryStock(
                fx.Inventory,
                fx.Products,
                new EmptyProductUnits(),
                fx.Balances,
                fx.Lots,
                new InventoryLotStockService(fx.Lots),
                fx.UnitOfWork,
                fx.Clock);
            var result = await adjust.ExecuteAsync(OrgA, fx.CokeId, "In", extraBranchB, "Branch B opening", ActorA, branchId: BranchB);
            Assert.True(result.IsSuccess);
        }

        return fx;
    }

    private sealed class Fixture
    {
        public Guid CokeId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public Guid SpriteId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public Guid WaterId { get; } = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        public InMemoryCatalog Products { get; } = new();
        public InMemoryInventory Inventory { get; } = new();
        public InMemoryTransfers Transfers { get; } = new();
        public InMemoryBalances Balances { get; } = new();
        public InMemoryLots Lots { get; } = new();
        public CapturingAlerts Alerts { get; } = new();
        public ImmediateUnitOfWork UnitOfWork { get; } = new();
        public FixedClock Clock { get; } = new(Utc);
        public CreateInventoryTransfer Create { get; }
        public DispatchInventoryTransfer Dispatch { get; }
        public ReceiveInventoryTransfer Receive { get; }
        public CancelInventoryTransfer Cancel { get; }
        public InventoryTransferQueryService Queries { get; }

        public Fixture()
        {
            var branches = new FakeBranches();
            var lotStock = new InventoryLotStockService(Lots);
            Create = new CreateInventoryTransfer(Transfers, Products, Lots, branches, UnitOfWork, Clock);
            Dispatch = new DispatchInventoryTransfer(Transfers, Inventory, Balances, Products, Lots, lotStock, branches, Alerts, UnitOfWork, Clock);
            Receive = new ReceiveInventoryTransfer(Transfers, Inventory, Balances, Products, Lots, lotStock, branches, Alerts, UnitOfWork, Clock);
            Cancel = new CancelInventoryTransfer(Transfers, Inventory, Balances, Products, lotStock, branches, UnitOfWork, Clock);
            Queries = new InventoryTransferQueryService(Transfers, branches);
        }

        public async Task EnableAsync(Guid productId, string name, decimal opening)
        {
            var product = CatalogProduct.Create(
                PosOrganizationId.From(OrgA),
                name,
                UnitOfMeasure.Piece,
                10m,
                Utc,
                id: CatalogProductId.From(productId));
            Products.Items.Add(product);
            var account = InventoryAccount.CreateUntracked(PosOrganizationId.From(OrgA), CatalogProductId.From(productId), Utc);
            var movement = account.Enable(opening, UnitOfMeasure.Piece, ActorA, Utc, hasOpeningStockAlready: false);
            Inventory.Accounts.Add(account);
            if (movement is not null)
            {
                Inventory.Movements.Add(movement);
            }
            await Task.CompletedTask;
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

    private sealed class FakeBranches : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(organizationId == OrgA && (branchId == BranchA || branchId == BranchB));

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(id => id, id => id == BranchA ? "Branch A" : "Branch B"));
    }

    private sealed class CapturingAlerts : IInventoryTransferAlertSink
    {
        public List<InventoryTransferAlert> Items { get; } = [];

        public Task PublishAsync(InventoryTransferAlert alert, CancellationToken cancellationToken = default)
        {
            Items.Add(alert);
            return Task.CompletedTask;
        }
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

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
            action([], cancellationToken);

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<bool> HasInventoryTransferMovementAsync(
            PosOrganizationId organizationId,
            InventoryTransferId transferId,
            CatalogProductId productId,
            StockMovementType movementType,
            InventoryLotId? lotId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == transferId.Value
                && m.MovementType == movementType
                && m.InventoryLotId == lotId));

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, InventoryAccountFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasAnyMovementAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasOpeningStockAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(PosOrganizationId organizationId, CatalogProductId productId, StockMovementFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> SumMovementEffectsAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasStockCountVarianceAsync(PosOrganizationId organizationId, StockCountId stockCountId, CatalogProductId productId, StockMovementType movementType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleDeductionAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasCustomerOrderDeductionAsync(PosOrganizationId organizationId, CustomerOrderId orderId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleVoidRestorationAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPurchaseReceiptAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasDirectPurchaseReceiptAsync(PosOrganizationId organizationId, DirectPurchaseReceiptId receiptId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == receiptId.Value
                && m.MovementType == StockMovementType.DirectPurchaseReceipt
                && m.SourceType == StockMovementSourceType.DirectPurchase));
        public Task<bool> HasSaleReturnRestockAsync(PosOrganizationId organizationId, SaleReturnId saleReturnId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class InMemoryTransfers : IInventoryTransferRepository
    {
        private readonly List<InventoryTransfer> _items = [];
        private long _sequence = 0;

        public Task<InventoryTransfer?> GetByIdAsync(PosOrganizationId organizationId, InventoryTransferId transferId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(t => t.OrganizationId == organizationId && t.Id == transferId));

        public Task<(IReadOnlyList<InventoryTransfer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            InventoryTransferFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var items = _items.Where(t => t.OrganizationId == organizationId).ToList();
            return Task.FromResult<(IReadOnlyList<InventoryTransfer>, int)>((items.Skip(skip).Take(take).ToList(), items.Count));
        }

        public Task AddAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default)
        {
            _items.Add(transfer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string> AllocateNextNumberAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, CancellationToken cancellationToken = default)
        {
            _sequence++;
            return Task.FromResult(InventoryTransferNumbers.Format(businessDateUtc, _sequence));
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
            Task.FromResult<(IReadOnlyList<InventoryLot>, int)>(([], 0));

        public Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(
            PosOrganizationId organizationId,
            DateOnly today,
            int warningDays,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0));

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

    private sealed class InMemoryBalances : IInventoryBranchBalanceRepository
    {
        public List<InventoryBranchBalance> Items { get; } = [];

        public decimal OnHand(Guid branchId, Guid productId) =>
            Items.FirstOrDefault(b => b.BranchId.Value == branchId && b.ProductId.Value == productId)?.OnHandQuantity ?? 0m;

        public Task<InventoryBranchBalance?> GetAsync(PosOrganizationId organizationId, PosBranchId branchId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(b => b.OrganizationId == organizationId && b.BranchId == branchId && b.ProductId == productId));

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryBranchBalance>>(
                Items.Where(b => b.OrganizationId == organizationId && productIds.Any(id => id == b.ProductId)).ToList());

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
        {
            var existing = Items.FindIndex(b =>
                b.OrganizationId == balance.OrganizationId
                && b.BranchId == balance.BranchId
                && b.ProductId == balance.ProductId);
            if (existing >= 0)
            {
                Items[existing] = balance;
            }
            else
            {
                Items.Add(balance);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class EmptyProductUnits : ICatalogProductUnitRepository
    {
        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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

        public Task ReplaceActiveUnitsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
