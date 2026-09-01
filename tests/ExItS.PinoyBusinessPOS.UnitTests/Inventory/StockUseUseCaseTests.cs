using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class StockUseUseCaseTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Utc = new(2026, 8, 29, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_decreases_tracked_stock_for_internal_operations()
    {
        var fx = await SeedAsync(cokeOnHand: 10m);
        var result = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateStockUseRequest(
                nameof(StockUseReason.InternalOperations),
                [new CreateStockUseLineRequest(fx.CokeId, 3m)]),
            Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(7m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(StockUseStatuses.ToCode(StockUseStatus.Posted), result.Value!.Status);
        Assert.Contains(
            fx.Inventory.Movements,
            m => m.MovementType == StockMovementType.StockUse
                && m.SourceType == StockMovementSourceType.StockUse
                && m.QuantityEffect == -3m);
        Assert.Equal(5m, result.Value.Lines[0].UnitCostSnapshot);
    }

    [Fact]
    public async Task Insufficient_stock_rejects_without_partial_decrease()
    {
        var fx = await SeedAsync(cokeOnHand: 2m, spriteOnHand: 5m);
        var beforeCoke = fx.Inventory.GetOnHand(fx.CokeId);
        var beforeSprite = fx.Inventory.GetOnHand(fx.SpriteId);
        var result = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateStockUseRequest(
                nameof(StockUseReason.StaffUse),
                [
                    new CreateStockUseLineRequest(fx.CokeId, 3m),
                    new CreateStockUseLineRequest(fx.SpriteId, 1m)
                ]),
            Actor);

        Assert.Equal(ApplicationErrorCodes.InsufficientStock, result.ErrorCode);
        Assert.Equal(beforeCoke, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(beforeSprite, fx.Inventory.GetOnHand(fx.SpriteId));
        Assert.DoesNotContain(fx.Inventory.Movements, m => m.MovementType == StockMovementType.StockUse);
        Assert.Empty(fx.StockUses.Items);
    }

    [Fact]
    public async Task Multi_line_decreases_atomically()
    {
        var fx = await SeedAsync(cokeOnHand: 10m, spriteOnHand: 8m);
        var result = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateStockUseRequest(
                nameof(StockUseReason.SampleOrTesting),
                [
                    new CreateStockUseLineRequest(fx.CokeId, 2m),
                    new CreateStockUseLineRequest(fx.SpriteId, 3m)
                ]),
            Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(8m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(5m, fx.Inventory.GetOnHand(fx.SpriteId));
        Assert.Equal(2, fx.Inventory.Movements.Count(m => m.MovementType == StockMovementType.StockUse));
    }

    [Fact]
    public async Task Unit_conversion_uses_multiplier_to_base()
    {
        var fx = await SeedAsync(cokeOnHand: 24m);
        var unit = CatalogProductUnit.Create(
            PosOrganizationId.From(OrgA),
            CatalogProductId.From(fx.CokeId),
            ProductUnitKind.Purchase,
            "Case of 12",
            "Case",
            12m,
            Utc);
        fx.Units.Items.Add(unit);

        var result = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateStockUseRequest(
                nameof(StockUseReason.Other),
                [new CreateStockUseLineRequest(fx.CokeId, 1m, unit.Id.Value)]),
            Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(12m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(1m, result.Value!.Lines[0].QuantityEntered);
        Assert.Equal(12m, result.Value.Lines[0].MultiplierToBase);
        Assert.Equal(12m, result.Value.Lines[0].BaseQuantity);
    }

    [Fact]
    public async Task Void_restores_stock_with_compensating_movement()
    {
        var fx = await SeedAsync(cokeOnHand: 10m);
        var created = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateStockUseRequest(
                nameof(StockUseReason.InternalOperations),
                [new CreateStockUseLineRequest(fx.CokeId, 4m)]),
            Actor);
        Assert.True(created.IsSuccess);
        Assert.Equal(6m, fx.Inventory.GetOnHand(fx.CokeId));

        var voided = await fx.Void.ExecuteAsync(OrgA, created.Value!.StockUseId, Actor);
        Assert.True(voided.IsSuccess);
        Assert.Equal(StockUseStatuses.ToCode(StockUseStatus.Voided), voided.Value!.Status);
        Assert.Equal(10m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Contains(
            fx.Inventory.Movements,
            m => m.MovementType == StockMovementType.StockUseVoidRestoration && m.QuantityEffect == 4m);
    }

    [Fact]
    public async Task Client_stock_use_id_replays_without_double_decrease()
    {
        var fx = await SeedAsync(cokeOnHand: 10m);
        var clientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var request = new CreateStockUseRequest(
            nameof(StockUseReason.StaffUse),
            [new CreateStockUseLineRequest(fx.CokeId, 2m)],
            StockUseId: clientId);

        var first = await fx.Create.ExecuteAsync(OrgA, request, Actor);
        Assert.True(first.IsSuccess);
        Assert.Equal(8m, fx.Inventory.GetOnHand(fx.CokeId));

        var second = await fx.Create.ExecuteAsync(OrgA, request, Actor);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.StockUseId, second.Value!.StockUseId);
        Assert.Equal(8m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Equal(1, fx.Inventory.Movements.Count(m => m.MovementType == StockMovementType.StockUse));
    }

    [Fact]
    public async Task Fractional_quantity_decreases_base_stock()
    {
        var fx = new Fixture();
        await fx.AddProductAsync(fx.CokeId, "Cleaning Liquid", 5.0m, track: true, UnitOfMeasure.Liter);
        var result = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateStockUseRequest(
                nameof(StockUseReason.SampleOrTesting),
                [new CreateStockUseLineRequest(fx.CokeId, 0.75m)]),
            Actor);

        Assert.True(result.IsSuccess, result.ErrorCode + ": " + result.ErrorMessage);
        Assert.Equal(4.25m, fx.Inventory.GetOnHand(fx.CokeId));
    }

    [Fact]
    public async Task Cross_org_product_is_rejected()
    {
        var fx = await SeedAsync(cokeOnHand: 10m);
        var foreignOrg = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var result = await fx.Create.ExecuteAsync(
            foreignOrg,
            new CreateStockUseRequest(
                nameof(StockUseReason.InternalOperations),
                [new CreateStockUseLineRequest(fx.CokeId, 1m)]),
            Actor);

        Assert.False(result.IsSuccess);
        Assert.Equal(10m, fx.Inventory.GetOnHand(fx.CokeId));
        Assert.Empty(fx.StockUses.Items);
        Assert.DoesNotContain(fx.Inventory.Movements, m => m.MovementType == StockMovementType.StockUse);
    }

    [Fact]
    public async Task Stock_use_does_not_change_product_business_usage()
    {
        var fx = await SeedAsync(cokeOnHand: 10m);
        var product = fx.Products.Items.Single(p => p.Id.Value == fx.CokeId);
        product.UpdateUsage(ProductUsageCapabilities.InternalUse, Utc);
        Assert.False(product.CanBeSold);
        Assert.Equal(
            ProductBusinessUsage.InternalUse,
            ProductBusinessUsages.Classify(
                ProductUsageCapabilities.Create(
                    product.CanBePurchased,
                    product.CanBeSold,
                    product.CanBeUsedAsIngredient,
                    product.IsProduced,
                    product.UsagePreset)));

        var result = await fx.Create.ExecuteAsync(
            OrgA,
            new CreateStockUseRequest(
                nameof(StockUseReason.InternalOperations),
                [new CreateStockUseLineRequest(fx.CokeId, 1m)]),
            Actor);

        Assert.True(result.IsSuccess);
        Assert.False(product.CanBeSold);
        Assert.Equal(ProductUsageCapabilities.InternalUseCode, product.UsagePreset);
        Assert.Equal(
            ProductBusinessUsage.InternalUse,
            ProductBusinessUsages.Classify(
                ProductUsageCapabilities.Create(
                    product.CanBePurchased,
                    product.CanBeSold,
                    product.CanBeUsedAsIngredient,
                    product.IsProduced,
                    product.UsagePreset)));
    }

    private static async Task<Fixture> SeedAsync(decimal cokeOnHand = 0m, decimal? spriteOnHand = null)
    {
        var fx = new Fixture();
        await fx.AddProductAsync(fx.CokeId, "Coke", cokeOnHand, track: true);
        if (spriteOnHand is decimal sprite)
        {
            await fx.AddProductAsync(fx.SpriteId, "Sprite", sprite, track: true);
        }

        return fx;
    }

    private sealed class Fixture
    {
        public Guid CokeId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public Guid SpriteId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public InMemoryCatalog Products { get; } = new();
        public InMemoryUnits Units { get; } = new();
        public InMemoryInventory Inventory { get; } = new();
        public InMemoryStockUses StockUses { get; } = new();
        public InMemoryBranchBalances Branches { get; } = new();
        public InMemoryLots Lots { get; } = new();
        public ImmediateUnitOfWork UnitOfWork { get; } = new();
        public FixedClock Clock { get; } = new(Utc);
        public CreateStockUse Create { get; }
        public VoidStockUse Void { get; }

        public Fixture()
        {
            var lots = new InventoryLotStockService(Lots);
            Create = new CreateStockUse(
                StockUses,
                Products,
                Units,
                Inventory,
                Branches,
                lots,
                UnitOfWork,
                Clock);
            Void = new VoidStockUse(
                StockUses,
                Products,
                Inventory,
                Branches,
                lots,
                UnitOfWork,
                Clock);
        }

        public Task AddProductAsync(
            Guid productId,
            string name,
            decimal opening,
            bool track,
            UnitOfMeasure unitOfMeasure = UnitOfMeasure.Piece)
        {
            var product = CatalogProduct.Create(
                PosOrganizationId.From(OrgA),
                name,
                unitOfMeasure,
                10m,
                Utc,
                id: CatalogProductId.From(productId));
            Products.Items.Add(product);
            if (track)
            {
                var account = InventoryAccount.CreateUntracked(
                    PosOrganizationId.From(OrgA),
                    CatalogProductId.From(productId),
                    Utc);
                var movement = account.Enable(
                    opening,
                    unitOfMeasure,
                    Actor,
                    Utc,
                    hasOpeningStockAlready: false,
                    openingUnitCost: 5m);
                Inventory.Accounts.Add(account);
                if (movement is not null)
                {
                    Inventory.Movements.Add(movement);
                }
            }

            return Task.CompletedTask;
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

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            Items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryUnits : ICatalogProductUnitRepository
    {
        public List<CatalogProductUnit> Items { get; } = [];

        public Task<CatalogProductUnit?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductUnitId unitId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.OrganizationId == organizationId && u.Id == unitId));

        public Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductUnit>>(
                Items.Where(u => u.OrganizationId == organizationId && u.ProductId == productId).ToList());

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>>(
                Items.Where(u => u.OrganizationId == organizationId && productIds.Any(id => id == u.ProductId))
                    .GroupBy(u => u.ProductId.Value)
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<CatalogProductUnit>)g.ToList()));

        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default)
        {
            Items.Add(unit);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ReplaceActiveUnitsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class InMemoryStockUses : IStockUseRepository
    {
        public List<StockUse> Items { get; } = [];
        private long _sequence;

        public Task<StockUse?> GetByIdAsync(
            PosOrganizationId organizationId,
            StockUseId stockUseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == stockUseId));

        public Task<StockUse?> FindByIdempotencyKeyAsync(
            PosOrganizationId organizationId,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(r =>
                r.OrganizationId == organizationId
                && string.Equals(r.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)));

        public Task<(IReadOnlyList<StockUse> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            StockUseFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = Items.Where(r => r.OrganizationId == organizationId).Skip(skip).Take(take).ToList();
            return Task.FromResult<(IReadOnlyList<StockUse>, int)>((list, Items.Count));
        }

        public Task AddAsync(StockUse stockUse, CancellationToken cancellationToken = default)
        {
            Items.Add(stockUse);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StockUse stockUse, CancellationToken cancellationToken = default)
        {
            var idx = Items.FindIndex(r => r.Id == stockUse.Id);
            if (idx >= 0)
            {
                Items[idx] = stockUse;
            }

            return Task.CompletedTask;
        }

        public Task<string> AllocateNextNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default)
        {
            _sequence++;
            return Task.FromResult(StockUseNumbers.Format(businessDateUtc, _sequence));
        }

        public Task<InventoryDocumentCostPeriodAggregate> AggregatePostedCostForPeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new InventoryDocumentCostPeriodAggregate(0m, 0, 0, 0, 0));
    }

    private sealed class InMemoryBranchBalances : IInventoryBranchBalanceRepository
    {
        public Task<InventoryBranchBalance?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<InventoryBranchBalance?>(null);

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryBranchBalance>>([]);

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
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
            action(
                Accounts.Where(a => a.OrganizationId == organizationId && productIds.Any(id => id == a.ProductId)).ToList(),
                cancellationToken);

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<StockMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            StockMovementId movementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.FirstOrDefault(m => m.OrganizationId == organizationId && m.Id == movementId));

        public Task<bool> HasStockUseAsync(
            PosOrganizationId organizationId,
            StockUseId stockUseId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == stockUseId.Value
                && m.MovementType == StockMovementType.StockUse));

        public Task<bool> HasStockUseVoidRestorationAsync(
            PosOrganizationId organizationId,
            StockUseId stockUseId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == stockUseId.Value
                && m.MovementType == StockMovementType.StockUseVoidRestoration));

        public Task<bool> HasProductionMaterialConsumptionAsync(
            PosOrganizationId organizationId,
            ProductionRunId productionRunId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == productionRunId.Value
                && m.MovementType == StockMovementType.ProductionMaterialConsumption));

        public Task<bool> HasProductionMaterialRestorationAsync(
            PosOrganizationId organizationId,
            ProductionRunId productionRunId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == productionRunId.Value
                && m.MovementType == StockMovementType.ProductionMaterialRestoration));

        public Task<bool> HasProductionOutputAsync(
            PosOrganizationId organizationId,
            ProductionRunId productionRunId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == productionRunId.Value
                && m.MovementType == StockMovementType.ProductionOutput));

        public Task<bool> HasProductionOutputReversalAsync(
            PosOrganizationId organizationId,
            ProductionRunId productionRunId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == productionRunId.Value
                && m.MovementType == StockMovementType.ProductionOutputReversal));

        public Task<bool> HasWasteLossAsync(
            PosOrganizationId organizationId,
            WasteLossId wasteLossId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == wasteLossId.Value
                && m.MovementType == StockMovementType.WasteLoss));

        public Task<bool> HasWasteLossVoidRestorationAsync(

            PosOrganizationId organizationId,
            WasteLossId wasteLossId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId
                && m.ProductId == productId
                && m.SourceId == wasteLossId.Value
                && m.MovementType == StockMovementType.WasteLossVoidRestoration));

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

        public Task<decimal?> GetLatestAcquisitionUnitCostAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default)
        {
            var cost = Movements
                .Where(m =>
                    m.OrganizationId == organizationId
                    && m.ProductId == productId
                    && m.UnitCost is not null
                    && m.MovementType is StockMovementType.OpeningStock
                        or StockMovementType.PurchaseReceipt
                        or StockMovementType.DirectPurchaseReceipt
                        or StockMovementType.ProductionOutput)
                .OrderByDescending(m => m.RecordedAtUtc)
                .Select(m => m.UnitCost)
                .FirstOrDefault();
            return Task.FromResult(cost);
        }

        public async Task<IReadOnlyDictionary<Guid, decimal?>> GetLatestAcquisitionUnitCostsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<Guid, decimal?>();
            foreach (var productId in productIds)
            {
                var cost = await GetLatestAcquisitionUnitCostAsync(organizationId, productId, cancellationToken)
                    .ConfigureAwait(false);
                if (cost is not null)
                {
                    result[productId.Value] = cost;
                }
            }

            return result;
        }
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, InventoryAccountFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListLowStockAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<InventoryAccount>> ListAllAccountsAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasAnyMovementAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasOpeningStockAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(PosOrganizationId organizationId, CatalogProductId productId, StockMovementFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<decimal> SumMovementEffectsAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<InventoryAccount> Items, int TotalCount)> ListReorderSuggestionsAsync(PosOrganizationId organizationId, string? search, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasStockCountVarianceAsync(PosOrganizationId organizationId, StockCountId stockCountId, CatalogProductId productId, StockMovementType movementType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockMovement>> ListMovementsForReportAsync(PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, Guid? branchId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockMovement>> ListSaleDeductionsAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleDeductionAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasCustomerOrderDeductionAsync(PosOrganizationId organizationId, CustomerOrderId orderId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleVoidRestorationAsync(PosOrganizationId organizationId, SaleId saleId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPurchaseReceiptAsync(PosOrganizationId organizationId, GoodsReceiptId goodsReceiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasDirectPurchaseReceiptAsync(PosOrganizationId organizationId, DirectPurchaseReceiptId receiptId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasSaleReturnRestockAsync(PosOrganizationId organizationId, SaleReturnId saleReturnId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasInventoryTransferMovementAsync(PosOrganizationId organizationId, InventoryTransferId transferId, CatalogProductId productId, StockMovementType movementType, InventoryLotId? lotId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(DateTimeOffset? LatestAt, int Count)> GetMovementSummaryAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, (DateTimeOffset? LatestAt, int Count)>> GetMovementSummariesAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0));

        public Task AdoptOrgLevelLotsForBranchAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            PosBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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

        public Task<IReadOnlyList<InventoryLotMovement>> ListBySourceAsync(
            PosOrganizationId organizationId,
            Guid sourceId,
            StockMovementType movementType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryLotMovement>>(
                Movements.Where(m =>
                    m.OrganizationId == organizationId
                    && m.SourceId == sourceId
                    && m.MovementType == movementType).ToList());

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
    }
}