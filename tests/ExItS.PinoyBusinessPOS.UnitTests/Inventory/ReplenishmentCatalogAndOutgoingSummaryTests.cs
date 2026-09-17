using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class ReplenishmentCatalogAndOutgoingSummaryTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Warehouse = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid RetailOnly = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Utc = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Catalog_search_by_name_and_sku_is_forwarded_and_returns_warehouse_stock()
    {
        var fx = await Fixture.CreateAsync();
        var productId = Guid.Parse("99999999-9999-9999-9999-999999999901");
        fx.Catalog.Rows =
        [
            new ReplenishmentCatalogRow(
                productId,
                "Rice 5kg",
                "SKU-RICE",
                "480001",
                null,
                null,
                "Piece",
                BranchOnHandQuantity: 2m,
                WarehouseAvailableQuantity: 40m,
                IsLowStock: true,
                IsTracked: true,
                SellingMode: "PerItem")
        ];
        fx.Inventory.Costs[productId] = 33.25m;
        fx.Prices.Prices[productId] = 55m;

        var byName = await fx.CatalogUseCase.ExecuteAsync(
            fx.RetailContext,
            Warehouse,
            search: "Rice",
            stockFilter: "all",
            categoryId: null,
            page: 1,
            pageSize: 40);
        Assert.True(byName.IsSuccess);
        Assert.Equal("Rice", fx.Catalog.LastFilter!.Search);
        Assert.Equal(40m, byName.Value!.Items[0].WarehouseAvailableQuantity);
        Assert.Equal(2m, byName.Value.Items[0].BranchOnHandQuantity);
        Assert.Equal("PerItem", byName.Value.Items[0].SellingMode);
        Assert.Equal(33.25m, byName.Value.Items[0].WarehouseUnitCost);
        Assert.Equal(55m, byName.Value.Items[0].BranchEffectiveSellingPrice);

        var bySku = await fx.CatalogUseCase.ExecuteAsync(
            fx.RetailContext,
            Warehouse,
            search: "SKU-RICE",
            stockFilter: null,
            categoryId: null,
            page: 1,
            pageSize: 40);
        Assert.True(bySku.IsSuccess);
        Assert.Equal("SKU-RICE", fx.Catalog.LastFilter!.Search);
        Assert.Equal(Warehouse, bySku.Value!.SupplyWarehouseBranchId);
        Assert.Equal("Main Warehouse", bySku.Value.SupplyWarehouseName);
    }

    [Fact]
    public async Task Catalog_rejects_invalid_warehouse_no_route_and_cross_org()
    {
        var fx = await Fixture.CreateAsync();

        var notWarehouse = await fx.CatalogUseCase.ExecuteAsync(
            fx.RetailContext,
            RetailOnly,
            search: null,
            stockFilter: "all",
            categoryId: null,
            page: 1,
            pageSize: 40);
        Assert.False(notWarehouse.IsSuccess);
        Assert.Equal(DomainErrorCodes.StockRequestSourceMustBeWarehouse, notWarehouse.ErrorCode);

        fx.Routes.Items.Clear();
        var noRoute = await fx.CatalogUseCase.ExecuteAsync(
            fx.RetailContext,
            Warehouse,
            search: null,
            stockFilter: "all",
            categoryId: null,
            page: 1,
            pageSize: 40);
        Assert.False(noRoute.IsSuccess);
        Assert.Equal(DomainErrorCodes.StockRequestRouteRequired, noRoute.ErrorCode);

        await fx.Routes.AddAsync(
            SupplyRoute.Create(
                PosOrganizationId.From(Org),
                PosBranchId.From(Warehouse),
                PosBranchId.From(Branch),
                Utc,
                isPreferred: true));

        var crossOrg = await fx.CatalogUseCase.ExecuteAsync(
            fx.RetailContext,
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            search: null,
            stockFilter: "all",
            categoryId: null,
            page: 1,
            pageSize: 40);
        Assert.False(crossOrg.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InventoryTransferBranchNotFound, crossOrg.ErrorCode);
    }

    [Fact]
    public async Task Outgoing_summary_counts_and_recent_top_five()
    {
        var fx = await Fixture.CreateAsync();
        var productId = CatalogProductId.From(Guid.Parse("99999999-9999-9999-9999-999999999902"));
        var draft = new StockRequestLineDraft(productId, 5m, "Item", UnitOfMeasure.Piece, SellingMode.PerItem);

        async Task<StockRequest> AddAsync(StockRequestStatus target, int minutes)
        {
            var request = StockRequest.Create(
                PosOrganizationId.From(Org),
                PosBranchId.From(Branch),
                PosBranchId.From(Warehouse),
                [draft],
                Actor,
                Utc.AddMinutes(minutes),
                requestNumber: StockRequestNumbers.Format(StockRequestNumbers.BusinessDateOf(Utc), minutes));
            if (target is StockRequestStatus.Approved or StockRequestStatus.Preparing or StockRequestStatus.InTransit)
            {
                request.Approve(Actor, Utc.AddMinutes(minutes).AddSeconds(1), new Dictionary<Guid, decimal> { [productId.Value] = 5m });
            }

            if (target is StockRequestStatus.Preparing or StockRequestStatus.InTransit)
            {
                request.StartPreparing(Actor, Utc.AddMinutes(minutes).AddSeconds(2));
            }

            if (target == StockRequestStatus.InTransit)
            {
                request.MarkDispatched(Actor, Utc.AddMinutes(minutes).AddSeconds(3), Guid.NewGuid());
            }

            await fx.Requests.AddAsync(request);
            return request;
        }

        await AddAsync(StockRequestStatus.Pending, 1);
        await AddAsync(StockRequestStatus.Pending, 2);
        await AddAsync(StockRequestStatus.Approved, 3);
        await AddAsync(StockRequestStatus.Preparing, 4);
        await AddAsync(StockRequestStatus.InTransit, 5);
        await AddAsync(StockRequestStatus.InTransit, 6);
        var latest = await AddAsync(StockRequestStatus.Pending, 7);

        var summary = await fx.Queries.GetOutgoingSummaryAsync(Org, Branch);
        Assert.Equal(3, summary.SubmittedCount);
        Assert.Equal(2, summary.InProgressCount);
        Assert.Equal(2, summary.InTransitCount);
        Assert.Equal(5, summary.Recent.Count);
        Assert.Equal(latest.RequestNumber, summary.Recent[0].RequestNumber);
    }

    [Fact]
    public async Task Outgoing_list_filters_by_status()
    {
        var fx = await Fixture.CreateAsync();
        var productId = CatalogProductId.From(Guid.Parse("99999999-9999-9999-9999-999999999903"));
        var draft = new StockRequestLineDraft(productId, 3m, "Item", UnitOfMeasure.Piece, SellingMode.PerItem);

        var pending = StockRequest.Create(
            PosOrganizationId.From(Org),
            PosBranchId.From(Branch),
            PosBranchId.From(Warehouse),
            [draft],
            Actor,
            Utc,
            StockRequestNumbers.Format(StockRequestNumbers.BusinessDateOf(Utc), 1));
        var approved = StockRequest.Create(
            PosOrganizationId.From(Org),
            PosBranchId.From(Branch),
            PosBranchId.From(Warehouse),
            [draft],
            Actor,
            Utc.AddMinutes(1),
            StockRequestNumbers.Format(StockRequestNumbers.BusinessDateOf(Utc), 2));
        approved.Approve(Actor, Utc.AddMinutes(2), new Dictionary<Guid, decimal> { [productId.Value] = 3m });
        await fx.Requests.AddAsync(pending);
        await fx.Requests.AddAsync(approved);

        var filtered = await fx.Queries.ListOutgoingAsync(
            Org,
            Branch,
            page: 1,
            pageSize: 20,
            statuses: [StockRequestStatus.Pending]);
        Assert.Equal(1, filtered.TotalCount);
        Assert.Equal("Pending", filtered.Items[0].Status);

        var approvedOnly = await fx.Queries.ListOutgoingAsync(
            Org,
            Branch,
            page: 1,
            pageSize: 20,
            statuses: [StockRequestStatus.Approved]);
        Assert.Equal(1, approvedOnly.TotalCount);
        Assert.Equal("Approved", approvedOnly.Items[0].Status);
    }

    private sealed class Fixture
    {
        public BranchInventoryContext RetailContext { get; } =
            new(Org, Branch, Warehouse, OrganizationGovernance: true);

        public FakeCatalogQuery Catalog { get; } = new();
        public InMemoryRoutes Routes { get; } = new();
        public InMemoryStockRequests Requests { get; } = new();
        public InMemoryTransfers Transfers { get; } = new();
        public FakeBranches Branches { get; } = new();
        public FakeInventory Inventory { get; } = new();
        public FakeProducts Products { get; } = new();
        public FakeEffectivePrices Prices { get; } = new();
        public ListReplenishmentCatalog CatalogUseCase { get; private set; } = null!;
        public StockRequestQueryService Queries { get; private set; } = null!;

        public static async Task<Fixture> CreateAsync()
        {
            var fx = new Fixture();
            await fx.Routes.AddAsync(
                SupplyRoute.Create(
                    PosOrganizationId.From(Org),
                    PosBranchId.From(Warehouse),
                    PosBranchId.From(Branch),
                    Utc,
                    isPreferred: true));
            fx.CatalogUseCase = new ListReplenishmentCatalog(
                fx.Catalog,
                fx.Routes,
                fx.Branches,
                fx.Inventory,
                fx.Products,
                fx.Prices);
            fx.Queries = new StockRequestQueryService(fx.Requests, fx.Transfers, fx.Branches);
            return fx;
        }
    }

    private sealed class FakeCatalogQuery : IBranchInventoryQueryRepository
    {
        public ReplenishmentCatalogFilter? LastFilter { get; private set; }
        public IReadOnlyList<ReplenishmentCatalogRow> Rows { get; set; } = [];

        public Task<(IReadOnlyList<BranchInventoryListRow> Items, int TotalCount)> ListAsync(
            BranchInventoryContext context,
            BranchInventoryListFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<Guid> ProductIds, int TotalCount)> ListProductIdsAsync(
            BranchInventoryContext context,
            BranchInventoryListFilter filter,
            int maxTake,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<ReplenishmentCatalogRow> Items, int TotalCount)> ListReplenishmentCatalogAsync(
            BranchInventoryContext retailContext,
            ReplenishmentCatalogFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return Task.FromResult<(IReadOnlyList<ReplenishmentCatalogRow>, int)>((Rows, Rows.Count));
        }
    }

    private sealed class FakeBranches : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                organizationId == Org
                && (branchId == Warehouse || branchId == Branch || branchId == RetailOnly));

        public Task<bool> IsActiveInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            ExistsInOrganizationAsync(organizationId, branchId, cancellationToken);

        public Task<string> GetBranchTypeAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(branchId == Warehouse ? "Warehouse" : "Retail");

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(
                    id => id,
                    id => id == Warehouse ? "Main Warehouse" : id == Branch ? "Retail" : "Other"));

        public Task<Guid?> GetPrimaryBranchIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(Warehouse);
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

    private sealed class FakeInventory : CostResolverInventoryStub;

    private sealed class FakeProducts : ICatalogProductRepository
    {
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                productIds.Select(id => CatalogProduct.Create(
                    organizationId,
                    $"P-{id.Value:N}",
                    UnitOfMeasure.Piece,
                    10m,
                    Utc,
                    id: id)).ToList());
        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlyList<Guid>> ListIdsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0, 0));
        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid? CategoryId, int Count)>>([]);
        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
    }

    private sealed class FakeEffectivePrices : IEffectivePriceResolver
    {
        public Dictionary<Guid, decimal> Prices { get; } = new();

        public Task<IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult>> ResolveAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            IReadOnlyList<CatalogProduct> products,
            IReadOnlyDictionary<CatalogProductId, IReadOnlyList<CatalogProductUnit>>? unitsByProduct = null,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<EffectivePriceKey, EffectivePriceResult>();
            foreach (var product in products)
            {
                var price = Prices.TryGetValue(product.Id.Value, out var overridePrice)
                    ? overridePrice
                    : product.SellingPrice;
                result[EffectivePriceKeys.ForBaseProduct(product.Id.Value)] =
                    new EffectivePriceResult(product.SellingPrice, null, price, false);
            }

            return Task.FromResult<IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult>>(result);
        }
    }
}
