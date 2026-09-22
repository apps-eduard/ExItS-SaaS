using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

/// <summary>
/// Regression: AllEligible Stop sharing → Share selected must clear exclusions
/// (not return Updated 0 / leave IsShared=false rows).
/// </summary>
public sealed class AllEligibleShareClearsExclusionsTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Supplier =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Stop_sharing_then_Share_selected_clears_exclusions()
    {
        var harness = CreateAllEligibleHarness(productCount: 5);
        var ids = harness.Products.Items.Select(p => p.Id.Value).ToList();

        var bulk = new BulkMutateBuyerProductShares(
            harness.Relationships, harness.Shares, harness.Products, harness.Inventory, harness.SetShares, harness.Access);

        var stop = await bulk.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerProductShareMutationRequest("Unshare", ProductIds: ids),
            CancellationToken.None);
        Assert.True(stop.IsSuccess, stop.ErrorMessage);
        Assert.Equal(5, stop.Value!.AffectedCount);
        Assert.Equal(5, harness.Shares.Items.Count(x => !x.IsShared));
        Assert.All(ids, id =>
            Assert.False(ConnectedBuyerCatalogProjection.IsEffectivelyShared(
                CatalogSharingMode.AllEligible,
                harness.Products.Items.Single(p => p.Id.Value == id),
                isInventoryTracked: true,
                harness.Shares.Items.Single(s => s.SupplierProductId.Value == id))));

        var shareAgain = await bulk.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerProductShareMutationRequest("Share", ProductIds: ids),
            CancellationToken.None);
        Assert.True(shareAgain.IsSuccess, shareAgain.ErrorMessage);
        Assert.Equal(5, shareAgain.Value!.AffectedCount);
        Assert.Equal(0, shareAgain.Value.AlreadySharedCount);
        Assert.Empty(harness.Shares.Items); // exclusions removed
        Assert.All(ids, id =>
            Assert.True(ConnectedBuyerCatalogProjection.IsEffectivelyShared(
                CatalogSharingMode.AllEligible,
                harness.Products.Items.Single(p => p.Id.Value == id),
                isInventoryTracked: true,
                share: null)));
    }

    [Fact]
    public async Task Share_selected_partial_clears_only_selected_exclusions()
    {
        var harness = CreateAllEligibleHarness(productCount: 5);
        var ids = harness.Products.Items.Select(p => p.Id.Value).ToList();
        var bulk = new BulkMutateBuyerProductShares(
            harness.Relationships, harness.Shares, harness.Products, harness.Inventory, harness.SetShares, harness.Access);

        Assert.True((await bulk.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerProductShareMutationRequest("Unshare", ProductIds: ids),
            CancellationToken.None)).IsSuccess);

        var five = ids.Take(2).ToList();
        var shareFive = await bulk.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerProductShareMutationRequest("Share", ProductIds: five),
            CancellationToken.None);
        Assert.True(shareFive.IsSuccess, shareFive.ErrorMessage);
        Assert.Equal(2, shareFive.Value!.AffectedCount);
        Assert.Equal(3, harness.Shares.Items.Count(x => !x.IsShared));
        Assert.DoesNotContain(harness.Shares.Items, s => five.Contains(s.SupplierProductId.Value));
    }

    [Fact]
    public async Task Share_selected_when_already_shared_reports_already_shared_not_zero_mutation_as_failure()
    {
        var harness = CreateAllEligibleHarness(productCount: 3);
        var ids = harness.Products.Items.Select(p => p.Id.Value).ToList();
        var bulk = new BulkMutateBuyerProductShares(
            harness.Relationships, harness.Shares, harness.Products, harness.Inventory, harness.SetShares, harness.Access);

        var result = await bulk.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerProductShareMutationRequest("Share", ProductIds: ids),
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(0, result.Value!.AffectedCount);
        Assert.Equal(3, result.Value.AlreadySharedCount);
    }

    [Fact]
    public async Task Exclusion_survives_until_explicit_Share_again()
    {
        var harness = CreateAllEligibleHarness(productCount: 1);
        var product = harness.Products.Items[0];
        var bulk = new BulkMutateBuyerProductShares(
            harness.Relationships, harness.Shares, harness.Products, harness.Inventory, harness.SetShares, harness.Access);

        Assert.True((await bulk.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerProductShareMutationRequest("Unshare", ProductIds: [product.Id.Value]),
            CancellationToken.None)).IsSuccess);

        var exclusion = Assert.Single(harness.Shares.Items);
        Assert.False(exclusion.IsShared);

        // Tracking OFF/ON cycle does not clear exclusion preference.
        Assert.Equal(
            "Excluded",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, true, exclusion));
        Assert.Equal(
            "Ineligible",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, false, exclusion));
        Assert.Equal(
            "Excluded",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, true, exclusion));

        Assert.True((await bulk.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            new BulkBuyerProductShareMutationRequest("Share", ProductIds: [product.Id.Value]),
            CancellationToken.None)).IsSuccess);
        Assert.Empty(harness.Shares.Items);
    }

    [Fact]
    public async Task SelectedOnly_Share_still_requires_explicit_share_row()
    {
        var harness = CreateSelectedOnlyHarness();
        var product = harness.Products.Items[0];
        product.SetDefaultConnectedPoPrice(10m, Now);
        harness.Inventory.Accounts.Add(
            EnableTracked(product.Id));

        var result = await harness.SetShares.ExecuteAsync(
            Supplier.Value,
            harness.Relationship.Id.Value,
            [new SetBuyerProductShareItem(product.Id.Value, true)],
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var share = Assert.Single(harness.Shares.Items);
        Assert.True(share.IsShared);
    }

    private static InventoryAccount EnableTracked(CatalogProductId productId)
    {
        var account = InventoryAccount.CreateUntracked(Supplier, productId, Now);
        account.Enable(0m, UnitOfMeasure.Piece, Guid.Empty, Now, hasOpeningStockAlready: false);
        return account;
    }

    private static Harness CreateAllEligibleHarness(int productCount)
    {
        var harness = CreateBaseHarness(CatalogSharingMode.AllEligible);
        for (var i = 0; i < productCount; i++)
        {
            var product = CatalogProduct.Create(Supplier, $"Product {i + 1}", UnitOfMeasure.Piece, 10m + i, Now);
            product.SetDefaultConnectedPoPrice(10m + i, Now);
            harness.Products.Seed(product);
            harness.Inventory.Accounts.Add(EnableTracked(product.Id));
            harness.Exposures.Items.Add(SupplierProductExposure.Expose(
                Supplier, product.Id, product.Name, "Piece", 10m + i, Now, product.Sku));
        }

        return harness;
    }

    private static Harness CreateSelectedOnlyHarness()
    {
        var harness = CreateBaseHarness(CatalogSharingMode.SelectedOnly);
        var product = CatalogProduct.Create(Supplier, "SelectedOnly Widget", UnitOfMeasure.Piece, 10m, Now);
        harness.Products.Seed(product);
        return harness;
    }

    private static Harness CreateBaseHarness(CatalogSharingMode mode)
    {
        var relationships = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));
        relationship.ConfigureCatalogSharing(mode, null, Now.AddMinutes(2));
        relationships.Seed(relationship);

        var exposures = new InMemoryExposures();
        var shares = new InMemoryShares();
        var products = new InMemoryProducts();
        var inventory = new TrackedInventory();
        var uow = new FakeUow();
        var access = new FakeAccess();
        var clock = new FixedTimeProvider(Now.AddMinutes(10));
        var setShares = new SetBuyerProductShares(
            relationships, exposures, shares, products, inventory, uow, access, clock);
        return new Harness(relationship, relationships, exposures, shares, products, inventory, uow, access, setShares);
    }

    private sealed record Harness(
        ConnectedSupplierRelationship Relationship,
        InMemoryRelationships Relationships,
        InMemoryExposures Exposures,
        InMemoryShares Shares,
        InMemoryProducts Products,
        TrackedInventory Inventory,
        FakeUow Uow,
        FakeAccess Access,
        SetBuyerProductShares SetShares);

    private sealed class TrackedInventory : CostResolverInventoryStub
    {
        public List<InventoryAccount> Accounts { get; } = [];

        public override Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.ProductId == productId));
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

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
        public Task AddAsync(SupplierProductExposure exposure, CancellationToken ct = default)
        {
            Items.Add(exposure);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(SupplierProductExposure exposure, CancellationToken ct = default) => Task.CompletedTask;
        public Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<SupplierProductExposure?> GetByProductAsync(PosOrganizationId supplier, CatalogProductId productId, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.SupplierOrganizationId == supplier && x.ProductId == productId));
        public Task<IReadOnlyList<SupplierProductExposure>> ListAsync(PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SupplierProductExposure>>(Items.Where(x => x.SupplierOrganizationId == supplier).ToList());
        public Task<(IReadOnlyList<SupplierProductExposure> Items, int Total)> SearchAsync(
            PosOrganizationId supplier, string? query, string? category, int skip, int take, CancellationToken ct = default) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, int)>(([], 0));
    }

    private sealed class InMemoryShares : IConnectedBuyerProductShareRepository
    {
        public List<ConnectedBuyerProductShare> Items { get; } = [];
        public Task<ConnectedBuyerProductShare?> GetAsync(ConnectedBuyerProductShareId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
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
                int skip, int take, CancellationToken ct = default,
                CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, IReadOnlyList<ConnectedBuyerProductShare>, int)>(([], [], 0));
        public Task<BuyerProductShareSearchPage> SearchForSupplierManagementAsync(
            ConnectedSupplierRelationshipId relationshipId, PosOrganizationId supplier, string? query, string? category,
            string? shareFilter, int skip, int take, bool idsOnly, CancellationToken ct = default,
            CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly) =>
            Task.FromResult(new BuyerProductShareSearchPage([], [], 0, 0, 0, []));
        public Task AddAsync(ConnectedBuyerProductShare share, CancellationToken ct = default)
        {
            Items.Add(share);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(ConnectedBuyerProductShare share, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveAsync(ConnectedBuyerProductShare share, CancellationToken ct = default)
        {
            Items.RemoveAll(x => x.Id == share.Id);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyDictionary<Guid, BuyerRelationshipShareStats>> ListShareStatsByRelationshipsAsync(
            IReadOnlyList<Guid> relationshipIds, CancellationToken ct = default) =>
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
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            Items.Add(product);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == productId));
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
        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
    }
}
