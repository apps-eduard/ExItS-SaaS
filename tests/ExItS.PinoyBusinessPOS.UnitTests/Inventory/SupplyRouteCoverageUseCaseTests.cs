using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class SupplyRouteCoverageUseCaseTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WarehouseA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid WarehouseB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid RetailA = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid RetailB = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset Utc = new(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Coverage_rejects_retail_source()
    {
        var routes = new FakeRouteRepo();
        var branches = new FakeBranches();
        var useCase = new UpsertSupplyCoverageBySource(routes, branches, new FakeUow(), new FixedClock(Utc));

        var result = await useCase.ExecuteAsync(
            Org,
            new UpsertSupplyCoverageBySourceRequest(RetailA, [RetailB]));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.SupplyRouteSourceMustBeWarehouse, result.ErrorCode);
    }

    [Fact]
    public async Task Coverage_accepts_warehouse_to_retail_and_warehouse_to_warehouse()
    {
        var routes = new FakeRouteRepo();
        var branches = new FakeBranches();
        var useCase = new UpsertSupplyCoverageBySource(routes, branches, new FakeUow(), new FixedClock(Utc));

        var result = await useCase.ExecuteAsync(
            Org,
            new UpsertSupplyCoverageBySourceRequest(WarehouseA, [RetailA, WarehouseB]));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count(r => r.IsActive));
        Assert.Contains(result.Value!, r => r.DestinationLocationId == RetailA);
        Assert.Contains(result.Value!, r => r.DestinationLocationId == WarehouseB);
    }

    [Fact]
    public async Task Coverage_rejects_same_source_destination()
    {
        var useCase = new UpsertSupplyCoverageBySource(
            new FakeRouteRepo(),
            new FakeBranches(),
            new FakeUow(),
            new FixedClock(Utc));

        var result = await useCase.ExecuteAsync(
            Org,
            new UpsertSupplyCoverageBySourceRequest(WarehouseA, [WarehouseA]));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.SupplyRouteSameLocation, result.ErrorCode);
    }

    [Fact]
    public async Task Multiple_warehouses_may_cover_same_retail_branch()
    {
        var routes = new FakeRouteRepo();
        var branches = new FakeBranches();
        var clock = new FixedClock(Utc);
        var uow = new FakeUow();
        var a = new UpsertSupplyCoverageBySource(routes, branches, uow, clock);
        var b = new UpsertSupplyCoverageBySource(routes, branches, uow, clock);

        Assert.True((await a.ExecuteAsync(Org, new UpsertSupplyCoverageBySourceRequest(WarehouseA, [RetailA]))).IsSuccess);
        Assert.True((await b.ExecuteAsync(Org, new UpsertSupplyCoverageBySourceRequest(WarehouseB, [RetailA]))).IsSuccess);

        var forDest = await routes.ListByDestinationAsync(
            PosOrganizationId.From(Org),
            PosBranchId.From(RetailA));
        Assert.Equal(2, forDest.Count(r => r.IsActive));
    }

    [Fact]
    public async Task Second_warehouse_does_not_steal_existing_preferred()
    {
        var routes = new FakeRouteRepo();
        var branches = new FakeBranches();
        var clock = new FixedClock(Utc);
        var uow = new FakeUow();
        var coverage = new UpsertSupplyCoverageBySource(routes, branches, uow, clock);

        Assert.True((await coverage.ExecuteAsync(Org, new UpsertSupplyCoverageBySourceRequest(WarehouseA, [RetailA]))).IsSuccess);
        var afterA = await routes.ListByDestinationAsync(PosOrganizationId.From(Org), PosBranchId.From(RetailA));
        Assert.Single(afterA.Where(r => r.IsPreferred && r.IsActive));
        Assert.Equal(WarehouseA, afterA.Single(r => r.IsPreferred).SourceLocationId.Value);

        Assert.True((await coverage.ExecuteAsync(Org, new UpsertSupplyCoverageBySourceRequest(WarehouseB, [RetailA]))).IsSuccess);
        var afterB = await routes.ListByDestinationAsync(PosOrganizationId.From(Org), PosBranchId.From(RetailA));
        Assert.Equal(WarehouseA, afterB.Single(r => r.IsPreferred && r.IsActive).SourceLocationId.Value);
        Assert.Contains(afterB, r => r.SourceLocationId.Value == WarehouseB && r.IsActive && !r.IsPreferred);
    }

    [Fact]
    public async Task Destination_upsert_rejects_active_retail_source()
    {
        var useCase = new UpsertSupplyRoutes(
            new FakeRouteRepo(),
            new FakeBranches(),
            new FakeUow(),
            new FixedClock(Utc));

        var result = await useCase.ExecuteAsync(
            Org,
            new UpsertSupplyRoutesRequest(
                RetailB,
                [new UpsertSupplyRouteItemRequest(RetailA, IsPreferred: true, IsActive: true)]));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.SupplyRouteSourceMustBeWarehouse, result.ErrorCode);
    }

    [Fact]
    public async Task Deactivate_non_warehouse_sources_clears_retail_routes()
    {
        var routes = new FakeRouteRepo();
        var orgId = PosOrganizationId.From(Org);
        var retailRoute = SupplyRoute.Create(
            orgId,
            PosBranchId.From(RetailA),
            PosBranchId.From(RetailB),
            Utc,
            isPreferred: true);
        await routes.AddAsync(retailRoute);
        var whRoute = SupplyRoute.Create(
            orgId,
            PosBranchId.From(WarehouseA),
            PosBranchId.From(RetailB),
            Utc);
        await routes.AddAsync(whRoute);

        var normalize = new DeactivateNonWarehouseSupplySources(
            routes,
            new FakeBranches(),
            new FakeUow(),
            new FixedClock(Utc.AddMinutes(1)));
        var count = await normalize.ExecuteAsync(Org);

        Assert.Equal(1, count);
        var all = await routes.ListAllAsync(orgId);
        Assert.False(all.Single(r => r.SourceLocationId.Value == RetailA).IsActive);
        Assert.True(all.Single(r => r.SourceLocationId.Value == WarehouseA).IsActive);
    }

    [Fact]
    public async Task Create_stock_request_rejects_retail_source_even_with_stale_route()
    {
        var routes = new FakeRouteRepo();
        var orgId = PosOrganizationId.From(Org);
        await routes.AddAsync(
            SupplyRoute.Create(orgId, PosBranchId.From(RetailA), PosBranchId.From(RetailB), Utc, isPreferred: true));

        // Minimal CreateStockRequest is heavy; assert coverage rule helper + branch type used by Create.
        var branches = new FakeBranches();
        Assert.False(
            SupplyRouteSourceRules.IsWarehouseBranchType(
                await branches.GetBranchTypeAsync(Org, RetailA)));
        Assert.True(
            SupplyRouteSourceRules.IsWarehouseBranchType(
                await branches.GetBranchTypeAsync(Org, WarehouseA)));
    }

    private sealed class FixedClock(DateTimeOffset utc) : IClock
    {
        public DateTimeOffset UtcNow => utc;
    }

    private sealed class FakeUow : IPosUnitOfWork
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
            Task.FromResult(organizationId == Org && Known(branchId));

        public Task<bool> IsActiveInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            ExistsInOrganizationAsync(organizationId, branchId, cancellationToken);

        public Task<string> GetBranchTypeAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                branchId == WarehouseA || branchId == WarehouseB ? "Warehouse" : "Retail");

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(id => id, id => id.ToString("N")[..8]));

        private static bool Known(Guid id) =>
            id is var x && (x == WarehouseA || x == WarehouseB || x == RetailA || x == RetailB);
    }

    private sealed class FakeRouteRepo : ISupplyRouteRepository
    {
        private readonly List<SupplyRoute> _items = [];

        public Task<SupplyRoute?> GetByIdAsync(
            PosOrganizationId organizationId,
            SupplyRouteId routeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == routeId));

        public Task<IReadOnlyList<SupplyRoute>> ListByDestinationAsync(
            PosOrganizationId organizationId,
            PosBranchId destinationLocationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplyRoute>>(
                _items.Where(r => r.OrganizationId == organizationId && r.DestinationLocationId == destinationLocationId).ToList());

        public Task<IReadOnlyList<SupplyRoute>> ListBySourceAsync(
            PosOrganizationId organizationId,
            PosBranchId sourceLocationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplyRoute>>(
                _items.Where(r => r.OrganizationId == organizationId && r.SourceLocationId == sourceLocationId).ToList());

        public Task<IReadOnlyList<SupplyRoute>> ListAllAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplyRoute>>(_items.Where(r => r.OrganizationId == organizationId).ToList());

        public Task AddAsync(SupplyRoute route, CancellationToken cancellationToken = default)
        {
            _items.Add(route);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SupplyRoute route, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
