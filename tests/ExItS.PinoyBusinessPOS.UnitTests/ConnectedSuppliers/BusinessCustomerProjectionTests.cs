using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class BusinessCustomerProjectionTests
{
    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        public List<ConnectedSupplierRelationship> Items { get; } = [];

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            Items.Add(relationship);
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.BuyerOrganizationId == buyer
                && x.SupplierOrganizationId == supplier
                && (x.Status is ConnectedSupplierRelationshipStatus.Pending
                    or ConnectedSupplierRelationshipStatus.Active)));

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id.Value == id.Value));

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                Items.Where(x => supplierView
                    ? x.SupplierOrganizationId == organizationId
                    : x.BuyerOrganizationId == organizationId).ToList());

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryShares : IConnectedBuyerProductShareRepository
    {
        public List<ConnectedBuyerProductShare> Items { get; } = [];
        public int EligibleCount { get; set; }

        public Task<ConnectedBuyerProductShare?> GetAsync(ConnectedBuyerProductShareId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<ConnectedBuyerProductShare?> FindAsync(
            ConnectedSupplierRelationshipId relationshipId,
            CatalogProductId supplierProductId,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.RelationshipId == relationshipId && x.SupplierProductId == supplierProductId));

        public Task<IReadOnlyList<ConnectedBuyerProductShare>> ListAsync(
            ConnectedSupplierRelationshipId relationshipId,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedBuyerProductShare>>(
                Items.Where(x => x.RelationshipId == relationshipId).ToList());

        public Task<(IReadOnlyList<SupplierProductExposure> Exposures, IReadOnlyList<ConnectedBuyerProductShare> Shares, int Total)>
            SearchSharedCatalogAsync(
                ConnectedSupplierRelationshipId relationshipId,
                PosOrganizationId supplier,
                string? query,
                string? category,
                int skip,
                int take,
                CancellationToken ct = default,
                CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, IReadOnlyList<ConnectedBuyerProductShare>, int)>(
                ([], [], 0));

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
            Task.FromResult(new BuyerProductShareSearchPage([], [], 0, EligibleCount, 0, []));

        public Task AddAsync(ConnectedBuyerProductShare share, CancellationToken ct = default)
        {
            Items.Add(share);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ConnectedBuyerProductShare share, CancellationToken ct = default) =>
            Task.CompletedTask;

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
            Task.FromResult(EligibleCount);
    }

    private sealed class FakePlatformOrgs : IPlatformOrganizationPublicResolve
    {
        public string? LiveDisplayName { get; set; }
        public string? LivePublicId { get; set; }
        public Guid? LiveOrganizationId { get; set; }
        public int ResolveCalls { get; private set; }

        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveOrganizationForConnectedSupplierAsync(
            string publicOrganizationIdOrQrPayload,
            CancellationToken cancellationToken = default)
        {
            ResolveCalls++;
            if (LiveDisplayName is null || LiveOrganizationId is null || LivePublicId is null)
            {
                return Task.FromResult(ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                    ConnectedSupplierErrorCodes.NotFound,
                    "not found"));
            }

            return Task.FromResult(ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(
                    LiveOrganizationId.Value,
                    LivePublicId,
                    LiveDisplayName)));
        }

        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> GetOrganizationPublicIdentityAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            ResolveOrganizationForConnectedSupplierAsync("ORG", cancellationToken);
    }

    private static ConnectedSupplierRelationship ActiveBuyer(
        PosOrganizationId buyer,
        PosOrganizationId supplier,
        string buyerName,
        string buyerPublicId,
        CatalogSharingMode mode = CatalogSharingMode.SelectedOnly,
        decimal? discount = null)
    {
        var now = DateTimeOffset.UtcNow;
        var r = ConnectedSupplierRelationship.Request(
            buyer,
            supplier,
            now,
            buyerDisplayName: buyerName,
            buyerPublicOrganizationId: buyerPublicId,
            supplierDisplayName: "Paul Supply",
            supplierPublicOrganizationId: "ORGPAUL01");
        r.Approve(now);
        r.ConfigureCatalogSharing(mode, discount, now);
        return r;
    }

    [Fact]
    public async Task List_shows_active_buyer_as_business_customer_without_creating_pos_customer()
    {
        var mica = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var paul = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var relationships = new InMemoryRelationships();
        var shares = new InMemoryShares { EligibleCount = 22 };
        var connection = ActiveBuyer(mica, paul, "Mica Store", "ORGMICA01", CatalogSharingMode.AllEligible, 10m);
        await relationships.AddAsync(connection);

        var share = ConnectedBuyerProductShare.Share(
            connection.Id, mica, paul, CatalogProductId.From(Guid.NewGuid()), DateTimeOffset.UtcNow);
        share.Unshare(DateTimeOffset.UtcNow);
        await shares.AddAsync(share);

        var list = new ListBusinessCustomers(relationships, shares, new FakeAccess());
        var result = await list.ExecuteAsync(paul.Value);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        var row = result.Value![0];
        Assert.Equal(connection.Id.Value, row.ConnectionId);
        Assert.Equal(mica.Value, row.BuyerOrganizationId);
        Assert.Equal("Mica Store", row.OrganizationDisplayName);
        Assert.Equal("Active", row.RelationshipStatus);
        Assert.Equal("AllEligible", row.CatalogSharingMode);
        Assert.Equal(10m, row.CustomerDiscountPercent);
        Assert.Equal(21, row.SharedCount);
        Assert.Equal(1, row.ExcludedCount);
        Assert.Equal(22, row.EligibleCount);
    }

    [Fact]
    public async Task List_excludes_pending_and_does_not_duplicate_organization_identity()
    {
        var mica = PosOrganizationId.From(Guid.NewGuid());
        var paul = PosOrganizationId.From(Guid.NewGuid());
        var relationships = new InMemoryRelationships();
        var pending = ConnectedSupplierRelationship.Request(
            mica,
            paul,
            DateTimeOffset.UtcNow,
            buyerDisplayName: "Mica Store",
            buyerPublicOrganizationId: "ORGMICA01",
            supplierDisplayName: "Paul",
            supplierPublicOrganizationId: "ORGPAUL01");
        await relationships.AddAsync(pending);

        var list = new ListBusinessCustomers(relationships, new InMemoryShares(), new FakeAccess());
        var result = await list.ExecuteAsync(paul.Value);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
        Assert.Single(relationships.Items);
        Assert.Equal(mica, relationships.Items[0].BuyerOrganizationId);
    }

    [Fact]
    public async Task Discount_change_is_scoped_to_one_connection()
    {
        var paul = PosOrganizationId.From(Guid.NewGuid());
        var mica = PosOrganizationId.From(Guid.NewGuid());
        var kizy = PosOrganizationId.From(Guid.NewGuid());
        var relationships = new InMemoryRelationships();
        var micaRel = ActiveBuyer(mica, paul, "Mica", "ORGMICA01", discount: 10m);
        var kizyRel = ActiveBuyer(kizy, paul, "Kizy", "ORGKIZY01", discount: 5m);
        await relationships.AddAsync(micaRel);
        await relationships.AddAsync(kizyRel);

        micaRel.ConfigureCatalogSharing(CatalogSharingMode.SelectedOnly, 12m, DateTimeOffset.UtcNow);

        var list = new ListBusinessCustomers(relationships, new InMemoryShares(), new FakeAccess());
        var result = await list.ExecuteAsync(paul.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal(12m, result.Value!.Single(x => x.BuyerOrganizationId == mica.Value).CustomerDiscountPercent);
        Assert.Equal(5m, result.Value!.Single(x => x.BuyerOrganizationId == kizy.Value).CustomerDiscountPercent);
    }

    [Fact]
    public async Task Get_prefers_live_organization_display_name()
    {
        var mica = PosOrganizationId.From(Guid.NewGuid());
        var paul = PosOrganizationId.From(Guid.NewGuid());
        var relationships = new InMemoryRelationships();
        var connection = ActiveBuyer(mica, paul, "Mica Store", "ORGMICA01");
        await relationships.AddAsync(connection);

        var platform = new FakePlatformOrgs
        {
            LiveOrganizationId = mica.Value,
            LivePublicId = "ORGMICA01",
            LiveDisplayName = "Mica Supermarket",
        };

        var get = new GetBusinessCustomer(relationships, new InMemoryShares(), platform, new FakeAccess());
        var result = await get.ExecuteAsync(paul.Value, connection.Id.Value);

        Assert.True(result.IsSuccess);
        Assert.Equal("Mica Supermarket", result.Value!.OrganizationDisplayName);
        Assert.True(result.Value.DisplayNameIsLive);
        Assert.Equal(1, platform.ResolveCalls);
        Assert.Equal("Mica Store", connection.BuyerDisplayNameSnapshot);
    }

    [Fact]
    public async Task Buyer_perspective_list_relationships_uses_same_connection_id()
    {
        var mica = PosOrganizationId.From(Guid.NewGuid());
        var paul = PosOrganizationId.From(Guid.NewGuid());
        var relationships = new InMemoryRelationships();
        var connection = ActiveBuyer(mica, paul, "Mica Store", "ORGMICA01");
        await relationships.AddAsync(connection);

        var business = await new ListBusinessCustomers(relationships, new InMemoryShares(), new FakeAccess())
            .ExecuteAsync(paul.Value);
        var buyerSuppliers = await new ListRelationships(relationships, new FakeAccess())
            .ExecuteAsync(mica.Value, supplierView: false);

        Assert.True(business.IsSuccess);
        Assert.True(buyerSuppliers.IsSuccess);
        Assert.Equal(business.Value![0].ConnectionId, buyerSuppliers.Value![0].RelationshipId);
        Assert.Equal("Paul Supply", buyerSuppliers.Value[0].CounterpartyDisplayName);
    }

    [Fact]
    public async Task Disconnected_excluded_from_default_list_but_history_row_retained()
    {
        var mica = PosOrganizationId.From(Guid.NewGuid());
        var paul = PosOrganizationId.From(Guid.NewGuid());
        var relationships = new InMemoryRelationships();
        var connection = ActiveBuyer(mica, paul, "Mica Store", "ORGMICA01");
        connection.Disconnect(DateTimeOffset.UtcNow);
        await relationships.AddAsync(connection);

        var list = new ListBusinessCustomers(relationships, new InMemoryShares(), new FakeAccess());
        Assert.Empty((await list.ExecuteAsync(paul.Value)).Value!);
        Assert.Single((await list.ExecuteAsync(paul.Value, includeDisconnected: true)).Value!);
        Assert.Single(relationships.Items);
    }
}
