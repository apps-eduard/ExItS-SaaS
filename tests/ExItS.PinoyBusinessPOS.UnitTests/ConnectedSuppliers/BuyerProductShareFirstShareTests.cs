using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class BuyerProductShareFirstShareTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Supplier =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_product_domain_create_is_eligible_not_shared_and_has_no_default_po()
    {
        var product = CatalogProduct.Create(Supplier, "Coke", UnitOfMeasure.Piece, 65m, Now);
        Assert.True(product.CanExposeToConnectedBuyers);
        Assert.False(product.IsBlockedFromConnectedBuyers);
        Assert.Null(product.DefaultConnectedPoPrice);
    }

    [Fact]
    public async Task Share_without_default_po_requires_EstablishDefaultPoPrice()
    {
        var harness = CreateHarness();
        var product = CatalogProduct.Create(Supplier, "Sprite", UnitOfMeasure.Piece, 63m, Now);
        harness.Products.Seed(product);

        var result = await harness.SetShares.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            [new SetBuyerProductShareItem(product.Id.Value, true)],
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.MissingDefaultPo, result.ErrorCode);
        Assert.Empty(harness.Exposures.Items);
        Assert.Empty(harness.Shares.Items);
    }

    [Fact]
    public async Task Share_with_EstablishDefaultPoPrice_creates_exposure_and_share_atomically()
    {
        var harness = CreateHarness();
        var product = CatalogProduct.Create(Supplier, "Pepsi", UnitOfMeasure.Piece, 60m, Now);
        harness.Products.Seed(product);

        var result = await harness.SetShares.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            [new SetBuyerProductShareItem(product.Id.Value, true, EstablishDefaultPoPrice: 48m)],
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, harness.Uow.SaveCount);
        var updated = Assert.Single(harness.Products.Items);
        Assert.Equal(48m, updated.DefaultConnectedPoPrice);
        Assert.False(updated.IsBlockedFromConnectedBuyers);
        var exposure = Assert.Single(harness.Exposures.Items);
        Assert.True(exposure.IsExposed);
        Assert.Equal(48m, exposure.SupplierOrderPrice);
        var share = Assert.Single(harness.Shares.Items);
        Assert.True(share.IsShared);
        Assert.Null(share.BuyerSpecificPoPrice);
        Assert.Equal(48m, result.Value![0].DefaultPoPrice);
        Assert.NotEqual(60m, result.Value![0].DefaultPoPrice);
    }

    [Fact]
    public async Task Blocked_product_cannot_be_shared()
    {
        var harness = CreateHarness();
        var product = CatalogProduct.Create(Supplier, "Blocked", UnitOfMeasure.Piece, 10m, Now);
        product.BlockFromConnectedBuyers(Now.AddMinutes(1));
        product.SetDefaultConnectedPoPrice(9m, Now.AddMinutes(2));
        harness.Products.Seed(product);

        var result = await harness.SetShares.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            [new SetBuyerProductShareItem(product.Id.Value, true)],
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.ProductBlocked, result.ErrorCode);
    }

    [Fact]
    public async Task Unblock_restores_prior_share_intent_when_default_po_exists()
    {
        var harness = CreateHarness();
        var product = CatalogProduct.Create(Supplier, "Rice", UnitOfMeasure.Kilogram, 55m, Now);
        product.SetDefaultConnectedPoPrice(50m, Now.AddMinutes(1));
        harness.Products.Seed(product);

        var shareResult = await harness.SetShares.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            [new SetBuyerProductShareItem(product.Id.Value, true)],
            CancellationToken.None);
        Assert.True(shareResult.IsSuccess);

        product.BlockFromConnectedBuyers(Now.AddMinutes(2));
        var exposure = Assert.Single(harness.Exposures.Items);
        exposure.Deactivate(Now.AddMinutes(2));
        Assert.False(exposure.IsExposed);
        Assert.True(Assert.Single(harness.Shares.Items).IsShared);

        product.AllowForConnectedBuyers(Now.AddMinutes(3));
        var restore = await harness.SetShares.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            [new SetBuyerProductShareItem(product.Id.Value, true)],
            CancellationToken.None);
        Assert.True(restore.IsSuccess);

        exposure = Assert.Single(harness.Exposures.Items);
        Assert.True(exposure.IsExposed);
        Assert.Equal(50m, exposure.SupplierOrderPrice);
        Assert.True(Assert.Single(harness.Shares.Items).IsShared);
    }

    [Fact]
    public async Task Query_returns_unconfigured_eligible_products()
    {
        var harness = CreateHarness();
        var product = CatalogProduct.Create(Supplier, "Unconfigured", UnitOfMeasure.Piece, 12m, Now);
        harness.Products.Seed(product);
        harness.Shares.SeedSearchPage(new BuyerProductShareSearchPage(
            [new BuyerProductShareManagementRow(product, null, null, null)],
            [product.Id.Value],
            1,
            1,
            0,
            []));

        var query = new QueryBuyerProductShares(harness.Relationships, harness.Shares, harness.Access);
        var result = await query.ExecuteAsync(
            Supplier.Value, harness.Relationship.Id.Value, null, null, null, 1, 25, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.False(item.IsShared);
        Assert.Null(item.DefaultPoPrice);
        Assert.Equal(12m, item.SellingPrice);
        Assert.Equal(1, result.Value.EligibleCount);
        Assert.Equal(0, result.Value.SharedCount);
    }

    [Fact]
    public async Task Bulk_share_returns_NeedsDefaultPo_without_mutating()
    {
        var harness = CreateHarness();
        var product = CatalogProduct.Create(Supplier, "NeedsPrice", UnitOfMeasure.Piece, 20m, Now);
        harness.Products.Seed(product);

        var bulk = new BulkMutateBuyerProductShares(
            harness.Relationships, harness.Shares, harness.Products, harness.SetShares, harness.Access);
        var result = await bulk.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerProductShareMutationRequest("Share", ProductIds: [product.Id.Value]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.AffectedCount);
        var missing = Assert.Single(result.Value.NeedsDefaultPo!);
        Assert.Equal(product.Id.Value, missing.ProductId);
        Assert.Equal(20m, missing.SellingPrice);
        Assert.Empty(harness.Shares.Items);
    }

    [Fact]
    public void ConnectedPoPricing_never_uses_retail_selling_price()
    {
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));
        var productId = CatalogProductId.New();
        var exposure = SupplierProductExposure.Expose(Supplier, productId, "Item", "Piece", 40m, Now);
        var share = ConnectedBuyerProductShare.Share(relationship.Id, Buyer, Supplier, productId, Now);

        Assert.True(ConnectedPoPricing.TryResolveEffectivePrice(exposure, share, out var price));
        Assert.Equal(40m, price);
        Assert.NotEqual(65m, price);
    }

    [Fact]
    public async Task Pricing_preview_returns_NeedsDefaultPo_list_instead_of_hard_fail()
    {
        var harness = CreateHarness();
        var withPo = CatalogProduct.Create(Supplier, "Apple", UnitOfMeasure.Piece, 220m, Now);
        withPo.SetDefaultConnectedPoPrice(200m, Now.AddMinutes(1));
        var missingPo = CatalogProduct.Create(Supplier, "Banana", UnitOfMeasure.Piece, 90m, Now);
        harness.Products.Seed(withPo);
        harness.Products.Seed(missingPo);

        var preview = new PreviewBuyerProductPricing(
            harness.Relationships, harness.Shares, harness.Products, harness.Access);
        var result = await preview.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerPricingRequest(
                "DiscountPercent",
                ProductIds: [withPo.Id.Value, missingPo.Id.Value],
                Percent: 10m));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(0, result.Value!.AffectedCount);
        Assert.Empty(result.Value.Items);
        var missing = Assert.Single(result.Value.NeedsDefaultPo!);
        Assert.Equal(missingPo.Id.Value, missing.ProductId);
        Assert.Equal("Banana", missing.Name);
    }

    [Fact]
    public async Task Pricing_preview_discount_succeeds_when_all_have_default_po()
    {
        var harness = CreateHarness();
        var apple = CatalogProduct.Create(Supplier, "Apple", UnitOfMeasure.Piece, 220m, Now);
        apple.SetDefaultConnectedPoPrice(200m, Now.AddMinutes(1));
        harness.Products.Seed(apple);

        var preview = new PreviewBuyerProductPricing(
            harness.Relationships, harness.Shares, harness.Products, harness.Access);
        var result = await preview.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerPricingRequest(
                "DiscountPercent",
                ProductIds: [apple.Id.Value],
                Percent: 10m));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(1, result.Value!.AffectedCount);
        Assert.Null(result.Value.NeedsDefaultPo);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(180m, item.ProposedBuyerPrice);
    }

    private static Harness CreateHarness()
    {
        var relationships = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));
        relationships.Seed(relationship);

        var exposures = new InMemoryExposures();
        var shares = new InMemoryShares();
        var products = new InMemoryProducts();
        var uow = new FakeUow();
        var access = new FakeAccess();
        var clock = new FixedTimeProvider(Now.AddMinutes(10));
        var setShares = new SetBuyerProductShares(relationships, exposures, shares, products, uow, access, clock);
        return new Harness(relationship, relationships, exposures, shares, products, uow, access, setShares);
    }

    private sealed record Harness(
        ConnectedSupplierRelationship Relationship,
        InMemoryRelationships Relationships,
        InMemoryExposures Exposures,
        InMemoryShares Shares,
        InMemoryProducts Products,
        FakeUow Uow,
        FakeAccess Access,
        SetBuyerProductShares SetShares);

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];
        public void Seed(ConnectedSupplierRelationship relationship) => _items.Add(relationship);
        public Task<ConnectedSupplierRelationship?> GetAsync(ConnectedSupplierRelationshipId id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));
        public Task<ConnectedSupplierRelationship?> FindOpenAsync(PosOrganizationId buyer, PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(null);
        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(PosOrganizationId organizationId, bool supplierView, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(_items);
        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _items.Add(relationship);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryExposures : ISupplierProductExposureRepository
    {
        public List<SupplierProductExposure> Items { get; } = [];
        public Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id.Value == id.Value));
        public Task<SupplierProductExposure?> GetByProductAsync(PosOrganizationId supplier, CatalogProductId productId, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.SupplierOrganizationId == supplier && x.ProductId == productId));
        public Task<IReadOnlyList<SupplierProductExposure>> ListAsync(PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SupplierProductExposure>>(Items.Where(x => x.SupplierOrganizationId == supplier).ToList());
        public Task<(IReadOnlyList<SupplierProductExposure> Items, int Total)> SearchAsync(
            PosOrganizationId supplier, string? query, string? category, int skip, int take, CancellationToken ct = default) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, int)>(([], 0));
        public Task AddAsync(SupplierProductExposure exposure, CancellationToken ct = default)
        {
            Items.Add(exposure);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(SupplierProductExposure exposure, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryShares : IConnectedBuyerProductShareRepository
    {
        public List<ConnectedBuyerProductShare> Items { get; } = [];
        private BuyerProductShareSearchPage? _searchPage;

        public void SeedSearchPage(BuyerProductShareSearchPage page) => _searchPage = page;

        public Task<ConnectedBuyerProductShare?> GetAsync(ConnectedBuyerProductShareId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id.Value == id.Value));
        public Task<ConnectedBuyerProductShare?> FindAsync(
            ConnectedSupplierRelationshipId relationshipId, CatalogProductId supplierProductId, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.RelationshipId == relationshipId && x.SupplierProductId == supplierProductId));
        public Task<IReadOnlyList<ConnectedBuyerProductShare>> ListAsync(
            ConnectedSupplierRelationshipId relationshipId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedBuyerProductShare>>(
                Items.Where(x => x.RelationshipId == relationshipId).ToList());
        public Task<(IReadOnlyList<SupplierProductExposure> Exposures, IReadOnlyList<ConnectedBuyerProductShare> Shares, int Total)>
            SearchSharedCatalogAsync(
                ConnectedSupplierRelationshipId relationshipId, PosOrganizationId supplier, string? query, string? category,
                int skip, int take, CancellationToken ct = default, CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, IReadOnlyList<ConnectedBuyerProductShare>, int)>(([], [], 0));
        public Task<BuyerProductShareSearchPage> SearchForSupplierManagementAsync(
            ConnectedSupplierRelationshipId relationshipId, PosOrganizationId supplier, string? query, string? category,
            string? shareFilter, int skip, int take, bool idsOnly, CancellationToken ct = default) =>
            Task.FromResult(_searchPage ?? new BuyerProductShareSearchPage([], [], 0, 0, 0, []));
        public Task AddAsync(ConnectedBuyerProductShare share, CancellationToken ct = default)
        {
            Items.Add(share);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(ConnectedBuyerProductShare share, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyDictionary<Guid, BuyerRelationshipShareStats>> ListShareStatsByRelationshipsAsync(
            IReadOnlyList<Guid> relationshipIds,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, BuyerRelationshipShareStats>>(
                Items
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

    private sealed class InMemoryProducts : ICatalogProductRepository
    {
        public List<CatalogProduct> Items { get; } = [];
        public void Seed(CatalogProduct product) => Items.Add(product);
        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == productId));
        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                Items.Where(x => x.OrganizationId == organizationId && productIds.Contains(x.Id)).ToList());
        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([], 0));
        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0, 0));
        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
            PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);
        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            Items.Add(product);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
