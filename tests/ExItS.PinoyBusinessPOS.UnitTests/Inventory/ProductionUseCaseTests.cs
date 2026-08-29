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

public sealed class ProductionUseCaseTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Utc = new(2026, 8, 29, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Produce_scales_materials_and_increases_output()
    {
        var fx = await SeedAsync();
        var definition = await CreateDefinitionAsync(fx, outputQty: 100m, flourQty: 10m);

        var result = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(definition.ProductionDefinitionId, OutputQuantity: 300m),
            Actor);

        Assert.True(result.IsSuccess, result.ErrorCode + ": " + result.ErrorMessage);
        Assert.Equal(20m, fx.Inventory.GetOnHand(fx.FlourId)); // 50 - 30
        Assert.Equal(300m, fx.Inventory.GetOnHand(fx.BreadId));
        Assert.Equal(30m, result.Value!.Materials[0].ExpectedQuantityEntered);
        Assert.Equal(30m, result.Value.Materials[0].ActualQuantityEntered);
        Assert.Contains(fx.Inventory.Movements, m => m.MovementType == StockMovementType.ProductionMaterialConsumption);
        Assert.Contains(fx.Inventory.Movements, m => m.MovementType == StockMovementType.ProductionOutput);
        Assert.DoesNotContain(fx.Inventory.Movements, m => m.MovementType == StockMovementType.StockUse);
        Assert.DoesNotContain(fx.Inventory.Movements, m => m.MovementType == StockMovementType.SaleDeduction);
    }

    [Fact]
    public async Task Actual_override_consumes_actual_not_expected()
    {
        var fx = await SeedAsync();
        var definition = await CreateDefinitionAsync(fx, outputQty: 100m, flourQty: 10m);

        var result = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(
                definition.ProductionDefinitionId,
                OutputQuantity: 100m,
                MaterialOverrides: [new CreateProductionRunMaterialOverrideRequest(fx.FlourId, 11m)]),
            Actor);

        Assert.True(result.IsSuccess, result.ErrorCode + ": " + result.ErrorMessage);
        Assert.Equal(10m, result.Value!.Materials[0].ExpectedQuantityEntered);
        Assert.Equal(11m, result.Value.Materials[0].ActualQuantityEntered);
        Assert.Equal(39m, fx.Inventory.GetOnHand(fx.FlourId));
    }

    [Fact]
    public async Task Insufficient_stock_fails_atomically()
    {
        var fx = await SeedAsync(flourOnHand: 5m);
        var definition = await CreateDefinitionAsync(fx, outputQty: 100m, flourQty: 10m);
        var beforeFlour = fx.Inventory.GetOnHand(fx.FlourId);
        var beforeBread = fx.Inventory.GetOnHand(fx.BreadId);

        var result = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(definition.ProductionDefinitionId, OutputQuantity: 100m),
            Actor);

        Assert.Equal(ApplicationErrorCodes.InsufficientStock, result.ErrorCode);
        Assert.Equal(beforeFlour, fx.Inventory.GetOnHand(fx.FlourId));
        Assert.Equal(beforeBread, fx.Inventory.GetOnHand(fx.BreadId));
        Assert.Empty(fx.Runs.Items);
    }

    [Fact]
    public async Task Unit_conversion_uses_multiplier_to_base()
    {
        var fx = await SeedAsync(flourOnHand: 100m);
        var unit = CatalogProductUnit.Create(
            PosOrganizationId.From(OrgA),
            CatalogProductId.From(fx.FlourId),
            ProductUnitKind.Purchase,
            "Bag of 25kg",
            "Bag",
            25m,
            Utc);
        fx.Units.Items.Add(unit);

        var created = await fx.CreateDefinition.ExecuteAsync(
            OrgA,
            new CreateProductionDefinitionRequest(
                "Bag recipe",
                fx.BreadId,
                50m,
                [new CreateProductionComponentRequest(fx.FlourId, 1m, unit.Id.Value)]),
            Actor);
        Assert.True(created.IsSuccess, created.ErrorCode + ": " + created.ErrorMessage);

        var result = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(created.Value!.ProductionDefinitionId, OutputQuantity: 50m),
            Actor);
        Assert.True(result.IsSuccess, result.ErrorCode + ": " + result.ErrorMessage);
        Assert.Equal(75m, fx.Inventory.GetOnHand(fx.FlourId));
        Assert.Equal(25m, result.Value!.Materials[0].ActualBaseQuantity);
    }

    [Fact]
    public async Task Fractional_material_quantities_are_supported()
    {
        var fx = await SeedAsync(flourOnHand: 10m);
        // Use kilogram base UOM so fractional quantities are allowed by movement normalization.
        var flour = fx.Products.Items.Single(p => p.Id.Value == fx.FlourId);
        // Recreate flour as kilogram-tracked via seed helper path: update account UOM through production with kg product.
        fx.Products.Items.Remove(flour);
        fx.Inventory.Accounts.RemoveAll(a => a.ProductId.Value == fx.FlourId);
        fx.Inventory.Movements.RemoveAll(m => m.ProductId.Value == fx.FlourId);
        await fx.AddProductAsync(fx.FlourId, "Flour", 10m, track: true, openingUnitCost: 5m, unitOfMeasure: UnitOfMeasure.Kilogram);
        fx.Products.Items.Single(p => p.Id.Value == fx.FlourId)
            .UpdateUsage(ProductUsageCapabilities.Ingredient, Utc);

        var definition = await CreateDefinitionAsync(fx, outputQty: 100m, flourQty: 2.5m);
        var result = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(definition.ProductionDefinitionId, OutputQuantity: 50m),
            Actor);
        Assert.True(result.IsSuccess, result.ErrorCode + ": " + result.ErrorMessage);
        Assert.Equal(1.25m, result.Value!.Materials[0].ActualQuantityEntered);
        Assert.Equal(8.75m, fx.Inventory.GetOnHand(fx.FlourId));
    }

    [Fact]
    public async Task Cost_complete_when_acquisition_known()
    {
        var fx = await SeedAsync();
        var definition = await CreateDefinitionAsync(fx, outputQty: 100m, flourQty: 10m);
        var result = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(definition.ProductionDefinitionId, OutputQuantity: 100m),
            Actor);
        Assert.True(result.IsSuccess);
        Assert.Equal(ProductionCostStatuses.ToCode(ProductionCostStatus.Complete), result.Value!.CostStatus);
        Assert.Equal(50m, result.Value.TotalMaterialCost); // 10 * 5
        Assert.Equal(0.5m, result.Value.OutputBaseUnitCost);
    }

    [Fact]
    public async Task Cost_partial_when_some_material_costs_missing()
    {
        var fx = await SeedAsync();
        // Sugar has no opening cost movement
        await fx.AddProductAsync(fx.SugarId, "Sugar", 20m, track: true, openingUnitCost: null);
        fx.Products.Items.Single(p => p.Id.Value == fx.SugarId)
            .UpdateUsage(ProductUsageCapabilities.Ingredient, Utc);

        var created = await fx.CreateDefinition.ExecuteAsync(
            OrgA,
            new CreateProductionDefinitionRequest(
                "Partial cost",
                fx.BreadId,
                100m,
                [
                    new CreateProductionComponentRequest(fx.FlourId, 10m),
                    new CreateProductionComponentRequest(fx.SugarId, 2m)
                ]),
            Actor);
        Assert.True(created.IsSuccess);

        var result = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(created.Value!.ProductionDefinitionId, OutputQuantity: 100m),
            Actor);
        Assert.True(result.IsSuccess, result.ErrorCode + ": " + result.ErrorMessage);
        Assert.Equal(ProductionCostStatuses.ToCode(ProductionCostStatus.Partial), result.Value!.CostStatus);
        Assert.Null(result.Value.OutputBaseUnitCost);
    }

    [Fact]
    public async Task Self_component_rejected_on_definition()
    {
        var fx = await SeedAsync();
        fx.Products.Items.Single(p => p.Id.Value == fx.BreadId)
            .UpdateUsage(
                ProductUsageCapabilities.Create(false, true, true, true, ProductUsageCapabilities.MadeProductCode),
                Utc);

        var result = await fx.CreateDefinition.ExecuteAsync(
            OrgA,
            new CreateProductionDefinitionRequest(
                "Self",
                fx.BreadId,
                10m,
                [new CreateProductionComponentRequest(fx.BreadId, 1m)]),
            Actor);

        Assert.Equal(DomainErrorCodes.ProductionSelfComponentForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task Cross_org_product_rejected()
    {
        var fx = await SeedAsync();
        var definition = await CreateDefinitionAsync(fx, 100m, 10m);
        var result = await fx.CreateRun.ExecuteAsync(
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            new CreateProductionRunRequest(definition.ProductionDefinitionId, 100m),
            Actor);
        Assert.False(result.IsSuccess);
        Assert.Equal(50m, fx.Inventory.GetOnHand(fx.FlourId));
    }

    [Fact]
    public async Task Client_run_id_replays_without_double_consume()
    {
        var fx = await SeedAsync();
        var definition = await CreateDefinitionAsync(fx, 100m, 10m);
        var clientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var request = new CreateProductionRunRequest(
            definition.ProductionDefinitionId,
            100m,
            ProductionRunId: clientId);

        var first = await fx.CreateRun.ExecuteAsync(OrgA, request, Actor);
        Assert.True(first.IsSuccess);
        Assert.Equal(40m, fx.Inventory.GetOnHand(fx.FlourId));

        var second = await fx.CreateRun.ExecuteAsync(OrgA, request, Actor);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.ProductionRunId, second.Value!.ProductionRunId);
        Assert.Equal(40m, fx.Inventory.GetOnHand(fx.FlourId));
        Assert.Equal(1, fx.Inventory.Movements.Count(m => m.MovementType == StockMovementType.ProductionMaterialConsumption));
    }

    [Fact]
    public async Task Void_restores_materials_and_reverses_output()
    {
        var fx = await SeedAsync();
        var definition = await CreateDefinitionAsync(fx, 100m, 10m);
        var created = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(definition.ProductionDefinitionId, 100m),
            Actor);
        Assert.True(created.IsSuccess);
        Assert.Equal(40m, fx.Inventory.GetOnHand(fx.FlourId));
        Assert.Equal(100m, fx.Inventory.GetOnHand(fx.BreadId));

        var voided = await fx.VoidRun.ExecuteAsync(OrgA, created.Value!.ProductionRunId, Actor);
        Assert.True(voided.IsSuccess, voided.ErrorCode + ": " + voided.ErrorMessage);
        Assert.Equal(ProductionRunStatuses.ToCode(ProductionRunStatus.Voided), voided.Value!.Status);
        Assert.Equal(50m, fx.Inventory.GetOnHand(fx.FlourId));
        Assert.Equal(0m, fx.Inventory.GetOnHand(fx.BreadId));
        Assert.Contains(fx.Inventory.Movements, m => m.MovementType == StockMovementType.ProductionMaterialRestoration);
        Assert.Contains(fx.Inventory.Movements, m => m.MovementType == StockMovementType.ProductionOutputReversal);
    }

    [Fact]
    public async Task Production_does_not_change_business_usage()
    {
        var fx = await SeedAsync();
        var bread = fx.Products.Items.Single(p => p.Id.Value == fx.BreadId);
        Assert.Equal(ProductBusinessUsage.ProducedItem, ProductBusinessUsages.Classify(
            ProductUsageCapabilities.Create(
                bread.CanBePurchased, bread.CanBeSold, bread.CanBeUsedAsIngredient, bread.IsProduced, bread.UsagePreset)));

        var definition = await CreateDefinitionAsync(fx, 100m, 10m);
        var result = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(definition.ProductionDefinitionId, 100m),
            Actor);
        Assert.True(result.IsSuccess);
        Assert.True(bread.IsProduced);
        Assert.Equal(ProductUsageCapabilities.MadeProductCode, bread.UsagePreset);
    }

    [Fact]
    public async Task Sale_of_produced_item_does_not_create_production_material_movements()
    {
        var fx = await SeedAsync();
        var definition = await CreateDefinitionAsync(fx, 100m, 10m);
        var produced = await fx.CreateRun.ExecuteAsync(
            OrgA,
            new CreateProductionRunRequest(definition.ProductionDefinitionId, 100m),
            Actor);
        Assert.True(produced.IsSuccess);

        // Lightweight: selling produced stock only needs SaleDeduction on output — materials already consumed.
        var account = fx.Inventory.Accounts.Single(a => a.ProductId.Value == fx.BreadId);
        var saleMovement = StockMovement.SaleDeduction(
            PosOrganizationId.From(OrgA),
            CatalogProductId.From(fx.BreadId),
            account.Id,
            1m,
            UnitOfMeasure.Piece,
            Guid.NewGuid(),
            Actor,
            Utc);
        account.ApplyMovementEffect(saleMovement.QuantityEffect);
        fx.Inventory.Movements.Add(saleMovement);

        Assert.Equal(99m, fx.Inventory.GetOnHand(fx.BreadId));
        Assert.Equal(40m, fx.Inventory.GetOnHand(fx.FlourId)); // unchanged by sale
        Assert.Equal(1, fx.Inventory.Movements.Count(m => m.MovementType == StockMovementType.ProductionMaterialConsumption));
    }

    private static async Task<ProductionDefinitionDto> CreateDefinitionAsync(
        Fixture fx,
        decimal outputQty,
        decimal flourQty)
    {
        var created = await fx.CreateDefinition.ExecuteAsync(
            OrgA,
            new CreateProductionDefinitionRequest(
                "Standard batch",
                fx.BreadId,
                outputQty,
                [new CreateProductionComponentRequest(fx.FlourId, flourQty)]),
            Actor);
        Assert.True(created.IsSuccess, created.ErrorCode + ": " + created.ErrorMessage);
        return created.Value!;
    }

    private static async Task<Fixture> SeedAsync(decimal flourOnHand = 50m)
    {
        var fx = new Fixture();
        await fx.AddProductAsync(fx.FlourId, "Flour", flourOnHand, track: true, openingUnitCost: 5m);
        fx.Products.Items.Single(p => p.Id.Value == fx.FlourId)
            .UpdateUsage(ProductUsageCapabilities.Ingredient, Utc);
        await fx.AddProductAsync(fx.BreadId, "Pandesal", 0m, track: true, openingUnitCost: null);
        fx.Products.Items.Single(p => p.Id.Value == fx.BreadId)
            .UpdateUsage(ProductUsageCapabilities.MadeProduct, Utc);
        return fx;
    }

    private sealed class Fixture
    {
        public Guid FlourId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public Guid BreadId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public Guid SugarId { get; } = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        public InMemoryCatalog Products { get; } = new();
        public InMemoryUnits Units { get; } = new();
        public InMemoryInventory Inventory { get; } = new();
        public InMemoryDefinitions Definitions { get; } = new();
        public InMemoryRuns Runs { get; } = new();
        public InMemoryBranchBalances Branches { get; } = new();
        public InMemoryLots Lots { get; } = new();
        public ImmediateUnitOfWork UnitOfWork { get; } = new();
        public FixedClock Clock { get; } = new(Utc);
        public CreateProductionDefinition CreateDefinition { get; }
        public CreateProductionRun CreateRun { get; }
        public VoidProductionRun VoidRun { get; }

        public Fixture()
        {
            var lots = new InventoryLotStockService(Lots);
            CreateDefinition = new CreateProductionDefinition(Definitions, Products, Units, UnitOfWork, Clock);
            CreateRun = new CreateProductionRun(
                Runs, Definitions, Products, Units, Inventory, Branches, lots, UnitOfWork, Clock);
            VoidRun = new VoidProductionRun(Runs, Products, Inventory, Branches, lots, UnitOfWork, Clock);
        }

        public Task AddProductAsync(
            Guid productId,
            string name,
            decimal opening,
            bool track,
            decimal? openingUnitCost = 5m,
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
                    openingUnitCost: openingUnitCost);
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
            PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
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

        public Task<CatalogProductUnit?> GetByIdAsync(PosOrganizationId organizationId, ProductUnitId unitId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.OrganizationId == organizationId && u.Id == unitId));

        public Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductUnit>>(
                Items.Where(u => u.OrganizationId == organizationId && u.ProductId == productId).ToList());

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
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
            PosOrganizationId organizationId, CatalogProductId productId, ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units, DateTimeOffset utcNow, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class InMemoryDefinitions : IProductionDefinitionRepository
    {
        public List<ProductionDefinition> Items { get; } = [];

        public Task<ProductionDefinition?> GetByIdAsync(PosOrganizationId organizationId, ProductionDefinitionId definitionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(d => d.OrganizationId == organizationId && d.Id == definitionId));

        public Task<(IReadOnlyList<ProductionDefinition> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, ProductionDefinitionFilter filter, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = Items.Where(d => d.OrganizationId == organizationId).Skip(skip).Take(take).ToList();
            return Task.FromResult<(IReadOnlyList<ProductionDefinition>, int)>((list, Items.Count));
        }

        public Task<IReadOnlyList<ProductionDefinition>> ListAllForCycleValidationAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductionDefinition>>(
                Items.Where(d => d.OrganizationId == organizationId).ToList());

        public Task AddAsync(ProductionDefinition definition, CancellationToken cancellationToken = default)
        {
            Items.Add(definition);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductionDefinition definition, CancellationToken cancellationToken = default)
        {
            var idx = Items.FindIndex(d => d.Id == definition.Id);
            if (idx >= 0) Items[idx] = definition;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRuns : IProductionRunRepository
    {
        public List<ProductionRun> Items { get; } = [];
        private long _sequence;

        public Task<ProductionRun?> GetByIdAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == productionRunId));

        public Task<ProductionRun?> FindByIdempotencyKeyAsync(PosOrganizationId organizationId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(r =>
                r.OrganizationId == organizationId
                && string.Equals(r.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)));

        public Task<(IReadOnlyList<ProductionRun> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, ProductionRunFilter filter, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = Items.Where(r => r.OrganizationId == organizationId).Skip(skip).Take(take).ToList();
            return Task.FromResult<(IReadOnlyList<ProductionRun>, int)>((list, Items.Count));
        }

        public Task AddAsync(ProductionRun productionRun, CancellationToken cancellationToken = default)
        {
            Items.Add(productionRun);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductionRun productionRun, CancellationToken cancellationToken = default)
        {
            var idx = Items.FindIndex(r => r.Id == productionRun.Id);
            if (idx >= 0) Items[idx] = productionRun;
            return Task.CompletedTask;
        }

        public Task<string> AllocateNextNumberAsync(PosOrganizationId organizationId, DateOnly businessDateUtc, CancellationToken cancellationToken = default)
        {
            _sequence++;
            return Task.FromResult(ProductionNumbers.Format(businessDateUtc, _sequence));
        }
    }

    private sealed class InMemoryBranchBalances : IInventoryBranchBalanceRepository
    {
        public Task<InventoryBranchBalance?> GetAsync(PosOrganizationId organizationId, PosBranchId branchId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<InventoryBranchBalance?>(null);

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryBranchBalance>>([]);

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryInventory : IInventoryRepository
    {
        public List<InventoryAccount> Accounts { get; } = [];
        public List<StockMovement> Movements { get; } = [];

        public decimal GetOnHand(Guid productId) =>
            Accounts.FirstOrDefault(a => a.ProductId.Value == productId)?.OnHandQuantity ?? 0m;

        public Task<InventoryAccount?> GetByProductIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.FirstOrDefault(a => a.OrganizationId == organizationId && a.ProductId == productId));

        public Task<IReadOnlyList<InventoryAccount>> ListByProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
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
            action(Accounts.Where(a => a.OrganizationId == organizationId && productIds.Any(id => id == a.ProductId)).ToList(), cancellationToken);

        public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
        {
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<StockMovement?> GetMovementByIdAsync(PosOrganizationId organizationId, StockMovementId movementId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.FirstOrDefault(m => m.OrganizationId == organizationId && m.Id == movementId));

        public Task<bool> HasStockUseAsync(PosOrganizationId organizationId, StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasStockUseVoidRestorationAsync(PosOrganizationId organizationId, StockUseId stockUseId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasProductionMaterialConsumptionAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId && m.ProductId == productId && m.SourceId == productionRunId.Value
                && m.MovementType == StockMovementType.ProductionMaterialConsumption));

        public Task<bool> HasProductionMaterialRestorationAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId && m.ProductId == productId && m.SourceId == productionRunId.Value
                && m.MovementType == StockMovementType.ProductionMaterialRestoration));

        public Task<bool> HasProductionOutputAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId && m.ProductId == productId && m.SourceId == productionRunId.Value
                && m.MovementType == StockMovementType.ProductionOutput));

        public Task<bool> HasProductionOutputReversalAsync(PosOrganizationId organizationId, ProductionRunId productionRunId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m =>
                m.OrganizationId == organizationId && m.ProductId == productId && m.SourceId == productionRunId.Value
                && m.MovementType == StockMovementType.ProductionOutputReversal));

        public Task<decimal?> GetLatestAcquisitionUnitCostAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default)
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

        public Task<bool> HasMovementAsync(PosOrganizationId organizationId, Guid sourceId, InventoryLotId lotId, StockMovementType movementType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Movements.Any(m => m.OrganizationId == organizationId && m.SourceId == sourceId && m.LotId == lotId && m.MovementType == movementType));

        public Task<IReadOnlyList<InventoryLotMovement>> ListBySourceAsync(PosOrganizationId organizationId, Guid sourceId, StockMovementType movementType, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryLotMovement>>(
                Movements.Where(m => m.OrganizationId == organizationId && m.SourceId == sourceId && m.MovementType == movementType).ToList());
    }
}
