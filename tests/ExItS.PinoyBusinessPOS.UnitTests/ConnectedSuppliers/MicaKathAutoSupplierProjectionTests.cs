using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Parties;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

/// <summary>
/// Exact Mica (seller) → Kath (buyer) regression for TASK-71A:
/// seller invites buyer as Business Customer; after accept, Kath Suppliers must show Mica
/// including PartyBranchAccess visibility for branch-scoped lists.
/// </summary>
public sealed class MicaKathAutoSupplierProjectionTests
{
    private static readonly PosOrganizationId Mica = PosOrganizationId.From(Guid.Parse("25db5008-1255-40e3-a0dd-6258951b6740"));
    private static readonly PosOrganizationId Kath = PosOrganizationId.From(Guid.Parse("bd4daf42-ff25-44c1-bb03-69047560883f"));
    private static readonly Guid KathBranchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Mica_invites_Kath_accept_creates_connected_supplier_with_branch_visibility()
    {
        var repo = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var branchAccess = new InMemoryPartyBranchAccess();
        var invite = ConnectedSupplierRelationship.InviteBuyer(
            Kath,
            Mica,
            Now,
            buyerDisplayName: "Kath Store",
            buyerPublicOrganizationId: "ORG622085",
            supplierDisplayName: "Mica store",
            supplierPublicOrganizationId: "ORG421278",
            supplierBranchId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            supplierBranchName: "Main");
        await repo.AddAsync(invite);

        Assert.Equal(ConnectionInitiatedByParty.Supplier, invite.InitiatedByParty);
        Assert.Equal(Mica, invite.SupplierOrganizationId);
        Assert.Equal(Kath, invite.BuyerOrganizationId);

        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now),
            suppliers: suppliers,
            partyBranchAccess: branchAccess.Service,
            actorAccessor: new FixedActorAccessor(KathBranchId),
            orgBranches: new FixedBranches(KathBranchId));

        var accepted = await respond.ExecuteAsync(
            Kath.Value,
            invite.Id.Value,
            approve: true,
            new RespondConnectionRequest());

        Assert.True(accepted.IsSuccess);
        var active = await repo.GetAsync(invite.Id);
        Assert.NotNull(active);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Active, active!.Status);
        Assert.Equal(Mica, active.SupplierOrganizationId);
        Assert.Equal(Kath, active.BuyerOrganizationId);

        Assert.Single(suppliers.Items);
        Assert.Equal(Kath, suppliers.Items[0].OrganizationId);
        Assert.Equal(invite.Id, suppliers.Items[0].ConnectedRelationshipId);
        Assert.Equal(SupplierConnectionType.ConnectedOrganization, suppliers.Items[0].ConnectionType);
        Assert.True(branchAccess.HasGrant(Kath.Value, KathBranchId, suppliers.Items[0].Id.Value));

        // Reverse: Mica must NOT get Kath as Connected Supplier from this direction.
        Assert.DoesNotContain(suppliers.Items, s => s.OrganizationId == Mica);
        Assert.Null(await repo.FindOpenAsync(Mica, Kath));
    }

    [Fact]
    public async Task Legacy_active_relationship_without_branch_grant_self_heals_on_reconcile()
    {
        var repo = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var branchAccess = new InMemoryPartyBranchAccess();
        var invite = ConnectedSupplierRelationship.InviteBuyer(
            Kath,
            Mica,
            Now,
            supplierDisplayName: "Mica store",
            supplierPublicOrganizationId: "ORG421278");
        invite.Approve(Now.AddMinutes(1));
        await repo.AddAsync(invite);

        // Pre-existing master WITHOUT branch grant (the live Kath bug).
        var master = Supplier.Create(Kath, "SUP-000001", "Mica store", Now, notes: "ORG421278");
        master.AttachConnectedRelationship(invite.Id, Now);
        await suppliers.AddAsync(master);
        Assert.False(branchAccess.HasGrant(Kath.Value, KathBranchId, master.Id.Value));

        var reconcile = new ReconcileBuyerConnectedSupplierProjections(
            repo,
            suppliers,
            new FakeUow(),
            new FixedClock(Now.AddMinutes(2)),
            branchAccess.Service,
            new FixedActorAccessor(KathBranchId),
            new FixedBranches(KathBranchId));

        var first = await reconcile.ExecuteAsync(Kath.Value);
        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value); // branch grant written
        Assert.Single(suppliers.Items);
        Assert.True(branchAccess.HasGrant(Kath.Value, KathBranchId, master.Id.Value));

        var second = await reconcile.ExecuteAsync(Kath.Value);
        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value);
        Assert.Single(suppliers.Items);
    }

    [Fact]
    public async Task Branch_scoped_list_filter_includes_healed_mica_supplier()
    {
        var suppliers = new InMemorySuppliers();
        var branchAccess = new InMemoryPartyBranchAccess();
        var invite = ConnectedSupplierRelationship.InviteBuyer(
            Kath,
            Mica,
            Now,
            supplierDisplayName: "Mica store",
            supplierPublicOrganizationId: "ORG421278");
        invite.Approve(Now.AddMinutes(1));

        var outcome = await BuyerConnectedSupplierMaster.EnsureAsync(
            suppliers,
            invite,
            Now.AddMinutes(1),
            default,
            branchAccess.Service,
            KathBranchId,
            new FixedBranches(KathBranchId));

        Assert.Equal(BuyerConnectedSupplierEnsureResult.Created, outcome.Result);
        Assert.True(outcome.BranchAccessGranted);

        var accessible = await branchAccess.Service.FilterSupplierIdsAccessibleAsync(
            Kath.Value,
            new PartyBranchAccessActor(PosRole.Owner, false, KathBranchId));
        Assert.NotNull(accessible);
        Assert.Contains(outcome.SupplierId!.Value, accessible!);
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

    private sealed class FixedActorAccessor(Guid branchId) : IPartyBranchAccessActorAccessor
    {
        public PartyBranchAccessActor GetActor() =>
            new(PosRole.Owner, OrganizationManagementAuthority: true, ActingBranchId: branchId);
    }

    private sealed class FixedBranches(Guid primary) : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(branchId == primary);
        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId, IReadOnlyCollection<Guid> branchIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        public Task<Guid?> GetPrimaryBranchIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(primary);
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
            CancellationToken cancellationToken = default)
        {
            var rows = Items.Where(x => x.OrganizationId == organizationId);
            if (restrictToSupplierIds is not null)
            {
                rows = rows.Where(x => restrictToSupplierIds.Contains(x.Id.Value));
            }
            var list = rows.ToList();
            return Task.FromResult<(IReadOnlyList<Supplier>, int)>((list, list.Count));
        }
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

    /// <summary>Minimal in-memory PartyBranchAccessService collaborator for visibility grants.</summary>
    private sealed class InMemoryPartyBranchAccess
    {
        private readonly HashSet<(Guid Org, Guid Branch, Guid Supplier)> _grants = [];
        private readonly FakeClock _clock = new(Now);

        public PartyBranchAccessService Service { get; }

        public InMemoryPartyBranchAccess()
        {
            Service = new PartyBranchAccessService(
                new NoOpCustomerAccess(),
                new MemorySupplierAccess(_grants),
                new PartyBranchAccessGovernanceAuthority(),
                new FakeUow(),
                _clock);
        }

        public bool HasGrant(Guid org, Guid branch, Guid supplier) =>
            _grants.Contains((org, branch, supplier));

        private sealed class FakeClock(DateTimeOffset utc) : IClock
        {
            public DateTimeOffset UtcNow => utc;
        }

        private sealed class NoOpCustomerAccess : ICustomerBranchAccessRepository
        {
            public Task<bool> HasAccessAsync(PosOrganizationId organizationId, PosBranchId branchId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
                Task.FromResult(false);
            public Task<IReadOnlyList<POSCustomerId>> ListAccessibleCustomerIdsAsync(PosOrganizationId organizationId, PosBranchId branchId, CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<POSCustomerId>>([]);
            public Task<IReadOnlyList<POSCustomerId>> FilterAccessibleCustomerIdsAsync(PosOrganizationId organizationId, PosBranchId branchId, IReadOnlyCollection<POSCustomerId> customerIds, CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<POSCustomerId>>([]);
            public Task GrantAsync(CustomerBranchAccess access, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RevokeGrantAsync(PosOrganizationId organizationId, PosBranchId branchId, POSCustomerId customerId, PartyBranchGrantSource grantSource, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<IReadOnlyList<CustomerBranchAccess>> ListByCustomerAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<CustomerBranchAccess>>([]);
        }

        private sealed class MemorySupplierAccess(HashSet<(Guid Org, Guid Branch, Guid Supplier)> grants) : ISupplierBranchAccessRepository
        {
            public Task<bool> HasAccessAsync(PosOrganizationId organizationId, PosBranchId branchId, SupplierId supplierId, CancellationToken cancellationToken = default) =>
                Task.FromResult(grants.Contains((organizationId.Value, branchId.Value, supplierId.Value)));
            public Task<IReadOnlyList<SupplierId>> ListAccessibleSupplierIdsAsync(PosOrganizationId organizationId, PosBranchId branchId, CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<SupplierId>>(
                    grants.Where(g => g.Org == organizationId.Value && g.Branch == branchId.Value)
                        .Select(g => SupplierId.From(g.Supplier)).ToList());
            public Task<IReadOnlyList<SupplierId>> FilterAccessibleSupplierIdsAsync(PosOrganizationId organizationId, PosBranchId branchId, IReadOnlyCollection<SupplierId> supplierIds, CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<SupplierId>>(
                    supplierIds.Where(id => grants.Contains((organizationId.Value, branchId.Value, id.Value))).ToList());
            public Task GrantAsync(SupplierBranchAccess access, CancellationToken cancellationToken = default)
            {
                grants.Add((access.OrganizationId.Value, access.BranchId.Value, access.SupplierId.Value));
                return Task.CompletedTask;
            }
            public Task RevokeGrantAsync(PosOrganizationId organizationId, PosBranchId branchId, SupplierId supplierId, PartyBranchGrantSource grantSource, CancellationToken cancellationToken = default)
            {
                grants.Remove((organizationId.Value, branchId.Value, supplierId.Value));
                return Task.CompletedTask;
            }
        }
    }
}
