using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class CreateBuyerProductAndLinkTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Supplier =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now =
        new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Link_existing_product_creates_link_without_creating_product()
    {
        var harness = CreateHarness();
        var product = Product(Buyer, "Buyer Rice", "RICE-BUYER", UnitOfMeasure.Kilogram, 68m);
        harness.Products.Seed(product);
        var useCase = harness.CreateLinkProduct();

        var result = await useCase.ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            new LinkProductRequest(product.Id.Value, harness.Exposure.Id.Value));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(product.Id.Value, result.Value!.BuyerProductId);
        Assert.Equal(harness.Exposure.ProductId.Value, result.Value.SupplierProductId);
        Assert.Equal(0, harness.Products.AddCount);
        Assert.Single(harness.Links.Items);
    }

    [Fact]
    public async Task Create_and_link_creates_exactly_one_product_and_link_with_independent_selling_price()
    {
        var harness = CreateHarness(supplierOrderPrice: 41m, buyerSpecificPoPrice: 37m);
        var useCase = harness.CreateQuickCreate();

        var result = await useCase.ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            Request(harness.Exposure, sellingPrice: 79.50m));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.True(result.Value!.CreatedNewProduct);
        Assert.False(result.Value.AlreadyLinked);
        Assert.Equal(79.50m, result.Value.BuyerSellingPrice);
        Assert.Equal(1, harness.Products.AddCount);
        var product = Assert.Single(harness.Products.Items);
        Assert.False(product.CanExposeToConnectedBuyers);
        Assert.Equal(79.50m, product.SellingPrice);
        Assert.Equal(79.50m, product.DefaultConnectedPoPrice);
        Assert.NotEqual(41m, product.DefaultConnectedPoPrice);
        var link = Assert.Single(harness.Links.Items);
        Assert.Equal(37m, link.LastKnownOrderPrice);
        Assert.NotEqual(link.LastKnownOrderPrice, product.SellingPrice);
    }

    [Fact]
    public async Task PNAME_PATH_04_Create_and_link_blocks_exact_normalized_name_duplicate()
    {
        var harness = CreateHarness();
        harness.Products.Seed(Product(Buyer, "Coke 1L", "COKE-EXISTING", UnitOfMeasure.Piece, 50m));

        var result = await harness.CreateQuickCreate().ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            Request(harness.Exposure, name: "  coke   1l  ", sellingPrice: 55m));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductNameConflict, result.ErrorCode);
        Assert.Equal(0, harness.Products.AddCount);
    }

    [Fact]
    public async Task Unshared_exposure_fails_before_product_is_added()
    {
        var harness = CreateHarness(includeShare: false);

        var result = await harness.CreateQuickCreate().ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            Request(harness.Exposure));

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.ExposureNotFound, result.ErrorCode);
        Assert.Equal(0, harness.Products.AddCount);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task Inactive_relationship_fails_before_product_is_added()
    {
        var harness = CreateHarness(activeRelationship: false);

        var result = await harness.CreateQuickCreate().ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            Request(harness.Exposure));

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.NotFound, result.ErrorCode);
        Assert.Equal(0, harness.Products.AddCount);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task Not_orderable_exposure_fails_before_product_is_added()
    {
        var harness = CreateHarness(orderable: false);

        var result = await harness.CreateQuickCreate().ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            Request(harness.Exposure));

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.ExposureNotFound, result.ErrorCode);
        Assert.Equal(0, harness.Products.AddCount);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task LinkProduct_rejects_buyer_product_owned_by_another_organization()
    {
        var harness = CreateHarness();
        var otherBuyer = PosOrganizationId.From(Guid.NewGuid());
        var foreignProduct = Product(otherBuyer, "Foreign Product", "FOREIGN-1");
        harness.Products.Seed(foreignProduct);

        var result = await harness.CreateLinkProduct().ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            new LinkProductRequest(foreignProduct.Id.Value, harness.Exposure.Id.Value));

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.ExposureNotFound, result.ErrorCode);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task Exposure_from_another_supplier_relationship_is_rejected_before_create()
    {
        var harness = CreateHarness(exposureSupplier: PosOrganizationId.From(Guid.NewGuid()));

        var result = await harness.CreateQuickCreate().ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            Request(harness.Exposure));

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.ExposureNotFound, result.ErrorCode);
        Assert.Equal(0, harness.Products.AddCount);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task Duplicate_create_and_link_retry_returns_already_linked_without_second_product()
    {
        var harness = CreateHarness();
        var useCase = harness.CreateQuickCreate();
        var request = Request(harness.Exposure, sellingPrice: 93m);

        var first = await useCase.ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value, request);
        var second = await useCase.ExecuteAsync(Buyer.Value, harness.Relationship.Id.Value, request);

        Assert.True(first.IsSuccess, $"{first.ErrorCode}: {first.ErrorMessage}");
        Assert.True(second.IsSuccess, $"{second.ErrorCode}: {second.ErrorMessage}");
        Assert.True(first.Value!.CreatedNewProduct);
        Assert.False(first.Value.AlreadyLinked);
        Assert.False(second.Value!.CreatedNewProduct);
        Assert.True(second.Value.AlreadyLinked);
        Assert.Equal(first.Value.BuyerProductId, second.Value.BuyerProductId);
        Assert.Equal(1, harness.Products.AddCount);
        Assert.Single(harness.Products.Items);
        Assert.Single(harness.Links.Items);
    }

    [Fact]
    public void Match_suggestions_rank_exact_sku_before_exact_name_with_compatible_uom()
    {
        var exactName = Product(Buyer, "Premium Rice", "OTHER", UnitOfMeasure.Kilogram, 61m);
        var exactSku = Product(Buyer, "Different Name", "SUP-RICE", UnitOfMeasure.Piece, 70m);
        var nameOnlyWrongUom = Product(Buyer, "Premium Rice", "THIRD", UnitOfMeasure.Piece, 65m);

        var ranked = BuyerCatalogMatchSuggestions.Rank(
            " premium rice ",
            "sup-rice",
            "Kilogram",
            [nameOnlyWrongUom, exactName, exactSku]);

        Assert.Collection(
            ranked,
            first =>
            {
                Assert.Equal(exactSku.Id.Value, first.ProductId);
                Assert.Equal(BuyerCatalogMatchSuggestions.ExactSku, first.MatchKind);
            },
            second =>
            {
                Assert.Equal(exactName.Id.Value, second.ProductId);
                Assert.Equal(BuyerCatalogMatchSuggestions.ExactNameCompatibleUom, second.MatchKind);
            },
            third =>
            {
                Assert.Equal(nameOnlyWrongUom.Id.Value, third.ProductId);
                Assert.Equal(BuyerCatalogMatchSuggestions.ExactName, third.MatchKind);
            });
    }

    [Fact]
    public void Create_and_link_source_does_not_reference_inventory_or_receiving_types()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "ConnectedSuppliers",
            "CreateBuyerProductAndLinkUseCases.cs");
        var source = File.ReadAllText(path);
        var start = source.IndexOf("public sealed class CreateBuyerProductAndLink", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = source[start..];

        Assert.DoesNotContain("Inventory", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StockMovement", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PurchaseStock", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GoodsReceipt", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_and_link_does_not_inject_inventory_dependencies_and_creates_catalog_product_only()
    {
        var harness = CreateHarness();

        var result = await harness.CreateQuickCreate().ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            Request(harness.Exposure, sellingPrice: 55m));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.True(result.Value!.CreatedNewProduct);
        Assert.Equal(1, harness.Products.AddCount);
        Assert.Single(harness.Products.Items);
        Assert.Single(harness.Links.Items);

        var ctorParams = typeof(CreateBuyerProductAndLink)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType.FullName ?? p.ParameterType.Name)
            .ToList();
        Assert.DoesNotContain(ctorParams, name => name.Contains("Inventory", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ctorParams, name => name.Contains("StockMovement", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ctorParams, name => name.Contains("GoodsReceipt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Uom_mismatch_fails_quick_create_before_product_is_added()
    {
        var harness = CreateHarness(exposureUom: "Kilogram");
        var request = Request(harness.Exposure) with { UnitOfMeasure = "Piece" };

        var result = await harness.CreateQuickCreate().ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            request);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, harness.Products.AddCount);
        Assert.Empty(harness.Links.Items);
    }

    [Fact]
    public async Task Missing_business_usage_fails_before_product_is_added()
    {
        var harness = CreateHarness();
        var request = Request(harness.Exposure) with { BusinessUsage = null };

        var result = await harness.CreateQuickCreate().ExecuteAsync(
            Buyer.Value,
            harness.Relationship.Id.Value,
            request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CatalogBulkValidation, result.ErrorCode);
        Assert.Equal(0, harness.Products.AddCount);
        Assert.Empty(harness.Links.Items);
    }

    private static Harness CreateHarness(
        bool activeRelationship = true,
        bool includeShare = true,
        bool orderable = true,
        decimal supplierOrderPrice = 45m,
        decimal? buyerSpecificPoPrice = null,
        PosOrganizationId? exposureSupplier = null,
        string exposureUom = "Kilogram")
    {
        var relationships = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        if (activeRelationship)
        {
            relationship.Approve(Now.AddMinutes(1));
        }
        relationships.Seed(relationship);

        var supplierForExposure = exposureSupplier ?? Supplier;
        var exposure = SupplierProductExposure.Expose(
            supplierForExposure,
            CatalogProductId.New(),
            "Premium Rice",
            exposureUom,
            supplierOrderPrice,
            Now.AddMinutes(2),
            sku: "SUP-RICE");
        if (!orderable)
        {
            exposure.MarkNotOrderable(Now.AddMinutes(3));
        }

        var exposures = new InMemoryExposures();
        exposures.Seed(exposure);
        var shares = new InMemoryShares();
        if (includeShare)
        {
            shares.Seed(ConnectedBuyerProductShare.Share(
                relationship.Id,
                Buyer,
                Supplier,
                exposure.ProductId,
                Now.AddMinutes(4),
                buyerSpecificPoPrice));
        }

        return new Harness(
            relationship,
            exposure,
            relationships,
            exposures,
            shares,
            new InMemoryLinks(),
            new InMemoryProducts(),
            new InMemoryUnits(),
            new InMemoryCategories(),
            new InMemoryBrands(),
            new FakeUow(),
            new FakeAccess(),
            new FixedClock(Now.AddMinutes(5)),
            new FixedTimeProvider(Now.AddMinutes(5)));
    }

    /// <summary>
    /// Valid create-and-link request matching the React contract (BusinessUsage=Resale).
    /// Negative cases must override with <c>with { BusinessUsage = null }</c> (or equivalent).
    /// </summary>
    private static CreateBuyerProductAndLinkRequest Request(
        SupplierProductExposure exposure,
        decimal sellingPrice = 72m,
        string? name = null) =>
        new(
            exposure.Id.Value,
            name ?? "Buyer Premium Rice",
            exposure.UnitOfMeasureCode,
            sellingPrice,
            Sku: $"BUY-{Guid.NewGuid():N}",
            BusinessUsage: "Resale");

    private static CatalogProduct Product(
        PosOrganizationId organizationId,
        string name,
        string? sku,
        UnitOfMeasure unitOfMeasure = UnitOfMeasure.Kilogram,
        decimal sellingPrice = 50m) =>
        CatalogProduct.Create(organizationId, name, unitOfMeasure, sellingPrice, Now, sku: sku);

    private sealed record Harness(
        ConnectedSupplierRelationship Relationship,
        SupplierProductExposure Exposure,
        InMemoryRelationships Relationships,
        InMemoryExposures Exposures,
        InMemoryShares Shares,
        InMemoryLinks Links,
        InMemoryProducts Products,
        InMemoryUnits Units,
        InMemoryCategories Categories,
        InMemoryBrands Brands,
        FakeUow Uow,
        FakeAccess Access,
        FixedClock Clock,
        FixedTimeProvider Time)
    {
        public CreateBuyerProductAndLink CreateQuickCreate() =>
            new(Relationships, Exposures, Shares, Links, Products, Units, Categories, Brands, Uow, Access, Clock, Time);

        public LinkProduct CreateLinkProduct() =>
            new(Relationships, Exposures, Links, Products, Units, Uow, Access, Shares, Time);
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

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
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

    private sealed class InMemoryShares : IConnectedBuyerProductShareRepository
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
            CancellationToken ct = default, CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>,
                IReadOnlyList<ConnectedBuyerProductShare>,
                int)>(([], [], 0));

        public Task<BuyerProductShareSearchPage> SearchForSupplierManagementAsync(
            ConnectedSupplierRelationshipId relationshipId,
            PosOrganizationId supplier,
            string? query,
            string? category,
            string? shareFilter,
            int skip,
            int take,
            bool idsOnly,
            CancellationToken ct = default,
            CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly) =>
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
                x.RelationshipId == relationshipId && x.BuyerProductId == buyerProductId));

        public Task<BuyerSupplierProductLink?> FindBySupplierProductAsync(
            ConnectedSupplierRelationshipId relationshipId,
            CatalogProductId supplierProductId,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.RelationshipId == relationshipId && x.SupplierProductId == supplierProductId));

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
        public int AddCount { get; private set; }

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

        public Task<CatalogProduct?> FindByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.NormalizedName == normalizedName));

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
            if (filter.UnitOfMeasure is not null)
            {
                matches = matches.Where(x => x.UnitOfMeasure == filter.UnitOfMeasure);
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                matches = matches.Where(x =>
                    x.Name.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                    || (x.Sku?.Contains(filter.Search, StringComparison.OrdinalIgnoreCase) ?? false));
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
                CancellationToken cancellationToken = default)
        {
            var products = Items.Where(x =>
                x.OrganizationId == organizationId && x.Status == CatalogProductStatus.Active).ToList();
            return Task.FromResult((
                products.Count,
                products.Count(x => x.CanExposeToConnectedBuyers),
                products.Count(x => !x.CanExposeToConnectedBuyers)));
        }

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
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId
                && x.PlatformGlobalProductId == platformGlobalProductId));

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(
                Items.Where(x =>
                        x.OrganizationId == organizationId
                        && x.PlatformGlobalProductId is Guid id
                        && platformGlobalProductIds.Contains(id))
                    .Select(x => x.PlatformGlobalProductId!.Value)
                    .ToHashSet());

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            AddCount++;
            Items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryUnits : ICatalogProductUnitRepository
    {
        private readonly List<CatalogProductUnit> _items = [];

        public Task<CatalogProductUnit?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductUnitId unitId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.Id == unitId));

        public Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductUnit>>(
                _items.Where(x => x.OrganizationId == organizationId && x.ProductId == productId).ToList());

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>>(
                _items.Where(x => x.OrganizationId == organizationId && productIds.Contains(x.ProductId))
                    .GroupBy(x => x.ProductId.Value)
                    .ToDictionary(x => x.Key, x => (IReadOnlyList<CatalogProductUnit>)x.ToList()));

        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default)
        {
            _items.Add(unit);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ReplaceActiveUnitsAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            ProductUnitKind kind,
            IReadOnlyList<CatalogProductUnit> units,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default)
        {
            _items.AddRange(units);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCategories : IProductCategoryRepository
    {
        private readonly List<ProductCategory> _items = [];

        public Task<ProductCategory?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductCategoryId categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.Id == categoryId));

        public Task<ProductCategory?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId == organizationId
                && x.NormalizedName == normalizedName
                && x.Status == ProductCategoryStatus.Active));

        public Task<ProductCategory?> FindActiveBySourceGlobalCategoryIdAsync(
            PosOrganizationId organizationId,
            Guid sourceGlobalCategoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId == organizationId
                && x.SourceGlobalCategoryId == sourceGlobalCategoryId
                && x.Status == ProductCategoryStatus.Active));

        public Task<(IReadOnlyList<ProductCategory> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            ProductCategoryStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var items = _items.Where(x => x.OrganizationId == organizationId).ToList();
            return Task.FromResult<(IReadOnlyList<ProductCategory>, int)>(
                (items.Skip(skip).Take(take).ToList(), items.Count));
        }

        public Task<IReadOnlyList<ProductCategory>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<ProductCategoryId> categoryIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductCategory>>(
                _items.Where(x =>
                    x.OrganizationId == organizationId && categoryIds.Contains(x.Id)).ToList());

        public Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default)
        {
            _items.Add(category);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryBrands : IProductBrandRepository
    {
        private readonly List<ProductBrand> _items = [];

        public Task<ProductBrand?> GetByIdAsync(
            PosOrganizationId organizationId,
            ProductBrandId brandId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.Id == brandId));

        public Task<ProductBrand?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId == organizationId
                && x.NormalizedName == normalizedName
                && x.Status == ProductBrandStatus.Active));

        public Task<(IReadOnlyList<ProductBrand> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            ProductBrandStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var items = _items.Where(x => x.OrganizationId == organizationId).ToList();
            return Task.FromResult<(IReadOnlyList<ProductBrand>, int)>(
                (items.Skip(skip).Take(take).ToList(), items.Count));
        }

        public Task<IReadOnlyList<ProductBrand>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<ProductBrandId> brandIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductBrand>>(
                _items.Where(x =>
                    x.OrganizationId == organizationId && brandIds.Contains(x.Id)).ToList());

        public Task AddAsync(ProductBrand brand, CancellationToken cancellationToken = default)
        {
            _items.Add(brand);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductBrand brand, CancellationToken cancellationToken = default) =>
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
