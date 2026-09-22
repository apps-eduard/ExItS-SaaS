using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

/// <summary>
/// Regression: AllEligible + tracking ON must auto-expose without re-share;
/// seller row Tracking must match eligibility; exclusions survive track cycles.
/// </summary>
public sealed class ConnectedBuyerTrackingAutoExposureRegressionTests
{
    private static readonly PosOrganizationId Supplier = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-20T10:00:00Z");

    [Fact]
    public async Task Enable_tracking_creates_exposure_without_re_reading_stale_inventory()
    {
        var product = CatalogProduct.Create(Supplier, "Product X", UnitOfMeasure.Piece, 25m, Now);
        var account = InventoryAccount.CreateUntracked(Supplier, product.Id, Now);
        // Simulate: Enable() applied in-memory but a stale AsNoTracking read would still see untracked.
        account.Enable(0m, UnitOfMeasure.Piece, Guid.Empty, Now, hasOpeningStockAlready: false);

        Assert.True(product.CanExposeToConnectedBuyers);

        var products = new MemoryProducts([product]);
        var inventory = new StaleUntrackedReadInventory(account);
        var exposures = new CapturingExposures();

        await ConnectedBuyerTrackingExposureSync.AfterTrackingEnabledAsync(
            product,
            products,
            exposures,
            Now,
            CancellationToken.None);

        var exposure = Assert.Single(exposures.Items);
        Assert.True(exposure.IsExposed);
        Assert.True(product.CanExposeToConnectedBuyers);
        Assert.Equal(product.Id, exposure.ProductId);
        // Stale inventory read must not have blocked exposure creation.
        Assert.False(await ConnectedBuyerSharingRules.IsTrackedAsync(
            inventory, Supplier, product.Id, CancellationToken.None));
    }

    [Fact]
    public void Eligible_requires_tracked_invariant()
    {
        var product = CatalogProduct.Create(Supplier, "Apple", UnitOfMeasure.Piece, 12m, Now);
        Assert.False(ConnectedBuyerCatalogProjection.IsEligible(product, isInventoryTracked: false));
        Assert.True(ConnectedBuyerCatalogProjection.IsEligible(product, isInventoryTracked: true));

        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now);
        relationship.ConfigureCatalogSharing(CatalogSharingMode.AllEligible, null, Now);

        var untracked = ConnectedSupplierMapper.MapForManagement(
            relationship, product, null, null, null, isInventoryTracked: false);
        Assert.False(untracked.IsEligible);
        Assert.False(untracked.IsInventoryTracked);
        Assert.False(untracked.IsEffectivelyShared);
        Assert.Equal("Ineligible", untracked.SharingStatus);

        var tracked = ConnectedSupplierMapper.MapForManagement(
            relationship, product, null, null, null, isInventoryTracked: true);
        Assert.True(tracked.IsEligible);
        Assert.True(tracked.IsInventoryTracked);
        Assert.True(tracked.IsEffectivelyShared);
        Assert.Equal("Shared", tracked.SharingStatus);
    }

    [Fact]
    public void Explicit_exclusion_survives_tracking_off_on()
    {
        var product = CatalogProduct.Create(Supplier, "Product X", UnitOfMeasure.Piece, 25m, Now);
        var share = ConnectedBuyerProductShare.Share(
            ConnectedSupplierRelationshipId.New(), Buyer, Supplier, product.Id, Now);
        share.Unshare(Now);

        Assert.Equal(
            "Excluded",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, true, share));
        Assert.Equal(
            "Ineligible",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, false, share));
        Assert.Equal(
            "Excluded",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, true, share));
    }

    [Fact]
    public void SelectedOnly_newly_tracked_does_not_auto_share()
    {
        var product = CatalogProduct.Create(Supplier, "Widget", UnitOfMeasure.Piece, 10m, Now);
        Assert.False(ConnectedBuyerCatalogProjection.IsEffectivelyShared(
            CatalogSharingMode.SelectedOnly, product, true, share: null));
    }

    [Fact]
    public async Task Disable_tracking_deactivates_exposure()
    {
        var product = CatalogProduct.Create(Supplier, "Product X", UnitOfMeasure.Piece, 25m, Now);
        product.EnableConnectedBuyerAvailability(Now);
        product.SetDefaultConnectedPoPrice(25m, Now);
        var exposure = SupplierProductExposure.Expose(
            Supplier, product.Id, product.Name, "Piece", 25m, Now, product.Sku);
        var exposures = new CapturingExposures();
        exposures.Items.Add(exposure);

        await ConnectedBuyerTrackingExposureSync.AfterTrackingDisabledAsync(
            product, exposures, Now, CancellationToken.None);

        Assert.False(exposure.IsExposed);
    }

    /// <summary>Inventory repo that always returns untracked — models pre-SaveChanges stale read.</summary>
    private sealed class StaleUntrackedReadInventory : CostResolverInventoryStub
    {
        private readonly InventoryAccount _live;

        public StaleUntrackedReadInventory(InventoryAccount live) => _live = live;

        public override Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default)
        {
            // Intentionally stale: ignore in-memory Enable and report untracked.
            var stale = InventoryAccount.CreateUntracked(organizationId, productId, Now);
            return Task.FromResult<InventoryAccount?>(stale);
        }

        public override Task UpdateAccountAsync(InventoryAccount account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MemoryProducts : ICatalogProductRepository
    {
        private readonly Dictionary<Guid, CatalogProduct> _items;

        public MemoryProducts(IEnumerable<CatalogProduct> items) =>
            _items = items.ToDictionary(p => p.Id.Value);

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(productId.Value, out var p) && p.OrganizationId == organizationId
                ? p
                : null);

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            _items[product.Id.Value] = product;
            return Task.CompletedTask;
        }

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            _items[product.Id.Value] = product;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                productIds.Select(id => _items.GetValueOrDefault(id.Value))
                    .Where(p => p is not null && p.OrganizationId == organizationId)
                    .Cast<CatalogProduct>()
                    .ToList());

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

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId,
            string normalizedSku,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId,
            string barcode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

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
    }

    private sealed class CapturingExposures : ISupplierProductExposureRepository
    {
        public List<SupplierProductExposure> Items { get; } = [];

        public Task AddAsync(SupplierProductExposure exposure, CancellationToken ct = default)
        {
            Items.Add(exposure);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SupplierProductExposure exposure, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<SupplierProductExposure?> GetByProductAsync(
            PosOrganizationId supplier,
            CatalogProductId productId,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.SupplierOrganizationId == supplier && x.ProductId == productId));

        public Task<IReadOnlyList<SupplierProductExposure>> ListAsync(
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SupplierProductExposure>>(
                Items.Where(x => x.SupplierOrganizationId == supplier).ToList());

        public Task<(IReadOnlyList<SupplierProductExposure> Items, int Total)> SearchAsync(
            PosOrganizationId supplier,
            string? query,
            string? category,
            int skip,
            int take,
            CancellationToken ct = default) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, int)>(([], 0));
    }
}
