using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

/// <summary>
/// POS-B2B-SELLER-INITIATED-CATALOG-DEFAULTS-01: buyer Accept of a seller invite
/// applies AllEligible + selling-price bootstrap without buyer catalog setup.
/// </summary>
public sealed class SellerInitiatedCatalogDefaultsTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"));
    private static readonly PosOrganizationId Supplier =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Seller_invite_accept_defaults_to_AllEligible_with_no_customer_discount()
    {
        var relationships = new InMemoryRelationships();
        var invite = ConnectedSupplierRelationship.InviteBuyer(Buyer, Supplier, Now);
        await relationships.AddAsync(invite);

        var respond = new RespondConnection(
            relationships,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now),
            products: new InMemoryProducts(),
            exposures: new InMemoryExposures());

        var result = await respond.ExecuteAsync(
            Buyer.Value,
            invite.Id.Value,
            approve: true,
            new RespondConnectionRequest());

        Assert.True(result.IsSuccess);
        var active = await relationships.GetAsync(invite.Id);
        Assert.NotNull(active);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Active, active!.Status);
        Assert.Equal(CatalogSharingMode.AllEligible, active.CatalogSharingMode);
        Assert.Null(active.CustomerDiscountPercent);
    }

    [Fact]
    public async Task Seller_invite_accept_bootstraps_selling_price_when_no_default_po()
    {
        var products = new InMemoryProducts();
        var exposures = new InMemoryExposures();
        var product = CatalogProduct.Create(Supplier, "Bread", UnitOfMeasure.Piece, 45m, Now);
        products.Seed(product);

        var relationships = new InMemoryRelationships();
        var invite = ConnectedSupplierRelationship.InviteBuyer(Buyer, Supplier, Now);
        await relationships.AddAsync(invite);

        var respond = new RespondConnection(
            relationships,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now),
            products: products,
            exposures: exposures);

        Assert.True((await respond.ExecuteAsync(
            Buyer.Value, invite.Id.Value, true, new RespondConnectionRequest())).IsSuccess);

        Assert.Equal(45m, Assert.Single(products.Items).DefaultConnectedPoPrice);
        var exposure = Assert.Single(exposures.Items);
        Assert.True(exposure.IsExposed);
        Assert.Equal(45m, exposure.SupplierOrderPrice);
    }

    [Fact]
    public async Task Seller_invite_accept_preserves_existing_default_connected_po_price()
    {
        var products = new InMemoryProducts();
        var exposures = new InMemoryExposures();
        var product = CatalogProduct.Create(Supplier, "Rice", UnitOfMeasure.Kilogram, 80m, Now);
        product.SetDefaultConnectedPoPrice(62m, Now.AddMinutes(1));
        products.Seed(product);

        var relationships = new InMemoryRelationships();
        var invite = ConnectedSupplierRelationship.InviteBuyer(Buyer, Supplier, Now);
        await relationships.AddAsync(invite);

        var respond = new RespondConnection(
            relationships,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now.AddMinutes(2)),
            products: products,
            exposures: exposures);

        Assert.True((await respond.ExecuteAsync(
            Buyer.Value, invite.Id.Value, true, new RespondConnectionRequest())).IsSuccess);

        Assert.Equal(62m, Assert.Single(products.Items).DefaultConnectedPoPrice);
        Assert.Equal(62m, Assert.Single(exposures.Items).SupplierOrderPrice);
    }

    [Fact]
    public async Task Seller_invite_accept_excludes_blocked_inactive_and_non_sellable()
    {
        var products = new InMemoryProducts();
        var exposures = new InMemoryExposures();

        var eligible = CatalogProduct.Create(Supplier, "Eligible", UnitOfMeasure.Piece, 10m, Now);
        var blocked = CatalogProduct.Create(Supplier, "Blocked", UnitOfMeasure.Piece, 11m, Now);
        blocked.BlockFromConnectedBuyers(Now.AddMinutes(1));
        var inactive = CatalogProduct.Create(Supplier, "Inactive", UnitOfMeasure.Piece, 12m, Now);
        inactive.Deactivate(Now.AddMinutes(1));
        var ingredient = CatalogProduct.Create(
            Supplier,
            "Flour",
            UnitOfMeasure.Kilogram,
            13m,
            Now,
            usage: ProductUsageCapabilities.Ingredient);

        products.Seed(eligible);
        products.Seed(blocked);
        products.Seed(inactive);
        products.Seed(ingredient);

        var relationships = new InMemoryRelationships();
        var invite = ConnectedSupplierRelationship.InviteBuyer(Buyer, Supplier, Now);
        await relationships.AddAsync(invite);

        var respond = new RespondConnection(
            relationships,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now.AddMinutes(2)),
            products: products,
            exposures: exposures);

        Assert.True((await respond.ExecuteAsync(
            Buyer.Value, invite.Id.Value, true, new RespondConnectionRequest())).IsSuccess);

        var exposure = Assert.Single(exposures.Items);
        Assert.Equal(eligible.Id, exposure.ProductId);
        Assert.Null(blocked.DefaultConnectedPoPrice);
        Assert.Null(inactive.DefaultConnectedPoPrice);
        Assert.Null(ingredient.DefaultConnectedPoPrice);
    }

    [Fact]
    public async Task Bootstrap_retry_does_not_duplicate_exposures()
    {
        var products = new InMemoryProducts();
        var exposures = new InMemoryExposures();
        var product = CatalogProduct.Create(Supplier, "Soap", UnitOfMeasure.Piece, 25m, Now);
        products.Seed(product);

        await AllEligibleCatalogBootstrap.EnsureExposuresFromSellingPriceAsync(
            Supplier, products, exposures, Now, CancellationToken.None);
        await AllEligibleCatalogBootstrap.EnsureExposuresFromSellingPriceAsync(
            Supplier, products, exposures, Now.AddMinutes(1), CancellationToken.None);

        Assert.Single(exposures.Items);
        Assert.Equal(25m, Assert.Single(products.Items).DefaultConnectedPoPrice);
    }

    [Fact]
    public async Task Buyer_initiated_accept_SelectedOnly_is_unchanged()
    {
        var relationships = new InMemoryRelationships();
        var request = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        await relationships.AddAsync(request);

        var respond = new RespondConnection(
            relationships,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now),
            products: new InMemoryProducts(),
            exposures: new InMemoryExposures());

        var result = await respond.ExecuteAsync(
            Supplier.Value,
            request.Id.Value,
            approve: true,
            new RespondConnectionRequest(CatalogSharingMode: "SelectedOnly"));

        Assert.True(result.IsSuccess);
        var active = await relationships.GetAsync(request.Id);
        Assert.Equal(CatalogSharingMode.SelectedOnly, active!.CatalogSharingMode);
        Assert.Null(active.CustomerDiscountPercent);
    }

    [Fact]
    public void Existing_Active_SelectedOnly_relationship_mode_is_preserved_without_migration()
    {
        var relationship = ConnectedSupplierRelationship.InviteBuyer(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));
        relationship.ConfigureCatalogSharing(CatalogSharingMode.SelectedOnly, 5m, Now.AddMinutes(2));

        Assert.Equal(CatalogSharingMode.SelectedOnly, relationship.CatalogSharingMode);
        Assert.Equal(5m, relationship.CustomerDiscountPercent);
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class OrgWideBranchAccess : IAuthorizedBranchGroupingDirectory
    {
        public static readonly OrgWideBranchAccess Instance = new();

        public Task<AuthorizedBranchScope> ListAuthorizedAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthorizedBranchScope(IsOrganizationWide: true, []));
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _items.Add(relationship);
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.BuyerOrganizationId == buyer
                && x.SupplierOrganizationId == supplier
                && (x.Status is ConnectedSupplierRelationshipStatus.Pending
                    or ConnectedSupplierRelationshipStatus.Active)));

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                _items.Where(x => supplierView
                    ? x.SupplierOrganizationId == organizationId
                    : x.BuyerOrganizationId == organizationId).ToList());

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryExposures : ISupplierProductExposureRepository
    {
        public List<SupplierProductExposure> Items { get; } = [];

        public Task<SupplierProductExposure?> GetAsync(
            SupplierProductExposureId id,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id.Value == id.Value));

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
            CancellationToken ct = default)
        {
            var matches = Items.Where(x => x.SupplierOrganizationId == supplier).ToList();
            return Task.FromResult<(IReadOnlyList<SupplierProductExposure>, int)>(
                (matches.Skip(skip).Take(take).ToList(), matches.Count));
        }

        public Task AddAsync(SupplierProductExposure exposure, CancellationToken ct = default)
        {
            Items.Add(exposure);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SupplierProductExposure exposure, CancellationToken ct = default) =>
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
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId,
            string barcode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

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
}
