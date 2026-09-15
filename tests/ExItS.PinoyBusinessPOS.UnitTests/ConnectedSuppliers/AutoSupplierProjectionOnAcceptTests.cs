using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

/// <summary>
/// Auto supplier projection invariant: accepted A→B (A sells to B) yields exactly one
/// buyer-side ConnectedSupplier of A for B — never a reverse B→A sale, never a new Organization.
/// </summary>
public sealed class AutoSupplierProjectionOnAcceptTests
{
    private static readonly PosOrganizationId SellerA = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId BuyerB = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Pending_seller_invite_does_not_create_buyer_supplier_master()
    {
        var repo = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var invite = ConnectedSupplierRelationship.InviteBuyer(
            BuyerB,
            SellerA,
            Now,
            supplierDisplayName: "Seller A",
            supplierPublicOrganizationId: "ORG111111");
        await repo.AddAsync(invite);

        Assert.Equal(ConnectedSupplierRelationshipStatus.Pending, invite.Status);
        Assert.Empty(suppliers.Items);

        var ensure = await BuyerConnectedSupplierMaster.EnsureAsync(suppliers, invite, Now, default);
        Assert.Equal(BuyerConnectedSupplierEnsureResult.AlreadyPresent, ensure);
        Assert.Empty(suppliers.Items);
    }

