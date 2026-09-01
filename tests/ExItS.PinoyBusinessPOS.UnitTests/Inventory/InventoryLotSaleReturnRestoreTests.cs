using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryLotSaleReturnRestoreTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Utc = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Utc.UtcDateTime);

    [Fact]
    public async Task Partial_return_restores_earliest_expiration_lot_first()
    {
        var lots = new InMemoryLotRepository();
        var lotA = InventoryLot.Create(Org, Product, Today.AddDays(5), 10m, Utc, lotNumber: "A");
        var lotB = InventoryLot.Create(Org, Product, Today.AddDays(20), 10m, Utc, lotNumber: "B");
        lots.Lots.Add(lotA);
        lots.Lots.Add(lotB);

        var saleId = Guid.NewGuid();
        // FEFO sale consumed 7 from A then 3 from B
        lots.Movements.Add(Movement(saleId, lotA.Id, StockMovementType.SaleDeduction, -7m));
        lots.Movements.Add(Movement(saleId, lotB.Id, StockMovementType.SaleDeduction, -3m));
        lotA.Apply(-7m, Utc);
        lotB.Apply(-3m, Utc);

        var service = new InventoryLotStockService(lots);
        var returnId = Guid.NewGuid();
        await service.RestoreForSaleReturnAsync(
            Org, saleId, returnId, Product, quantityToRestore: 5m, priorSaleReturnIds: [], Actor, Utc);

        Assert.Equal(8m, lotA.QuantityOnHand); // 3 left + 5 restored
        Assert.Equal(7m, lotB.QuantityOnHand); // unchanged
        var restores = lots.Movements
            .Where(m => m.SourceId == returnId && m.MovementType == StockMovementType.SaleReturnRestock)
            .ToList();
        Assert.Single(restores);
        Assert.Equal(lotA.Id, restores[0].LotId);
        Assert.Equal(5m, restores[0].QuantityEffect);
        Assert.Equal(StockMovementSourceType.SaleReturn, restores[0].SourceType);
    }

    [Fact]
    public async Task Partial_return_spills_to_next_lot_after_A_exhausted()
    {
        var lots = new InMemoryLotRepository();
        var lotA = InventoryLot.Create(Org, Product, Today.AddDays(5), 10m, Utc, lotNumber: "A");
        var lotB = InventoryLot.Create(Org, Product, Today.AddDays(20), 10m, Utc, lotNumber: "B");
        lots.Lots.Add(lotA);
        lots.Lots.Add(lotB);

        var saleId = Guid.NewGuid();
        lots.Movements.Add(Movement(saleId, lotA.Id, StockMovementType.SaleDeduction, -7m));
        lots.Movements.Add(Movement(saleId, lotB.Id, StockMovementType.SaleDeduction, -3m));
        lotA.Apply(-7m, Utc);
        lotB.Apply(-3m, Utc);

        var service = new InventoryLotStockService(lots);
        var firstReturn = Guid.NewGuid();
        await service.RestoreForSaleReturnAsync(Org, saleId, firstReturn, Product, 5m, [], Actor, Utc);

        var secondReturn = Guid.NewGuid();
        await service.RestoreForSaleReturnAsync(
            Org, saleId, secondReturn, Product, 4m, [firstReturn], Actor, Utc);

        Assert.Equal(10m, lotA.QuantityOnHand); // full 7 restored across returns
        Assert.Equal(9m, lotB.QuantityOnHand); // 2 of 3 restored
        var second = lots.Movements
            .Where(m => m.SourceId == secondReturn && m.MovementType == StockMovementType.SaleReturnRestock)
            .OrderBy(m => m.LotId.Value)
            .ToList();
        Assert.Equal(2, second.Count);
        Assert.Contains(second, m => m.LotId == lotA.Id && m.QuantityEffect == 2m);
        Assert.Contains(second, m => m.LotId == lotB.Id && m.QuantityEffect == 2m);
    }

    [Fact]
    public async Task Expired_lot_may_receive_restore()
    {
        var lots = new InMemoryLotRepository();
        var expired = InventoryLot.Create(Org, Product, Today.AddDays(-2), 5m, Utc, lotNumber: "E");
        lots.Lots.Add(expired);
        var saleId = Guid.NewGuid();
        lots.Movements.Add(Movement(saleId, expired.Id, StockMovementType.SaleDeduction, -5m));
        expired.Apply(-5m, Utc);

        var service = new InventoryLotStockService(lots);
        await service.RestoreForSaleReturnAsync(
            Org, saleId, Guid.NewGuid(), Product, 3m, [], Actor, Utc);

        Assert.Equal(3m, expired.QuantityOnHand);
        Assert.False(expired.IsSellable(Today));
    }

    [Fact]
    public async Task Same_return_id_is_idempotent()
    {
        var lots = new InMemoryLotRepository();
        var lot = InventoryLot.Create(Org, Product, Today.AddDays(10), 10m, Utc);
        lots.Lots.Add(lot);
        var saleId = Guid.NewGuid();
        lots.Movements.Add(Movement(saleId, lot.Id, StockMovementType.SaleDeduction, -4m));
        lot.Apply(-4m, Utc);

        var service = new InventoryLotStockService(lots);
        var returnId = Guid.NewGuid();
        await service.RestoreForSaleReturnAsync(Org, saleId, returnId, Product, 2m, [], Actor, Utc);
        await service.RestoreForSaleReturnAsync(Org, saleId, returnId, Product, 2m, [], Actor, Utc);

        Assert.Equal(8m, lot.QuantityOnHand);
        Assert.Equal(
            1,
            lots.Movements.Count(m =>
                m.SourceId == returnId && m.MovementType == StockMovementType.SaleReturnRestock));
    }

    [Fact]
    public async Task Over_restore_beyond_original_consumed_fails_closed()
    {
        var lots = new InMemoryLotRepository();
        var lot = InventoryLot.Create(Org, Product, Today.AddDays(10), 10m, Utc);
        lots.Lots.Add(lot);
        var saleId = Guid.NewGuid();
        lots.Movements.Add(Movement(saleId, lot.Id, StockMovementType.SaleDeduction, -2m));
        lot.Apply(-2m, Utc);

        var service = new InventoryLotStockService(lots);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.RestoreForSaleReturnAsync(Org, saleId, Guid.NewGuid(), Product, 3m, [], Actor, Utc));
        Assert.Equal(ApplicationErrorCodes.SaleReturnLotRestoreInsufficient, ex.ErrorCode);
    }

    private static InventoryLotMovement Movement(
        Guid sourceId,
        InventoryLotId lotId,
        StockMovementType type,
        decimal qty) =>
        InventoryLotMovement.Create(
            Org,
            lotId,
            Product,
            type,
            qty,
            type == StockMovementType.SaleDeduction
                ? StockMovementSourceType.Sale
                : StockMovementSourceType.SaleReturn,
            Actor,
            Utc,
            sourceId);

    private sealed class InMemoryLotRepository : IInventoryLotRepository
    {
        public List<InventoryLot> Lots { get; } = [];
        public List<InventoryLotMovement> Movements { get; } = [];

        public Task<InventoryLot?> GetByIdAsync(
            PosOrganizationId organizationId,
            InventoryLotId lotId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Lots.FirstOrDefault(l => l.Id == lotId && l.OrganizationId == organizationId));

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
            Task.FromResult<IReadOnlyList<InventoryLot>>(
                Lots.Where(l => l.OrganizationId == organizationId && l.ProductId == productId).ToList());

        public Task<IReadOnlyList<InventoryLot>> ListOrgLevelOnHandAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            bool includeDepleted,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryLot>>(
                Lots.Where(l => l.OrganizationId == organizationId && l.ProductId == productId && l.BranchId is null).ToList());

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
            throw new NotSupportedException();

        public Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(
            PosOrganizationId organizationId,
            DateOnly today,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AdoptOrgLevelLotsForBranchAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken = default)
        {
            Lots.Add(lot);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(InventoryLot lot, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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
                Movements
                    .Where(m =>
                        m.OrganizationId == organizationId
                        && m.SourceId == sourceId
                        && m.MovementType == movementType)
                    .ToList());
    }
}
