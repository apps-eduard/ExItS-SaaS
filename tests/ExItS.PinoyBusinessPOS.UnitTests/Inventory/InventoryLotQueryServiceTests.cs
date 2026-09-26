using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryLotQueryServiceTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Branch = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task ListExpiring_passes_branch_to_count_and_list()
    {
        var lots = new CapturingLots();
        var service = new InventoryLotQueryService(
            lots,
            new EmptyProducts(),
            new ImmediateUnitOfWork(),
            new FixedClock());

        await service.ListExpiringAsync(Org, Branch, "Days30", null, null, null, 1, 20);

        Assert.Equal(Branch, lots.ListBranchId?.Value);
        Assert.Equal(Branch, lots.CountBranchId?.Value);
    }

    private sealed class CapturingLots : IInventoryLotRepository
    {
        public PosBranchId? ListBranchId { get; private set; }
        public PosBranchId? CountBranchId { get; private set; }

        public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListExpiringPagedAsync(
            PosOrganizationId organizationId,
            PosBranchId? branchId,
            DateOnly expireOnOrBefore,
            DateOnly? expireOnOrAfter,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            ListBranchId = branchId;
            return Task.FromResult<(IReadOnlyList<InventoryLot>, int)>(([], 0));
        }

        public Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(
            PosOrganizationId organizationId,
            DateOnly today,
            PosBranchId? branchId = null,
            CancellationToken cancellationToken = default)
        {
            CountBranchId = branchId;
            return Task.FromResult((0, 0));
        }

        public Task<InventoryLot?> GetByIdAsync(PosOrganizationId organizationId, InventoryLotId lotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<InventoryLot?>(null);

        public Task<InventoryLot?> FindAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            DateOnly expirationDate,
            string normalizedLotNumber,
            PosBranchId? branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<InventoryLot?>(null);

        public Task<IReadOnlyList<InventoryLot>> ListOnHandAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId? branchId,
            bool includeDepleted,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryLot>>([]);

        public Task<IReadOnlyList<InventoryLot>> ListOrgLevelOnHandAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            bool includeDepleted,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryLot>>([]);

        public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListPagedAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId? branchId,
            bool includeDepleted,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<InventoryLot>, int)>(([], 0));

        public Task AdoptOrgLevelLotsForBranchAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(InventoryLot lot, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddMovementAsync(InventoryLotMovement movement, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> HasMovementAsync(
            PosOrganizationId organizationId,
            Guid sourceId,
            InventoryLotId lotId,
            StockMovementType movementType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<InventoryLotMovement>> ListBySourceAsync(
            PosOrganizationId organizationId,
            Guid sourceId,
            StockMovementType movementType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryLotMovement>>([]);
    }

    private sealed class EmptyProducts : ICatalogProductRepository
    {
        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>([]);

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([], 0));

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
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);

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

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ImmediateUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
    }
}
