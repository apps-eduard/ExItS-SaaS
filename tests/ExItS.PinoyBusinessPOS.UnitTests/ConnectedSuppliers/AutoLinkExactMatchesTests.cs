using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class AutoLinkExactMatchesTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Supplier =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);

    private const string ValidBarcode = "4006381333931";

    [Fact]
    public async Task Exact_match_auto_links_once_and_counts_ready()
    {
        var harness = CreateHarness();
        var buyerProduct = Product(Buyer, "Premium Rice", "SUP-RICE", UnitOfMeasure.Kilogram, ValidBarcode);
        harness.Products.Seed(buyerProduct);
        SeedSupplierCatalogProduct(harness, ValidBarcode);

        var first = await harness.CreateAutoLink().ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value);
        var second = await harness.CreateAutoLink().ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value);

        Assert.True(first.IsSuccess, $"{first.ErrorCode}: {first.ErrorMessage}");
        Assert.True(second.IsSuccess, $"{second.ErrorCode}: {second.ErrorMessage}");
        Assert.Equal(1, first.Value!.LinkedNow);
        Assert.Equal(0, first.Value.AlreadyReady);
        Assert.Equal(0, first.Value.Review);
        Assert.Equal(0, first.Value.New);
        Assert.Equal(0, first.Value.Conflict);
        Assert.Equal(0, second.Value!.LinkedNow);
        Assert.Equal(1, second.Value.AlreadyReady);
        Assert.Single(harness.Links.Items);
        Assert.Equal(buyerProduct.Id, harness.Links.Items[0].BuyerProductId);
        Assert.Equal(2, harness.Uow.TransactionCount);
    }

    [Fact]
    public async Task Incompatible_uom_does_not_auto_link()
    {
        var harness = CreateHarness();
        harness.Products.Seed(Product(Buyer, "Premium Rice", "SUP-RICE", UnitOfMeasure.Piece, ValidBarcode));
        SeedSupplierCatalogProduct(harness, ValidBarcode);

        var result = await harness.CreateAutoLink().ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(0, result.Value!.LinkedNow);
        Assert.Equal(1, result.Value.Review);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task Name_sku_only_without_barcode_is_review()
    {
        var harness = CreateHarness();
        harness.Products.Seed(Product(Buyer, "Premium Rice", "SUP-RICE", UnitOfMeasure.Kilogram, barcode: null));
        // Supplier live product has no barcode either → missing identifier → no auto-link

        var result = await harness.CreateAutoLink().ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(0, result.Value!.LinkedNow);
        Assert.Equal(1, result.Value.Review);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task No_match_counts_as_new()
    {
        var harness = CreateHarness();
        harness.Products.Seed(Product(Buyer, "Other", "OTHER", UnitOfMeasure.Piece, "036000291452"));
        SeedSupplierCatalogProduct(harness, ValidBarcode);

        var result = await harness.CreateAutoLink().ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(1, result.Value!.New);
        Assert.Equal(0, result.Value.LinkedNow);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task Barcode_and_sku_disagreement_counts_as_conflict()
    {
        var harness = CreateHarness();
        var productA = Product(Buyer, "Product A", "SKU-A", UnitOfMeasure.Kilogram, ValidBarcode);
        var productB = Product(Buyer, "Product B", "SUP-RICE", UnitOfMeasure.Kilogram, "036000291452");
        harness.Products.Seed(productA);
        harness.Products.Seed(productB);
        SeedSupplierCatalogProduct(harness, ValidBarcode);

        var result = await harness.CreateAutoLink().ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(1, result.Value!.Conflict);
        Assert.Equal(0, result.Value.LinkedNow);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task Already_linked_counts_already_ready_without_duplicate()
    {
        var harness = CreateHarness();
        var buyerProduct = Product(Buyer, "Premium Rice", "SUP-RICE", UnitOfMeasure.Kilogram, ValidBarcode);
        harness.Products.Seed(buyerProduct);
        SeedSupplierCatalogProduct(harness, ValidBarcode);

        var link = BuyerSupplierProductLink.Create(
            harness.Relationship.Id,
            Buyer,
            Supplier,
            buyerProduct.Id,
            harness.Exposure,
            Now.AddMinutes(6),
            effectiveOrderPrice: 45m);
        harness.Links.Items.Add(link);

        var result = await harness.CreateAutoLink().ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(0, result.Value!.LinkedNow);
        Assert.Equal(1, result.Value.AlreadyReady);
        Assert.Single(harness.Links.Items);
    }

    [Fact]
    public async Task Cross_tenant_buyer_product_is_not_auto_linked()
    {
        var harness = CreateHarness();
        var foreign = Product(
            PosOrganizationId.From(Guid.NewGuid()),
            "Premium Rice",
            "SUP-RICE",
            UnitOfMeasure.Kilogram,
            ValidBarcode);
        harness.Products.Seed(foreign);
        SeedSupplierCatalogProduct(harness, ValidBarcode);

        var result = await harness.CreateAutoLink().ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(1, result.Value!.New);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task Classify_readiness_is_read_only()
    {
        var harness = CreateHarness();
        var buyerProduct = Product(Buyer, "Premium Rice", "SUP-RICE", UnitOfMeasure.Kilogram, ValidBarcode);
        harness.Products.Seed(buyerProduct);
        SeedSupplierCatalogProduct(harness, ValidBarcode);

        var result = await harness.CreateClassify().ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value);

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(1, result.Value!.Ready);
        var item = Assert.Single(result.Value.Items);
        Assert.True(item.CanAutoLink);
        Assert.Equal("Ready", item.Status);
        Assert.Empty(harness.Links.Items);
        Assert.Equal(0, harness.Uow.SaveCount);
        Assert.Equal(0, harness.Uow.TransactionCount);
    }

    [Fact]
    public void Auto_link_source_does_not_reference_inventory_or_receiving_types()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "ConnectedSuppliers",
            "BuyerCatalogMatchReadinessUseCases.cs");
        var source = File.ReadAllText(path);

        Assert.DoesNotContain("Inventory", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StockMovement", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PurchaseStock", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GoodsReceipt", source, StringComparison.OrdinalIgnoreCase);
    }

    private static void SeedSupplierCatalogProduct(Harness harness, string barcode)
    {
        var supplierProduct = CatalogProduct.Create(
            Supplier,
            harness.Exposure.NameSnapshot,
            UnitOfMeasure.Kilogram,
            40m,
            Now,
            sku: harness.Exposure.SkuSnapshot,
            barcode: barcode,
            id: harness.Exposure.ProductId);
        harness.Products.Seed(supplierProduct);
    }

    private static Harness CreateHarness()
    {
        var relationships = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));
        relationships.Seed(relationship);

        var exposure = SupplierProductExposure.Expose(
            Supplier,
            CatalogProductId.New(),
            "Premium Rice",
            "Kilogram",
            45m,
            Now.AddMinutes(2),
            sku: "SUP-RICE");
        var exposures = new InMemoryExposures();
        exposures.Seed(exposure);

        var share = ConnectedBuyerProductShare.Share(
            relationship.Id,
            Buyer,
            Supplier,
            exposure.ProductId,
            Now.AddMinutes(4),
            buyerSpecificPoPrice: null);
        var shares = new InMemoryShares(exposures);
        shares.Seed(share);

        return new Harness(
            relationship,
            exposure,
            relationships,
            exposures,
            shares,
            new InMemoryLinks(),
            new InMemoryProducts(),
            new InMemoryUnits(),
            new FakeUow(),
            new FakeAccess(),
            new FixedTimeProvider(Now.AddMinutes(5)));
    }

    private static CatalogProduct Product(
        PosOrganizationId organizationId,
        string name,
        string? sku,
        UnitOfMeasure unitOfMeasure,
        string? barcode,
        decimal sellingPrice = 50m) =>
        CatalogProduct.Create(organizationId, name, unitOfMeasure, sellingPrice, Now, sku: sku, barcode: barcode);

    private sealed record Harness(
        ConnectedSupplierRelationship Relationship,
        SupplierProductExposure Exposure,
        InMemoryRelationships Relationships,
        InMemoryExposures Exposures,
        InMemoryShares Shares,
        InMemoryLinks Links,
        InMemoryProducts Products,
        InMemoryUnits Units,
        FakeUow Uow,
        FakeAccess Access,
        FixedTimeProvider Time)
    {
        public AutoLinkExactMatches CreateAutoLink() =>
            new(Relationships, Exposures, Shares, Links, Products, Units, Uow, Access, Time);

        public ClassifyCatalogReadiness CreateClassify() =>
            new(Relationships, Shares, Links, Products, Access);
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public int SaveCount { get; private set; }
        public int TransactionCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            TransactionCount++;
            return action(cancellationToken);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];

        public void Seed(ConnectedSupplierRelationship relationship) => _items.Add(relationship);

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.BuyerOrganizationId == buyer
                && x.SupplierOrganizationId == supplier
                && x.Status is ConnectedSupplierRelationshipStatus.Pending or ConnectedSupplierRelationshipStatus.Active));

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                _items.Where(x => supplierView
                    ? x.SupplierOrganizationId == organizationId
                    : x.BuyerOrganizationId == organizationId).ToList());

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _items.Add(relationship);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryExposures : ISupplierProductExposureRepository
    {
        private readonly List<SupplierProductExposure> _items = [];

        public void Seed(SupplierProductExposure exposure) => _items.Add(exposure);

        public Task<SupplierProductExposure?> GetAsync(
            SupplierProductExposureId id,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));

        public Task<SupplierProductExposure?> GetByProductAsync(
            PosOrganizationId supplier,
            CatalogProductId productId,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.SupplierOrganizationId == supplier && x.ProductId == productId));

        public Task<IReadOnlyList<SupplierProductExposure>> ListAsync(
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SupplierProductExposure>>(
                _items.Where(x => x.SupplierOrganizationId == supplier).ToList());

        public Task<(IReadOnlyList<SupplierProductExposure> Items, int Total)> SearchAsync(
            PosOrganizationId supplier,
            string? query,
            string? category,
            int skip,
            int take,
            CancellationToken ct = default)
        {
            var matches = _items.Where(x => x.SupplierOrganizationId == supplier).ToList();
            return Task.FromResult<(IReadOnlyList<SupplierProductExposure>, int)>(
                (matches.Skip(skip).Take(take).ToList(), matches.Count));
        }

        public Task AddAsync(SupplierProductExposure exposure, CancellationToken ct = default)
        {
            _items.Add(exposure);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SupplierProductExposure exposure, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryShares(InMemoryExposures exposures) : IConnectedBuyerProductShareRepository
    {
        private readonly List<ConnectedBuyerProductShare> _items = [];

        public void Seed(ConnectedBuyerProductShare share) => _items.Add(share);

        public Task<ConnectedBuyerProductShare?> GetAsync(
            ConnectedBuyerProductShareId id,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));

        public Task<ConnectedBuyerProductShare?> FindAsync(
            ConnectedSupplierRelationshipId relationshipId,
            CatalogProductId supplierProductId,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.RelationshipId == relationshipId && x.SupplierProductId == supplierProductId));

        public Task<IReadOnlyList<ConnectedBuyerProductShare>> ListAsync(
            ConnectedSupplierRelationshipId relationshipId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedBuyerProductShare>>(
                _items.Where(x => x.RelationshipId == relationshipId).ToList());

        public Task<(IReadOnlyList<SupplierProductExposure> Exposures,
            IReadOnlyList<ConnectedBuyerProductShare> Shares,
            int Total)> SearchSharedCatalogAsync(
            ConnectedSupplierRelationshipId relationshipId,
            PosOrganizationId supplier,
            string? query,
            string? category,
            int skip,
            int take,
            CancellationToken ct = default, CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly)
        {
            var shares = _items.Where(x => x.RelationshipId == relationshipId && x.IsShared).ToList();
            var sharedProductIds = shares.Select(x => x.SupplierProductId.Value).ToHashSet();
            var exposureItems = exposures.ListAsync(supplier, ct).GetAwaiter().GetResult()
                .Where(x => sharedProductIds.Contains(x.ProductId.Value) && x.IsExposed && x.IsOrderable)
                .ToList();
            var page = exposureItems.Skip(skip).Take(take).ToList();
            var pageShares = shares
                .Where(s => page.Any(e => e.ProductId == s.SupplierProductId))
                .ToList();
            return Task.FromResult<(IReadOnlyList<SupplierProductExposure>,
                IReadOnlyList<ConnectedBuyerProductShare>,
                int)>((page, pageShares, exposureItems.Count));
        }

        public Task<BuyerProductShareSearchPage> SearchForSupplierManagementAsync(
            ConnectedSupplierRelationshipId relationshipId,
            PosOrganizationId supplier,
            string? query,
            string? category,
            string? shareFilter,
            int skip,
            int take,
            bool idsOnly,
            CancellationToken ct = default) =>
            Task.FromResult(new BuyerProductShareSearchPage([], [], 0, 0, 0, []));

        public Task AddAsync(ConnectedBuyerProductShare share, CancellationToken ct = default)
        {
            _items.Add(share);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ConnectedBuyerProductShare share, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyDictionary<Guid, BuyerRelationshipShareStats>> ListShareStatsByRelationshipsAsync(
            IReadOnlyList<Guid> relationshipIds,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, BuyerRelationshipShareStats>>(
                _items
                    .Where(x => relationshipIds.Contains(x.RelationshipId.Value))
                    .GroupBy(x => x.RelationshipId.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => new BuyerRelationshipShareStats(
                            g.Count(x => x.IsShared),
                            g.Count(x => !x.IsShared),
                            g.Count(x => x.IsShared && x.BuyerSpecificPoPrice is not null))));

        public Task<int> CountEligibleSupplierProductsAsync(PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult(0);
    }

    private sealed class InMemoryLinks : IBuyerSupplierProductLinkRepository
    {
        public List<BuyerSupplierProductLink> Items { get; } = [];

        public Task<BuyerSupplierProductLink?> GetAsync(
            BuyerSupplierProductLinkId id,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id.Value == id.Value));

        public Task<BuyerSupplierProductLink?> FindAsync(
            ConnectedSupplierRelationshipId relationshipId,
            CatalogProductId buyerProductId,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.RelationshipId == relationshipId && x.BuyerProductId == buyerProductId && x.IsActive));

        public Task<BuyerSupplierProductLink?> FindBySupplierProductAsync(
            ConnectedSupplierRelationshipId relationshipId,
            CatalogProductId supplierProductId,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.RelationshipId == relationshipId && x.SupplierProductId == supplierProductId && x.IsActive));

        public Task<IReadOnlyList<BuyerSupplierProductLink>> ListAsync(
            ConnectedSupplierRelationshipId relationshipId,
            PosOrganizationId buyer,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BuyerSupplierProductLink>>(
                Items.Where(x => x.RelationshipId == relationshipId && x.BuyerOrganizationId == buyer).ToList());

        public Task<IReadOnlyList<BuyerSupplierProductLink>> DeltaAsync(
            ConnectedSupplierRelationshipId relationshipId,
            PosOrganizationId buyer,
            long sinceVersion,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BuyerSupplierProductLink>>(
                Items.Where(x =>
                    x.RelationshipId == relationshipId
                    && x.BuyerOrganizationId == buyer
                    && x.SyncVersion > sinceVersion).ToList());

        public Task AddAsync(BuyerSupplierProductLink link, CancellationToken ct = default)
        {
            Items.Add(link);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BuyerSupplierProductLink link, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryProducts : ICatalogProductRepository
    {
        public List<CatalogProduct> Items { get; } = [];

        public void Seed(CatalogProduct product) => Items.Add(product);

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.Id == productId));

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId,
            string normalizedSku,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.NormalizedSku == normalizedSku));

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId,
            string barcode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.Barcode == barcode));

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                Items.Where(x => x.OrganizationId == organizationId && productIds.Contains(x.Id)).ToList());

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<CatalogProduct> matches = Items.Where(x => x.OrganizationId == organizationId);
            if (filter.Status is not null)
            {
                matches = matches.Where(x => x.Status == filter.Status);
            }

            var list = matches.ToList();
            return Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(
                (list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(
                Items.Where(x => x.OrganizationId == organizationId)
                    .Skip(skip).Take(take).Select(x => x.Id.Value).ToList());

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)>
            CountConnectedBuyerAvailabilityAsync(
                PosOrganizationId organizationId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0, 0));

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>>
            ListConnectedBuyerAvailabilityCategoryFacetsAsync(
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

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            Items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryUnits : ICatalogProductUnitRepository
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
                new Dictionary<Guid, IReadOnlyList<CatalogProductUnit>>());

        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ReplaceActiveUnitsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