    [Fact]
    public async Task Accept_creates_buyer_supplier_and_seller_customer_same_direction()
    {
        var repo = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var invite = ConnectedSupplierRelationship.InviteBuyer(
            BuyerB,
            SellerA,
            Now,
            buyerDisplayName: "Buyer B",
            buyerPublicOrganizationId: "ORG222222",
            supplierDisplayName: "Seller A",
            supplierPublicOrganizationId: "ORG111111");
        await repo.AddAsync(invite);

        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now),
            suppliers: suppliers);
        var result = await respond.ExecuteAsync(
            BuyerB.Value,
            invite.Id.Value,
            approve: true,
            new RespondConnectionRequest());

        Assert.True(result.IsSuccess);
        var active = await repo.GetAsync(invite.Id);
        Assert.NotNull(active);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Active, active!.Status);
        Assert.Equal(SellerA, active.SupplierOrganizationId);
        Assert.Equal(BuyerB, active.BuyerOrganizationId);

        // B sees A as Connected Supplier
        Assert.Single(suppliers.Items);
        Assert.Equal(BuyerB, suppliers.Items[0].OrganizationId);
        Assert.Equal(invite.Id, suppliers.Items[0].ConnectedRelationshipId);
        Assert.Equal(SupplierConnectionType.ConnectedOrganization, suppliers.Items[0].ConnectionType);

        // A as buyer of B (reverse) must NOT exist
        Assert.Null(await repo.FindOpenAsync(SellerA, BuyerB));
        Assert.DoesNotContain(suppliers.Items, s => s.OrganizationId == SellerA);
    }

    [Fact]
    public async Task Ensure_is_idempotent_for_same_relationship()
    {
        var suppliers = new InMemorySuppliers();
        var invite = ConnectedSupplierRelationship.InviteBuyer(
            BuyerB,
            SellerA,
            Now,
            supplierDisplayName: "Seller A",
            supplierPublicOrganizationId: "ORG111111");
        invite.Approve(Now.AddMinutes(1));

        var first = await BuyerConnectedSupplierMaster.EnsureAsync(suppliers, invite, Now.AddMinutes(1), default);
        var second = await BuyerConnectedSupplierMaster.EnsureAsync(suppliers, invite, Now.AddMinutes(2), default);

        Assert.Equal(BuyerConnectedSupplierEnsureResult.Created, first);
        Assert.Equal(BuyerConnectedSupplierEnsureResult.AlreadyPresent, second);
        Assert.Single(suppliers.Items);
    }

    [Fact]
    public async Task Decline_creates_no_supplier_projection()
    {
        var repo = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var invite = ConnectedSupplierRelationship.InviteBuyer(BuyerB, SellerA, Now);
        await repo.AddAsync(invite);

        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now),
            suppliers: suppliers);
        var result = await respond.ExecuteAsync(
            BuyerB.Value,
            invite.Id.Value,
            approve: false,
            new RespondConnectionRequest());

        Assert.True(result.IsSuccess);
        Assert.Empty(suppliers.Items);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Declined, (await repo.GetAsync(invite.Id))!.Status);
    }

    [Fact]
    public async Task Both_trade_directions_can_coexist_as_distinct_relationships()
    {
        var repo = new InMemoryRelationships();
        var suppliersB = new InMemorySuppliers();
        var suppliersA = new InMemorySuppliers();

        var aSellsToB = ConnectedSupplierRelationship.InviteBuyer(
            BuyerB, SellerA, Now,
            supplierDisplayName: "Seller A",
            supplierPublicOrganizationId: "ORG111111");
        aSellsToB.Approve(Now.AddMinutes(1));
        await repo.AddAsync(aSellsToB);
        await BuyerConnectedSupplierMaster.EnsureAsync(suppliersB, aSellsToB, Now.AddMinutes(1), default);

        var bSellsToA = ConnectedSupplierRelationship.InviteBuyer(
            SellerA, BuyerB, Now.AddMinutes(2),
            supplierDisplayName: "Buyer B Co",
            supplierPublicOrganizationId: "ORG222222");
        bSellsToA.Approve(Now.AddMinutes(3));
        await repo.AddAsync(bSellsToA);
        await BuyerConnectedSupplierMaster.EnsureAsync(suppliersA, bSellsToA, Now.AddMinutes(3), default);

        Assert.NotNull(await repo.FindOpenAsync(BuyerB, SellerA));
        Assert.NotNull(await repo.FindOpenAsync(SellerA, BuyerB));
        Assert.NotEqual(aSellsToB.Id, bSellsToA.Id);
        Assert.Single(suppliersB.Items);
        Assert.Single(suppliersA.Items);
        Assert.Equal(aSellsToB.Id, suppliersB.Items[0].ConnectedRelationshipId);
        Assert.Equal(bSellsToA.Id, suppliersA.Items[0].ConnectedRelationshipId);
    }

    [Fact]
    public async Task Reconcile_heals_missing_active_projection_without_duplicate()
    {
        var repo = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var invite = ConnectedSupplierRelationship.InviteBuyer(
            BuyerB, SellerA, Now,
            supplierDisplayName: "Seller A",
            supplierPublicOrganizationId: "ORG111111");
        invite.Approve(Now.AddMinutes(1));
        await repo.AddAsync(invite);

        var reconcile = new ReconcileBuyerConnectedSupplierProjections(
            repo, suppliers, new FakeUow(), new FixedClock(Now.AddMinutes(2)));
        var first = await reconcile.ExecuteAsync(BuyerB.Value);
        var second = await reconcile.ExecuteAsync(BuyerB.Value);

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value);
        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value);
        Assert.Single(suppliers.Items);
    }

    [Fact]
    public async Task External_manual_supplier_with_matching_notes_is_linked_not_duplicated()
    {
        var suppliers = new InMemorySuppliers();
        var manual = Supplier.Create(BuyerB, "SUP-000001", "Seller A", Now, notes: "ORG111111");
        await suppliers.AddAsync(manual);

        var invite = ConnectedSupplierRelationship.InviteBuyer(
            BuyerB, SellerA, Now,
            supplierDisplayName: "Seller A",
            supplierPublicOrganizationId: "ORG111111");
        invite.Approve(Now.AddMinutes(1));

        var result = await BuyerConnectedSupplierMaster.EnsureAsync(suppliers, invite, Now.AddMinutes(1), default);
        Assert.Equal(BuyerConnectedSupplierEnsureResult.LinkedExistingExternal, result);
        Assert.Single(suppliers.Items);
        Assert.Equal(invite.Id, suppliers.Items[0].ConnectedRelationshipId);
        Assert.Equal(SupplierConnectionType.ConnectedOrganization, suppliers.Items[0].ConnectionType);
        // Local notes preserved
        Assert.Equal("ORG111111", suppliers.Items[0].Notes);
    }

    [Fact]
    public async Task Ambiguous_name_conflict_preserves_manual_and_creates_distinct_projection()
    {
        var suppliers = new InMemorySuppliers();
        var manual = Supplier.Create(BuyerB, "SUP-000001", "Seller A", Now, notes: "unrelated shop notes");
        await suppliers.AddAsync(manual);

        var invite = ConnectedSupplierRelationship.InviteBuyer(
            BuyerB, SellerA, Now,
            supplierDisplayName: "Seller A",
            supplierPublicOrganizationId: "ORG111111");
        invite.Approve(Now.AddMinutes(1));

        var result = await BuyerConnectedSupplierMaster.EnsureAsync(suppliers, invite, Now.AddMinutes(1), default);
        Assert.Equal(BuyerConnectedSupplierEnsureResult.CreatedDistinctDueToNameConflict, result);
        Assert.Equal(2, suppliers.Items.Count);
        Assert.Null(suppliers.Items[0].ConnectedRelationshipId);
        Assert.Equal(invite.Id, suppliers.Items[1].ConnectedRelationshipId);
    }

    [Fact]
    public async Task Disconnect_blocks_new_commerce_but_keeps_supplier_master_history()
    {
        var repo = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var invite = ConnectedSupplierRelationship.InviteBuyer(
            BuyerB, SellerA, Now,
            supplierDisplayName: "Seller A",
            supplierPublicOrganizationId: "ORG111111");
        invite.Approve(Now.AddMinutes(1));
        await repo.AddAsync(invite);
        await BuyerConnectedSupplierMaster.EnsureAsync(suppliers, invite, Now.AddMinutes(1), default);

        invite.Disconnect(Now.AddMinutes(5));
        await repo.UpdateAsync(invite);

        Assert.Equal(ConnectedSupplierRelationshipStatus.Disconnected, invite.Status);
        Assert.Single(suppliers.Items);
        Assert.Equal(invite.Id, suppliers.Items[0].ConnectedRelationshipId);
        // Ensure must not recreate for non-Active
        var ensure = await BuyerConnectedSupplierMaster.EnsureAsync(suppliers, invite, Now.AddMinutes(6), default);
        Assert.Equal(BuyerConnectedSupplierEnsureResult.AlreadyPresent, ensure);
        Assert.Single(suppliers.Items);
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class OrgWideBranchAccess : IAuthorizedBranchGroupingDirectory
    {
        public static readonly OrgWideBranchAccess Instance = new();
        public Task<AuthorizedBranchScope> ListAuthorizedAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
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
            PosOrganizationId buyer, PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.BuyerOrganizationId == buyer
                && x.SupplierOrganizationId == supplier
                && (x.Status is ConnectedSupplierRelationshipStatus.Pending or ConnectedSupplierRelationshipStatus.Active)));
        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));
        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId, bool supplierView, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                _items.Where(x => supplierView
                    ? x.SupplierOrganizationId == organizationId
                    : x.BuyerOrganizationId == organizationId).ToList());
        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemorySuppliers : ISupplierRepository
    {
        public List<Supplier> Items { get; } = [];
        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            Items.Add(supplier);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            var idx = Items.FindIndex(x => x.Id == supplier.Id);
            if (idx >= 0) Items[idx] = supplier;
            return Task.CompletedTask;
        }
        public Task<Supplier?> GetByIdAsync(
            PosOrganizationId organizationId, SupplierId supplierId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == supplierId));
        public Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierFilter filter,
            int skip,
            int take,
            IReadOnlyCollection<Guid>? restrictToSupplierIds = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Supplier>, int)>((Items.Where(x => x.OrganizationId == organizationId).ToList(), Items.Count));
        public Task<Supplier?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId, string normalizedName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId
                && x.Status == SupplierStatus.Active
                && string.Equals(x.NormalizedName, normalizedName, StringComparison.Ordinal)));
        public Task<Supplier?> FindActiveByNormalizedEmailAsync(
            PosOrganizationId organizationId, string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> FindActiveByNormalizedMobileAsync(
            PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> FindActiveByNormalizedTaxAsync(
            PosOrganizationId organizationId, string normalizedTax, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> FindByConnectedRelationshipIdAsync(
            PosOrganizationId organizationId,
            ConnectedSupplierRelationshipId relationshipId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.ConnectedRelationshipId == relationshipId));
        public Task<string> AllocateNextSupplierCodeAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult($"SUP-{Items.Count + 1:D6}");
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> supplierIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    }
}
