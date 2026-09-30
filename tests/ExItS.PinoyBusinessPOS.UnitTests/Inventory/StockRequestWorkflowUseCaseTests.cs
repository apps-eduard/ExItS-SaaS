using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class StockRequestWorkflowUseCaseTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Warehouse = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Utc = new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Submit_publishes_notification_to_warehouse()
    {
        var fx = await Fixture.CreateAsync();
        var result = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(
                Branch,
                Warehouse,
                [new StockRequestLineRequest(fx.ProductId, 10m)]),
            Actor,
            Branch);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Value!.Status);
        Assert.Contains(
            fx.Notifications.Items,
            n => n.RelatedType == StockRequestNotificationTypes.Submitted
                 && n.TargetBranchId == Warehouse);
    }

    [Fact]
    public async Task Create_accepts_decimal_quantity_for_by_weight_product()
    {
        var fx = await Fixture.CreateAsync();
        var weight = CatalogProduct.Create(
            PosOrganizationId.From(Org),
            "Bulk Rice",
            UnitOfMeasure.Kilogram,
            80m,
            Utc,
            sellingMode: SellingMode.ByWeight);
        fx.Products.Items.Add(weight);
        fx.SeedWarehouseStock(weight.Id.Value, 1_000m);

        var result = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(
                Branch,
                Warehouse,
                [new StockRequestLineRequest(weight.Id.Value, 1.25m)]),
            Actor,
            Branch);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(1.25m, result.Value!.Lines[0].RequestedQuantity);
    }

    [Fact]
    public async Task Create_rejects_decimal_quantity_for_per_item_product()
    {
        var fx = await Fixture.CreateAsync();
        var result = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(
                Branch,
                Warehouse,
                [new StockRequestLineRequest(fx.ProductId, 1.5m)]),
            Actor,
            Branch);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidSaleLineQuantity, result.ErrorCode);
    }

    [Fact]
    public async Task Create_accepts_request_at_warehouse_available()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 10m);

        var result = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 10m)]),
            Actor,
            Branch);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Single(fx.Requests.Items);
    }

    [Fact]
    public async Task Create_rejects_when_requested_exceeds_warehouse_available()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 55m);

        var result = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 60m)]),
            Actor,
            Branch);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, result.ErrorCode);
        Assert.Contains("55", result.ErrorMessage);
        Assert.Contains("60", result.ErrorMessage);
        Assert.Contains("Iloilo Jaro Warehouse", result.ErrorMessage);
        Assert.Empty(fx.Requests.Items);
    }

    [Fact]
    public async Task Create_rejects_when_warehouse_available_is_zero()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 0m);

        var result = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 1m)]),
            Actor,
            Branch);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, result.ErrorCode);
        Assert.Contains("out of stock", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_rejects_weighted_over_available()
    {
        var fx = await Fixture.CreateAsync();
        var weight = CatalogProduct.Create(
            PosOrganizationId.From(Org),
            "Banana Lakatan",
            UnitOfMeasure.Kilogram,
            100m,
            Utc,
            sellingMode: SellingMode.ByWeight);
        fx.Products.Items.Add(weight);
        fx.SeedWarehouseStock(weight.Id.Value, 55m);

        var result = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(weight.Id.Value, 55.001m)]),
            Actor,
            Branch);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, result.ErrorCode);
        Assert.Contains("Banana Lakatan", result.ErrorMessage);
        Assert.Contains("55", result.ErrorMessage);
    }

    [Fact]
    public async Task Create_does_not_use_other_warehouse_or_org_stock_to_bypass()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 2m);
        // Extra stock at retail branch must not authorize warehouse request qty.
        fx.Balances.Items.Add(
            InventoryBranchBalance.Create(
                PosOrganizationId.From(Org),
                PosBranchId.From(Branch),
                CatalogProductId.From(fx.ProductId),
                500m,
                Utc));
        // Extra stock at a different warehouse must not authorize either.
        var otherWarehouse = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        fx.SeedWarehouseStock(fx.ProductId, 900m, PosBranchId.From(otherWarehouse));

        var result = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 10m)]),
            Actor,
            Branch);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, result.ErrorCode);
        Assert.Contains("2", result.ErrorMessage);
    }

    [Fact]
    public async Task Create_does_not_move_or_reserve_stock()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 100m);
        var before = fx.Balances.Items.Single(b =>
            b.ProductId.Value == fx.ProductId && b.BranchId.Value == Warehouse);

        var result = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 25m)]),
            Actor,
            Branch);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        var after = fx.Balances.Items.Single(b =>
            b.ProductId.Value == fx.ProductId && b.BranchId.Value == Warehouse);
        Assert.Equal(before.OnHandQuantity, after.OnHandQuantity);
        Assert.Equal(before.AvailableQuantity, after.AvailableQuantity);
        Assert.Empty(fx.Transfers.Items);
    }

    [Fact]
    public async Task Approve_lower_qty_keeps_requested_and_notifies_destination()
    {
        var fx = await Fixture.CreateAsync();
        var created = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 10m)]),
            Actor,
            Branch);
        Assert.True(created.IsSuccess);

        var approved = await fx.Approve.ExecuteAsync(
            Org,
            created.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 6m)]),
            Actor,
            Warehouse);

        Assert.True(approved.IsSuccess);
        Assert.Equal("Approved", approved.Value!.Status);
        Assert.Equal(6m, approved.Value.Lines[0].ApprovedQuantity);
        Assert.Equal(10m, approved.Value.Lines[0].RequestedQuantity);
        Assert.Contains(
            fx.Notifications.Items,
            n => n.RelatedType == StockRequestNotificationTypes.Approved && n.TargetBranchId == Branch);
    }

    [Fact]
    public async Task Pending_stock_request_has_zero_commitment()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 100m);
        var created = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 40m)]),
            Actor,
            Branch);
        Assert.True(created.IsSuccess);

        var committed = await fx.Commitments.SumRemainingToDispatchByProductAsync(
            PosOrganizationId.From(Org),
            PosBranchId.From(Warehouse),
            [CatalogProductId.From(fx.ProductId)]);
        Assert.Equal(0m, committed.GetValueOrDefault(fx.ProductId));
    }

    [Fact]
    public async Task Approve_commits_approved_qty_without_changing_reserved_or_on_hand()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 100m);
        var balance = fx.Balances.Items.Single(b =>
            b.ProductId.Value == fx.ProductId && b.BranchId.Value == Warehouse);
        balance.Reserve(10m, Utc);

        var created = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 70m)]),
            Actor,
            Branch);
        Assert.True(created.IsSuccess);

        var approved = await fx.Approve.ExecuteAsync(
            Org,
            created.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 70m)]),
            Actor,
            Warehouse);
        Assert.True(approved.IsSuccess, $"{approved.ErrorCode}: {approved.ErrorMessage}");

        var after = fx.Balances.Items.Single(b =>
            b.ProductId.Value == fx.ProductId && b.BranchId.Value == Warehouse);
        Assert.Equal(100m, after.OnHandQuantity);
        Assert.Equal(10m, after.ReservedQuantity);

        var committed = await fx.Commitments.SumRemainingToDispatchByProductAsync(
            PosOrganizationId.From(Org),
            PosBranchId.From(Warehouse),
            [CatalogProductId.From(fx.ProductId)]);
        Assert.Equal(70m, committed[fx.ProductId]);
        Assert.Equal(20m, after.AvailableQuantity - committed[fx.ProductId]);
    }

    [Fact]
    public async Task Multiple_open_requests_aggregate_commitment_by_product()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 100m);

        var first = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 30m)]),
            Actor,
            Branch);
        var second = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 25m)]),
            Actor,
            Branch);
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        Assert.True((await fx.Approve.ExecuteAsync(
            Org,
            first.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 30m)]),
            Actor,
            Warehouse)).IsSuccess);
        Assert.True((await fx.Approve.ExecuteAsync(
            Org,
            second.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 25m)]),
            Actor,
            Warehouse)).IsSuccess);

        var committed = await fx.Commitments.SumRemainingToDispatchByProductAsync(
            PosOrganizationId.From(Org),
            PosBranchId.From(Warehouse),
            [CatalogProductId.From(fx.ProductId)]);
        Assert.Equal(55m, committed[fx.ProductId]);
    }

    [Fact]
    public async Task Commitment_is_isolated_by_source_warehouse()
    {
        var fx = await Fixture.CreateAsync();
        var otherWarehouse = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        fx.Branches.ExtraWarehouseIds.Add(otherWarehouse);
        fx.SeedWarehouseStock(fx.ProductId, 100m);
        fx.SeedWarehouseStock(fx.ProductId, 100m, PosBranchId.From(otherWarehouse));
        await fx.Routes.AddAsync(
            SupplyRoute.Create(
                PosOrganizationId.From(Org),
                PosBranchId.From(otherWarehouse),
                PosBranchId.From(Branch),
                Utc,
                isPreferred: false));

        var created = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 40m)]),
            Actor,
            Branch);
        Assert.True(created.IsSuccess);
        Assert.True((await fx.Approve.ExecuteAsync(
            Org,
            created.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 40m)]),
            Actor,
            Warehouse)).IsSuccess);

        var atWarehouse = await fx.Commitments.SumRemainingToDispatchByProductAsync(
            PosOrganizationId.From(Org),
            PosBranchId.From(Warehouse),
            [CatalogProductId.From(fx.ProductId)]);
        var atOther = await fx.Commitments.SumRemainingToDispatchByProductAsync(
            PosOrganizationId.From(Org),
            PosBranchId.From(otherWarehouse),
            [CatalogProductId.From(fx.ProductId)]);
        Assert.Equal(40m, atWarehouse[fx.ProductId]);
        Assert.Equal(0m, atOther.GetValueOrDefault(fx.ProductId));
    }

    [Fact]
    public async Task Approve_rejects_when_over_available_after_other_commitments_and_sales_reserved()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 100m);
        fx.Balances.Items.Single(b =>
            b.ProductId.Value == fx.ProductId && b.BranchId.Value == Warehouse).Reserve(10m, Utc);

        var other = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 30m)]),
            Actor,
            Branch);
        Assert.True(other.IsSuccess);
        Assert.True((await fx.Approve.ExecuteAsync(
            Org,
            other.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 30m)]),
            Actor,
            Warehouse)).IsSuccess);

        // Available for new approval = 100 - 10 reserved - 30 committed = 60
        var over = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 70m)]),
            Actor,
            Branch);
        Assert.True(over.IsSuccess);
        var approved = await fx.Approve.ExecuteAsync(
            Org,
            over.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 70m)]),
            Actor,
            Warehouse);
        Assert.False(approved.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, approved.ErrorCode);
    }

    [Fact]
    public async Task Sequential_approvals_cannot_overcommit_same_product()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 50m);

        var a = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 40m)]),
            Actor,
            Branch);
        var b = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 40m)]),
            Actor,
            Branch);
        Assert.True(a.IsSuccess);
        Assert.True(b.IsSuccess);

        Assert.True((await fx.Approve.ExecuteAsync(
            Org,
            a.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 40m)]),
            Actor,
            Warehouse)).IsSuccess);

        var second = await fx.Approve.ExecuteAsync(
            Org,
            b.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 40m)]),
            Actor,
            Warehouse);
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, second.ErrorCode);
    }

    [Fact]
    public async Task Reject_releases_commitment()
    {
        var fx = await Fixture.CreateAsync();
        fx.SeedWarehouseStock(fx.ProductId, 80m);
        var created = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 50m)]),
            Actor,
            Branch);
        Assert.True(created.IsSuccess);
        Assert.True((await fx.Approve.ExecuteAsync(
            Org,
            created.Value!.StockRequestId,
            new ApproveStockRequestRequest([new ApproveStockRequestLineRequest(fx.ProductId, 50m)]),
            Actor,
            Warehouse)).IsSuccess);

        Assert.True((await fx.Reject.ExecuteAsync(
            Org,
            created.Value.StockRequestId,
            new RejectStockRequestRequest("No longer needed"),
            Actor,
            Warehouse)).IsSuccess);

        var committed = await fx.Commitments.SumRemainingToDispatchByProductAsync(
            PosOrganizationId.From(Org),
            PosBranchId.From(Warehouse),
            [CatalogProductId.From(fx.ProductId)]);
        Assert.Equal(0m, committed.GetValueOrDefault(fx.ProductId));
    }

    [Fact]
    public async Task Decline_notifies_destination()
    {
        var fx = await Fixture.CreateAsync();
        var created = await fx.Create.ExecuteAsync(
            Org,
            new CreateStockRequestRequest(Branch, Warehouse, [new StockRequestLineRequest(fx.ProductId, 4m)]),
            Actor,
            Branch);

        var declined = await fx.Reject.ExecuteAsync(
            Org,
            created.Value!.StockRequestId,
            new RejectStockRequestRequest("No stock"),
            Actor,
            Warehouse);

        Assert.True(declined.IsSuccess);
        Assert.Equal("Rejected", declined.Value!.Status);
        Assert.Contains(
            fx.Notifications.Items,
            n => n.RelatedType == StockRequestNotificationTypes.Declined && n.TargetBranchId == Branch);
    }

    [Fact]
    public async Task Transfer_alert_sink_skips_stock_request_linked_alerts()
    {
        var notifications = new CapturingNotifications();
        var sink = new OrganizationBusinessInventoryTransferAlertSink(notifications);

        await sink.PublishAsync(
            new InventoryTransferAlert(
                "dispatched",
                Org,
                Branch,
                Guid.NewGuid(),
                "TR-1",
                "on the way",
                StockRequestId: Guid.NewGuid()));
        Assert.Empty(notifications.Items);

        await sink.PublishAsync(
            new InventoryTransferAlert(
                "dispatched",
                Org,
                Branch,
                Guid.NewGuid(),
                "TR-2",
                "on the way"));
        Assert.Contains(notifications.Items, n => n.RelatedType == InventoryTransferNotificationTypes.Dispatched);
    }

    private sealed class Fixture
    {
        public Guid ProductId { get; private set; }
        public InMemoryCatalog Products { get; } = new();
        public InMemoryStockRequests Requests { get; } = new();
        public InMemoryRoutes Routes { get; } = new();
        public InMemoryTransfers Transfers { get; } = new();
        public InMemoryBalances Balances { get; } = new();
        public InMemoryInventory Inventory { get; } = new();
        public CapturingNotifications Notifications { get; } = new();
        public ImmediateUnitOfWork UnitOfWork { get; } = new();
        public FixedClock Clock { get; } = new(Utc);
        public FakeBranches Branches { get; } = new();
        public CreateStockRequest Create { get; private set; } = null!;
        public ApproveStockRequest Approve { get; private set; } = null!;
        public RejectStockRequest Reject { get; private set; } = null!;
        public StockRequestCommitmentQuery Commitments { get; private set; } = null!;

        public void SeedWarehouseStock(Guid productId, decimal available, PosBranchId? branchId = null)
        {
            var orgId = PosOrganizationId.From(Org);
            var product = CatalogProductId.From(productId);
            var warehouseBranch = branchId ?? PosBranchId.From(Warehouse);
            Balances.Items.RemoveAll(b => b.ProductId == product && b.BranchId == warehouseBranch);
            Balances.Items.Add(
                InventoryBranchBalance.Create(orgId, warehouseBranch, product, Math.Max(0m, available), Utc));
        }

        public static async Task<Fixture> CreateAsync()
        {
            var fx = new Fixture();
            var product = CatalogProduct.Create(
                PosOrganizationId.From(Org),
                "Rice 5kg",
                UnitOfMeasure.Piece,
                50m,
                Utc);
            fx.ProductId = product.Id.Value;
            fx.Products.Items.Add(product);
            fx.SeedWarehouseStock(fx.ProductId, 1_000m);

            await fx.Routes.AddAsync(
                SupplyRoute.Create(
                    PosOrganizationId.From(Org),
                    PosBranchId.From(Warehouse),
                    PosBranchId.From(Branch),
                    Utc,
                    isPreferred: true));

            var queries = new StockRequestQueryService(fx.Requests, fx.Transfers, fx.Branches);
            fx.Commitments = new StockRequestCommitmentQuery(fx.Requests, fx.Transfers);
            fx.Create = new CreateStockRequest(
                fx.Requests,
                fx.Routes,
                fx.Products,
                fx.Branches,
                fx.Balances,
                queries,
                fx.Notifications,
                fx.UnitOfWork,
                fx.Clock);
            fx.Approve = new ApproveStockRequest(
                fx.Requests,
                fx.Inventory,
                fx.Balances,
                fx.Commitments,
                queries,
                fx.Notifications,
                fx.UnitOfWork,
                fx.Clock);
            fx.Reject = new RejectStockRequest(
                fx.Requests, fx.Transfers, queries, fx.Notifications, fx.UnitOfWork, fx.Clock);
            return fx;
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
        public HashSet<Guid> ExtraWarehouseIds { get; } = [];

        public Task<bool> ExistsInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(organizationId == Org && (branchId == Warehouse || branchId == Branch || ExtraWarehouseIds.Contains(branchId)));

        public Task<bool> IsActiveInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            ExistsInOrganizationAsync(organizationId, branchId, cancellationToken);

        public Task<string> GetBranchTypeAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(branchId == Branch ? "Retail" : "Warehouse");

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(
                    id => id,
                    id => id == Warehouse
                        ? "Iloilo Jaro Warehouse"
                        : id == Branch
                            ? "Pac Passi"
                            : "Other Warehouse"));

        public Task<Guid?> GetPrimaryBranchIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(Warehouse);
    }

    private sealed class CapturingNotifications : IOrganizationBusinessNotificationPublisher
    {
        public List<(string RelatedType, Guid? TargetBranchId, string RelatedId)> Items { get; } = [];

        public Task PublishAsync(
            Guid sourceOrganizationId,
            Guid recipientOrganizationId,
            string relatedType,
            string relatedId,
            string title,
            string preview,
            CancellationToken cancellationToken = default,
            Guid? targetBranchId = null)
        {
            Items.Add((relatedType, targetBranchId, relatedId));
            return Task.CompletedTask;
        }

        public Task MarkRelatedReadAsync(
            Guid organizationId,
            string relatedType,
            string relatedId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryRoutes : ISupplyRouteRepository
    {
        public List<SupplyRoute> Items { get; } = [];

        public Task<SupplyRoute?> GetByIdAsync(PosOrganizationId organizationId, SupplyRouteId routeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == routeId));

        public Task<IReadOnlyList<SupplyRoute>> ListAllAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplyRoute>>(Items.Where(r => r.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<SupplyRoute>> ListByDestinationAsync(
            PosOrganizationId organizationId,
            PosBranchId destinationLocationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplyRoute>>(
                Items.Where(r => r.OrganizationId == organizationId && r.DestinationLocationId == destinationLocationId).ToList());

        public Task<IReadOnlyList<SupplyRoute>> ListBySourceAsync(
            PosOrganizationId organizationId,
            PosBranchId sourceLocationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplyRoute>>(
                Items.Where(r => r.OrganizationId == organizationId && r.SourceLocationId == sourceLocationId).ToList());

        public Task AddAsync(SupplyRoute route, CancellationToken cancellationToken = default)
        {
            Items.Add(route);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SupplyRoute route, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryStockRequests : IStockRequestRepository
    {
        public List<StockRequest> Items { get; } = [];
        private long _seq = 1;

        public Task<StockRequest?> GetByIdAsync(
            PosOrganizationId organizationId,
            StockRequestId stockRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == stockRequestId));

        public Task<(IReadOnlyList<StockRequest> Items, int TotalCount)> ListByDestinationAsync(
            PosOrganizationId organizationId,
            PosBranchId destinationLocationId,
            int skip,
            int take,
            IReadOnlyCollection<StockRequestStatus>? statuses = null,
            CancellationToken cancellationToken = default)
        {
            var q = Items.Where(r => r.OrganizationId == organizationId && r.DestinationLocationId == destinationLocationId);
            if (statuses is { Count: > 0 })
            {
                var set = statuses.ToHashSet();
                q = q.Where(r => set.Contains(r.Status)
                    || (set.Contains(StockRequestStatus.Preparing) && r.Status == StockRequestStatus.InProgress)
                    || (set.Contains(StockRequestStatus.InProgress) && r.Status == StockRequestStatus.Preparing));
            }

            var list = q.OrderByDescending(r => r.UpdatedAtUtc).ToList();
            return Task.FromResult<(IReadOnlyList<StockRequest>, int)>((list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<StockRequest> Items, int TotalCount)> ListBySourceAsync(
            PosOrganizationId organizationId,
            PosBranchId sourceLocationId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var q = Items.Where(r => r.OrganizationId == organizationId && r.RequestedSourceLocationId == sourceLocationId).ToList();
            return Task.FromResult<(IReadOnlyList<StockRequest>, int)>((q.Skip(skip).Take(take).ToList(), q.Count));
        }

        public Task<IReadOnlyList<StockRequest>> ListOpenCommittingBySourceAndProductIdsAsync(
            PosOrganizationId organizationId,
            PosBranchId sourceLocationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            var productSet = productIds.Select(p => p.Value).ToHashSet();
            var open = StockRequestCommitmentQuery.OpenCommittingStatuses.ToHashSet();
            var list = Items
                .Where(r =>
                    r.OrganizationId == organizationId
                    && r.RequestedSourceLocationId == sourceLocationId
                    && (open.Contains(r.Status)
                        || r.Status is StockRequestStatus.InProgress or StockRequestStatus.Preparing)
                    && r.Lines.Any(l => productSet.Contains(l.ProductId.Value)))
                .ToList();
            return Task.FromResult<IReadOnlyList<StockRequest>>(list);
        }

        public Task<IReadOnlyDictionary<string, int>> CountByDestinationStatusAsync(
            PosOrganizationId organizationId,
            PosBranchId destinationLocationId,
            CancellationToken cancellationToken = default)
        {
            var raw = Items
                .Where(r => r.OrganizationId == organizationId && r.DestinationLocationId == destinationLocationId)
                .GroupBy(r => r.Status == StockRequestStatus.InProgress
                    ? nameof(StockRequestStatus.InProgress)
                    : StockRequestStatuses.ToCode(r.Status))
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            return Task.FromResult<IReadOnlyDictionary<string, int>>(raw);
        }

        public Task<IReadOnlyList<StockRequest>> ListRecentByDestinationAsync(
            PosOrganizationId organizationId,
            PosBranchId destinationLocationId,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = Items
                .Where(r => r.OrganizationId == organizationId && r.DestinationLocationId == destinationLocationId)
                .OrderByDescending(r => r.UpdatedAtUtc)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<StockRequest>>(list);
        }

        public Task AddAsync(StockRequest stockRequest, CancellationToken cancellationToken = default)
        {
            Items.Add(stockRequest);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StockRequest stockRequest, CancellationToken cancellationToken = default)
        {
            var idx = Items.FindIndex(r => r.Id == stockRequest.Id);
            if (idx >= 0)
            {
                Items[idx] = stockRequest;
            }

            return Task.CompletedTask;
        }

        public Task<string> AllocateNextNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(StockRequestNumbers.Format(businessDateUtc, _seq++));
    }

    private sealed class InMemoryTransfers : IInventoryTransferRepository
    {
        public List<InventoryTransfer> Items { get; } = [];

        public Task<InventoryTransfer?> GetByIdAsync(
            PosOrganizationId organizationId,
            InventoryTransferId transferId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(t => t.Id == transferId));

        public Task<(IReadOnlyList<InventoryTransfer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            InventoryTransferFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<InventoryTransfer>, int)>((Items, Items.Count));

        public Task<IReadOnlyList<InventoryTransfer>> ListByStockRequestIdAsync(
            PosOrganizationId organizationId,
            StockRequestId stockRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryTransfer>>(
                Items.Where(t => t.StockRequestId == stockRequestId).ToList());

        public Task<IReadOnlyList<InventoryTransfer>> ListByStockRequestIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<StockRequestId> stockRequestIds,
            CancellationToken cancellationToken = default)
        {
            var ids = stockRequestIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<InventoryTransfer>>(
                Items.Where(t => t.StockRequestId is StockRequestId sid && ids.Contains(sid)).ToList());
        }

        public Task<IReadOnlyList<InventoryTransfer>> ListByRootTransferIdAsync(
            PosOrganizationId organizationId,
            InventoryTransferId rootTransferId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryTransfer>>(
                Items.Where(t => t.Id == rootTransferId || t.RootTransferId == rootTransferId).ToList());

        public Task<IReadOnlyDictionary<Guid, string?>> GetTransferNumbersAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> transferIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string?>>(
                Items.Where(t => transferIds.Contains(t.Id.Value))
                    .ToDictionary(t => t.Id.Value, t => t.TransferNumber));

        public Task<IReadOnlyDictionary<Guid, InventoryTransferQueueHint>> GetTransferQueueHintsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> transferIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, InventoryTransferQueueHint>>(
                Items.Where(t => transferIds.Contains(t.Id.Value))
                    .ToDictionary(
                        t => t.Id.Value,
                        t => new InventoryTransferQueueHint(t.TransferNumber, t.DestinationBranchId.Value)));

        public Task AddAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default)
        {
            Items.Add(transfer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyDictionary<Guid, InventoryTransferTransactionRef>> ResolveStockMovementTransactionRefsAsync(
            PosOrganizationId organizationId,
            IReadOnlyList<StockMovement> movements,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, InventoryTransferTransactionRef>>(
                new Dictionary<Guid, InventoryTransferTransactionRef>());

        public Task<IReadOnlyList<InventoryTransferOpenCommitment>> ListOpenCommitmentsForBranchAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            IReadOnlyCollection<CatalogProductId>? productIds = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryTransferOpenCommitment>>([]);

        public Task<string> AllocateNextNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("IT-20260906-000001");
    }

    private sealed class InMemoryBalances : IInventoryBranchBalanceRepository
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
                Items.Where(b => b.OrganizationId == organizationId && productIds.Any(id => id == b.ProductId)).ToList());

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

    private sealed class InMemoryInventory : IInventoryRepository
    {
        public Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<InventoryAccount?>(null);

        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>([]);

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            InventoryAccountFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<InventoryAccount>, int)>(([], 0));

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(
            PosOrganizationId organizationId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<InventoryAccount>, int)>(([], 0));

        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryAccount>>([]);

        public Task AddAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ExecuteWithProductReservationLocksAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            Func<IReadOnlyList<InventoryAccount>, CancellationToken, Task> action,
            CancellationToken cancellationToken = default) =>
            action([], cancellationToken);

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<StockMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            StockMovementId movementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StockMovement?>(null);

        public Task<bool> HasAnyMovementAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

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
            Task.FromResult<(IReadOnlyList<StockMovement>, int)>(([], 0));

        public Task<decimal> SumMovementEffectsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(
            PosOrganizationId organizationId,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<InventoryAccount>, int)>(([], 0));

        public Task<bool> HasStockCountVarianceAsync(
            PosOrganizationId organizationId,
            StockCountId stockCountId,
            CatalogProductId productId,
            StockMovementType movementType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StockMovement>>([]);

        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StockMovement>>([]);

        public Task<bool> HasSaleDeductionAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasCustomerOrderDeductionAsync(
            PosOrganizationId organizationId,
            CustomerOrderId orderId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasSaleVoidRestorationAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasPurchaseReceiptAsync(
            PosOrganizationId organizationId,
            GoodsReceiptId goodsReceiptId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasDirectPurchaseReceiptAsync(
            PosOrganizationId organizationId,
            DirectPurchaseReceiptId receiptId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasStockUseAsync(
            PosOrganizationId organizationId,
            StockUseId stockUseId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasStockUseVoidRestorationAsync(
            PosOrganizationId organizationId,
            StockUseId stockUseId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasProductionMaterialConsumptionAsync(
            PosOrganizationId organizationId,
            ProductionRunId productionRunId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasProductionMaterialRestorationAsync(
            PosOrganizationId organizationId,
            ProductionRunId productionRunId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasProductionOutputAsync(
            PosOrganizationId organizationId,
            ProductionRunId productionRunId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasProductionOutputReversalAsync(
            PosOrganizationId organizationId,
            ProductionRunId productionRunId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasWasteLossAsync(
            PosOrganizationId organizationId,
            WasteLossId wasteLossId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasWasteLossVoidRestorationAsync(
            PosOrganizationId organizationId,
            WasteLossId wasteLossId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
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

        public Task<bool> HasConnectedPurchaseFulfillmentAsync(
            PosOrganizationId organizationId,
            ConnectedPurchaseOrderId connectedPurchaseOrderId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<decimal?> GetLatestAcquisitionUnitCostAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<decimal?>(null);

        public Task<IReadOnlyDictionary<Guid, decimal?>> GetLatestAcquisitionUnitCostsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal?>>(new Dictionary<Guid, decimal?>());

        public Task<bool> HasSaleReturnRestockAsync(
            PosOrganizationId organizationId,
            SaleReturnId saleReturnId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasInventoryTransferMovementAsync(
            PosOrganizationId organizationId,
            InventoryTransferId transferId,
            CatalogProductId productId,
            StockMovementType movementType,
            InventoryLotId? lotId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(DateTimeOffset?, int)>((null, 0));

        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, (DateTimeOffset?, int)>>(
                new Dictionary<Guid, (DateTimeOffset?, int)>());
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
}
