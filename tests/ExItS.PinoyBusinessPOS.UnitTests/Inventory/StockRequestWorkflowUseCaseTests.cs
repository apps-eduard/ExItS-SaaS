using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

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
        public CapturingNotifications Notifications { get; } = new();
        public ImmediateUnitOfWork UnitOfWork { get; } = new();
        public FixedClock Clock { get; } = new(Utc);
        public FakeBranches Branches { get; } = new();
        public CreateStockRequest Create { get; private set; } = null!;
        public ApproveStockRequest Approve { get; private set; } = null!;
        public RejectStockRequest Reject { get; private set; } = null!;

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

            await fx.Routes.AddAsync(
                SupplyRoute.Create(
                    PosOrganizationId.From(Org),
                    PosBranchId.From(Warehouse),
                    PosBranchId.From(Branch),
                    Utc,
                    isPreferred: true));

            var queries = new StockRequestQueryService(fx.Requests, fx.Transfers, fx.Branches);
            fx.Create = new CreateStockRequest(
                fx.Requests, fx.Routes, fx.Products, fx.Branches, queries, fx.Notifications, fx.UnitOfWork, fx.Clock);
            fx.Approve = new ApproveStockRequest(
                fx.Requests, queries, fx.Notifications, fx.UnitOfWork, fx.Clock);
            fx.Reject = new RejectStockRequest(
                fx.Requests, queries, fx.Notifications, fx.UnitOfWork, fx.Clock);
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
        public Task<bool> ExistsInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(organizationId == Org && (branchId == Warehouse || branchId == Branch));

        public Task<bool> IsActiveInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            ExistsInOrganizationAsync(organizationId, branchId, cancellationToken);

        public Task<string> GetBranchTypeAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(branchId == Warehouse ? "Warehouse" : "Retail");

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(id => id, id => id == Warehouse ? "Warehouse" : "Branch"));

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
        public Task<InventoryTransfer?> GetByIdAsync(
            PosOrganizationId organizationId,
            InventoryTransferId transferId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<InventoryTransfer?>(null);

        public Task<(IReadOnlyList<InventoryTransfer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            InventoryTransferFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<InventoryTransfer>, int)>(([], 0));

        public Task<IReadOnlyList<InventoryTransfer>> ListByStockRequestIdAsync(
            PosOrganizationId organizationId,
            StockRequestId stockRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryTransfer>>([]);

        public Task AddAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(InventoryTransfer transfer, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string> AllocateNextNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("IT-20260906-000001");
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
